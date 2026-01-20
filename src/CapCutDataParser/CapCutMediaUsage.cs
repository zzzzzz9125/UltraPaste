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