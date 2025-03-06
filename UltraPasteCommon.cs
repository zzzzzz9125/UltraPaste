#if !Sony
using ScriptPortal.Vegas;
using Region = ScriptPortal.Vegas.Region;
#else
using Sony.Vegas;
using Region = Sony.Vegas.Region;
#endif

using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;

namespace UltraPaste
{
    public static class UltraPasteCommon
    {
        public static string AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static string SettingsFolder = Path.Combine(Common.VegasVersion < 14 ? Path.Combine(AppFolder, "Sony") : AppFolder, "VEGAS Pro", Common.VegasVersion + ".0");
        public static UltraPasteSettings Settings = UltraPasteSettings.LoadFromFile();
        public const string VERSION = "v1.00";

        public static Vegas myVegas;
        public static KeyValuePair<Image, string> LastImageAndPath = new KeyValuePair<Image, string>();
        public static KeyValuePair<Stream, string> LastAudioStreamAndPath = new KeyValuePair<Stream, string>();

        public static void DoPaste()
        {
            string projectFileToOpen = null, scriptFileToRun = null;
            using (UndoBlock undo = new UndoBlock(myVegas.Project, L.UltraPaste))
            {

                Timecode start = myVegas.Transport.SelectionStart, length = myVegas.Transport.SelectionLength;
                if (length.Nanos < 0)
                {
                    start += length;
                    length = new Timecode(0) - length;
                }

                string baseFolder = !myVegas.Project.IsUntitled ? Path.GetDirectoryName(myVegas.Project.FilePath) : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

                List<TrackEvent> evs = new List<TrackEvent>();

                if (Clipboard.ContainsImage())
                {
                    string path = LastImageAndPath.Value;
                    string fileName = Path.Combine(baseFolder, string.Format("Clipboard_{0}.png", DateTime.Now.ToString("yyyyMMdd-HHmmss"))); ;
                    Image img = Clipboard.GetImage();
                    if (img == null && Clipboard.ContainsData(DataFormats.Dib))
                    {
                        using (MemoryStream ms = Clipboard.GetData(DataFormats.Dib) as MemoryStream)
                        {
                            img = DibImageData.ConvertToBitmap(ms.ToArray());
                        }
                    }
                    if (baseFolder != Path.GetDirectoryName(path) || !img.IsSameTo(LastImageAndPath.Key))
                    {
                        path = fileName;
                        img.Save(path, System.Drawing.Imaging.ImageFormat.Png);
                    }
                    LastImageAndPath.Key?.Dispose();
                    LastImageAndPath = new KeyValuePair<Image, string>(img, path);
                    evs.AddRange(myVegas.GenerateEvents<VideoEvent>(path, start, length, true));
                }
                else if (Clipboard.ContainsAudio())
                {
                    Stream audio = Clipboard.GetAudioStream();
                    string path = LastAudioStreamAndPath.Value;
                    if (baseFolder != Path.GetDirectoryName(path) || !audio.IsSameTo(LastAudioStreamAndPath.Key))
                    {
                        path = Path.Combine(baseFolder, string.Format("Clipboard_{0}.wav", DateTime.Now.ToString("yyyyMMdd-HHmmss")));
                        using (FileStream fs = File.Create(path))
                        {
                            audio.Seek(0, SeekOrigin.Begin);
                            audio.CopyTo(fs);
                        }
                    }
                    LastAudioStreamAndPath = new KeyValuePair<Stream, string>(audio, path);
                    evs.AddRange(myVegas.GenerateEvents<AudioEvent>(path, start, length, true));
                }
                else if (Clipboard.ContainsFileDropList())
                {
                    List<string> filePaths = Clipboard.GetFileDropList().GetFilePathsFromPathList(out string uniformExtension);
                    List<string> paths = new List<string>();
                    Timecode startTime = start;

                    if (uniformExtension == ".txt")
                    {
                        List<string> strs = new List<string>();
                        foreach (string path in filePaths)
                        {
                            string str = Encoding.UTF8.GetString(File.ReadAllBytes(path));
                            if (string.IsNullOrEmpty(str))
                            {
                                continue;
                            }
                            strs.Add(str);
                        }
                        foreach (string str in strs)
                        {
                            List<VideoEvent> addedEvents = TextMediaGenerator.GenerateTitlesAndTextEvents(startTime, length, str, null, true);
                            startTime = addedEvents.GetEndTimeFromEvents();
                            evs.AddRange(addedEvents);
                        }
                    }
                    foreach (string path in filePaths)
                    {
                        if (Common.IsPathMatch(path, "*.veg.bak;*.sfvp0;*.sfap0;*.sfk;*.sfl;*.rpp-bak;*.reapeaks") || !File.Exists(path))
                        {
                            continue;
                        }
                        List<TrackEvent> addedEvents = null;
                        string ext = Path.GetExtension(path).ToLower();
                        if (ext == ".veg")
                        {
                            // Opening the project file in UndoBlock causes an UndoBlock error, so we have to jump out of UndoBlock...
                            projectFileToOpen = path;
                            break;
                        }
                        else if (ext == ".rpp")
                        {
                            ReaperData rd = ReaperData.Parser.Parse(path);
                            addedEvents = rd.GenerateEventsToVegas(startTime, true);
                        }
                        else if (ext == ".srt" || ext == ".lrc")
                        {
                            SubtitlesData subtitles = SubtitlesData.Parser.Parse(path);
                            addedEvents = subtitles.GenerateEventsToVegas(startTime, 1);
                            List<Region> regions = subtitles.GenerateRegionsToVegas(startTime);
                            if (regions.Count > 0)
                            {
                                myVegas.Transport.CursorPosition = regions.GetEndTimeFromMarkers();
                            }
                        }
                        else if (ext == ".cs" || ext == ".js" || ext == ".vb" || ext == ".dll")
                        {
                            // Running the script file in UndoBlock causes an UndoBlock error, so we have to jump out of UndoBlock...
                            scriptFileToRun = path;
                            break;
                        }
                        else
                        {
                            paths.Add(path);
                        }
                        if (addedEvents == null)
                        {
                            continue;
                        }
                        startTime = addedEvents.GetEndTimeFromEvents();
                        evs.AddRange(addedEvents);
                    }

                    List<Media> mediaList = new List<Media>();
                    bool isImageSequence = false;
                    if (uniformExtension == ".png" || uniformExtension == ".jpg" || uniformExtension == ".bmp" || uniformExtension == ".gif")
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

                    Timecode singleLength = evs.Count > 0 || mediaList.Count == 0 ? null : Timecode.FromNanos(length.Nanos / mediaList.Count);
                    foreach (Media media in mediaList)
                    {
                        List<TrackEvent> addedEvents = myVegas.Project.GenerateEvents<TrackEvent>(media, startTime, singleLength, true);
                        startTime = addedEvents.GetEndTimeFromEvents();
                        evs.AddRange(addedEvents);
                    }
                }
                else
                {
                    bool success = false;
                    foreach (string format in Clipboard.GetDataObject().GetFormats())
                    {
                        object obj = Clipboard.GetData(format);
                        if (obj is MemoryStream)
                        {
                            byte[] bytes = null;
                            using (MemoryStream ms = obj as MemoryStream)
                            {
                                bytes = new byte[ms.Capacity];
                                if (ms.CanRead)
                                {
                                    ms.Read(bytes, 0, ms.Capacity);
                                }
                            }

                            if (bytes == null)
                            {
                                continue;
                            }

                            File.WriteAllBytes(Path.Combine(baseFolder, string.Format("{0}.txt", format)), bytes);

                            switch (format.ToUpper())
                            {
                                // "REAPERMedia" for Cockos REAPER
                                case "REAPERMEDIA":
                                    ReaperData rd = ReaperData.Parser.Parse(bytes);
                                    evs.AddRange(rd.GenerateEventsToVegas(start, true));
                                    success = true;
                                    break;

                                // "PProAE/Exchange/TrackItem" for Adobe After Effects (not be implemented...)
                                case "PPROAE/EXCHANGE/TRACKITEM":

                                    break;

                                default:
                                    break;
                            }
                        }

                        else if (Clipboard.ContainsText())
                        {
                            string str = Clipboard.GetText();
                            evs.AddRange(TextMediaGenerator.GenerateTitlesAndTextEvents(start, length, str, null, true));
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
                    myVegas.UpdateUI();
                    myVegas.Transport.CursorPosition = evs.GetEndTimeFromEvents();
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
        }
    }
}