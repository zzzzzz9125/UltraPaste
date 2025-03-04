#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

public class SrtData
{
    public List<SrtSubtitle> Subtitles { get; } = new List<SrtSubtitle>();
    public class SrtSubtitle
    {
        public int Number { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public List<string> TextLines { get; set; }

        public SrtSubtitle()
        {
            TextLines = new List<string>();
        }
    }

    public List<TrackEvent> GenerateEventsToVegas(Timecode start)
    {
        List<TrackEvent> evs = new List<TrackEvent>();
        foreach (SrtSubtitle subtitle in Subtitles)
        {
            Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.End.TotalMilliseconds - subtitle.Start.TotalMilliseconds);
            string text = string.Join("\n", subtitle.TextLines.ToArray());
            evs.AddRange(UltraPasteCommon.GenerateTitlesAndTextEvents(subStart, subLength, text, null, true));
        }
        return evs;
    }

    public List<Region> GenerateRegionsToVegas(Timecode start)
    {
        List<Region> regions = new List<Region>();
        foreach (SrtSubtitle subtitle in Subtitles)
        {
            Timecode subStart = Timecode.FromMilliseconds(subtitle.Start.TotalMilliseconds) + start, subLength = Timecode.FromMilliseconds(subtitle.End.TotalMilliseconds - subtitle.Start.TotalMilliseconds);
            string text = string.Join("\n", subtitle.TextLines.ToArray());
            Region r = new Region(subStart, subLength, text);
            regions.Add(r);
            UltraPasteCommon.myVegas.Project.Regions.Add(r);
        }
        return regions;
    }

    public static class Parser
    {
        public static SrtData Parse(string path)
        {
            SrtData data = new SrtData();
            string srtContent = Encoding.UTF8.GetString(File.ReadAllBytes(path));
            string[] lines = srtContent.Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string line in lines)
            {
                SrtSubtitle subtitle = ParseSubtitle(line.Trim());
                if (subtitle != null)
                {
                    data.Subtitles.Add(subtitle);
                }
            }
            return data;
        }

        private static SrtSubtitle ParseSubtitle(string str)
        {
            string[] lines = str.Split('\n');
            if (lines.Length < 3)
            {
                return null;
            }

            SrtSubtitle subtitle = new SrtSubtitle();

            if (int.TryParse(lines[0], out int n))
            {
                subtitle.Number = n;
            }

            string[] timeParts = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
            if (timeParts.Length == 2)
            {
                subtitle.Start = ParseTimeCode(timeParts[0]);
                subtitle.End = ParseTimeCode(timeParts[1]);
            }

            for (int i = 2; i < lines.Length; i++)
            {
                subtitle.TextLines.Add(lines[i].TrimEnd());
            }

            return subtitle;
        }

        private static TimeSpan ParseTimeCode(string timeCode)
        {
            return TimeSpan.ParseExact(timeCode.Trim(), timeCode.Contains(',') ? @"hh\:mm\:ss\,fff" : @"hh\:mm\:ss\.fff", CultureInfo.InvariantCulture);
        }
    }
}

