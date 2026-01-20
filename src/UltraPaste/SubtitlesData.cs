#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UltraPaste
{
    internal class SubtitlesData
    {
        public List<Subtitle> Subtitles { get; set; }
        public bool IsFromStrings { get; set; }

        public SubtitlesData()
        {
            Subtitles = new List<Subtitle>();
            IsFromStrings = true;
        }

        public class Subtitle
        {
            public TimeSpan Start { get; set; }
            public TimeSpan Length { get; set; }
            public TimeSpan End { get { return Start + Length; } set { Length = value - Start; } }
            public List<string> TextLines { get; set; }

            public Subtitle()
            {
                TextLines = new List<string>();
                Length = TimeSpan.FromSeconds(UltraPasteCommon.Settings.SubtitlesImport.DefaultLengthSeconds);
            }

            public string GetTextString()
            {
                return string.Join("\r\n", TextLines);
            }

            public void SplitCharacters(int maxCharacters, bool ignoreWord = false)
            {
                if (maxCharacters < 1)
                {
                    return;
                }

                List<string> oldTextLines = TextLines;
                TextLines = new List<string>();
                if (ignoreWord)
                {
                    foreach (string line in oldTextLines)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            TextLines.Add(string.Empty);
                            continue;
                        }

                        for (int i = 0; i < line.Length; i += maxCharacters)
                        {
                            TextLines.Add(line.Substring(i, Math.Min(line.Length - i, maxCharacters)));
                        }
                    }
                }
                else
                {
                    foreach (string line in oldTextLines)
                    {
                        if (string.IsNullOrEmpty(line))
                        {
                            TextLines.Add(string.Empty);
                            continue;
                        }

                        string[] parts = Regex.Split(line, @"(\s+)");

                        string current = null;
                        foreach (string part in parts)
                        {
                            if (current == null)
                            {
                                current = part;
                                continue;
                            }

                            string str = current + part;

                            if (str.Length > maxCharacters)
                            {
                                if (!Regex.IsMatch(current, @"^(\s+)$"))
                                {
                                    TextLines.Add(Regex.Replace(current, @"(\s+)$", string.Empty));
                                }
                                current = Regex.IsMatch(part, @"^(\s+)$") ? null : part;
                            }
                            else
                            {
                                current = str;
                            }
                        }
                        if (current?.Length > 0 && !Regex.IsMatch(current, @"^(\s+)$"))
                        {
                            TextLines.Add(current);
                        }
                    }
                }
            }
        }

        public void SplitLines(int maxLines)
        {
            if (maxLines < 1)
            {
                return;
            }

            List<Subtitle> oldSubtitles = Subtitles;
            Subtitles = new List<Subtitle>();
            foreach (Subtitle sub in oldSubtitles)
            {
                if (sub.TextLines.Count == 0)
                {
                    continue;
                }
                TimeSpan start = sub.Start, length = new TimeSpan(sub.Length.Ticks / (int)Math.Ceiling(sub.TextLines.Count * 1.0 / maxLines));
                for (int i = 0; i < sub.TextLines.Count; i += maxLines)
                {
                    Subtitles.Add(new Subtitle() { Start = start, Length = length, TextLines = sub.TextLines.GetRange(i, Math.Min(sub.TextLines.Count - i, maxLines)) });
                    start += length;
                }
            }
        }

        public void SplitCharactersAndLines(int maxCharacters, bool ignoreWord, int maxLines, bool multipleTracks)
        {
            if (Subtitles.Count == 0)
            {
                return;
            }
            TimeSpan singleLength = Subtitles[0].Length;
            if (maxCharacters > 0)
            {
                foreach (Subtitle sub in Subtitles)
                {
                    sub.SplitCharacters(maxCharacters, ignoreWord);
                }
            }

            SplitLines(maxLines);

            if (IsFromStrings && Subtitles.Count > 0)
            {
                TimeSpan start = new TimeSpan(0);

                singleLength = TimeSpan.FromTicks(singleLength.Ticks / Subtitles.Count);
                foreach (Subtitle sub in Subtitles)
                {
                    sub.Start = start;
                    sub.Length = singleLength;
                    start += singleLength;
                }
            }

            if (!multipleTracks)
            {
                foreach (Subtitle sub in Subtitles)
                {
                    sub.TextLines = new List<string>(new string[] { string.Join("\n", sub.TextLines) });
                }
            }
        }

        public List<VideoEvent> GenerateEventsToVegas(Timecode start, int type = 0, string presetName = null, bool closeGap = false, bool useMultipleSelectedTracks = false)
        {
            List<VideoEvent> evs = new List<VideoEvent>();
            if (Subtitles.Count == 0)
            {
                return evs;
            }

            if (closeGap)
            {
                TimeSpan offset = Subtitles[0].Start;
                foreach (Subtitle subtitle in Subtitles)
                {
                    if (subtitle.Start < offset)
                    {
                        offset = subtitle.Start;
                    }
                }
                start -= Timecode.FromMilliseconds(offset.TotalMilliseconds);
            }

            foreach (Subtitle subtitle in Subtitles)
            {
                Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.Length.TotalMilliseconds);
                Timecode subEnd = subStart + subLength;
                int newTrackIndex = -1;
                List<Track> overlapTracks = new List<Track>();
                foreach (string text in subtitle.TextLines)
                {
                    foreach (VideoEvent ev in evs)
                    {
                        if (ev.Start < subEnd && subStart < ev.End)
                        {
                            if (newTrackIndex < ev.Track.Index)
                            {
                                newTrackIndex = ev.Track.Index;
                            }
                            if (!overlapTracks.Contains(ev.Track))
                            {
                                ev.Track.Selected = false;
                                overlapTracks.Add(ev.Track);
                            }
                        }
                    }
                    evs.AddRange(TextMediaGeneratorHelper.GenerateTextEvents(subStart, subLength, text, type, presetName, useMultipleSelectedTracks, newTrackIndex));
                }

                foreach (Track trk in overlapTracks)
                {
                    trk.Selected = true;
                }
            }
            return evs;
        }

        public List<Region> GenerateRegionsToVegas(Timecode start, bool closeGap)
        {
            List<Region> regions = new List<Region>();

            if (closeGap)
            {
                TimeSpan offset = Subtitles[0].Start;
                foreach (Subtitle subtitle in Subtitles)
                {
                    if (subtitle.Start < offset)
                    {
                        offset = subtitle.Start;
                    }
                }
                start -= Timecode.FromMilliseconds(offset.TotalMilliseconds);
            }

            foreach (Subtitle subtitle in Subtitles)
            {
                Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.Length.TotalMilliseconds);
                // unfortunately, VEGAS Pro Markers don't display multiple lines of text correctly, so we use spaces to separate them for a visual experience
                string text = string.Join(" \n", subtitle.TextLines.ToArray());
                Region r = new Region(subStart, subLength, text);
                UltraPasteCommon.Vegas.Project.Regions.Add(r);
                regions.Add(r);
            }
            return regions;
        }

        public static class Parser
        {
            public static SubtitlesData ParseFromFile(string path, Timecode length = null)
            {
                string ext = Path.GetExtension(path).ToLowerInvariant();
                if (ext == ".srt")
                {
                    return ParseFromSrt(path);
                }
                else if (ext == ".lrc")
                {
                    return ParseFromLrc(path);
                }
                else if (ext == ".txt")
                {
                    return ParseFromStrings(File.ReadAllText(path), length);
                }
                else
                {
                    return null;
                }
            }

            public static SubtitlesData ParseFromStrings(string content, Timecode length = null)
            {
                SubtitlesData data = new SubtitlesData() { IsFromStrings = true };
                string[] lines = !string.IsNullOrEmpty(content) ? content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries) : new string[] { string.Empty };
                if (lines.Length == 0)
                {
                    lines = new string[] { string.Empty };
                }
                Subtitle subtitle = new Subtitle() { TextLines = new List<string>(lines) };
                if (length?.Nanos > 0)
                {
                    subtitle.Length = TimeSpan.FromMilliseconds(length.ToMilliseconds());
                }
                data.Subtitles.Add(subtitle);
                return data;
            }

            public static SubtitlesData ParseFromSrt(string path)
            {
                SubtitlesData data = new SubtitlesData() { IsFromStrings = false };
                string content = File.ReadAllText(path);
                string[] lines = content.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string line in lines)
                {
                    Subtitle subtitle = ParseSrtSubtitle(line.Trim());
                    if (subtitle != null)
                    {
                        data.Subtitles.Add(subtitle);
                    }
                }

                return data;
            }

            private static Subtitle ParseSrtSubtitle(string str)
            {
                string[] lines = str.Split(new[] { "\n", "\r\n" }, StringSplitOptions.None);
                if (lines.Length < 3)
                {
                    return null;
                }

                Subtitle subtitle = new Subtitle();

                string[] timeParts = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
                if (timeParts.Length == 2 && TryParseSrtTimeCode(timeParts[0], out TimeSpan tmpStart) && TryParseSrtTimeCode(timeParts[1], out TimeSpan tmpEnd))
                {
                    subtitle.Start = tmpStart;
                    subtitle.End = tmpEnd;
                }

                for (int i = 2; i < lines.Length; i++)
                {
                    subtitle.TextLines.Add(lines[i].TrimEnd());
                }

                return subtitle;
            }

            private static bool TryParseSrtTimeCode(string timeCode, out TimeSpan time)
            {
                return TimeSpan.TryParseExact(timeCode.Trim(), new string[] { @"hh\:mm\:ss\.FFFFFFF", @"hh\:mm\:ss\,FFFFFFF" }, CultureInfo.InvariantCulture, out time);
            }

            public static SubtitlesData ParseFromLrc(string path)
            {
                SubtitlesData data = new SubtitlesData() { IsFromStrings = false };
                string content = File.ReadAllText(path);
                string[] lines = content.Split(new[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

                Subtitle lastSub = null;
                foreach (string line in lines)
                {
                    Subtitle subtitle = ParseLrcSubtitle(line.Trim());
                    if (subtitle != null)
                    {
                        if (lastSub != null)
                        {
                            lastSub.End = subtitle.Start;
                        }
                        if (subtitle.TextLines.Count > 0)
                        {
                            data.Subtitles.Add(subtitle);
                        }
                        lastSub = subtitle;
                    }
                }
                return data;
            }

            private static Subtitle ParseLrcSubtitle(string str)
            {
                string[] tokens = str.Split(new[] { '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0 || !TryParseLrcTimeCode(tokens[0], out TimeSpan start))
                {
                    return null;
                }

                Subtitle subtitle = new Subtitle { Start = start };
                if (tokens.Length > 1)
                {
                    foreach (string text in tokens[1].Split('|'))
                    {
                        subtitle.TextLines.Add(text);
                    }
                }

                return subtitle;
            }

            private static bool TryParseLrcTimeCode(string timeCode, out TimeSpan time)
            {
                return TimeSpan.TryParseExact(timeCode.Trim(), new string[] { @"mm\:ss\.FFFFFFF", @"mm\:ss\,FFFFFFF" }, CultureInfo.InvariantCulture, out time);
            }
        }
    }
}