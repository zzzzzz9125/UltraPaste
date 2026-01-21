using System;

namespace CapCutDataParser
{
    public sealed class CapCutMediaUsage
    {
        public string MaterialId { get; set; }
        public string Name { get; set; }

        private string filePath;
        private string filePathFull;

        public string Path
        {
            get
            {
                return System.IO.File.Exists(filePathFull) ? filePathFull : filePath;
            }
            set
            {
                filePath = value;
            }
        }

        public CapCutMediaType MediaType { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
        public TimeSpan? SourceStart { get; set; }
        public TimeSpan? SourceDuration { get; set; }
        public TimeSpan? SourceEnd => SourceStart.HasValue && SourceDuration.HasValue ? SourceStart.Value + SourceDuration.Value : (TimeSpan?)null;
        public bool HasSourceRange => SourceStart.HasValue && SourceDuration.HasValue;
        public double PlaybackRate { get; set; } = 1d;
        public double Volume { get; set; } = 1d;
        public bool Reverse { get; set; }
        public int TrackOrder { get; set; } = -1;
        public string TrackId { get; set; }
        public string TrackName { get; set; }
        public string TrackType { get; set; }
        public TimeSpan? FadeIn { get; set; }
        public TimeSpan? FadeOut { get; set; }
        public bool HasSoundSeparated { get; set; } = true;

        public void UpdateFullFilePath(string draftPath)
        {
            if (string.IsNullOrEmpty(draftPath) || string.IsNullOrEmpty(filePath))
            {
                return;
            }
            
            string path = System.IO.Path.Combine(draftPath, CapCutData.DraftPathRegex.Replace(filePath, ""));
            if (System.IO.File.Exists(path))
            {
                filePathFull = path;
            }
        }
    }
}