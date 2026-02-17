#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using ReaperDataParser.Models;
using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Melanchall.DryWetMidi.Common;

namespace UltraPaste.Utilities
{
    using UltraPaste.Core;

    internal static class YtpmvTriggerHelper
    {
        private struct TriggerTiming
        {
            public Timecode Start { get; }
            public Timecode Length { get; }
            public Timecode End { get; }
            public float Gain { get; }

            public TriggerTiming(Timecode start, Timecode length, Timecode end, float gain)
            {
                Start = start;
                Length = length;
                End = end;
                Gain = gain;
            }
        }

        private sealed class TriggerTimingSource
        {
            public List<TriggerTiming> Timings { get; }

            public TriggerTimingSource(MidiFile midi, Timecode baseStart)
            {
                Timings = BuildMidiTimings(midi, baseStart);
            }

            public Timecode GetMinStart()
            {
                Timecode min = null;
                foreach (TriggerTiming timing in Timings)
                {
                    if (min == null || timing.Start < min)
                    {
                        min = timing.Start;
                    }
                }
                return min;
            }

            private static List<TriggerTiming> BuildMidiTimings(MidiFile midi, Timecode baseStart)
            {
                List<TriggerTiming> timings = new List<TriggerTiming>();
                if (midi == null)
                {
                    return timings;
                }

                TempoMap tempoMap = midi.GetTempoMap();

                foreach (Note note in midi.GetNotes())
                {
                    if (note.Length <= 0)
                    {
                        continue;
                    }

                    MetricTimeSpan metricStart = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time, tempoMap);
                    MetricTimeSpan metricEnd = TimeConverter.ConvertTo<MetricTimeSpan>(note.Time + note.Length, tempoMap);
                    Timecode noteStart = baseStart + Timecode.FromMilliseconds(metricStart.TotalMicroseconds / 1000.0);
                    Timecode noteEnd = baseStart + Timecode.FromMilliseconds(metricEnd.TotalMicroseconds / 1000.0);
                    Timecode noteLength = noteEnd - noteStart;
                    float gain = (float)Math.Max(0.0, Math.Min(1.0, note.Velocity / 127.0));
                    timings.Add(new TriggerTiming(noteStart, noteLength, noteEnd, gain));
                }

                return timings;
            }
        }

        public static List<TrackEvent> GenerateEventsToVegas(this MidiFile midi, IEnumerable<TrackEvent> selectedEvents, Timecode start, bool closeGap, bool fillGaps = true)
        {
            List<TrackEvent> evs = new List<TrackEvent>();

            if (selectedEvents == null || !selectedEvents.Any())
            {
                return evs;
            }

            if (start == null || start < new Timecode(0))
            {
                start = new Timecode(0);
            }

            TriggerTimingSource timingSource = new TriggerTimingSource(midi, new Timecode(0));
            if (timingSource.Timings.Count == 0)
            {
                return evs;
            }

            if (closeGap)
            {
                Timecode offset = timingSource.GetMinStart();
                if (offset != null)
                {
                    start -= offset;
                }
            }

            return GenerateEventsFromTimings(selectedEvents, timingSource.Timings, start, fillGaps);
        }

        public static List<TrackEvent> GenerateEventsToVegas(this ReaperData data, IEnumerable<TrackEvent> selectedEvents, Timecode start, bool closeGap = true)
        {
            List<TrackEvent> l = new List<TrackEvent>();

            if (selectedEvents == null || !selectedEvents.Any())
            {
                return l;
            }

            if (start == null || start < new Timecode(0))
            {
                start = new Timecode(0);
            }

            bool hasOnlyMidi = true;
            bool hasAnyMidi = false;
            bool hasAnyNonMidi = false;

            foreach (ReaperTrack track in data.Tracks)
            {
                foreach (ReaperItem item in track.Items)
                {
                    if (item?.Source is ReaperSourceMidi)
                    {
                        hasAnyMidi = true;
                    }
                    else
                    {
                        hasOnlyMidi = false;
                        hasAnyNonMidi = true;
                    }
                }
            }

            if (closeGap)
            {
                Timecode offset = null;

                if (hasOnlyMidi)
                {
                    foreach (ReaperTrack track in data.Tracks)
                    {
                        foreach (ReaperItem item in track.Items)
                        {
                            if (item?.Source is ReaperSourceMidi midiSource)
                            {
                                MidiFile midiFile = ParseMidiFromSource(midiSource);
                                if (midiFile != null)
                                {
                                    TriggerTimingSource timingSource = new TriggerTimingSource(midiFile, Timecode.FromSeconds(item.Position));
                                    Timecode timingOffset = timingSource.GetMinStart();
                                    if (timingOffset != null && (offset == null || timingOffset < offset))
                                    {
                                        offset = timingOffset;
                                    }
                                }
                            }
                        }
                    }
                }
                else if (hasAnyNonMidi)
                {
                    foreach (ReaperTrack track in data.Tracks)
                    {
                        foreach (ReaperItem item in track.Items)
                        {
                            Timecode tmp = Timecode.FromSeconds(item.Position);
                            if (offset == null || tmp < offset)
                            {
                                offset = tmp;
                            }
                        }
                    }
                }

                if (offset != null)
                {
                    start -= offset;
                }
            }

            foreach (ReaperTrack track in data.Tracks)
            {
                foreach (ReaperItem item in track.Items)
                {
                    if (item == null || item.Source == null)
                    {
                        continue;
                    }

                    if (item.Source is ReaperSourceMidi midiSource)
                    {
                        MidiFile midiFile = ParseMidiFromSource(midiSource);
                        if (midiFile == null)
                        {
                            continue;
                        }

                        EnsureTempoMap(midiFile);
                        TriggerTimingSource timingSource = new TriggerTimingSource(midiFile, Timecode.FromSeconds(item.Position));
                        l.AddRange(GenerateEventsFromTimings(selectedEvents, timingSource.Timings, start, true));
                    }
                    else
                    {
                        l.AddRange(GenerateRegularEvents(item, selectedEvents, start));
                    }
                }
            }

            foreach (TrackEvent ev in l)
            {
                ev.Selected = true;
            }

            return l;
        }

        private static void EnsureTempoMap(MidiFile midiFile)
        {
            if (midiFile == null || midiFile.GetTimedEvents().Any(ev => ev.Event is SetTempoEvent))
            {
                return;
            }

            double bpm = UltraPasteCommon.Vegas.Project.Ruler.BeatsPerMinute;
            Tempo tempo = Tempo.FromBeatsPerMinute(bpm);
            TrackChunk trackChunk = midiFile.GetTrackChunks().FirstOrDefault();
            if (trackChunk == null)
            {
                trackChunk = new TrackChunk();
                midiFile.Chunks.Add(trackChunk);
            }
            trackChunk.Events.Insert(0, new SetTempoEvent(tempo.MicrosecondsPerQuarterNote));
        }

        private static List<TrackEvent> GenerateEventsFromTimings(IEnumerable<TrackEvent> selectedEvents, List<TriggerTiming> timings, Timecode start, bool fillGaps)
        {
            List<TrackEvent> evs = new List<TrackEvent>();
            if (timings == null || timings.Count == 0)
            {
                return evs;
            }

            List<(Timecode Start, Timecode Length, float Gain, int TrackIndex)> noteAssignments = new List<(Timecode Start, Timecode Length, float Gain, int TrackIndex)>();
            List<Timecode> lastEnds = new List<Timecode>();

            foreach (TriggerTiming timing in timings.OrderBy(note => note.Start))
            {
                int trackIndex = -1;
                for (int i = 0; i < lastEnds.Count; i++)
                {
                    if (timing.Start >= lastEnds[i])
                    {
                        trackIndex = i;
                        lastEnds[i] = timing.End;
                        break;
                    }
                }

                if (trackIndex < 0)
                {
                    trackIndex = lastEnds.Count;
                    lastEnds.Add(timing.End);
                }

                noteAssignments.Add((timing.Start, timing.Length, timing.Gain, trackIndex));
            }

            List<AudioTrack> audioTracks = new List<AudioTrack>();
            List<VideoTrack> videoTracks = new List<VideoTrack>();

            foreach (TrackEvent selectedEvent in selectedEvents)
            {
                bool isAudio = selectedEvent is AudioEvent;

                foreach (var assignment in noteAssignments)
                {
                    Track trk = null;
                    if (isAudio)
                    {
                        while (audioTracks.Count <= assignment.TrackIndex)
                        {
                            AudioTrack newTrack = new AudioTrack(UltraPasteCommon.Vegas.Project, 0, "MIDI");
                            UltraPasteCommon.Vegas.Project.Tracks.Add(newTrack);
                            audioTracks.Add(newTrack);
                        }
                        trk = audioTracks[assignment.TrackIndex];
                    }
                    else
                    {
                        while (videoTracks.Count <= assignment.TrackIndex)
                        {
                            VideoTrack newTrack = new VideoTrack(UltraPasteCommon.Vegas.Project, 0, "MIDI");
                            UltraPasteCommon.Vegas.Project.Tracks.Add(newTrack);
                            videoTracks.Add(newTrack);
                        }
                        trk = videoTracks[assignment.TrackIndex];
                    }

                    TrackEvent ev = selectedEvent.Copy(trk, assignment.Start + start);
                    if (ev == null)
                    {
                        continue;
                    }

                    ev.Length = assignment.Length;
                    if (false)
                    {
                        ev.FadeIn.Gain = assignment.Gain;
                    }

                    if (!isAudio)
                    {
                        foreach (VideoMotionKeyframe kf in (ev as VideoEvent).VideoMotion.Keyframes)
                        {
                            kf.ScaleBy(new VideoMotionVertex(ev.Track.Events.Count % 2 == 0 ? -1 : 1, 1));
                        }
                    }

                    evs.Add(ev);
                }
            }

            if (fillGaps)
            {
                FillGaps(audioTracks, videoTracks);
            }

            foreach (AudioTrack track in audioTracks)
            {
                if (track.Events.Count == 0)
                {
                    UltraPasteCommon.Vegas.Project.Tracks.Remove(track);
                }
            }

            foreach (VideoTrack track in videoTracks)
            {
                if (track.Events.Count == 0)
                {
                    UltraPasteCommon.Vegas.Project.Tracks.Remove(track);
                }
            }

            return evs;
        }

        private static void FillGaps(IEnumerable<AudioTrack> audioTracks, IEnumerable<VideoTrack> videoTracks)
        {
            foreach (AudioTrack track in audioTracks)
            {
                List<TrackEvent> trackEvents = track.Events.OrderBy(ev => ev.Start).ToList();
                for (int i = 0; i + 1 < trackEvents.Count; i++)
                {
                    TrackEvent current = trackEvents[i];
                    TrackEvent next = trackEvents[i + 1];
                    if (next.Start > current.Start)
                    {
                        current.End = next.Start;
                    }
                }
            }

            foreach (VideoTrack track in videoTracks)
            {
                List<TrackEvent> trackEvents = track.Events.OrderBy(ev => ev.Start).ToList();
                for (int i = 0; i + 1 < trackEvents.Count; i++)
                {
                    TrackEvent current = trackEvents[i];
                    TrackEvent next = trackEvents[i + 1];
                    if (next.Start > current.Start)
                    {
                        current.End = next.Start;
                    }
                }
            }
        }

        private static MidiFile ParseMidiFromSource(ReaperSourceMidi midiSource)
        {
            if (midiSource == null || !midiSource.HasData)
            {
                return null;
            }

            try
            {
                TrackChunk trackChunk = new TrackChunk();
                long currentTime = 0;

                if (!string.IsNullOrEmpty(midiSource.MIDIData))
                {
                    string[] lines = midiSource.MIDIData.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);

                    foreach (string line in lines)
                    {
                        string[] parts = line.Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 2)
                        {
                            continue;
                        }

                        if (parts[0] == "E" && parts.Length >= 4)
                        {
                            if (long.TryParse(parts[1], out long delta) &&
                                byte.TryParse(parts[2], System.Globalization.NumberStyles.HexNumber, null, out byte status) &&
                                byte.TryParse(parts[3], System.Globalization.NumberStyles.HexNumber, null, out byte data1))
                            {
                                byte data2 = 0;
                                if (parts.Length >= 5)
                                {
                                    byte.TryParse(parts[4], System.Globalization.NumberStyles.HexNumber, null, out data2);
                                }

                                currentTime += delta;
                                MidiEvent midiEvent = CreateMidiEvent(status, data1, data2);
                                if (midiEvent != null)
                                {
                                    midiEvent.DeltaTime = delta;
                                    trackChunk.Events.Add(midiEvent);
                                }
                            }
                        }
                    }
                }

                MidiFile midiFile = new MidiFile(trackChunk);
                midiFile.TimeDivision = new TicksPerQuarterNoteTimeDivision((short)midiSource.PPQ);
                return midiFile;
            }
            catch
            {
                return null;
            }
        }

        private static MidiEvent CreateMidiEvent(byte status, byte data1, byte data2)
        {
            byte eventType = (byte)(status & 0xF0);
            byte channel = (byte)(status & 0x0F);

            switch (eventType)
            {
                case 0x80:
                    return new NoteOffEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)channel };
                case 0x90:
                    return new NoteOnEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)channel };
                case 0xB0:
                    return new ControlChangeEvent((SevenBitNumber)data1, (SevenBitNumber)data2) { Channel = (FourBitNumber)channel };
                case 0xC0:
                    return new ProgramChangeEvent((SevenBitNumber)data1) { Channel = (FourBitNumber)channel };
                case 0xE0:
                    return new PitchBendEvent((ushort)((data2 << 7) | data1)) { Channel = (FourBitNumber)channel };
                default:
                    return null;
            }
        }

        private static List<TrackEvent> GenerateRegularEvents(ReaperItem item, IEnumerable<TrackEvent> selectedEvents, Timecode start)
        {
            List<TrackEvent> evs = new List<TrackEvent>();

            foreach (TrackEvent selectedEvent in selectedEvents)
            {
                Timecode itemStart = start + Timecode.FromSeconds(item.Position);
                Timecode itemLength = Timecode.FromSeconds(item.Length);

                TrackEvent ev = selectedEvent.Copy(selectedEvent.Track, itemStart);
                if (ev == null)
                {
                    continue;
                }

                ev.Length = itemLength;
                ev.Loop = item.Loop;
                ev.Selected = item.Selected;
                ev.SnapOffset = Timecode.FromSeconds(item.SnapOffs);
                ev.Mute = item.Mute?.Length > 0 && item.Mute[0] != 0;

                if (item.PlayRate?.Length > 0)
                {
                    ev.AdjustPlaybackRate(item.PlayRate[0], true);
                }

                if (ev is AudioEvent audioEv)
                {
                    audioEv.Channels = item.ChanMode == 1 ? ChannelRemapping.Swap : item.ChanMode == 2 ? ChannelRemapping.Mono : item.ChanMode == 3 ? ChannelRemapping.DisableRight : item.ChanMode == 4 ? ChannelRemapping.DisableLeft : ChannelRemapping.None;

                    if (item.FadeIn?.Length > 1)
                    {
                        audioEv.FadeIn.Length = Timecode.FromSeconds(item.FadeIn[1]);
                        if (item.FadeIn?.Length > 3)
                        {
                            audioEv.FadeIn.Curve = item.FadeIn[3] == 0 ? CurveType.Linear : (item.FadeIn[3] == 2 || item.FadeIn[3] == 4) ? CurveType.Slow : item.FadeIn[3] == 5 ? CurveType.Smooth : item.FadeIn[3] == 6 ? CurveType.Sharp : CurveType.Fast;
                        }
                    }
                    if (item.FadeOut?.Length > 1)
                    {
                        audioEv.FadeOut.Length = Timecode.FromSeconds(item.FadeOut[1]);
                        if (item.FadeOut?.Length > 3)
                        {
                            audioEv.FadeOut.Curve = item.FadeOut[3] == 0 ? CurveType.Linear : (item.FadeOut[3] == 2 || item.FadeOut[3] == 4) ? CurveType.Fast : item.FadeOut[3] == 5 ? CurveType.Smooth : item.FadeOut[3] == 6 ? CurveType.Sharp : CurveType.Slow;
                        }
                    }

                    if (item.VolPan?.Length > 0)
                    {
                        audioEv.FadeIn.Gain = item.VolPan[0] < 1 ? (float)item.VolPan[0] : 1;
                    }
                }

                evs.Add(ev);
            }

            return evs;
        }
    }
}
