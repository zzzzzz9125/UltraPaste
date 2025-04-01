#if !Sony
using ScriptPortal.Vegas;
using Region = ScriptPortal.Vegas.Region;
#else
using Sony.Vegas;
using Region = Sony.Vegas.Region;
#endif

using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste
{
    using static VirtualKeyboard;
    public static class UltraPasteCommon
    {
        public static string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Vegas Application Extensions", "UltraPaste");
        public static UltraPasteSettings Settings = UltraPasteSettings.LoadFromFile();
        public const string VERSION = "v1.01 Beta";
        public static Vegas Vegas { get { return myVegas; } set { myVegas = value; } }
        private static Vegas myVegas;
        public static KeyValuePair<string, byte[]> LastPathAndImageBytes = new KeyValuePair<string, byte[]>();
        public static TextBox InputBoxTextBox { get; set; }
        public static SubtitlesData InputBoxSubtitlesData { get; set; }
        public static string InputBoxString { get; set; }

        public static void DoPaste(bool isSubtitlesInput = false)
        {
            string projectFileToOpen = null, scriptFileToRun = null;
            bool pasteAttributes = false;

            if (Clipboard.GetDataObject() == null)
            {
                return;
            }

            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.UltraPaste))
            {
                Timecode start = myVegas.Transport.SelectionStart, length = myVegas.Transport.SelectionLength;
                if (length.Nanos < 0)
                {
                    start += length;
                    length = new Timecode(0) - length;
                }

                List<TrackEvent> evs = new List<TrackEvent>();

                if ((Settings.SubtitlesImport.InputBoxUseUniversal || isSubtitlesInput) && InputBoxSubtitlesData?.Subtitles.Count > 0)
                {
                    InputBoxSubtitlesData.SplitCharactersAndLines(Settings.SubtitlesImport.InputBoxMaxCharacters, Settings.SubtitlesImport.InputBoxIgnoreWord, Settings.SubtitlesImport.InputBoxMaxLines, Settings.SubtitlesImport.InputBoxMultipleTracks);
                    SubtitlesData data = new SubtitlesData();
                    data.Subtitles.Add(InputBoxSubtitlesData.Subtitles[0]);
                    data.Subtitles[0].Length = TimeSpan.FromMilliseconds(length.ToMilliseconds());
                    evs.AddRange(data.DoPaste_Subtitles(ref start, Settings.SubtitlesImport, true));
                    string str = string.Empty;
                    for (int i = 1; i < InputBoxSubtitlesData.Subtitles.Count; i++)
                    {
                        str += string.Join("\r\n", InputBoxSubtitlesData.Subtitles[i].TextLines) + (i != InputBoxSubtitlesData.Subtitles.Count - 1 ? "\r\n" : string.Empty);
                    }
                    if (InputBoxTextBox != null)
                    {
                        InputBoxTextBox.Text = str;
                    }
                    else
                    {
                        InputBoxString = str;
                    }
                    InputBoxSubtitlesData = SubtitlesData.Parser.ParseFromStrings(InputBoxString, null);
                }

                if (evs.Count == 0)
                {
                    if (isSubtitlesInput)
                    {
                        return;
                    }

                    if (Clipboard.ContainsImage())
                    {
                        DoPaste_ClipboardImage(evs, ref start, length);
                    }
                    else if (Clipboard.ContainsFileDropList())
                    {
                        DoPaste_FileDrop(evs, start, length, out projectFileToOpen, out scriptFileToRun);
                    }
                    else
                    {
                        bool success = false;
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

                                switch (format.ToUpper())
                                {
                                    // "Sony Vegas Meta-Data 5.0" or "Vegas Meta-Data 5.0" for VEGAS Pro
                                    case "SONY VEGAS META-DATA 5.0" when Common.VegasVersion < 14:
                                    case "VEGAS META-DATA 5.0" when Common.VegasVersion > 13:
                                        if (Settings.VegasData.SelectivelyPasteEventAttributes && myVegas.Project.GetSelectedEvents<TrackEvent>().Count > 0)
                                        {
                                            pasteAttributes = true;
                                            success = true;
                                        }
                                        break;

                                    // "REAPERMedia" for Cockos REAPER
                                    case "REAPERMEDIA":
                                        evs.AddRange(DoPaste_Clipboard_ReaperData(bytes, ref start));
                                        success = true;
                                        break;

                                    // "PProAE/Exchange/TrackItem" for Adobe Premiere Pro and Adobe After Effects (not be implemented...)
                                    case "PPROAE/EXCHANGE/TRACKITEM":

                                        break;

                                    default:
                                        break;
                                }
                            }

                            else if (Clipboard.ContainsText())
                            {
                                evs.AddRange(DoPaste_Subtitles_Strings(Clipboard.GetText(), ref start, length));
                                success = true;
                            }

                            if (success)
                            {
                                break;
                            }
                        }
                    }
                }

                if (evs.Count > 0)
                {
                    foreach (TrackEvent ev in myVegas.Project.GetSelectedEvents<TrackEvent>())
                    {
                        ev.Selected = false;
                    }
                    foreach (TrackEvent ev in evs)
                    {
                        ev.Selected = true;
                    }
                    myVegas.UpdateUI();
                }
            }
            if (projectFileToOpen != null)
            {
                myVegas.OpenFile(projectFileToOpen);
            }
            else if (scriptFileToRun != null)
            {
                myVegas.RunScriptFile(scriptFileToRun);
            }
            else if (pasteAttributes)
            {
                SendKeyboardMouse sendKeyMouse = new SendKeyboardMouse();

                sendKeyMouse.SendAllKeysUp();

                // Alt + E ("Edit" Menu)
                sendKeyMouse.SendKeyPress(VKCODE.VK_MENU, VKCODE.VK_E);

                // V (Paste Event Attributes)
                sendKeyMouse.SendKeyPress(VKCODE.VK_V);

                if (Common.VegasVersion > 14)
                {
                    // Down (Selectively Paste Event Attributes)
                    sendKeyMouse.SendKeyPress(VKCODE.VK_DOWN);
                }

                // Enter
                sendKeyMouse.SendKeyPress(VKCODE.VK_RETURN);
            }
        }

        public static void DoPaste_ClipboardImage(List<TrackEvent> evs, ref Timecode start, Timecode length, UltraPasteSettings.ClipboardImageSettings set = null)
        {
            string path = LastPathAndImageBytes.Key;
            if (set == null)
            {
                set = Settings.ClipboardImage;
            }
            set.ChangeImportStart(myVegas, ref start);
            string filePath = set.GetTrueFilePath();

            Image img = null;
            byte[] imgBytes = null;
            if (Clipboard.ContainsData("PNG"))
            {
                using (MemoryStream ms = Clipboard.GetData("PNG") as MemoryStream)
                {
                    imgBytes = ms.ToArray();
                }
            }
            else
            {
                img = Clipboard.GetImage();
                if (img == null && Clipboard.ContainsData(DataFormats.Dib))
                {
                    using (MemoryStream ms = Clipboard.GetData(DataFormats.Dib) as MemoryStream)
                    {
                        img = DibImageData.ConvertToBitmap(ms.ToArray());
                    }
                }
                if (img != null)
                {
                    using (MemoryStream ms = Clipboard.GetData(DataFormats.Dib) as MemoryStream)
                    {
                        img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                        imgBytes = ms.ToArray();
                    }
                }
            }

            if (imgBytes?.Length > 0 && Path.GetDirectoryName(filePath) != Path.GetDirectoryName(path) || LastPathAndImageBytes.Value == null || Convert.ToBase64String(imgBytes) != Convert.ToBase64String(LastPathAndImageBytes.Value))
            {
                path = filePath;
                string ext = Path.GetExtension(path)?.ToLower();
                if (img != null)
                {
                    img.Save(path, ext == ".jpg" || ext == ".jpeg" ? System.Drawing.Imaging.ImageFormat.Jpeg : ext == ".bmp" ? System.Drawing.Imaging.ImageFormat.Bmp : ext == ".gif" ? System.Drawing.Imaging.ImageFormat.Gif : System.Drawing.Imaging.ImageFormat.Png);
                }
                else
                {
                    File.WriteAllBytes(path, imgBytes);
                }
                LastPathAndImageBytes = new KeyValuePair<string, byte[]>(path, imgBytes);
            }

            List<VideoEvent> addedEvents = myVegas.GenerateEvents<VideoEvent>(path, start, length);
            if (set.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(addedEvents.GetEndTimeFromEvents());
            }
            evs.AddRange(addedEvents);
        }

        public static void DoPaste_FileDrop(List<TrackEvent> evs, Timecode start, Timecode length, out string projectFileToOpen, out string scriptFileToRun)
        {
            List<string> filePaths = Common.GetFilePathsFromPathList(Clipboard.GetFileDropList());
            List<string> mediaPaths = new List<string>();
            projectFileToOpen = null;
            scriptFileToRun = null;
            foreach (string path in filePaths)
            {
                if (Common.IsPathMatch(path, Settings.General.ExcludedFiles) || !File.Exists(path))
                {
                    continue;
                }
                List<TrackEvent> addedEvents = null;
                switch (Path.GetExtension(path)?.ToLower())
                {
                    case ".veg":
                    case ".bak" when Path.GetExtension(Path.GetFileNameWithoutExtension(path))?.ToLower() == ".veg":
                        if (Settings.VegasData.VegImportType == 1)
                        {
                            mediaPaths.Add(path);
                        }
                        else if (Settings.VegasData.VegImportType == 2)
                        {
                            myVegas.ImportMediaFromProject(path, false, false);
                        }
                        else
                        {
                            projectFileToOpen = path;
                        }
                        break;

                    case ".rpp":
                        addedEvents = DoPaste_FileDrop_ReaperData(path, ref start);
                        break;

                    case ".srt":
                    case ".lrc":
                    case ".txt":
                        addedEvents = DoPaste_FileDrop_Subtitles(path, ref start, length);
                        break;

                    case ".psd":
                        addedEvents = DoPaste_FileDrop_Psd(path, ref start, length);
                        break;

                    case ".cs":
                    case ".js":
                    case ".vb":
                    case ".dll":
                        if (Settings.VegasData.RunScript)
                        {
                            scriptFileToRun = path;
                        }
                        break;

                    default:
                        mediaPaths.Add(path);
                        break;
                }

                if (projectFileToOpen != null || scriptFileToRun != null)
                {
                    // either opening a project file or running a script in UndoBlock causes an UndoBlock error, so we have to jump out of UndoBlock...
                    break;
                }
                else if (addedEvents == null)
                {
                    continue;
                }
                start = addedEvents.GetEndTimeFromEvents();
                evs.AddRange(addedEvents);
            }

            if (mediaPaths.Count == 0)
            {
                return;
            }

            DoPaste_FileDrop_MediaFiles(ref evs, mediaPaths, ref start, length);
        }

        public static List<TrackEvent> DoPaste_FileDrop_ReaperData(string path, ref Timecode start, UltraPasteSettings.ReaperDataSettings set = null)
        {
            return ReaperData.Parser.Parse(path).DoPaste_ReaperData(ref start, set);
        }

        public static List<TrackEvent> DoPaste_Clipboard_ReaperData(byte[] bytes, ref Timecode start, UltraPasteSettings.ReaperDataSettings set = null)
        {
            return ReaperData.Parser.Parse(bytes).DoPaste_ReaperData(ref start, set);
        }

        public static List<TrackEvent> DoPaste_ReaperData(this ReaperData rd, ref Timecode start, UltraPasteSettings.ReaperDataSettings set = null)
        {
            if (set == null)
            {
                set = Settings.ReaperData;
            }
            set.ChangeImportStart(myVegas, ref start);
            List<TrackEvent> addedEvents = rd.GenerateEventsToVegas(start, set.CloseGap, set.AddVideoStreams);
            if (set.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static List<TrackEvent> DoPaste_FileDrop_Subtitles(string path, ref Timecode start, Timecode length, UltraPasteSettings.SubtitlesImportSettings set = null)
        {
            return SubtitlesData.Parser.ParseFromFile(path, length).DoPaste_Subtitles(ref start, set);
        }

        public static List<TrackEvent> DoPaste_Subtitles_Strings(string str, ref Timecode start, Timecode length, UltraPasteSettings.SubtitlesImportSettings set = null)
        {
            return SubtitlesData.Parser.ParseFromStrings(str, length).DoPaste_Subtitles(ref start, set);
        }

        public static List<TrackEvent> DoPaste_Subtitles(this SubtitlesData subtitles, ref Timecode start, UltraPasteSettings.SubtitlesImportSettings set = null, bool isInputBox = false)
        {
            List<TrackEvent> addedEvents = new List<TrackEvent>();
            List<Region> regions = null;
            if (set == null)
            {
                set = Settings.SubtitlesImport;
            }
            set.ChangeImportStart(myVegas, ref start);
            if (isInputBox)
            {
                subtitles.SplitCharactersAndLines(set.InputBoxMaxCharacters, set.InputBoxIgnoreWord, set.InputBoxMaxLines, set.InputBoxMultipleTracks);
            }
            else
            {
                subtitles.SplitCharactersAndLines(set.MaxCharacters, set.IgnoreWord, set.MaxLines, set.MultipleTracks);
            }
            if (set.AddTextMediaGenerators)
            {
                addedEvents.AddRange(subtitles.GenerateEventsToVegas(start, set.MediaGeneratorType, set.PresetNames[set.MediaGeneratorType], set.CloseGap));
            }
            if (set.AddRegions)
            {
                regions = subtitles.GenerateRegionsToVegas(start);
            }
            if (set.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(!set.AddTextMediaGenerators && set.AddRegions ? regions.GetEndTimeFromMarkers() : addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static List<TrackEvent> DoPaste_FileDrop_Psd(string path, ref Timecode start, Timecode length, UltraPasteSettings.PsdImportSettings set = null)
        {
            List<TrackEvent> addedEvents = myVegas.GenerateEvents<TrackEvent>(path, start, length);
            if (set == null)
            {
                set = Settings.PsdImport;
            }
            set.ChangeImportStart(myVegas, ref start);
            if (set.ExpandAllLayers && addedEvents.Count > 0 && addedEvents[0]?.ActiveTake.Media.StreamCount(MediaType.Video) > 1)
            {
                foreach (TrackEvent ev in addedEvents)
                {
                    ev.Mute = true;
                }
                addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents, MediaType.Unknown, true, -1, false, 1));
            }
            if (set.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static void DoPaste_FileDrop_MediaFiles(ref List<TrackEvent> evs, List<string> paths, ref Timecode start, Timecode length, UltraPasteSettings.MediaImportSettings set = null)
        {
            List<Media> mediaList = new List<Media>();

            bool isImageSequence = false;
            string uniformExtension = paths.GetUniformExtention();

            if (set == null)
            {
                set = Settings.MediaImport;
            }

            foreach (UltraPasteSettings.CustomMediaImportSettings cmis in Settings.Customs)
            {
                bool isMatch = true;
                foreach (string path in paths)
                {
                    if (!Common.IsPathMatch(path, cmis.IncludedFiles))
                    {
                        isMatch = false;
                        break;
                    }
                }
                if (isMatch)
                {
                    set = cmis;
                    break;
                }
            }

            set.ChangeImportStart(myVegas, ref start);
            Timecode startTime = start;


            if (set.ImageSequence && (uniformExtension == ".png" || uniformExtension == ".jpg" || uniformExtension == ".bmp" || uniformExtension == ".gif"))
            {
                string imageSequenceFirstFile = ImageSequenceValidator.GetValidFirstFileName(paths);
                if (imageSequenceFirstFile != null)
                {
                    try
                    {
                        Media m = myVegas.Project.MediaPool.AddImageSequence(imageSequenceFirstFile, paths.Count, length.Nanos > 0 ? (paths.Count * 1000 / length.ToMilliseconds()) : myVegas.Project.Video.FrameRate);
                        mediaList.Add(m);
                        isImageSequence = true;
                    }
                    catch { }
                }
            }

            if (!isImageSequence)
            {
                mediaList = myVegas.GetValidMedia(paths);
            }

            Timecode singleLength = evs.Count > 0 || mediaList.Count == 0 || set.EventLengthType == 0 ? null : set.AddType == 0 && set.EventLengthType == 2 ? Timecode.FromNanos(length.Nanos / mediaList.Count) : length;
            MediaType type = set.StreamType == 1 ? MediaType.Video : set.StreamType == 2 ? MediaType.Audio : MediaType.Unknown;
            if (set.AddType == 2)
            {
                List<TrackEvent> evsAsTake = myVegas.Project.GetSelectedEvents<TrackEvent>();

                Timecode maxLength = new Timecode(0);
                foreach (Media media in mediaList)
                {
                    if (maxLength < media.Length)
                    {
                        maxLength = media.Length;
                    }
                }
                foreach (Media media in mediaList)
                {
                    bool success = false;
                    foreach (TrackEvent ev in evsAsTake)
                    {
                        success = ev.AddTake(media) != null || success;
                    }

                    if (success)
                    {
                        continue;
                    }

                    List<TrackEvent> addedEvents = myVegas.Project.GenerateEvents(media, startTime, singleLength ?? maxLength, type);
                    addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents, type));
                    evsAsTake.AddRange(addedEvents);
                    evs.AddRange(addedEvents);
                }
            }
            else
            {
                foreach (Media media in mediaList)
                {
                    List<TrackEvent> addedEvents = myVegas.Project.GenerateEvents(media, startTime, singleLength, type);
                    addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents, type));
                    if (set.AddType == 1)
                    {
                        foreach (TrackEvent ev in addedEvents)
                        {
                            ev.Track.Selected = false;
                        }
                    }
                    else
                    {
                        startTime = addedEvents.GetEndTimeFromEvents();
                    }

                    evs.AddRange(addedEvents);
                }
                if (set.AddType == 1 && evs.Count > 0)
                {
                    TrackEventGroup g = evs[0].Group;
                    if (g == null)
                    {
                        g = new TrackEventGroup(myVegas.Project);
                        myVegas.Project.Groups.Add(g);
                    }
                    foreach (TrackEvent ev in evs)
                    {
                        if (!g.Contains(ev))
                        {
                            g.Add(ev);
                        }
                    }
                }
            }
            if (set.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(evs.GetEndTimeFromEvents());
            }
        }

        public static void SaveSnapshotToClipboardAndFile(object o, EventArgs e)
        {
            myVegas.SaveSnapshot();
            string path = Settings.ClipboardImage.GetTrueFilePath();
            if (string.IsNullOrEmpty(path))
            {
                return;
            }
            string ext = Path.GetExtension(path)?.ToLower();
            if (myVegas.SaveSnapshot(path, ext == ".jpg" || ext == ".jpeg" ? ImageFileFormat.JPEG : ImageFileFormat.PNG) == RenderStatus.Complete && Clipboard.ContainsImage())
            {
                Image img = Clipboard.GetImage();
                byte[] imgBytes = null;
                using (MemoryStream ms = new MemoryStream())
                {
                    img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                    imgBytes = ms.ToArray();
                }
                if (imgBytes?.Length > 0)
                {
                    LastPathAndImageBytes = new KeyValuePair<string, byte[]>(path, imgBytes);
                }
            }
        }

        public static void ExportSelectedEventsToReaperData(object o, EventArgs e)
        {
            byte[] rpData = ReaperData.Parser.SerializeToBytes(ReaperData.From(myVegas.Project.GetSelectedEvents<TrackEvent>()));
            MemoryStream memoStream = new MemoryStream();
            memoStream.Write(rpData, 0, rpData.Length);
            Clipboard.SetData("REAPERMedia", memoStream);
        }

        public static void ExportSelectedTracksToReaperData(object o, EventArgs e)
        {
            byte[] rpData = ReaperData.Parser.SerializeToBytes(ReaperData.From(myVegas.Project.GetSelectedTracks<Track>()));
            MemoryStream memoStream = new MemoryStream();
            memoStream.Write(rpData, 0, rpData.Length);
            Clipboard.SetData("REAPERMedia", memoStream);
        }

        public static void PsdAddOtherLayers(object o, EventArgs e)
        {
            List<VideoEvent> vEvents = new List<VideoEvent>();
            vEvents.AddRange(myVegas.Project.GetSelectedEvents<VideoEvent>().Where(ev => Path.GetExtension(ev.ActiveTake?.MediaPath)?.ToLower() == ".psd"));
            if (vEvents.Count == 0)
            {
                return;
            }
            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.PsdAddOtherLayers))
            {
                myVegas.Project.AddMissingStreams(vEvents, MediaType.Unknown, true, -1, false, 0);
            }
        }

        public static void SubtitlesApplyToSelectedEvents(object o, EventArgs e)
        {
            PlugInNode plugIn;
            if ((plugIn = TextMediaGenerator.TextPlugIns[Settings.SubtitlesImport.MediaGeneratorType]) == null)
            {
                return;
            }

            List<VideoEvent> vEvents = myVegas.Project.GetSelectedEvents<VideoEvent>();
            if (vEvents.Count == 0)
            {
                return;
            }

            List<Effect> efs = new List<Effect>();

            foreach (VideoEvent vEvent in vEvents)
            {
                if (vEvent.ActiveTake?.Media.Generator?.PlugIn.IsOFX == true && vEvent.ActiveTake?.Media.Generator?.PlugIn?.UniqueID == plugIn.UniqueID)
                {
                    efs.Add(vEvent.ActiveTake?.Media.Generator);
                }

                foreach (Effect ef in vEvent.Effects)
                {
                    if (ef?.PlugIn.IsOFX == true && ef.PlugIn.UniqueID == plugIn.UniqueID)
                    {
                        efs.Add(ef);
                    }
                }
            }

            if (efs.Count == 0)
            {
                return;
            }

            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.SubtitlesApplyToSelectedEvents))
            {
                foreach (Effect ef in efs)
                {
                    ef.SetTextPreset(Settings.SubtitlesImport.PresetNames[Settings.SubtitlesImport.MediaGeneratorType]);
                }
            }
        }

        public static void SubtitlesTitlesAndTextToProTypeTitler(object o, EventArgs e)
        {
            List<VideoEvent> vEvents = new List<VideoEvent>();
            vEvents.AddRange(myVegas.Project.GetSelectedEvents<VideoEvent>().Where(ev => ev.ActiveTake?.Media.Generator?.PlugIn.UniqueID == TextMediaGenerator.PlugInTitlesAndText.UniqueID));
            if (vEvents.Count == 0)
            {
                return;
            }

            Dictionary<Media, Media> mediaPairs = new Dictionary<Media, Media>();

            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.SubtitlesTitlesAndTextToProTypeTitler))
            {
                foreach (VideoEvent vEvent in vEvents)
                {
                    Media newMedia;
                    Timecode offset = vEvent.ActiveTake.Offset;
                    if ((newMedia = mediaPairs.ContainsKey(vEvent.ActiveTake.Media) ? mediaPairs[vEvent.ActiveTake.Media] : null) == null)
                    {
                        TextMediaGenerator.TextMediaProperties properties = TextMediaGenerator.TextMediaProperties.GetFromTitlesAndText(vEvent.ActiveTake.Media.Generator.OFXEffect);
                        properties.MediaSize = (vEvent.ActiveTake.MediaStream as VideoStream)?.Size ?? properties.MediaSize;
                        properties.MediaSeconds = vEvent.ActiveTake.MediaStream.Length.ToMilliseconds() / 1000;
                        newMedia = properties.GenerateProTypeTitlerMedia();
                        mediaPairs.Add(vEvent.ActiveTake.Media, newMedia);
                    }
                    if (newMedia == null)
                    {
                        continue;
                    }
                    vEvent.AddTake(newMedia.GetVideoStreamByIndex(0), true).Offset = offset;
                }
            }
        }

        public static void MediaAddMissingStreams(object o, EventArgs e)
        {
            List<TrackEvent> vEvents = myVegas.Project.GetSelectedEvents<TrackEvent>();
            if (vEvents.Count == 0)
            {
                return;
            }
            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.AddMissingStreams))
            {
                myVegas.Project.AddMissingStreams(vEvents);
            }
        }

        public static void GenerateMixedVegasClipboardData(object o, EventArgs e)
        {
            Common.GenerateMixedVegasClipboardData();
        }
    }
}