#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System.IO;

namespace UltraPaste
{
    public class UltraPasteSettings
    {
        public GeneralSettings General { get { return temp?.General ?? general;  } set { if (IsTemp) { temp.General = value; } else { general = value; } } }
        private GeneralSettings general = new GeneralSettings();
        public MediaImportSettings MediaImport { get { return temp?.MediaImport ?? mediaImport; } set { if (IsTemp) { temp.MediaImport = value; } else { mediaImport = value; } } }
        private MediaImportSettings mediaImport = new MediaImportSettings();
        public ReaperDataSettings ReaperData { get { return temp?.ReaperData ?? reaperData; } set { if (IsTemp) { temp.ReaperData = value; } else { reaperData = value; } } }
        private ReaperDataSettings reaperData = new ReaperDataSettings();
        public ClipboardImageSettings ClipboardImage { get { return temp?.ClipboardImage ?? clipboardImage; } set { if (IsTemp) { temp.ClipboardImage = value; } else { clipboardImage = value; } } }
        private ClipboardImageSettings clipboardImage = new ClipboardImageSettings();
        public VegImportSettings VegImport { get { return temp?.VegImport ?? vegImport; } set { if (temp != null) { temp.VegImport = value; } else { vegImport = value; } } }
        private VegImportSettings vegImport = new VegImportSettings();
        public SubtitlesImportSettings SubtitlesImport { get { return temp?.SubtitlesImport ?? subtitlesImport; } set { if (temp != null) { temp.SubtitlesImport = value; } else { subtitlesImport = value; } } }
        private SubtitlesImportSettings subtitlesImport = new SubtitlesImportSettings();
        public ScriptRunSettings ScriptRun { get { return temp?.ScriptRun ?? scriptRun; } set { if (temp != null) { temp.ScriptRun = value; } else { scriptRun = value; } } }
        private ScriptRunSettings scriptRun = new ScriptRunSettings();
        public PsdImportSettings PsdImport { get { return temp?.PsdImport ?? psdImport; } set { if (temp != null) { temp.PsdImport = value; } else { psdImport = value; } } }
        private PsdImportSettings psdImport = new PsdImportSettings();
        [System.Xml.Serialization.XmlIgnore]
        public bool IsTemp { get { return temp != null; } set { if (value) { temp = this.DeepClone(); } else { temp = null; } } }
        private UltraPasteSettings temp = null;

        public UltraPasteSettings()
        {

        }

        public class GeneralSettings
        {
            public string CurrentLanguage = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            public string ExcludedFiles = "*.exe;*.sfvp0;*.sfap0";
        }

        public class VegImportSettings
        {
            public int Type = 0;                   // Open Project File / Import as Nested Project / Import Media from Project
        }

        public class BaseImportSettings
        {
            public int StartPositionType = 0;      // Cursor / Play Cursor / Project Start
            public bool CursorToEnd = true;        // Whether to Put Cursor to Event End after Pasting

            public void ChangeImportStart(Vegas vegas, ref Timecode start)
            {
                start = StartPositionType == 1 ? vegas.Transport.PlayCursorPosition : StartPositionType == 2 ? new Timecode(0) : start;
            }
        }

        public class MediaImportSettings : BaseImportSettings
        {
            public int AddType = 0;                // Across Time / Across Tracks / As Takes
            public int StreamType = 0;             // All / Video only / Audio Only
            public int EventLengthType = 0;        // Media itself / Loop / Average of Loop (Across Time only)
            public bool ImageSequence = true;      // Whether to Attempt to Import Valid Image Sequence
        }

        public class ReaperDataSettings : BaseImportSettings
        {
            public bool CloseGap = false;          // 
            public bool AddVideoStreams = true;
        }

        public class ClipboardImageSettings : BaseImportSettings
        {
            public string FilePath = @"%PROJECTFOLDER%\Clipboard\<yyyyMMdd_HHmmss>.png";
        }

        public class SubtitlesImportSettings : BaseImportSettings
        {
            public int ImportType = 0;             // Text Media Generators / Regions / Text Media Generators and Regions
            public int MediaGeneratorType = 0;     // Titles And Text / ProType Titler / Legacy Text / Text OFX
            public string[] PresetNames = new string[4];
            public bool CloseGap = false;
            public int MaxCharacters = 0;
            public bool IgnoreWord = false;
            public int MaxLines = 0;
            public double DefaultLengthMilliseconds = 5000;
        }

        public class PsdImportSettings : BaseImportSettings
        {
            public bool ExpandAllLayers = true;
        }

        public class ScriptRunSettings
        {
            public bool Enabled = true;
        }

        public static UltraPasteSettings LoadFromFile(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(UltraPasteCommon.SettingsFolder, "Settings.xml");
            return filePath.DeserializeFromFile<UltraPasteSettings>() ?? new UltraPasteSettings();
        }

        public void SaveToFile(string filePath = null)
        {
            filePath = filePath ?? Path.Combine(UltraPasteCommon.SettingsFolder, "Settings.xml");
            string dirPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }

            File.WriteAllText(filePath, this.SerializeXml(), System.Text.Encoding.UTF8);
        }

        public void DeepCloneFromTemp()
        {
            if (!IsTemp)
            {
                return;
            }
            UltraPasteSettings tmp = temp.DeepClone();
            IsTemp = false;
            general = tmp.General;
            mediaImport = tmp.MediaImport;
            reaperData = tmp.ReaperData;
            clipboardImage = tmp.ClipboardImage;
        }
    }
}