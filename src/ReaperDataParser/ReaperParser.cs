using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace ReaperDataParser
{
    public static class ReaperParser
    {
        private static string[] EnvelopeStrings = new[] { "ENVSEG", "VOLENV", "VOLENV2", "PANENV", "PANENV2", "MUTEENV" };
        private static readonly HashSet<string> EnvelopeTypeSet = new HashSet<string>(EnvelopeStrings, StringComparer.OrdinalIgnoreCase);
        private static readonly char[] TokenSeparators = { ' ', '\t' };
        private static readonly StringComparer BlockComparer = StringComparer.OrdinalIgnoreCase;
        private static readonly CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
        private static readonly NumberStyles FloatStyles = NumberStyles.Float;
        private static readonly NumberStyles IntStyles = NumberStyles.Integer;

        public static ReaperData ParseFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("File path cannot be null or whitespace.", nameof(filePath));
            }
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Unable to locate the specified Reaper project file.", filePath);
            }

            ReaperData data = Parse(File.ReadAllText(filePath));
            if (data != null)
            {
                data.ProjectFilePath = filePath;
            }
            return data;
        }

        public static ReaperData Parse(string txt)
        {
            if (txt == null)
            {
                throw new ArgumentNullException(nameof(txt));
            }

            return Parse(Encoding.UTF8.GetBytes(txt));
        }

        public static ReaperData Parse(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            List<string> lines = SplitLines(bytes);
            ReaperBlock masterBlock = new ReaperBlock(lines);
            return ParseFromBlock<ReaperData>(masterBlock);
        }

        private static List<string> SplitLines(byte[] data)
        {
            List<string> lines = new List<string>();
            int start = 0;
            for (int i = 0; i < data.Length; i++)
            {
                if (data[i] == 0x00)
                {
                    if (i > start)
                    {
                        lines.Add(Encoding.UTF8.GetString(data, start, i - start).Trim());
                    }
                    start = i + 1;
                }
                else if (data[i] == 0x0D && i + 1 < data.Length && data[i + 1] == 0x0A)
                {
                    if (i > start)
                    {
                        lines.Add(Encoding.UTF8.GetString(data, start, i - start).Trim());
                    }
                    start = i + 2;
                }
            }
            if (start < data.Length)
            {
                lines.Add(Encoding.UTF8.GetString(data, start, data.Length - start).Trim());
            }
            return lines;
        }

        private static T ParseFromBlock<T>(ReaperBlock block) where T : class
        {
            if (block == null)
            {
                return null;
            }

            if (typeof(T) == typeof(ReaperData))
            {
                return ParseDataBlock(block) as T;
            }
            if (typeof(T) == typeof(ReaperTrack))
            {
                return ParseTrackBlock(block) as T;
            }
            if (typeof(T) == typeof(ReaperItem))
            {
                return ParseItemBlock(block) as T;
            }
            if (typeof(T) == typeof(ReaperSource))
            {
                return ParseSourceBlock(block) as T;
            }
            if (typeof(T) == typeof(ReaperEnvelope))
            {
                return ParseEnvelopeBlock(block) as T;
            }

            return null;
        }

        private static ReaperData ParseDataBlock(ReaperBlock block)
        {
            if (block == null)
            {
                return null;
            }

            ReaperData data = new ReaperData();
            ReaperTrack currentTrack = null;

            foreach (ReaperBlock child in block.Children)
            {
                ReaperTrack track = ParseTrackBlock(child);
                if (track != null)
                {
                    data.IsTrackData = true;
                    data.Tracks.Add(track);
                    currentTrack = null;
                    continue;
                }

                ReaperItem item = ParseItemBlock(child);
                if (item != null)
                {
                    if (currentTrack == null)
                    {
                        currentTrack = new ReaperTrack();
                        data.Tracks.Add(currentTrack);
                    }
                    currentTrack.Items.Add(item);
                    continue;
                }

                ReaperEnvelope env = ParseEnvelopeBlock(child);
                if (env != null)
                {
                    if (currentTrack == null)
                    {
                        currentTrack = new ReaperTrack();
                        data.Tracks.Add(currentTrack);
                    }
                    currentTrack.Envelopes.Add(env);
                    continue;
                }

                if (child.Lines?.Count > 0)
                {
                    string[] tokens = SplitTokens(child.Lines[0]);
                    if (tokens.Length > 0 && BlockComparer.Equals(tokens[0], "TRACKSKIP"))
                    {
                        currentTrack = new ReaperTrack();
                        data.Tracks.Add(currentTrack);
                    }
                }
            }

            if (data.Tracks.Count == 0)
            {
                foreach (ReaperBlock child in block.Children)
                {
                    ReaperData nested = ParseDataBlock(child);
                    if (nested?.Tracks.Count > 0)
                    {
                        return nested;
                    }
                }
            }

            return data;
        }

        private static ReaperTrack ParseTrackBlock(ReaperBlock block)
        {
            if (block?.Type != "TRACK")
            {
                return null;
            }

            ReaperTrack track = new ReaperTrack();
            foreach (string[] tokens in TokenizeLines(block.Lines))
            {
                switch (tokens[0].ToUpperInvariant())
                {
                    case "NAME":
                        track.Name = ParsePathString(tokens);
                        break;
                    case "VOLPAN":
                        track.VolPan = ParseDoubleArray(tokens);
                        break;
                    case "MUTESOLO":
                        track.MuteSolo = ParseIntArray(tokens);
                        break;
                    case "IPHASE":
                        EnsureTokenCount(tokens, 2, "track IPHASE flag");
                        track.IPhase = ParseDoubleToken(tokens[1]) != 0;
                        break;
                    default:
                        break;
                }
            }

            foreach (ReaperBlock child in block.Children)
            {
                ReaperItem item = ParseItemBlock(child);
                if (item != null)
                {
                    track.Items.Add(item);
                    continue;
                }

                ReaperEnvelope env = ParseEnvelopeBlock(child);
                if (env != null)
                {
                    track.Envelopes.Add(env);
                }
            }

            return track;
        }

        private static ReaperItem ParseItemBlock(ReaperBlock block)
        {
            if (block?.Type != "ITEM")
            {
                return null;
            }

            ReaperItem item = new ReaperItem();
            ReaperTake currentTake = item;
            List<ReaperStretchMarker> stretchMarkers = new List<ReaperStretchMarker>();

            foreach (string[] tokens in TokenizeLines(block.Lines))
            {
                switch (tokens[0].ToUpperInvariant())
                {
                    case "POSITION":
                        EnsureTokenCount(tokens, 2, "item POSITION");
                        item.Position = ParseDoubleToken(tokens[1]);
                        break;
                    case "SNAPOFFS":
                        EnsureTokenCount(tokens, 2, "item SNAPOFFS");
                        item.SnapOffs = ParseDoubleToken(tokens[1]);
                        break;
                    case "LENGTH":
                        EnsureTokenCount(tokens, 2, "item LENGTH");
                        item.Length = ParseDoubleToken(tokens[1]);
                        break;
                    case "LOOP":
                        EnsureTokenCount(tokens, 2, "item LOOP flag");
                        item.Loop = ParseBoolToken(tokens[1]);
                        break;
                    case "ALLTAKES":
                        EnsureTokenCount(tokens, 2, "item ALLTAKES flag");
                        item.AllTakes = ParseBoolToken(tokens[1]);
                        break;
                    case "FADEIN":
                        item.FadeIn = ParseDoubleArray(tokens);
                        break;
                    case "FADEOUT":
                        item.FadeOut = ParseDoubleArray(tokens);
                        break;
                    case "MUTE":
                        item.Mute = ParseIntArray(tokens);
                        break;
                    case "SEL":
                        EnsureTokenCount(tokens, 2, "item SEL flag");
                        item.Selected = ParseBoolToken(tokens[1]);
                        break;
                    case "SM":
                        foreach (double[] arr in ParseDoubleArrayWithSeparator(tokens, "+"))
                        {
                            stretchMarkers.Add(new ReaperStretchMarker(arr[0], arr[1], arr.Length > 2 ? arr[2] : 0));
                        }
                        break;
                    case "TAKE":
                        currentTake = new ReaperTake
                        {
                            Selected = tokens.Length > 1 && tokens[1].Equals("SEL", StringComparison.OrdinalIgnoreCase)
                        };
                        item.Takes.Add(currentTake);
                        break;
                    case "NAME":
                        currentTake.Name = ParsePathString(tokens);
                        break;
                    case "VOLPAN":
                        currentTake.VolPan = ParseDoubleArray(tokens);
                        break;
                    case "SOFFS":
                        EnsureTokenCount(tokens, 2, "take SOFFS value");
                        currentTake.SOffs = ParseDoubleToken(tokens[1]);
                        break;
                    case "PLAYRATE":
                        currentTake.PlayRate = ParseDoubleArray(tokens);
                        break;
                    case "CHANMODE":
                        EnsureTokenCount(tokens, 2, "take CHANMODE value");
                        currentTake.ChanMode = ParseIntToken(tokens[1]);
                        break;
                    default:
                        break;
                }
            }

            item.StretchSegments = ReaperStretchSegments.GetFromMarkers(stretchMarkers);
            currentTake = item;
            int takeNum = -1;

            foreach (ReaperBlock child in block.Children)
            {
                ReaperSource source = ParseSourceBlock(child);
                if (source != null)
                {
                    currentTake.Source = source;
                    takeNum += 1;
                    if (takeNum < item.Takes.Count)
                    {
                        currentTake = item.Takes[takeNum];
                    }
                    continue;
                }

                ReaperEnvelope env = ParseEnvelopeBlock(child);
                if (env != null)
                {
                    item.Envelopes.Add(env);
                }
            }

            return item;
        }

        private static ReaperSource ParseSourceBlock(ReaperBlock block)
        {
            if (block?.Type != "SOURCE")
            {
                return null;
            }

            ReaperSource source = new ReaperSource();

            foreach (string[] tokens in TokenizeLines(block.Lines))
            {
                switch (tokens[0].ToUpperInvariant())
                {
                    case "<SOURCE":
                        if (tokens.Length > 1 && tokens[1].Equals("SECTION", StringComparison.OrdinalIgnoreCase))
                        {
                            source = new ReaperSourceSection();
                        }
                        source.Type = tokens.Length > 1 ? tokens[1] : source.Type;
                        break;
                    case "FILE":
                        source.FilePath = ParsePathString(tokens);
                        break;
                    default:
                        break;
                }
            }

            if (source is ReaperSourceSection section)
            {
                foreach (string[] tokens in TokenizeLines(block.Lines))
                {
                    switch (tokens[0].ToUpperInvariant())
                    {
                        case "LENGTH":
                            EnsureTokenCount(tokens, 2, "source section LENGTH");
                            section.Length = ParseDoubleToken(tokens[1]);
                            break;
                        case "MODE":
                            EnsureTokenCount(tokens, 2, "source section MODE");
                            section.Mode = ParseIntToken(tokens[1]);
                            break;
                        case "STARTPOS":
                            EnsureTokenCount(tokens, 2, "source section STARTPOS");
                            section.StartPos = ParseDoubleToken(tokens[1]);
                            break;
                        case "OVERLAP":
                            EnsureTokenCount(tokens, 2, "source section OVERLAP");
                            section.Overlap = ParseDoubleToken(tokens[1]);
                            break;
                        default:
                            break;
                    }
                }

                foreach (ReaperBlock child in block.Children)
                {
                    ReaperSource childSource = ParseSourceBlock(child);
                    if (childSource != null)
                    {
                        section.Source = childSource;
                        section.FilePath = childSource.FilePath;
                        break;
                    }
                }
            }

            return source;
        }

        private static ReaperEnvelope ParseEnvelopeBlock(ReaperBlock block)
        {
            if (block?.Type == null || !EnvelopeTypeSet.Contains(block.Type))
            {
                return null;
            }

            ReaperEnvelope env = new ReaperEnvelope { Type = block.Type };

            foreach (string[] tokens in TokenizeLines(block.Lines))
            {
                switch (tokens[0].ToUpperInvariant())
                {
                    case "<ENVSEG":
                        if (tokens.Length > 1)
                        {
                            env.Type = tokens[1];
                        }
                        break;
                    case "ACT":
                        env.Act = ParseIntArray(tokens);
                        break;
                    case "SEG_RANGE":
                        env.SegRange = ParseDoubleArray(tokens);
                        break;
                    case "PT":
                        env.Points.Add(ParseDoubleArray(tokens));
                        break;
                    default:
                        break;
                }
            }

            return env;
        }

        private static double[] ParseDoubleArray(string[] tokens)
        {
            double[] values = new double[tokens.Length - 1];
            for (int i = 1; i < tokens.Length; i++)
            {
                values[i - 1] = ParseDoubleToken(tokens[i]);
            }
            return values;
        }

        private static List<double[]> ParseDoubleArrayWithSeparator(string[] tokens, string separator)
        {
            List<double[]> result = new List<double[]>();
            List<double> buffer = new List<double>();

            for (int i = 1; i < tokens.Length; i++)
            {
                if (!tokens[i].Equals(separator, StringComparison.Ordinal))
                {
                    buffer.Add(ParseDoubleToken(tokens[i]));
                    continue;
                }

                if (buffer.Count > 0)
                {
                    result.Add(buffer.ToArray());
                    buffer = new List<double>();
                }
            }

            if (buffer.Count > 0)
            {
                result.Add(buffer.ToArray());
            }

            return result;
        }

        private static int[] ParseIntArray(string[] tokens)
        {
            int[] values = new int[tokens.Length - 1];
            for (int i = 1; i < tokens.Length; i++)
            {
                values[i - 1] = ParseIntToken(tokens[i]);
            }
            return values;
        }

        private static string ParsePathString(string[] tokens)
        {
            if (tokens.Length < 2)
            {
                return string.Empty;
            }

            StringBuilder builder = new StringBuilder();
            for (int i = 1; i < tokens.Length; i++)
            {
                if (builder.Length > 0)
                {
                    builder.Append(' ');
                }
                builder.Append(tokens[i]);
                if (!string.IsNullOrEmpty(tokens[i]) && tokens[i][tokens[i].Length - 1] == '\"')
                {
                    break;
                }
            }
            return builder.ToString().Trim(' ').Trim('\"');
        }

        private static string[] SplitTokens(string line)
        {
            if (string.IsNullOrEmpty(line))
            {
                return Array.Empty<string>();
            }
            return line.Split(TokenSeparators, StringSplitOptions.RemoveEmptyEntries);
        }

        private static IEnumerable<string[]> TokenizeLines(IEnumerable<string> lines)
        {
            if (lines == null)
            {
                yield break;
            }

            foreach (string line in lines)
            {
                string[] tokens = SplitTokens(line);
                if (tokens.Length > 0)
                {
                    yield return tokens;
                }
            }
        }

        private static void EnsureTokenCount(string[] tokens, int minimumLength, string context)
        {
            if (tokens == null)
            {
                throw new ArgumentNullException(nameof(tokens));
            }

            if (tokens.Length >= minimumLength)
            {
                return;
            }

            string directive = tokens.Length > 0 ? tokens[0] : "<unknown>";
            string line = string.Join(" ", tokens);
            throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Malformed {0}. '{1}' expects at least {2} value token(s). Line content: '{3}'.", context, directive, minimumLength - 1, line));
        }

        private static double ParseDoubleToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidDataException("Encountered an empty value while parsing a floating-point token.");
            }

            if (!double.TryParse(token, FloatStyles, InvariantCulture, out double value))
            {
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Invalid floating-point value '{0}'.", token));
            }

            return value;
        }

        private static int ParseIntToken(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                throw new InvalidDataException("Encountered an empty value while parsing an integer token.");
            }

            if (!int.TryParse(token, IntStyles, InvariantCulture, out int value))
            {
                throw new InvalidDataException(string.Format(CultureInfo.InvariantCulture, "Invalid integer value '{0}'.", token));
            }

            return value;
        }

        private static bool ParseBoolToken(string token)
        {
            return ParseIntToken(token) != 0;
        }

        public static string SerializeToString(ReaperData rd)
        {
            byte[] bytes = SerializeToBytes(rd);
            for (int i = 0; i < bytes.Length; i++)
            {
                if (bytes[i] == 0x00)
                {
                    bytes[i] = 0x20;
                }
            }
            return Encoding.UTF8.GetString(bytes);
        }

        public static byte[] SerializeToBytes(ReaperData rd)
        {
            List<byte[]> tokens = CollectTokens(rd);
            return JoinTokensWithNullSeparator(tokens);
        }

        private static List<byte[]> CollectTokens(ReaperData data)
        {
            List<byte[]> tokens = new List<byte[]>();
            foreach (ReaperTrack track in data.Tracks)
            {
                if (data.IsTrackData)
                {
                    tokens.Add(Encoding.UTF8.GetBytes("<TRACK"));
                    AddPropertyTokens(tokens, "NAME", track.Name);
                    AddPropertyTokens(tokens, "VOLPAN", track.VolPan);
                    AddPropertyTokens(tokens, "MUTESOLO", track.MuteSolo);
                    AddPropertyTokens(tokens, "IPHASE", track.IPhase);
                }
                foreach (ReaperItem item in track.Items)
                {
                    tokens.Add(Encoding.UTF8.GetBytes("<ITEM"));
                    AddPropertyTokens(tokens, "POSITION", item.Position);
                    AddPropertyTokens(tokens, "SNAPOFFS", item.SnapOffs);
                    AddPropertyTokens(tokens, "LENGTH", item.Length);
                    AddPropertyTokens(tokens, "LOOP", item.Loop);
                    AddPropertyTokens(tokens, "ALLTAKES", item.AllTakes);
                    AddPropertyTokens(tokens, "FADEIN", item.FadeIn);
                    AddPropertyTokens(tokens, "FADEOUT", item.FadeOut);
                    AddPropertyTokens(tokens, "MUTE", item.Mute);
                    AddPropertyTokens(tokens, "SEL", item.Sel);
                    CollectTakeTokens(item, tokens);

                    if (item.Takes != null)
                    {
                        foreach (ReaperTake t in item.Takes)
                        {
                            CollectTakeTokens(t, tokens);
                        }
                    }
                    foreach (ReaperEnvelope env in item.Envelopes)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes(string.Format("<{0}", env.Type)));
                        foreach (double[] p in env.Points)
                        {
                            AddPropertyTokens(tokens, "PT", p);
                        }
                        tokens.Add(Encoding.UTF8.GetBytes(">"));
                    }
                    tokens.Add(Encoding.UTF8.GetBytes(">"));
                }
                foreach (ReaperEnvelope env in track.Envelopes)
                {
                    tokens.Add(Encoding.UTF8.GetBytes(string.Format(data.IsTrackData ? "<{0}" : "<ENVSEG {0}", env.Type)));
                    if (!data.IsTrackData && env.SegRange != null)
                    {
                        AddPropertyTokens(tokens, "SEG_RANGE", env.SegRange);
                    }
                    foreach (double[] p in env.Points)
                    {
                        AddPropertyTokens(tokens, "PT", p);
                    }
                    tokens.Add(Encoding.UTF8.GetBytes(">"));
                }
                if (data.IsTrackData)
                {
                    tokens.Add(Encoding.UTF8.GetBytes(">"));
                }
                else
                {
                    AddPropertyTokens(tokens, "TRACKSKIP", new int[] { 1, 1 });
                }
            }

            return tokens;
        }

        private static List<byte[]> CollectTakeTokens<T>(T t, List<byte[]> tokens = null) where T : ReaperTake
        {
            if (tokens == null)
            {
                tokens = new List<byte[]>();
            }
            if (t == null)
            {
                return tokens;
            }
            if (!(t is ReaperItem))
            {
                AddPropertyTokens(tokens, "TAKE", t.Selected ? "SEL" : null);
            }
            AddPropertyTokens(tokens, "NAME", t.Name, true);
            AddPropertyTokens(tokens, t is ReaperItem ? "VOLPAN" : "TAKEVOLPAN", t.VolPan);
            AddPropertyTokens(tokens, "SOFFS", t.SOffs);
            AddPropertyTokens(tokens, "PLAYRATE", t.PlayRate);
            AddPropertyTokens(tokens, "CHANMODE", t.ChanMode);
            if (t.Source != null)
            {
                AddPropertyTokens(tokens, "<SOURCE", t.Source.Type);
                AddPropertyTokens(tokens, "FILE", t.Source.FilePath, true);
                tokens.Add(Encoding.UTF8.GetBytes(">"));
            }
            return tokens;
        }

        private static void AddPropertyTokens<T>(List<byte[]> tokens, string key, T value, bool isQuoted = false)
        {
            if (value == null)
            {
                return;
            }
            string valueStr = ConvertValueToString(value, isQuoted);
            tokens.Add(Encoding.UTF8.GetBytes(string.Format("{0} {1}", key, valueStr)));
        }

        private static string ConvertValueToString<T>(T value, bool isQuoted)
        {
            if (value is Array array)
            {
                List<string> elements = new List<string>();
                foreach (object item in array)
                {
                    elements.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
                }
                return string.Join(" ", elements);
            }
            if (value is bool boolValue)
            {
                return boolValue ? "1" : "0";
            }
            if (isQuoted)
            {
                return string.Format("\"{0}\"", value);
            }
            return value.ToString();
        }

        private static byte[] JoinTokensWithNullSeparator(List<byte[]> tokens)
        {
            byte[] separator = { 0x00 };
            using (MemoryStream ms = new MemoryStream())
            {
                foreach (byte[] token in tokens)
                {
                    ms.Write(token, 0, token.Length);
                    ms.Write(separator, 0, 1);
                }
                return ms.ToArray();
            }
        }
    }
}
