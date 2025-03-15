#if !Sony
using ScriptPortal.Vegas;
using Region = ScriptPortal.Vegas.Region;
#else
using Sony.Vegas;
using Region = Sony.Vegas.Region;
#endif

using System;
using System.IO;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace UltraPaste
{
    using static VirtualKeyboard;
    public static class UltraPasteCommon
    {
        public static string SettingsFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents", "Vegas Application Extensions", "UltraPaste");
        public static UltraPasteSettings Settings = UltraPasteSettings.LoadFromFile();
        public const string VERSION = "v1.00";
        public static Vegas Vegas { get { return myVegas; } set { myVegas = value; } }
        private static Vegas myVegas;
        public static KeyValuePair<Image, string> LastImageAndPath = new KeyValuePair<Image, string>();

        public static void DoPaste()
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

                if (Clipboard.ContainsImage())
                {
                    Settings.ClipboardImage.ChangeImportStart(myVegas, ref start);
                    DoPaste_ClipboardImage(ref evs, start, length);
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    DoPaste_FileDrop(ref evs, start, length, out projectFileToOpen, out scriptFileToRun);
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

                            if (bytes == null)
                            {
                                continue;
                            }

                            switch (format.ToUpper())
                            {
                                // "Sony Vegas Meta-Data 5.0" or "Vegas Meta-Data 5.0" for VEGAS Pro
                                case "SONY VEGAS META-DATA 5.0" when Common.VegasVersion < 14:
                                case "VEGAS META-DATA 5.0" when Common.VegasVersion > 13:
                                    if (myVegas.Project.GetSelectedEvents<TrackEvent>().Count > 0)
                                    {
                                        pasteAttributes = true;
                                        success = true;
                                    }
                                    break;

                                // "REAPERMedia" for Cockos REAPER
                                case "REAPERMEDIA":
                                    Settings.ReaperData.ChangeImportStart(myVegas, ref start);
                                    evs.AddRange(DoPaste_Clipboard_ReaperData(bytes, start));
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
                            string str = Clipboard.GetText();
                            evs.AddRange(TextMediaGenerator.GenerateTextEvents(start, length, str, 0, null));
                            success = true;
                        }

                        if (success)
                        {
                            break;
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

        public static void DoPaste_ClipboardImage(ref List<TrackEvent> evs, Timecode start, Timecode length)
        {
            string path = LastImageAndPath.Value;
            string filePath = Environment.ExpandEnvironmentVariables(Regex.Replace(Settings.ClipboardImage.FilePath, @"%PROJECTFOLDER%", Path.GetDirectoryName(myVegas.Project.FilePath)?.Trim('\\', '/') ?? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), RegexOptions.IgnoreCase));
            foreach (Match m in Regex.Matches(filePath, @"<.*?>"))
            {
                filePath = filePath?.Replace(m.Value, DateTime.Now.ToString(m.Value)?.Trim('<', '>'));
            }
            filePath = Path.GetFullPath(filePath);
            string dir = Path.GetDirectoryName(filePath);
            try
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
            }
            catch
            {
                filePath = Path.Combine(dir, "Clipboard", string.Format("{0}.png", DateTime.Now.ToString("yyyyMMdd_HHmmss")));
                dir = Path.GetDirectoryName(filePath);
            }

            Image img = Clipboard.GetImage();
            if (img == null && Clipboard.ContainsData(DataFormats.Dib))
            {
                using (MemoryStream ms = Clipboard.GetData(DataFormats.Dib) as MemoryStream)
                {
                    img = DibImageData.ConvertToBitmap(ms.ToArray());
                }
            }
            if (dir != Path.GetDirectoryName(path) || !img.IsSameTo(LastImageAndPath.Key))
            {
                path = filePath;
                string ext = Path.GetExtension(path)?.ToLower();
                img.Save(path, ext == ".jpg" || ext == ".jpeg" ? System.Drawing.Imaging.ImageFormat.Jpeg : ext == ".bmp" ? System.Drawing.Imaging.ImageFormat.Bmp : ext == ".gif" ? System.Drawing.Imaging.ImageFormat.Gif : System.Drawing.Imaging.ImageFormat.Png);
            }
            LastImageAndPath.Key?.Dispose();
            LastImageAndPath = new KeyValuePair<Image, string>(img, path);
            evs.AddRange(myVegas.GenerateEvents<VideoEvent>(path, start, length));
        }

        public static void DoPaste_FileDrop(ref List<TrackEvent> evs, Timecode start, Timecode length, out string projectFileToOpen, out string scriptFileToRun)
        {
            List<string> filePaths = Clipboard.GetFileDropList().GetFilePathsFromPathList();
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
                        if (Settings.VegImport.Type == 0)
                        {
                            projectFileToOpen = path;
                        }
                        else if (Settings.VegImport.Type == 2)
                        {
                            myVegas.ImportMediaFromProject(path, false, false);
                        }
                        break;

                    case ".rpp":
                        Settings.ReaperData.ChangeImportStart(myVegas, ref start);
                        addedEvents = DoPaste_FileDrop_ReaperData(path, start);
                        break;

                    case ".srt":
                    case ".lrc":
                    case ".txt":
                        Settings.SubtitlesImport.ChangeImportStart(myVegas, ref start);
                        addedEvents = DoPaste_FileDrop_Subtitles(path, start);
                        break;

                    case ".psd":
                        Settings.PsdImport.ChangeImportStart(myVegas, ref start);
                        addedEvents = DoPaste_FileDrop_Psd(path, start, length);
                        break;

                    case ".cs":
                    case ".js":
                    case ".vb":
                    case ".dll":
                        if (Settings.ScriptRun.Enabled)
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

            Settings.MediaImport.ChangeImportStart(myVegas, ref start);
            DoPaste_FileDrop_MediaFiles(ref evs, mediaPaths, start, length);
        }

        public static List<TrackEvent> DoPaste_FileDrop_ReaperData(string path, Timecode start)
        {
            return ReaperData.Parser.Parse(path).DoPaste_ReaperData(start);
        }

        public static List<TrackEvent> DoPaste_Clipboard_ReaperData(byte[] bytes, Timecode start)
        {
            return ReaperData.Parser.Parse(bytes).DoPaste_ReaperData(start);
        }

        public static List<TrackEvent> DoPaste_ReaperData(this ReaperData rd, Timecode start)
        {
            List<TrackEvent> addedEvents = rd.GenerateEventsToVegas(start, Settings.ReaperData.CloseGap, Settings.ReaperData.AddVideoStreams);
            if (Settings.ReaperData.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static List<TrackEvent> DoPaste_FileDrop_Subtitles(string path, Timecode start)
        {
            List<TrackEvent> addedEvents = new List<TrackEvent>();
            List<Region> regions = null;
            SubtitlesData subtitles = SubtitlesData.Parser.ParseFromFile(path);
            subtitles.SplitCharactersAndLines(Settings.SubtitlesImport.MaxCharacters, Settings.SubtitlesImport.IgnoreWord, Settings.SubtitlesImport.MaxLines);
            if (Settings.SubtitlesImport.ImportType == 0 || Settings.SubtitlesImport.ImportType > 1)
            {
                addedEvents.AddRange(subtitles.GenerateEventsToVegas(start, Settings.SubtitlesImport.MediaGeneratorType, Settings.SubtitlesImport.PresetNames[Settings.SubtitlesImport.MediaGeneratorType], Settings.SubtitlesImport.CloseGap));
            }
            if (Settings.SubtitlesImport.ImportType > 1)
            {
                regions = subtitles.GenerateRegionsToVegas(start);
            }
            if (Settings.SubtitlesImport.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(Settings.SubtitlesImport.ImportType == 1 ? regions.GetEndTimeFromMarkers() : addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static List<TrackEvent> DoPaste_FileDrop_Psd(string path, Timecode start, Timecode length)
        {
            List<TrackEvent> addedEvents = myVegas.GenerateEvents<TrackEvent>(path, start, length);
            if (Settings.PsdImport.ExpandAllLayers)
            {
                foreach (TrackEvent ev in addedEvents)
                {
                    ev.Mute = true;
                }
                addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents, MediaType.Unknown, true));
            }
            if (Settings.PsdImport.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(addedEvents.GetEndTimeFromEvents());
            }
            return addedEvents;
        }

        public static void DoPaste_FileDrop_MediaFiles(ref List<TrackEvent> evs, List<string> paths, Timecode start, Timecode length)
        {
            List<Media> mediaList = new List<Media>();

            bool isImageSequence = false;
            string uniformExtension = paths.GetUniformExtention();

            if (Settings.MediaImport.ImageSequence && (uniformExtension == ".png" || uniformExtension == ".jpg" || uniformExtension == ".bmp" || uniformExtension == ".gif"))
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

            Timecode singleLength = evs.Count > 0 || mediaList.Count == 0 ? null : Settings.MediaImport.AddType == 0 ? Timecode.FromNanos(length.Nanos / mediaList.Count) : length;
            MediaType type = Settings.MediaImport.StreamType == 1 ? MediaType.Video : Settings.MediaImport.StreamType == 2 ? MediaType.Audio : MediaType.Unknown;
            if (Settings.MediaImport.AddType == 2)
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

                    List<TrackEvent> addedEvents = myVegas.Project.GenerateEvents(media, start, singleLength ?? maxLength, type);
                    addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents));
                    evsAsTake.AddRange(addedEvents);
                    evs.AddRange(addedEvents);
                }
            }
            else
            {
                foreach (Media media in mediaList)
                {
                    List<TrackEvent> addedEvents = myVegas.Project.GenerateEvents(media, start, singleLength, type);
                    addedEvents.AddRange(myVegas.Project.AddMissingStreams(addedEvents));
                    if (Settings.MediaImport.AddType == 1)
                    {
                        foreach (TrackEvent ev in addedEvents)
                        {
                            ev.Track.Selected = false;
                        }
                    }
                    else
                    {
                        start = addedEvents.GetEndTimeFromEvents();
                    }

                    evs.AddRange(addedEvents);
                }
            }
            if (Settings.MediaImport.CursorToEnd)
            {
                myVegas.RefreshCursorPosition(evs.GetEndTimeFromEvents());
            }
        }
    }
}