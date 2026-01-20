#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Collections.Generic;
using CapCutDataParser;
using System.Windows.Forms;
using ReaperDataParser;

namespace UltraPaste.ExtensionMethods
{
    internal static class CapCutDataExtensions
    {
        public static List<TrackEvent> GenerateEventsToVegas(this CapCutData data, Timecode start, bool closeGap, bool subtitlesOnly, out SubtitlesData subtitles)
        {
            subtitles = null;
            List<TrackEvent> events = new List<TrackEvent>();

            if (data == null)
            {
                return events;
            }

            if (data.MediaUsages != null && !subtitlesOnly)
            {
                if (closeGap)
                {
                    Timecode offset = null;
                    foreach (CapCutMediaUsage usage in data.MediaUsages)
                    {
                        Timecode tmp = ToTimecode(usage.Start);
                        if (offset == null || tmp < offset)
                        {
                            offset = tmp;
                        }
                    }
                    if (offset != null)
                    {
                        start -= offset;
                    }
                }

                foreach (CapCutMediaUsage usage in data.MediaUsages)
                {
                    if (usage == null || string.IsNullOrWhiteSpace(usage.Path))
                    {
                        continue;
                    }

                    TimeSpan duration = usage.End - usage.Start;
                    if (duration <= TimeSpan.Zero)
                    {
                        continue;
                    }

                    Timecode usageStart = start + ToTimecode(usage.Start);
                    Timecode usageLength = ToTimecode(duration);

                    if (usage.MediaType == CapCutMediaType.Audio)
                    {
                        foreach (AudioEvent ev in UltraPasteCommon.Vegas.GenerateEvents<AudioEvent>(usage.Path, usageStart, usageLength))
                        {
                            events.Add(ev);
                        }
                    }
                    else
                    {
                        foreach (VideoEvent ev in UltraPasteCommon.Vegas.GenerateEvents<VideoEvent>(usage.Path, usageStart, usageLength))
                        {
                            events.Add(ev);
                        }
                    }
                }
            }

            if (data.Subtitles != null && data.Subtitles.Count > 0)
            {
                SubtitlesData subtitlesData = new SubtitlesData { IsFromStrings = false };
                List<CapCutSubtitleBlock> blocks = new List<CapCutSubtitleBlock>(data.Subtitles);
                blocks.Sort(CompareByStart);

                foreach (CapCutSubtitleBlock block in blocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    TimeSpan length = block.End - block.Start;
                    if (length <= TimeSpan.Zero)
                    {
                        continue;
                    }

                    SubtitlesData.Subtitle subtitle = new SubtitlesData.Subtitle
                    {
                        Start = block.Start,
                        Length = length
                    };

                    foreach (string line in SplitTextLines(block.Text))
                    {
                        subtitle.TextLines.Add(line);
                    }

                    subtitlesData.Subtitles.Add(subtitle);
                }

                if (subtitlesData.Subtitles.Count > 0)
                {
                    subtitles = subtitlesData;
                }
            }

            return events;
        }

        private static Timecode ToTimecode(TimeSpan span)
        {
            return Timecode.FromMilliseconds(span.TotalMilliseconds);
        }

        private static int CompareByStart(CapCutSubtitleBlock left, CapCutSubtitleBlock right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.Start.CompareTo(right.Start);
        }

        private static IEnumerable<string> SplitTextLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield return string.Empty;
                yield break;
            }

            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length == 0)
            {
                yield return string.Empty;
                yield break;
            }

            foreach (string line in lines)
            {
                yield return line;
            }
        }
    }
}
