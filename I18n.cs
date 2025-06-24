using System.IO;
using System.Reflection;
using System.Collections.Generic;

namespace UltraPaste
{
    public static class I18n
    {
        public static TranslationSettings Settings { get; private set; }
        public class TranslationSettings
        {
            public string Current = System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            public string LastUpdatedVersion = UltraPasteCommon.VERSION;
            public List<TranslationLanguage> Languages { get { return languages; } }
            private readonly List<TranslationLanguage> languages = new List<TranslationLanguage>();
        }
        public static string FilePath = Path.Combine(UltraPasteCommon.SettingsFolder, "Languages.xml");
        public static TranslationStrings Translation { get; private set; }

        public static Dictionary<string, string> LanguageDictionary { get; private set; }

        public class TranslationLanguage
        {
            public string ShortName, DisplayName;

            public TranslationStrings Translation { get { return translation; } set { translation = value; } }
            private TranslationStrings translation = new TranslationStrings();
        }

        public class TranslationStrings
        {
            public string Font, UltraPaste, UltraPasteWindow, General, Language, LanguageChange, ExcludedFiles, OneClick, SupportMe, CheckForUpdate, StartPosition, CursorToEnd, ClipboardImage, ClipboardImageFilePath, SaveSnapshotToClipboard, SaveSnapshotToClipboardAndFile, ReaperData, ExportSelectedEventsToReaperData, ExportSelectedTracksToReaperData, CloseGap, AddVideoStreams, PsdImport, ExpandAllLayers, PsdAddOtherLayers, SubtitlesImport, AddTextMediaGenerators, AddRegions, TextMediaGenerator, TextMediaGeneratorPresetName, SubtitlesMaxCharacters, SubtitlesIgnoreWord, SubtitlesMaxLines, SubtitlesMultipleTracks, SubtitlesDefaultLength, SubtitlesInputBoxEnable, SubtitlesInputBoxUseUniversal, SubtitlesInputBoxApplyTextSplitting, SubtitlesInputBoxAddToTimeline, SubtitlesApplyToSelectedEvents, SubtitlesTitlesAndTextToProTypeTitler, SubtitlesInputBox, SubtitlesInputLabel, MediaImport, MediaImportAdd, MediaImportStream, MediaImportEventLength, MediaImportImageSequence, MediaImportCustom, MediaImportCustomIncludedFiles, AddMissingStreams, VegasData, VegImport, SelectivelyPasteEventAttributes, RunScript, GenerateMixedVegasClipboardData;

            public string[] StartPositionType, MediaImportAddType, MediaImportStreamType, MediaImportEventLengthType, VegImportType;

            public void FillMissingTranslations(TranslationStrings source)
            {
                PropertyInfo[] properties = typeof(TranslationStrings).GetProperties(BindingFlags.Public | BindingFlags.Instance);

                foreach (PropertyInfo property in properties)
                {
                    if (property.PropertyType != typeof(string)) continue;

                    string targetValue = (string)property.GetValue(this);

                    if (string.IsNullOrEmpty(targetValue))
                    {
                        string sourceValue = (string)property.GetValue(source);

                        property.SetValue(this, sourceValue);
                    }
                }
            }

            public TranslationStrings()
            {
                Font = "Arial";
                UltraPaste = "Ultra Paste!";
                UltraPasteWindow = "Ultra Paste! - Window";
                General = "General";
                Language = "Language";
                LanguageChange = "After changing the language, you need to reload UltraPaste windows and open them manually again.\n\nClose all UltraPaste windows?";
                ExcludedFiles = "Excluded File Types";
                OneClick = "One-Click";
                SupportMe = "Support Me on Ko-fi";
                CheckForUpdate = "Check For Update";
                StartPosition = "Start Pos";
                CursorToEnd = "Move Cursor to End";
                ClipboardImage = "Clipboard Image";
                ClipboardImageFilePath = "File Path";
                SaveSnapshotToClipboard = "Save Snap: Clip";
                SaveSnapshotToClipboardAndFile = "Save Snap: Clip+File";
                ReaperData = "REAPER Data";
                CloseGap = "Close Start Gap";
                AddVideoStreams = "Add Video Streams";
                ExportSelectedEventsToReaperData = "Sel.Events to Reaper";
                ExportSelectedTracksToReaperData = "Sel.Tracks to Reaper";
                PsdImport = "PSD Images";
                ExpandAllLayers = "Expand All Layers";
                PsdAddOtherLayers = "Add Other Layers";
                SubtitlesImport = "Subtitles";
                AddTextMediaGenerators = "Text Generators";
                AddRegions = "Regions";
                TextMediaGenerator = "Gen Type";
                TextMediaGeneratorPresetName = "Preset";
                SubtitlesMaxCharacters = "Max Chars";
                SubtitlesIgnoreWord = "Ignore Word";
                SubtitlesMaxLines = "Max Lines";
                SubtitlesMultipleTracks = "Multi-Tracks";
                SubtitlesDefaultLength = "Def. Length";
                SubtitlesInputBoxEnable = "Enable Input Box";
                SubtitlesApplyToSelectedEvents = "Apply to Sel.Events";
                SubtitlesTitlesAndTextToProTypeTitler = "T&T to PTT";
                SubtitlesInputBox = "Text Input Box";
                SubtitlesInputLabel = "Text Input";
                SubtitlesInputBoxEnable = "Enable Input Box";
                SubtitlesInputBoxUseUniversal = "Univ. Key";
                SubtitlesInputBoxApplyTextSplitting = "Apply Text Splitting";
                SubtitlesInputBoxAddToTimeline = "Add to Timeline";
                MediaImport = "Media";
                MediaImportAdd = "Add Method";
                MediaImportStream = "Stream Type";
                MediaImportEventLength = "Event Length";
                MediaImportImageSequence = "Auto Image Sequence";
                MediaImportCustom = "Custom";
                MediaImportCustomIncludedFiles = "Included Files";
                AddMissingStreams = "Add Missing Streams";
                VegasData = "VEGAS Data";
                VegImport = "VEG Import Type";
                SelectivelyPasteEventAttributes = "Paste Event Attributes";
                RunScript = "Run Script";
                GenerateMixedVegasClipboardData = "Generate Mixed Vegas Clipboard Data";

                StartPositionType = new string[] { "Cursor", "Play Cursor", "Project Start" };
                MediaImportAddType = new string[] { "Across Time", "Across Tracks", "As Takes" };
                MediaImportStreamType = new string[] { "All", "Video Only", "Audio Only" };
                MediaImportEventLengthType = new string[] { "Media itself", "Loop", "Average of Loop" };
                VegImportType = new string[] { "Open Project File", "Import as Nested Project", "Import Media from Project" };
            }

            public TranslationStrings(string shortName) : this()
            {
                switch (shortName)
                {
                    case "zh":
                        Font = "Microsoft Yahei UI";
                        UltraPaste = "����ճ����";
                        UltraPasteWindow = "����ճ����- ����";
                        General = "����";
                        Language = "����";
                        LanguageChange = "�޸����Ժ���Ҫ���¼��ش��ڣ�������Ҫ�ٴ��ֶ��򿪡�\n\n�Ƿ�ر����� UltraPaste ���ڣ�";
                        ExcludedFiles = "�ų��ļ�����";
                        OneClick = "һ������";
                        SupportMe = "������֧��";
                        CheckForUpdate = "������";
                        StartPosition = "��ʼλ��";
                        CursorToEnd = "���������ĩβ";
                        ClipboardImage = "������ͼ��";
                        ClipboardImageFilePath = "����·��";
                        SaveSnapshotToClipboard = "���������������";
                        SaveSnapshotToClipboardAndFile = "�����������������ļ�";
                        ReaperData = "REAPER ����";
                        CloseGap = "�رտ�ͷ��϶";
                        AddVideoStreams = "�����Ƶ��";
                        ExportSelectedEventsToReaperData = "������ѡ�¼��� Reaper ����";
                        ExportSelectedTracksToReaperData = "������ѡ����� Reaper ����";
                        PsdImport = "PSD ͼ��";
                        ExpandAllLayers = "չ������ͼ��";
                        PsdAddOtherLayers = "�������ͼ��";
                        SubtitlesImport = "��Ļ";
                        AddTextMediaGenerators = "����������";
                        AddRegions = "����";
                        TextMediaGenerator = "����������";
                        TextMediaGeneratorPresetName = "Ԥ������";
                        SubtitlesMaxCharacters = "����ַ���";
                        SubtitlesIgnoreWord = "���Ե���";
                        SubtitlesMaxLines = "�������";
                        SubtitlesMultipleTracks = "����";
                        SubtitlesDefaultLength = "Ĭ�ϳ���";
                        SubtitlesApplyToSelectedEvents = "Ԥ��Ӧ�õ�ѡ���¼�";
                        SubtitlesTitlesAndTextToProTypeTitler = "T&T �� PTT��Ļ";
                        SubtitlesInputBox = "�ı����� - ����";
                        SubtitlesInputLabel = "�ı�����";
                        SubtitlesInputBoxEnable = "���������";
                        SubtitlesInputBoxUseUniversal = "ͨ�ü�";
                        SubtitlesInputBoxApplyTextSplitting = "Ӧ���ı�����";
                        SubtitlesInputBoxAddToTimeline = "��ӵ�ʱ����";
                        MediaImport = "ý��";
                        MediaImportAdd = "��ӷ�ʽ";
                        MediaImportStream = "������";
                        MediaImportEventLength = "�¼�����";
                        MediaImportImageSequence = "�Զ�����ͼ������";
                        MediaImportCustom = "�Զ���";
                        MediaImportCustomIncludedFiles = "�����ļ�";
                        AddMissingStreams = "���ȱʧ��";
                        VegasData = "VEGAS ����";
                        VegImport = "VEG ��������";
                        SelectivelyPasteEventAttributes = "ճ���¼�����";
                        RunScript = "���нű�";
                        GenerateMixedVegasClipboardData = "���ɻ�� VEGAS ����������";

                        StartPositionType = new string[] { "���", "���Ź��", "��Ŀ��ʼ��" };
                        MediaImportAddType = new string[] { "��ʱ��", "����", "��ΪƬ��" };
                        MediaImportStreamType = new string[] { "����", "����Ƶ", "����Ƶ" };
                        MediaImportEventLengthType = new string[] { "ý������", "ѭ��", "ѭ��ȡƽ��" };
                        VegImportType = new string[] { "����Ŀ�ļ�", "��ΪǶ����Ŀ����", "������Ŀ�е�ý��" };
                        break;

                    default:
                        break;
                }
            }
        }

        public static void Localize()
        {
            Settings = new TranslationSettings();
            if (File.Exists(FilePath))
            {
                Settings = File.ReadAllText(FilePath).DeserializeXml<TranslationSettings>();
                if (Settings.LastUpdatedVersion != UltraPasteCommon.VERSION)
                {
                    foreach (TranslationLanguage tran in Settings.Languages)
                    {
                        tran.Translation.FillMissingTranslations(new TranslationStrings(tran.ShortName));
                    }
                    SaveSettingsToXml();
                }
            }

            if (!File.Exists(FilePath) || Settings.Languages.Count == 0)
            {
                Settings = new TranslationSettings();
                TranslationLanguage tran = new TranslationLanguage()
                {
                    ShortName = "en",
                    DisplayName = "English",
                    Translation = new TranslationStrings("en")
                };
                Settings.Languages.Add(tran);
                tran = new TranslationLanguage()
                {
                    ShortName = "zh",
                    DisplayName = "��������",
                    Translation = new TranslationStrings("zh")
                };
                Settings.Languages.Add(tran);

                SaveSettingsToXml();
            }

            string language = Settings.Current ?? System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;

            bool success = false;

            foreach (TranslationLanguage lang in Settings.Languages)
            {
                if (lang.ShortName == language)
                {
                    Translation = lang.Translation;
                    success = true;
                    break;
                }
            }

            if (!success)
            {
                Translation = Settings.Languages[0].Translation;
            }

            LanguageDictionary = new Dictionary<string, string>();
            foreach (TranslationLanguage lang in Settings.Languages)
            {
                LanguageDictionary.Add(lang.ShortName, lang.DisplayName);
            }
        }

        public static void SaveSettingsToXml()
        {
            string xmlStr = Settings.SerializeXml();
            File.WriteAllText(FilePath, xmlStr);
        }
    }
}