#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

namespace UltraPaste
{
    public class SubtitlesData
    {
        public List<Subtitle> Subtitles { get; } = new List<Subtitle>();
        public class Subtitle
        {
            public TimeSpan Start { get; set; }
            public TimeSpan End { get; set; }
            public List<string> TextLines { get; set; }

            public Subtitle()
            {
                TextLines = new List<string>();
            }
        }

        public List<TrackEvent> GenerateEventsToVegas(Timecode start, int type = 0)
        {
            List<TrackEvent> evs = new List<TrackEvent>();
            foreach (Subtitle subtitle in Subtitles)
            {
                Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.End.TotalMilliseconds - subtitle.Start.TotalMilliseconds);
                string text = string.Join("\n", subtitle.TextLines.ToArray());
                List<VideoEvent> vEvents = type == 1 ? TextMediaGenerator.GenerateProTypeTitlerEvents(subStart, subLength, text, null, true)
                                         : type == 2 ?       TextMediaGenerator.GenerateTextOfxEvents(subStart, subLength, text, null, true) 
                                                     : TextMediaGenerator.GenerateTitlesAndTextEvents(subStart, subLength, text, null, true);
                evs.AddRange(vEvents);
            }
            return evs;
        }

        public List<Region> GenerateRegionsToVegas(Timecode start)
        {
            List<Region> regions = new List<Region>();
            foreach (Subtitle subtitle in Subtitles)
            {
                Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.End.TotalMilliseconds - subtitle.Start.TotalMilliseconds);
                // unfortunately, VEGAS Pro Markers don't display multiple lines of text correctly, so we use spaces to separate them for a visual experience
                string text = string.Join(" \n", subtitle.TextLines.ToArray());
                Region r = new Region(subStart, subLength, text);
                UltraPasteCommon.myVegas.Project.Regions.Add(r);
                regions.Add(r);
            }
            return regions;
        }

        public static class Parser
        {
            public static SubtitlesData Parse(string path)
            {
                string ext = Path.GetExtension(path).ToLower();
                if (ext == ".srt")
                {
                    return ParseFromSrt(path);
                }
                else if (ext == ".lrc")
                {
                    return ParseFromLrc(path);
                }
                else
                {
                    return null;
                }
            }

            public static SubtitlesData ParseFromSrt(string path)
            {
                SubtitlesData data = new SubtitlesData();
                string content = Encoding.UTF8.GetString(File.ReadAllBytes(path));
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
                SubtitlesData data = new SubtitlesData();
                string content = Encoding.UTF8.GetString(File.ReadAllBytes(path));
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
                string[] tokens = str.Split(new[] { "[", "]" }, StringSplitOptions.RemoveEmptyEntries);

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