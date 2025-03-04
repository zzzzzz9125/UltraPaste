#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;

public static class UltraPasteCommon
{
    //public static UltraPasteSettings Settings = UltraPasteSettings.LoadFromFile();
    public const string VERSION = "v1.00";

    public static Vegas myVegas;
    public const string UID_TITLES_AND_TEXT = "{Svfx:com.vegascreativesoftware:titlesandtext}";
    public const string UID_TITLES_AND_TEXT_SONY = "{Svfx:com.sonycreativesoftware:titlesandtext}";
    public static PlugInNode plugInTitlesAndText = null;

    public static void DoUltraPaste()
    {
        plugInTitlesAndText = myVegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT)
                            ?? myVegas.Generators.FindChildByUniqueID(UID_TITLES_AND_TEXT_SONY);

        Timecode start = myVegas.Transport.SelectionStart, length = myVegas.Transport.SelectionLength;
        if (length.Nanos < 0)
        {
            start += length;
            length = new Timecode(0) - length;
        }

        string baseFolder = !myVegas.Project.IsUntitled ? Path.GetDirectoryName(myVegas.Project.FilePath) : Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        //baseFolder = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        string timeString = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        List<TrackEvent> evs = new List<TrackEvent>();

        if (Clipboard.ContainsImage())
        {
            Image img = Clipboard.GetImage();
            string path = Path.Combine(baseFolder, string.Format("Clipboard_{0}.png", timeString));
            img.Save(path);
            evs.AddRange(GenerateEvents<VideoEvent>(path, start, length, true));
        }
        else if (Clipboard.ContainsAudio())
        {
            Stream audio = Clipboard.GetAudioStream();
            string path = Path.Combine(baseFolder, string.Format("Clipboard_{0}.wav", timeString));
            using (FileStream fs = File.Create(path))
            {
                audio.Seek(0, SeekOrigin.Begin);
                audio.CopyTo(fs);
            }
            evs.AddRange(GenerateEvents<AudioEvent>(path, start, length, true));
        }
        else if (Clipboard.ContainsFileDropList())
        {
            List<string> filePaths = GetFilePathsFromPathList(Clipboard.GetFileDropList(), out string uniformExtension);
            List<string> paths = new List<string>();
            Timecode startTime = start;

            if (uniformExtension == ".txt" && plugInTitlesAndText != null)
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
                    List<VideoEvent> addedEvents = GenerateTitlesAndTextEvents(startTime, length, str, null, true);
                    startTime = GetEndTimeFromEvents(addedEvents);
                    evs.AddRange(addedEvents);
                }
            }
            foreach (string path in filePaths)
            {
                if (IsPathMatch(path, "*.veg.bak;*.sfvp0;*.sfap0;*.sfk;*.sfl;*.rpp-bak;*.reapeaks") || !File.Exists(path))
                {
                    continue;
                }
                List<TrackEvent> addedEvents = null;
                if (Path.GetExtension(path).ToLower() == ".rpp")
                {
                    ReaperData rd = ReaperData.Parser.Parse(path);
                    addedEvents = rd.GenerateEventsToVegas(myVegas, startTime, true);
                }
                else
                {
                    paths.Add(path);
                }
                if (addedEvents == null)
                {
                    continue;
                }
                startTime = GetEndTimeFromEvents(addedEvents);
                evs.AddRange(addedEvents);
            }

            List<Media> mediaList = new List<Media>();
            bool isImageSequence = false;
            if (uniformExtension == ".png" || uniformExtension == ".jpg" || uniformExtension == ".bmp" || uniformExtension == ".jpg")
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
                mediaList = GetValidMedia(paths);
            }

            Timecode singleLength = evs.Count > 0 || mediaList.Count == 0 ? null : Timecode.FromNanos(length.Nanos / mediaList.Count);
            foreach (Media media in mediaList)
            {
                List<TrackEvent> addedEvents = GenerateEvents<TrackEvent>(media, startTime, singleLength, true);
                startTime = GetEndTimeFromEvents(addedEvents);
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
                            evs.AddRange(rd.GenerateEventsToVegas(myVegas, start, true));
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
                    if (plugInTitlesAndText != null)
                    {
                        evs.AddRange(GenerateTitlesAndTextEvents(start, length, str, null, true));
                        success = true;
                    }
                }

                if (success)
                {
                    break;
                }
            }
        }

        if (evs.Count > 0)
        {
            foreach (TrackEvent ev in GetSelectedEvents<TrackEvent>())
            {
                ev.Selected = false;
            }

            foreach (TrackEvent ev in evs)
            {
                ev.Selected = true;
            }
            myVegas.UpdateUI();
            myVegas.Transport.CursorPosition = GetEndTimeFromEvents(evs);
        }
    }

    public static bool IsPathMatch(string path, string dosExpression)
    {
        if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(dosExpression))
        {
            return false;
        }

        string fileName = Path.GetFileName(path);
        if (string.IsNullOrEmpty(fileName))
        {
            return false;
        }

        List<string> validPatterns = new List<string>();
        foreach (string rawPattern in dosExpression.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
        {
            string trimmedPattern = rawPattern.Trim();
            if (!string.IsNullOrEmpty(trimmedPattern))
            {
                validPatterns.Add(trimmedPattern);
            }
        }

        foreach (string pattern in validPatterns)
        {
            string regexPattern = string.Format("^{0}$", Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", "."));
            if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    public static string GetShortCutTarget(string lnk)
    {
        if (lnk != null && File.Exists(lnk))
        {
            dynamic objWshShell = null, objShortcut = null;
            try
            {
                objWshShell = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
                objShortcut = objWshShell.CreateShortcut(lnk);
                Console.WriteLine(objShortcut.TargetPath);
                return objShortcut.TargetPath;
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.ReleaseComObject(objShortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(objWshShell);
            }
        }
        return string.Empty;
    }

    // get all valid paths
    public static List<string> GetFilePathsFromPathList(System.Collections.Specialized.StringCollection pathList)
    {
        return GetFilePathsFromPathList(pathList, out _);
    }

    // get all valid paths, and output the extension when all files have a uniform extension
    public static List<string> GetFilePathsFromPathList(System.Collections.Specialized.StringCollection pathList, out string uniformExtension)
    {
        List<string> filePaths = new List<string>();
        foreach (string path in pathList)
        {
            string filePath = path;
            if (Path.GetExtension(filePath).ToLower() == ".lnk")
            {
                filePath = GetShortCutTarget(filePath);
            } 
            if (File.Exists(filePath))
            {
                filePaths.Add(filePath);
            }
            else if (Directory.Exists(filePath))
            {
                foreach (string child in Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories))
                {
                    filePaths.Add(child);
                }
            }
        }
        uniformExtension = null;
        if (filePaths.Count > 0)
        {
            uniformExtension = Path.GetExtension(filePaths[0]).ToLower();
            foreach (string path in filePaths)
            {
                if (Path.GetExtension(path).ToLower() != uniformExtension)
                {
                    uniformExtension = null;
                    break;
                }
            }
        }
        return filePaths;
    }

    public static List<string> GetFilePathsFromPathList<T>(T pathList) where T : IEnumerable<string>
    {
        List<string> filePaths = new List<string>();
        foreach (string path in pathList)
        {
            if (File.Exists(path))
            {
                filePaths.Add(path);
            }
            else if (Directory.Exists(path))
            {
                foreach (string child in Directory.GetFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    filePaths.Add(child);
                }
            }
        }
        return filePaths;
    }

    public static List<VideoEvent> GenerateTitlesAndTextEvents(Timecode start, Timecode length = null, string text = null, string presetName = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
    {
        Media media = Media.CreateInstance(myVegas.Project, plugInTitlesAndText, presetName);
        if (length.Nanos > 0)
        {
            media.Length = length;
        }

        if (!string.IsNullOrEmpty(text))
        {
            OFXStringParameter textPara = (OFXStringParameter)media.Generator.OFXEffect["Text"];
            RichTextBox rtb = new RichTextBox() { Rtf = textPara.Value };
            rtb.Text = text;
            textPara.Value = rtb.Rtf;
        }

        return GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
    }

    public static Timecode GetEndTimeFromEvents<T>(IEnumerable<T> evs) where T : TrackEvent
    {
        Timecode end = new Timecode(0);
        foreach (T ev in evs)
        {
            if (end < ev.End)
            {
                end = ev.End;
            }
        }
        return end;
    }

    public static List<Media> GetValidMedia(IEnumerable<string> paths)
    {
        List<Media> mediaList = new List<Media>();
        foreach (string path in paths)
        {
            Media media;
            if ((media = GetValidMedia(path)) != null)
            {
                mediaList.Add(media);
            }
        }
        return mediaList;
    }

    public static Media GetValidMedia(string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            System.Reflection.MethodInfo method;
            if ((method = typeof(Vegas).GetMethod("MediaInfo", new Type[] { typeof(string) })) != null)
            {
                // VEGAS Pro 18+
                method.Invoke(myVegas, new object[] { path });
            }
            else if ((method = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool), typeof(bool) })) != null)
            {
                // VEGAS Pro 22 Build 122+ (not recommended, for compatibility only)
                method.Invoke(myVegas, new object[] { path, true, false });
            }
            else if ((method = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool) })) != null)
            {
                // VEGAS Pro 22 Build 93-
                method.Invoke(myVegas, new object[] { path, true });
            }
            Media media = Media.CreateInstance(myVegas.Project, path);
            return media;
        }
        catch
        {
            return null;
        }
    }

    // a complex implementation to import File as Events to Timeline
    // when media path is invalid, it won't generate any event
    public static List<T> GenerateEvents<T>(string path, Timecode start, Timecode length = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1) where T : TrackEvent
    {
        Media media = GetValidMedia(path);
        if (media == null)
        {
            return new List<T>();
        }
        return GenerateEvents<T>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
    }

    // a complex implementation to import Media as Events to Timeline
    // when media is null, it will generate blank events
    public static List<T> GenerateEvents<T>(Media media, Timecode start, Timecode length = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1) where T : TrackEvent
    {
        List<T> l = new List<T>();

        if (length == null || length.Nanos <= 0)
        {
            length = media != null ? media.Length : Timecode.FromSeconds(5);
        }

        bool isVideoOnly = typeof(T) == typeof(VideoEvent) || (media != null && !media.HasAudio() && media.HasVideo());
        bool isAudioOnly = typeof(T) == typeof(AudioEvent) || (media != null && !media.HasVideo() && media.HasAudio());

        List<Track> selectedTracks = new List<Track>();

        int trackCount = useMultipleSelectedTracks ? 0 : 1;
        if (isVideoOnly)
        {
            selectedTracks.AddRange(GetSelectedTracks<VideoTrack>(trackCount));
        }
        else if (isAudioOnly)
        {
            selectedTracks.AddRange(GetSelectedTracks<AudioTrack>(trackCount));
        }
        else
        {
            selectedTracks.AddRange(GetSelectedTracks<Track>(trackCount));
        }

        if (selectedTracks.Count == 0)
        {
            foreach (Track trk in myVegas.Project.Tracks)
            {
                if (trk.Index >= newTrackIndex && (isVideoOnly && trk.IsVideo() || (isAudioOnly && trk.IsAudio())))
                {
                    trk.Selected = true;
                    selectedTracks.Add(trk);
                    break;
                }
            }
            if (selectedTracks.Count == 0)
            {
                Track trk = typeof(T) == typeof(AudioEvent) || (media != null && !media.HasVideo()) ? (Track)new AudioTrack(myVegas.Project, newTrackIndex, null) : new VideoTrack(myVegas.Project, newTrackIndex, null);
                myVegas.Project.Tracks.Add(trk);
                trk.Selected = true;
                selectedTracks.Add(trk);
            }
        }

        Dictionary<AudioTrack, VideoTrack> audioVideoPair = new Dictionary<AudioTrack, VideoTrack>();

        foreach (Track myTrack in selectedTracks)
        {
            if ((typeof(T) == typeof(VideoEvent) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioEvent) && !myTrack.IsAudio()))
            {
                continue;
            }

            T ev = null;
            MediaStream ms = null;

            if (myTrack.IsVideo())
            {
                ev = (T)(TrackEvent)new VideoEvent(myVegas.Project, start, length, null);
                if (media != null && media.HasVideo())
                {
                    ms = media.GetVideoStreamByIndex(0);
                }
            }
            else if (myTrack.IsAudio())
            {
                if (audioVideoPair.ContainsKey((AudioTrack)myTrack))
                {
                    continue;
                }
                ev = (T)(TrackEvent)new AudioEvent(myVegas.Project, start, length, null);
                if (media != null && media.HasAudio())
                {
                    ms = media.GetAudioStreamByIndex(0);
                }
            }

            if (ev == null)
            {
                continue;
            }
            myTrack.Events.Add(ev);
            if (ms != null)
            {
                ev.AddTake(ms);
            }
            l.Add(ev);

            if (myTrack.IsVideo() && media != null && media.HasVideo() && media.HasAudio())
            {
                TrackEventGroup group = ev.Group;
                if (group == null)
                {
                    group = new TrackEventGroup(myVegas.Project);
                    myVegas.Project.TrackEventGroups.Add(group);
                    group.Add(ev);
                }
                for (int i = 0; i < media.StreamCount(MediaType.Audio); i++)
                {
                    AudioStream streamAudio = media.GetAudioStreamByIndex(i);
                    AudioEvent eventAudio = new AudioEvent(myVegas.Project, start, length, null);
                    Track trackBelow = myTrack.Index + i < myVegas.Project.Tracks.Count - 1 ? myVegas.Project.Tracks[myTrack.Index + i + 1] : null;
                    if (trackBelow == null || !trackBelow.IsAudio())
                    {
                        trackBelow = new AudioTrack(myVegas.Project, myTrack.Index + i + 1, null);
                        myVegas.Project.Tracks.Add(trackBelow);
                    }
                    trackBelow.Events.Add(eventAudio);
                    eventAudio.AddTake(streamAudio);
                    group.Add(eventAudio);
                    l.Add((T)(TrackEvent)eventAudio);
                    audioVideoPair.Add((AudioTrack)trackBelow, (VideoTrack)myTrack);
                }
            }
        }
        return l;
    }

    public static List<T> GetSelectedTracks<T>(int maxCount = 0) where T : Track
    {
        List<T> l = new List<T>();
        foreach (Track myTrack in myVegas.Project.Tracks)
        {
            if (myTrack.Selected)
            {
                if ((typeof(T) == typeof(VideoTrack) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioTrack) && !myTrack.IsAudio()))
                {
                    continue;
                }
                l.Add((T)myTrack);
            }
        }
        if (maxCount > 0 && maxCount < l.Count)
        {
            l = l.GetRange(0, maxCount);
        }
        return l;
    }

    public static List<T> GetSelectedEvents<T>(int maxCount = 0) where T : TrackEvent
    {
        List<T> l = new List<T>();
        foreach (Track myTrack in myVegas.Project.Tracks)
        {
            if ((typeof(T) == typeof(VideoEvent) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioEvent) && !myTrack.IsAudio()))
            {
                continue;
            }
            foreach (TrackEvent ev in myTrack.Events)
            {
                if (ev.Selected)
                {
                    l.Add((T)ev);
                }
            }
        }
        if (maxCount > 0 && maxCount < l.Count)
        {
            l = l.GetRange(0, maxCount);
        }
        return l;
    }
}