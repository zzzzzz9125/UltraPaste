using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

public class SrtData
{
    public List<SrtSubtitle> Subtitles { get; } = new List<SrtSubtitle>();
    public class SrtSubtitle
    {
        public int SequenceNumber { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public List<string> TextLines { get; set; }

        public SrtSubtitle()
        {
            TextLines = new List<string>();
        }
    }

    public static class Parser
    {
        public static SrtData Parse(string srtContent)
        {
            SrtData data = new SrtData();
            string[] lines = srtContent.Split(new[] { "\n\n" }, StringSplitOptions.RemoveEmptyEntries);

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
            if (lines.Length < 3) return null;

            SrtSubtitle subtitle = new SrtSubtitle();

            if (int.TryParse(lines[0], out int seq))
            {
                subtitle.SequenceNumber = seq;
            }

            string[] timeParts = lines[1].Split(new[] { " --> " }, StringSplitOptions.None);
            if (timeParts.Length == 2)
            {
                subtitle.StartTime = ParseTimeCode(timeParts[0]);
                subtitle.EndTime = ParseTimeCode(timeParts[1]);
            }

            for (int i = 2; i < lines.Length; i++)
            {
                subtitle.TextLines.Add(lines[i].TrimEnd());
            }

            return subtitle;
        }

        private static TimeSpan ParseTimeCode(string timeCode)
        {
            var format = timeCode.Contains(',') ? @"hh\:mm\:ss\,fff" : @"hh\:mm\:ss\.fff";
            return TimeSpan.ParseExact(timeCode.Trim(), format, CultureInfo.InvariantCulture);
        }
    }
}

