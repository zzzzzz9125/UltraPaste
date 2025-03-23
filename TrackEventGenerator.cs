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

        public static List<TrackEvent> GenerateEvents(this Project project, Media media, Timecode start, Timecode length = null, MediaType type = MediaType.Unknown, bool useMultipleSelectedTracks = false, int newTrackIndex = -1)
        {
            List<TrackEvent> l = new List<TrackEvent>();
            if (type == MediaType.Video)
            {
                l.AddRange(project.GenerateEvents<VideoEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex));
            }
            else if (type == MediaType.Audio)
            {
                l.AddRange(project.GenerateEvents<AudioEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex));
            }
            else
            {
                l.AddRange(project.GenerateEvents<TrackEvent>(media, start, length, useMultipleSelectedTracks, newTrackIndex));
            }
            return l;
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
                if (useMultipleSelectedTracks)
                {
                    foreach (Track trk in project.Tracks)
                    {
                        if (trk.Index >= newTrackIndex && ((isVideoOnly && trk.IsVideo()) || (isAudioOnly && trk.IsAudio()) || (!isVideoOnly && !isAudioOnly)))
                        {
                            trk.Selected = true;
                            selectedTracks.Add(trk);
                            break;
                        }
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

            foreach (Track myTrack in selectedTracks)
            {
                if ((typeof(T) == typeof(VideoEvent) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioEvent) && !myTrack.IsAudio()))
                {
                    continue;
                }


                T ev = (T)(myTrack.IsVideo() ? (TrackEvent)new VideoEvent(project, start, length, null) : new AudioEvent(project, start, length, null));
                if (ev == null)
                {
                    continue;
                }

                myTrack.Events.Add(ev);
                ev.AddTake(media);
                l.Add(ev);
            }

            if (l.Count > 1)
            {
                TrackEventGroup g = l[0].Group;
                if (g == null)
                {
                    g = new TrackEventGroup();
                    project.Groups.Add(g);
                    g.Add(l[0]);
                }
                foreach (TrackEvent ev in l)
                {
                    if (!g.Contains(ev))
                    {
                        g.Add(ev);
                    }
                }
            }

            return l;
        }

        public static Take AddTake(this TrackEvent ev, Media media, bool makeActive = true, string name = null)
        {
            MediaStream ms = (ev != null && media != null && (ev.IsVideo() ? media.HasVideo() : media.HasAudio())) ? (ev.IsVideo() ? (MediaStream)media.GetVideoStreamByIndex(0) : media.GetAudioStreamByIndex(0)) : null;
            if (ms == null)
            {
                return null;
            }
            return ev.AddTake(ms, makeActive, name);
        }

        public static List<TrackEvent> AddMissingStreams<T>(this Project project, IEnumerable<T> evs, MediaType type = MediaType.Unknown, bool reverse = false, int offset = 0, bool alwaysGenerateNewTracks = false, int videoTrackNestedDiff = 0) where T : TrackEvent
        {
            List<TrackEvent> l = new List<TrackEvent>();
            foreach (T ev in evs)
            {
                l.AddRange(AddMissingStreams(project, ev, type, reverse, offset, alwaysGenerateNewTracks, videoTrackNestedDiff));
            }
            return l;
        }

        public static List<TrackEvent> AddMissingStreams<T>(this Project project, T ev, MediaType type = MediaType.Unknown, bool reverse = false, int offset = 0, bool alwaysGenerateNewTracks = false, int videoTrackNestedDiff = 0) where T : TrackEvent
        {
            List<TrackEvent> l = new List<TrackEvent>();
            if (type == MediaType.Video)
            {
                AddMissingStreams(project, ev, out List<VideoEvent> list, reverse, offset, alwaysGenerateNewTracks, videoTrackNestedDiff);
                l.AddRange(list);
            }
            else if (type == MediaType.Audio)
            {
                AddMissingStreams(project, ev, out List<AudioEvent> list, reverse, offset, alwaysGenerateNewTracks, videoTrackNestedDiff);
                l.AddRange(list);
            }
            else
            {
                AddMissingStreams(project, ev, out List<TrackEvent> list, reverse, offset, alwaysGenerateNewTracks, videoTrackNestedDiff);
                l.AddRange(list);
            }
            return l;
        }

        public static void AddMissingStreams<T, U>(this Project project, IEnumerable<T> evs, out List<U> l, bool reverse = false, int offset = 0, bool alwaysGenerateNewTracks = false, int videoTrackNestedDiff = 0) where T : TrackEvent where U : TrackEvent
        {
            l = new List<U>();
            foreach (T ev in evs)
            {
                AddMissingStreams(project, ev, out List<U> list, reverse, offset, alwaysGenerateNewTracks, videoTrackNestedDiff);
                l.AddRange(list);
            }
        }

        public static void AddMissingStreams<T, U>(this Project project, T ev, out List<U> l, bool reverse = false, int offset = 0, bool alwaysGenerateNewTracks = false, int videoTrackNestedDiff = 0) where T : TrackEvent where U : TrackEvent
        {
            l = new List<U>();
            if (ev.Takes.Count == 0)
            {
                return;
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

            while (offset < 0)
            {
                offset += ev.ActiveTake.Media.Streams.Count;
            }
            int index = (ev.ActiveTake.MediaStream.Index + offset) % ev.ActiveTake.Media.Streams.Count;

            streams.Sort((a, b) => { return Math.Abs((a.Index + offset) % ev.ActiveTake.Media.Streams.Count - index) - Math.Abs((b.Index + offset) % ev.ActiveTake.Media.Streams.Count - index); });


            int nestingLevel = ev.Track is VideoTrack vTrack ? Math.Max(0, vTrack.CompositeNestingLevel + videoTrackNestedDiff) : -1;
            foreach (MediaStream stream in streams)
            {
                if ((typeof(U) == typeof(VideoEvent) && stream.MediaType != MediaType.Video) || (typeof(U) == typeof(AudioEvent) && stream.MediaType != MediaType.Audio))
                {
                    continue;
                }

                Track track = null;
                int indexOffset = stream.Index - ev.ActiveTake.MediaStream.Index;
                if (indexOffset == 0)
                {
                    continue;
                }

                if (offset != 0)
                {
                    indexOffset = (stream.Index + offset) % ev.ActiveTake.Media.Streams.Count - index;
                }

                indexOffset *= reverse ? -1 : 1;

                int trackIndex = ev.Track.Index + (indexOffset > 0 ? 1 : -1);
                if (!alwaysGenerateNewTracks)
                {
                    while (trackIndex > -1 && trackIndex < project.Tracks.Count)
                    {
                        Track trk = project.Tracks[trackIndex];
                        if (usedTrack.Contains(trk))
                        {
                            trackIndex += indexOffset > 0 ? 1 : -1;
                        }
                        else
                        {
                            if (trk.MediaType == stream.MediaType && (!(trk is VideoTrack vTrk) || nestingLevel < 0 || vTrk.CompositeNestingLevel == nestingLevel))
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
                }

                if (track == null)
                {
                    trackIndex = Math.Max(0, trackIndex);
                    track = stream.MediaType == MediaType.Video ? (Track)new VideoTrack(project, trackIndex, null) : new AudioTrack(project, trackIndex, null);
                    project.Tracks.Add(track);
                    if (track is VideoTrack vTrk && track.Index > 0 && nestingLevel > -1)
                    {
                        vTrk.CompositeNestingLevel = nestingLevel;
                    }
                    track.Name = ev.Track.Name;
                    track.Solo = ev.Track.Solo;
                    track.Mute = ev.Track.Mute;
                }

                TrackEvent nev = stream.MediaType == MediaType.Video ? (TrackEvent)new VideoEvent(project, ev.Start, ev.Length, null) : new AudioEvent(project, ev.Start, ev.Length, null);
                track.Events.Add(nev);
                nev.AddTake(stream);
                nev.PlaybackRate = ev.PlaybackRate;
                nev.ActiveTake.Offset = ev.ActiveTake.Offset;
                nev.Loop = ev.Loop;
                group.Add(nev);
                usedTrack.Add(nev.Track);
                l.Add((U)nev);
            }
        }
    }
}
