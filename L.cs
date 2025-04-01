using System.Collections.Generic;

namespace UltraPaste
{
    public static class L
    {
        public static string Font, UltraPaste, UltraPasteWindow, General, Language, LanguageChange, ExcludedFiles, OneClick, SupportMe, CheckForUpdate, StartPosition, CursorToEnd, ClipboardImage, ClipboardImageFilePath, SaveSnapshotToClipboard, SaveSnapshotToClipboardAndFile, ReaperData, ExportSelectedEventsToReaperData, ExportSelectedTracksToReaperData, CloseGap, AddVideoStreams, PsdImport, ExpandAllLayers, PsdAddOtherLayers, SubtitlesImport, AddTextMediaGenerators, AddRegions, TextMediaGenerator, TextMediaGeneratorPresetName, SubtitlesMaxCharacters, SubtitlesIgnoreWord, SubtitlesMaxLines, SubtitlesMultipleTracks, SubtitlesDefaultLength, SubtitlesInputBoxEnable, SubtitlesInputBoxUseUniversal, SubtitlesInputBoxApplyTextSplitting, SubtitlesInputBoxAddToTimeline, SubtitlesApplyToSelectedEvents, SubtitlesTitlesAndTextToProTypeTitler, SubtitlesInputBox, SubtitlesInputLabel, MediaImport, MediaImportAdd, MediaImportStream, MediaImportEventLength, MediaImportImageSequence, MediaImportCustom, MediaImportCustomIncludedFiles, AddMissingStreams, VegasData, VegImport, SelectivelyPasteEventAttributes, RunScript, GenerateMixedVegasClipboardData;

        public static string[] StartPositionType, MediaImportAddType, MediaImportStreamType, MediaImportEventLengthType, VegImportType;

        public static Dictionary<string, string> LanguageList = new Dictionary<string, string> { { "en", "English" }, { "zh", "简体中文" } };

        // Some text localization.
        public static void Localize()
        {
            string language = UltraPasteCommon.Settings?.General.CurrentLanguage ?? System.Globalization.CultureInfo.CurrentCulture.TwoLetterISOLanguageName;
            switch (language)
            {
                case "zh":
                    Font = "Microsoft Yahei UI";
                    UltraPaste = "超级粘贴！"; UltraPasteWindow = "超级粘贴！- 窗口";
                    General = "常规"; Language = "语言"; LanguageChange = "修改语言后需要重新加载窗口，并且需要再次手动打开。\n\n是否关闭所有 UltraPaste 窗口？"; ExcludedFiles = "排除文件类型"; OneClick = "一键操作"; SupportMe = "爱发电支持"; CheckForUpdate = "检查更新";
                    StartPosition = "起始位置"; CursorToEnd = "将光标移至末尾";
                    ClipboardImage = "剪贴板图像"; ClipboardImageFilePath = "保存路径"; SaveSnapshotToClipboard = "保存快照至剪贴板"; SaveSnapshotToClipboardAndFile = "保存快照至剪贴板和文件";
                    ReaperData = "REAPER 数据"; CloseGap = "关闭开头间隙"; AddVideoStreams = "添加视频流"; ExportSelectedEventsToReaperData = "导出所选事件到 Reaper 数据"; ExportSelectedTracksToReaperData = "导出所选轨道到 Reaper 数据";
                    PsdImport = "PSD 图像"; ExpandAllLayers = "展开所有图层"; PsdAddOtherLayers = "添加其他图层";
                    SubtitlesImport = "字幕"; AddTextMediaGenerators = "文字生成器"; AddRegions = "区域"; TextMediaGenerator = "生成器类型"; TextMediaGeneratorPresetName = "预设名称";
                    SubtitlesMaxCharacters = "最大字符数"; SubtitlesIgnoreWord = "忽略单词"; SubtitlesMaxLines = "最大行数"; SubtitlesMultipleTracks = "多轨道"; SubtitlesDefaultLength = "默认长度"; SubtitlesApplyToSelectedEvents = "预设应用到选中事件"; SubtitlesTitlesAndTextToProTypeTitler = "T&T 到 PTT字幕";
                    SubtitlesInputBox = "文本输入 - 窗口"; SubtitlesInputLabel = "文本输入"; SubtitlesInputBoxEnable = "启用输入框"; SubtitlesInputBoxUseUniversal = "通用键"; SubtitlesInputBoxApplyTextSplitting = "应用文本裁切"; SubtitlesInputBoxAddToTimeline = "添加到时间线";
                    MediaImport = "媒体"; MediaImportAdd = "添加方式"; MediaImportStream = "流类型"; MediaImportEventLength = "事件长度"; MediaImportImageSequence = "自动导入图像序列"; MediaImportCustom = "自定义"; MediaImportCustomIncludedFiles = "包含文件"; AddMissingStreams = "添加缺失流";
                    VegasData = "VEGAS 数据"; VegImport = "VEG 导入类型"; SelectivelyPasteEventAttributes = "粘贴事件属性"; RunScript = "运行脚本"; GenerateMixedVegasClipboardData = "生成混合 VEGAS 剪贴板数据";

                    StartPositionType = new string[] { "光标", "播放光标", "项目起始处" };
                    MediaImportAddType = new string[] { "跨时间", "跨轨道", "作为片段" };
                    MediaImportStreamType = new string[] { "所有", "仅视频", "仅音频" };
                    MediaImportEventLengthType = new string[] { "媒体自身", "循环", "循环取平均" };
                    VegImportType = new string[] { "打开项目文件" , "作为嵌套项目导入" , "导入项目中的媒体" };
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