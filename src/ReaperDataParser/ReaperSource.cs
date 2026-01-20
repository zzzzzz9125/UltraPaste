using System.IO;

namespace ReaperDataParser
{
    public class ReaperSource
    {
        public string Type { get; set; }
        private string filePath;
        private string filePathFull;

        public string FilePath
        {
            get
            {
                return File.Exists(filePathFull) ? filePathFull : filePath;
            }
            set
            {
                filePath = value;
            }
        }

        public void UpdateFullFilePath(string folder)
        {
            if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(filePath))
            {
                return;
            }
            string path = Path.Combine(folder, filePath);
            if (File.Exists(path))
            {
                filePathFull = path;
            }
        }
    }
}
