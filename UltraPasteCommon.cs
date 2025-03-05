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
using System.Text.RegularExpressions;
using System.Runtime.InteropServices.ComTypes;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

public static class UltraPasteCommon
{
    public static UltraPasteSettings Settings = UltraPasteSettings.LoadFromFile();
    public const string VERSION = "v1.00";

    public static Vegas myVegas;
    public const string UID_TITLES_AND_TEXT = "{Svfx:com.vegascreativesoftware:titlesandtext}";
    public const string UID_TITLES_AND_TEXT_SONY = "{Svfx:com.sonycreativesoftware:titlesandtext}";
    public static PlugInNode plugInTitlesAndText = null;
    public static KeyValuePair<Image, string> LastImageAndPath = new KeyValuePair<Image, string>();
    public static KeyValuePair<Stream, string> LastAudioStreamAndPath = new KeyValuePair<Stream, string>();

    public static void DoPaste()
    {
        string projectFileToOpen = null, scriptFileToRun = null;
        using (UndoBlock undo = new UndoBlock(myVegas.Project, L.UltraPaste))
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
                    img.Save(path, ImageFormat.Png);
                }
                LastImageAndPath.Key?.Dispose();
                LastImageAndPath = new KeyValuePair<Image, string>(img, path);
                evs.AddRange(GenerateEvents<VideoEvent>(path, start, length, true));
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
                evs.AddRange(GenerateEvents<AudioEvent>(path, start, length, true));
            }
            else if (Clipboard.ContainsFileDropList())
            {
                List<string> filePaths = Common.GetFilePathsFromPathList(Clipboard.GetFileDropList(), out string uniformExtension);
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
                    if (Common.IsPathMatch(path, "*.veg.bak;*.sfvp0;*.sfap0;*.sfk;*.sfl;*.rpp-bak;*.reapeaks") || !File.Exists(path))
                    {
                        continue;
                    }
                    List<TrackEvent> addedEvents = null;
                    if (Path.GetExtension(path).ToLower() == ".veg")
                    {
                        // Opening the project file in UndoBlock causes an UndoBlock error, so we have to jump out of UndoBlock...
                        projectFileToOpen = path;
                        break;
                    }
                    else if (Path.GetExtension(path).ToLower() == ".rpp")
                    {
                        ReaperData rd = ReaperData.Parser.Parse(path);
                        addedEvents = rd.GenerateEventsToVegas(startTime, true);
                    }
                    else if (Path.GetExtension(path).ToLower() == ".srt")
                    {
                        SrtData srt = SrtData.Parser.Parse(path);
                        addedEvents = srt.GenerateEventsToVegas(startTime);
                        srt.GenerateRegionsToVegas(startTime);
                    }
                    else if (Path.GetExtension(path).ToLower() == ".cs" || Path.GetExtension(path).ToLower() == ".js" || Path.GetExtension(path).ToLower() == ".vb" || Path.GetExtension(path).ToLower() == ".dll")
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
                    startTime = GetEndTimeFromEvents(addedEvents);
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
        if (projectFileToOpen != null)
        {
            myVegas.OpenFile(projectFileToOpen);
        }
        else if (scriptFileToRun != null)
        {
            myVegas.RunScriptFile(scriptFileToRun);
        }
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

    public static Timecode GetEndTimeFromMarkers<T>(IEnumerable<T> markers) where T : Marker
    {
        Timecode end = new Timecode(0);
        foreach (T m in markers)
        {
            Region r = m as Region;
            Timecode t = r != null ? r.End : m.Position;
            if (end < t)
            {
                end = t;
            }
        }
        return end;
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

    public static List<T> AddMissingStreams<T>(IEnumerable<T> evs) where T : TrackEvent
    {
        List<T> l = new List<T>();
        foreach (TrackEvent ev in evs)
        {
            if (ev.Takes.Count == 0)
            {
                continue;
            }

            List<MediaStream> streams = new List<MediaStream>();
            streams.AddRange(ev.ActiveTake.Media.Streams);
            streams.Remove(ev.ActiveTake.MediaStream);
            TrackEventGroup group = null;
            List<Track> usedTrack = new List<Track>();
            if (ev.IsGrouped && ev.Group != null)
            {
                group = ev.Group;
                foreach (TrackEvent gev in group)
                {
                    if (gev.Takes.Count == 0)
                    {
                        continue;
                    }
                    streams.Remove(gev.ActiveTake.MediaStream);
                    usedTrack.Add(gev.Track);
                }
            }
            else
            {
                group = new TrackEventGroup(myVegas.Project);
                myVegas.Project.Groups.Add(group);
                group.Add(ev);
            }

            streams.Sort((a, b) => { return Math.Abs(a.Index - ev.ActiveTake.MediaStream.Index) - Math.Abs(b.Index - ev.ActiveTake.MediaStream.Index); });

            foreach (MediaStream stream in streams)
            {
                Track track = null;
                int indexOffset = stream.Index - ev.ActiveTake.MediaStream.Index;
                if (indexOffset == 0)
                {
                    continue;
                }

                int trackIndex = ev.Track.Index + (indexOffset > 0 ? 1 : -1);
                while (trackIndex > -1 && trackIndex < myVegas.Project.Tracks.Count)
                {
                    Track trk = myVegas.Project.Tracks[trackIndex];
                    if (usedTrack.Contains(trk))
                    {
                        trackIndex += indexOffset > 0 ? 1 : -1;
                    }
                    else
                    {
                        if (trk.MediaType == stream.MediaType)
                        {
                            track = trk;
                        }
                        else if (indexOffset < 0)
                        {
                            trackIndex += 1;
                        }
                        break;
                    }
                }

                if (track == null)
                {
                    track = stream.MediaType == MediaType.Video ? (Track)new VideoTrack(myVegas.Project, trackIndex, null) : new AudioTrack(myVegas.Project, trackIndex, null);
                    myVegas.Project.Tracks.Add(track);
                }

                TrackEvent nev = stream.MediaType == MediaType.Video ? (TrackEvent)new VideoEvent(myVegas.Project, ev.Start, ev.Length, null) : new AudioEvent(myVegas.Project, ev.Start, ev.Length, null);
                track.Events.Add(nev);
                nev.AddTake(stream);
                nev.PlaybackRate = ev.PlaybackRate;
                nev.ActiveTake.Offset = ev.ActiveTake.Offset;
                group.Add(nev);
                usedTrack.Add(nev.Track);
                l.Add((T)nev);
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