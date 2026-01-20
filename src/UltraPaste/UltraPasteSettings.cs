#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UltraPaste
{
    using CapCutDataParser;
    using ExtensionMethods;
    public class UltraPasteSettings
    {
        public GeneralSettings General
        {
            get { return general; }
            set { general = value; }
        }
        private GeneralSettings general = new GeneralSettings();
        public MediaImportSettings MediaImport
        {
            get { return mediaImport; }
            set { mediaImport = value; }
        }
        private MediaImportSettings mediaImport = new MediaImportSettings();
        public ReaperDataSettings ReaperData
        {
            get { return reaperData; }
            set { reaperData = value; }
        }
        private ReaperDataSettings reaperData = new ReaperDataSettings();
        public CapCutDataSettings CapCutData
        {
            get { return capCutData; }
            set { capCutData = value; }
        }
        private CapCutDataSettings capCutData = new CapCutDataSettings();
        public ClipboardImageSettings ClipboardImage
        {
            get { return clipboardImage; }
            set { clipboardImage = value; }
        }
        private ClipboardImageSettings clipboardImage = new ClipboardImageSettings();
        public VegasDataSettings VegasData
        {
            get { return vegasData; }
            set { vegasData = value; }
        }
        private VegasDataSettings vegasData = new VegasDataSettings();
        public SubtitlesImportSettings SubtitlesImport
        {
            get { return subtitlesImport; }
            set { subtitlesImport = value; }
        }
        private SubtitlesImportSettings subtitlesImport = new SubtitlesImportSettings();
        public PsdImportSettings PsdImport
        {
            get { return psdImport; }
            set { psdImport = value; }
        }
        private PsdImportSettings psdImport = new PsdImportSettings();
        public List<CustomMediaImportSettings> Customs
        {
            get { return customs; }
            set { customs = value; }
        }
        private List<CustomMediaImportSettings> customs = new List<CustomMediaImportSettings>();

        public class GeneralSettings
        {
            public string ExcludedFiles = "*.exe;*.sfvp0;*.sfap0";
            public int LastTabIndex = 0;
        }

        public class VegasDataSettings
        {
            public int VegImportType = 0;          // Open Project File / Import as Nested Project / Import Media from Project
            public bool SelectivelyPasteEventAttributes = true;
            public bool RunScript = true;
            public List<VegasDataClipboardCollection> Collections = new List<VegasDataClipboardCollection>();

            public class VegasDataClipboardCollection
            {
                public string Name { get; set; }
                //public Dictionary<string, byte[]> Data { get; set; }

                public VegasDataClipboardCollection()
                {
                    foreach (string format in Clipboard.GetDataObject().GetFormats())
                    {
                        object obj = Clipboard.GetData(format);
                        if (obj is MemoryStream ms)
                        {
                            byte[] bytes = ms.ToArray();

                            if (bytes == null || bytes.Length == 0)
                            {
                                continue;
                            }

                            if (format?.ToLowerInvariant().Contains("vegas") == true || UltraPasteCommon.Vegas.VideoFX.FindChildByUniqueID(format) != null)
                            {
                                //Data.Add(format, bytes);
                            }
                        }
                    }
                }
            }
        }

        public class BaseImportSettings
        {
            public int StartPositionType = 0;      // Cursor / Play Cursor / Project Start
            public bool CursorToEnd = true;        // Whether to Put Cursor to Event End after Pasting

            public void ChangeImportStart(Vegas vegas, ref Timecode start)
            {
                start = StartPositionType == 1 ? vegas.Transport.PlayCursorPosition : StartPositionType == 2 ? new Timecode(0) : (start ?? vegas.Transport.CursorPosition);
            }
        }

        public class MediaImportSettings : BaseImportSettings
        {
            public int AddType = 0;                // Across Time / Across Tracks / As Takes
            public int StreamType = 0;             // All / Video only / Audio Only
            public int EventLengthType = 0;        // Media itself / Loop / Average of Loop (Across Time only)
            public bool ImageSequence = true;      // Whether to Attempt to Import Valid Image Sequence
        }

        public class CustomMediaImportSettings : MediaImportSettings
        {
            public string IncludedFiles { get; set; }

            public CustomMediaImportSettings()
            {
                IncludedFiles = string.Empty;
            }
        }

        public class ReaperDataSettings : BaseImportSettings
        {
            public bool CloseGap = true;
            public bool AddVideoStreams = true;
        }

        public class CapCutDataSettings : BaseImportSettings
        {
            public bool CloseGap = true;
            public bool SubtitlesOnly = false;
        }

        public class ClipboardImageSettings : BaseImportSettings
        {
            public string FilePath = @".\Clipboard\<yyyyMMdd_HHmmss>.png";

            public string GetTrueFilePath()
            {
                string projectDir = Path.GetDirectoryName(UltraPasteCommon.Vegas.Project.FilePath)?.Trim('\\', '/') ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string filePath = Environment.ExpandEnvironmentVariables(Regex.Replace(FilePath, @"%PROJECTFOLDER%", projectDir, RegexOptions.IgnoreCase))?.Trim('\\', '/');
                foreach (Match m in Regex.Matches(filePath, @"<.*?>"))
                {
                    filePath = filePath?.Replace(m.Value, DateTime.Now.ToString(m.Value)?.Trim('<', '>'));
                }
                if (!Path.IsPathRooted(filePath))
                {
                    filePath = Path.Combine(projectDir, filePath);
                }
                try
                {
                    filePath = Path.GetFullPath(filePath);
                    string dir = Path.GetDirectoryName(filePath);

                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }
                catch
                {
                    filePath = Path.Combine(projectDir, "Clipboard", string.Format("{0}.png", DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                }
                return filePath;
            }
        }

        public class SubtitlesImportSettings : BaseImportSettings
        {
            public bool AddTextMediaGenerators = true;
            public bool AddRegions = false;
            public int MediaGeneratorType = 0;     // 0: Titles & Text / 1: ProType Titler / 2: Legacy Text / 3: Text OFX / 4: Ignite Text / 5: Ignite 360Text / 6: Universe Text Typographic / 7: Universe Text Hacker Text / 8: OFXClock
            public string[] PresetNames
            {
                get
                {
                    return presetNames;
                }
                set
                {
                    if (value == null)
                    {
                        return;
                    }
                    presetNames = new string[9];
                    int count = Math.Min(presetNames.Length, value.Length);
                    for (int i = 0; i < count; i++)
                    {
                        presetNames[i] = value[i];
                    }
                }
            }
            private string[] presetNames = new string[9];
            public int MaxCharacters = 0;
            public bool IgnoreWord = false;
            public int MaxLines = 0;
            public bool MultipleTracks = false;
            public bool CloseGap = true;
            public double DefaultLengthSeconds = 5;
            public bool InputBoxUseUniversal = true;
            public int InputBoxMaxCharacters = 0;
            public bool InputBoxIgnoreWord = false;
            public int InputBoxMaxLines = 0;
            public bool InputBoxMultipleTracks = false;
        }

        public class PsdImportSettings : BaseImportSettings
        {
            public bool ExpandAllLayers = true;
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
    }
}