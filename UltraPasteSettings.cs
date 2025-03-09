using System.IO;

namespace UltraPaste
{
    public class UltraPasteSettings
    {
        public GeneralSettings General { get; set; }

        public UltraPasteSettings()
        {
            General = new GeneralSettings();
        }

        public class GeneralSettings
        {
            public string CurrentLanguage = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
        }

        public class BaseImportSettings
        {
            public int AddType = 0;           // Across Time / Across Tracks / As Takes
            public int StreamType = 0;        // All / Video only / Audio Only
            public int StartPositionType = 0; // Cursor / Play Cursor / Project Start
            public int EventLengthType = 0;   // Media itself / Loop / Average of Loop (Across Time only)
            public int RepeatTimes = 0;       // Number of times to repeat
            public bool CursorToEnd = false;  // Put Cursor to Event End after Pasting
        }

        public static UltraPasteSettings LoadFromFile(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(UltraPasteCommon.SettingsFolder, "UltraPasteSettings.xml");
            return filePath.DeserializeXml<UltraPasteSettings>() ?? new UltraPasteSettings();
        }

        public void SaveToFile(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(UltraPasteCommon.SettingsFolder, "UltraPasteSettings.xml");
            string dirPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                byte[] buffer = System.Text.Encoding.Default.GetBytes(this.SerializeXml());
                fileStream.Write(buffer, 0, buffer.Length);
            }
        }
    }
}