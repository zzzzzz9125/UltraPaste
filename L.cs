using System.Collections.Generic;

namespace UltraPaste
{
    public static class L
    {
        public static string Font, UltraPaste, UltraPasteWindow, General, Language, LanguageChange, ExcludedFiles, OneClick, SupportMe, CheckForUpdate, StartPosition, CursorToEnd, ClipboardImage, ClipboardImageFilePath, SaveSnapshotToClipboard, SaveSnapshotToClipboardAndFile, ReaperData, ExportSelectedEventsToReaperData, ExportSelectedTracksToReaperData, CloseGap, AddVideoStreams, PsdImport, ExpandAllLayers, PsdAddOtherLayers, SubtitlesImport, AddTextMediaGenerators, AddRegions, TextMediaGenerator, TextMediaGeneratorPresetName, SubtitlesMaxCharacters, SubtitlesIgnoreWord, SubtitlesMaxLines, SubtitlesMultipleTracks, SubtitlesDefaultLength, SubtitlesInputBoxEnable, SubtitlesInputBoxUseUniversal, SubtitlesInputBoxApplyTextSplitting, SubtitlesInputBoxAddToTimeline, SubtitlesApplyToSelectedEvents, SubtitlesTitlesAndTextToProTypeTitler, SubtitlesInputBox, SubtitlesInputLabel, MediaImport, MediaImportAdd, MediaImportStream, MediaImportEventLength, MediaImportImageSequence, MediaImportCustom, MediaImportCustomIncludedFiles, AddMissingStreams, VegasData, VegImport, SelectivelyPasteEventAttributes, RunScript, GenerateMixedVegasClipboardData;

        public static string[] StartPositionType, MediaImportAddType, MediaImportStreamType, MediaImportEventLengthType, VegImportType;

        public static Dictionary<string, string> LanguageList = new Dictionary<string, string> { { "en", "English" }, { "zh", "��������" } };

        // Some text localization.
        public static void Localize()
        {
            string language = UltraPasteCommon.Settings?.General.CurrentLanguage ?? System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            switch (language)
            {
                case "zh":
                    Font = "Microsoft Yahei UI";
                    UltraPaste = "����ճ����"; UltraPasteWindow = "����ճ����- ����";
                    General = "����"; Language = "����"; LanguageChange = "�޸����Ժ���Ҫ���¼��ش��ڣ�������Ҫ�ٴ��ֶ��򿪡�\n\n�Ƿ�ر����� UltraPaste ���ڣ�"; ExcludedFiles = "�ų��ļ�����"; OneClick = "һ������"; SupportMe = "������֧��"; CheckForUpdate = "������";
                    StartPosition = "��ʼλ��"; CursorToEnd = "���������ĩβ";
                    ClipboardImage = "������ͼ��"; ClipboardImageFilePath = "����·��"; SaveSnapshotToClipboard = "���������������"; SaveSnapshotToClipboardAndFile = "�����������������ļ�";
                    ReaperData = "REAPER ����"; CloseGap = "�رտ�ͷ��϶"; AddVideoStreams = "�����Ƶ��"; ExportSelectedEventsToReaperData = "������ѡ�¼��� Reaper ����"; ExportSelectedTracksToReaperData = "������ѡ����� Reaper ����";
                    PsdImport = "PSD ͼ��"; ExpandAllLayers = "չ������ͼ��"; PsdAddOtherLayers = "�������ͼ��";
                    SubtitlesImport = "��Ļ"; AddTextMediaGenerators = "����������"; AddRegions = "����"; TextMediaGenerator = "����������"; TextMediaGeneratorPresetName = "Ԥ������";
                    SubtitlesMaxCharacters = "����ַ���"; SubtitlesIgnoreWord = "���Ե���"; SubtitlesMaxLines = "�������"; SubtitlesMultipleTracks = "����"; SubtitlesDefaultLength = "Ĭ�ϳ���"; SubtitlesApplyToSelectedEvents = "Ԥ��Ӧ�õ�ѡ���¼�"; SubtitlesTitlesAndTextToProTypeTitler = "T&T �� PTT��Ļ";
                    SubtitlesInputBox = "�ı����� - ����"; SubtitlesInputLabel = "�ı�����"; SubtitlesInputBoxEnable = "���������"; SubtitlesInputBoxUseUniversal = "ͨ�ü�"; SubtitlesInputBoxApplyTextSplitting = "Ӧ���ı�����"; SubtitlesInputBoxAddToTimeline = "��ӵ�ʱ����";
                    MediaImport = "ý��"; MediaImportAdd = "��ӷ�ʽ"; MediaImportStream = "������"; MediaImportEventLength = "�¼�����"; MediaImportImageSequence = "�Զ�����ͼ������"; MediaImportCustom = "�Զ���"; MediaImportCustomIncludedFiles = "�����ļ�"; AddMissingStreams = "���ȱʧ��";
                    VegasData = "VEGAS ����"; VegImport = "VEG ��������"; SelectivelyPasteEventAttributes = "ճ���¼�����"; RunScript = "���нű�"; GenerateMixedVegasClipboardData = "���ɻ�� VEGAS ����������";

                    StartPositionType = new string[] { "���", "���Ź��", "��Ŀ��ʼ��" };
                    MediaImportAddType = new string[] { "��ʱ��", "����", "��ΪƬ��" };
                    MediaImportStreamType = new string[] { "����", "����Ƶ", "����Ƶ" };
                    MediaImportEventLengthType = new string[] { "ý������", "ѭ��", "ѭ��ȡƽ��" };
                    VegImportType = new string[] { "����Ŀ�ļ�" , "��ΪǶ����Ŀ����" , "������Ŀ�е�ý��" };
                    break;

                default:
                    Font = "Arial";
                    UltraPaste = "Ultra Paste!"; UltraPasteWindow = "Ultra Paste! - Window";
                    General = "General"; Language = "Language"; LanguageChange = "After changing the language, you need to reload UltraPaste windows and open them manually again.\n\nClose all UltraPaste windows?"; ExcludedFiles = "Excluded File Types"; OneClick = "One-Click"; SupportMe = "Support Me on Ko-fi"; CheckForUpdate = "Check For Update";
                    StartPosition = "Start Pos"; CursorToEnd = "Move Cursor to End";
                    ClipboardImage = "Clipboard Image"; ClipboardImageFilePath = "File Path"; SaveSnapshotToClipboard = "Save Snap: Clip"; SaveSnapshotToClipboardAndFile = "Save Snap: Clip+File";
                    ReaperData = "REAPER Data"; CloseGap = "Close Start Gap"; AddVideoStreams = "Add Video Streams"; ExportSelectedEventsToReaperData = "Sel.Events to Reaper"; ExportSelectedTracksToReaperData = "Sel.Tracks to Reaper";
                    PsdImport = "PSD Images"; ExpandAllLayers = "Expand All Layers"; PsdAddOtherLayers = "Add Other Layers";
                    SubtitlesImport = "Subtitles"; AddTextMediaGenerators = "Text Generators"; AddRegions = "Regions"; TextMediaGenerator = "Gen Type"; TextMediaGeneratorPresetName = "Preset";
                    SubtitlesMaxCharacters = "Max Chars"; SubtitlesIgnoreWord = "Ignore Word"; SubtitlesMaxLines = "Max Lines"; SubtitlesMultipleTracks = "Multi-Tracks"; SubtitlesDefaultLength = "Def. Length"; SubtitlesInputBoxEnable = "Enable Input Box"; SubtitlesApplyToSelectedEvents = "Apply to Sel.Events"; SubtitlesTitlesAndTextToProTypeTitler = "T&T to PTT";
                    SubtitlesInputBox = "Text Input Box"; SubtitlesInputLabel = "Text Input"; SubtitlesInputBoxEnable = "Enable Input Box"; SubtitlesInputBoxUseUniversal = "Univ. Key"; SubtitlesInputBoxApplyTextSplitting = "Apply Text Splitting"; SubtitlesInputBoxAddToTimeline = "Add to Timeline";
                    MediaImport = "Media"; MediaImportAdd = "Add Method"; MediaImportStream = "Stream Type"; MediaImportEventLength = "Event Length"; MediaImportImageSequence = "Auto Image Sequence"; MediaImportCustom = "Custom"; MediaImportCustomIncludedFiles = "Included Files"; AddMissingStreams = "Add Missing Streams";
                    VegasData = "VEGAS Data"; VegImport = "VEG Import Type"; SelectivelyPasteEventAttributes = "Paste Event Attributes"; RunScript = "Run Script"; GenerateMixedVegasClipboardData = "Generate Mixed Vegas Clipboard Data";

                    StartPositionType = new string[] { "Cursor", "Play Cursor", "Project Start" };
                    MediaImportAddType = new string[] { "Across Time", "Across Tracks", "As Takes" };
                    MediaImportStreamType = new string[] { "All", "Video Only", "Audio Only" };
                    MediaImportEventLengthType = new string[] { "Media itself", "Loop", "Average of Loop" };
                    VegImportType = new string[] { "Open Project File", "Import as Nested Project", "Import Media from Project" };
                    break;
            }
        }
    }
}