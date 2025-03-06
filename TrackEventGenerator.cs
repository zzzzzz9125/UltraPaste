#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Collections.Generic;

namespace UltraPaste
{
    public static class TrackEventGenerator
    {
        // a complex implementation to import File as Events to Timeline
        // when media path is invalid, it won't generate any event
        public static List<T> GenerateEvents<T>(this Vegas vegas, string path, Timecode start, Timecode length = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1) where T : TrackEvent
        {
            Media media = vegas.GetValidMedia(path);
            if (media == null)
            {
                return new List<T>();
            }
            return vegas.Project.GenerateEvents<T>(media, start, length, useMultipleSelectedTracks, newTrackIndex);
        }

        // a complex implementation to import Media as Events to Timeline
        // when media is null, it will generate blank events
        public static List<T> GenerateEvents<T>(this Project project, Media media, Timecode start, Timecode length = null, bool useMultipleSelectedTracks = false, int newTrackIndex = -1) where T : TrackEvent
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
                selectedTracks.AddRange(project.GetSelectedTracks<VideoTrack>(trackCount));
            }
            else if (isAudioOnly)
            {
                selectedTracks.AddRange(project.GetSelectedTracks<AudioTrack>(trackCount));
            }
            else
            {
                selectedTracks.AddRange(project.GetSelectedTracks<Track>(trackCount));
            }

            if (selectedTracks.Count == 0)
            {
                foreach (Track trk in project.Tracks)
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
                    Track trk = typeof(T) == typeof(AudioEvent) || (media != null && !media.HasVideo()) ? (Track)new AudioTrack(project, newTrackIndex, null) : new VideoTrack(project, newTrackIndex, null);
                    project.Tracks.Add(trk);
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
                    ev = (T)(TrackEvent)new VideoEvent(project, start, length, null);
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
                    ev = (T)(TrackEvent)new AudioEvent(project, start, length, null);
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
                        group = new TrackEventGroup(project);
                        project.TrackEventGroups.Add(group);
                        group.Add(ev);
                    }
                    for (int i = 0; i < media.StreamCount(MediaType.Audio); i++)
                    {
                        AudioStream streamAudio = media.GetAudioStreamByIndex(i);
                        AudioEvent eventAudio = new AudioEvent(project, start, length, null);
                        Track trackBelow = myTrack.Index + i < project.Tracks.Count - 1 ? project.Tracks[myTrack.Index + i + 1] : null;
                        if (trackBelow == null || !trackBelow.IsAudio())
                        {
                            trackBelow = new AudioTrack(project, myTrack.Index + i + 1, null);
                            project.Tracks.Add(trackBelow);
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

        public static List<T> AddMissingStreams<T>(this Project project, IEnumerable<T> evs) where T : TrackEvent
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
                    group = new TrackEventGroup(project);
                    project.Groups.Add(group);
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
                    while (trackIndex > -1 && trackIndex < project.Tracks.Count)
                    {
                        Track trk = project.Tracks[trackIndex];
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
                        track = stream.MediaType == MediaType.Video ? (Track)new VideoTrack(project, trackIndex, null) : new AudioTrack(project, trackIndex, null);
                        project.Tracks.Add(track);
                    }

                    TrackEvent nev = stream.MediaType == MediaType.Video ? (TrackEvent)new VideoEvent(project, ev.Start, ev.Length, null) : new AudioEvent(project, ev.Start, ev.Length, null);
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
    }
}
