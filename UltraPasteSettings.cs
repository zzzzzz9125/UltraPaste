using System.IO;

namespace UltraPaste
{
    public class UltraPasteSettings
    {
        public string CurrentLanguage { get; set; }
        public UltraPasteSettings()
        {
            CurrentLanguage = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
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