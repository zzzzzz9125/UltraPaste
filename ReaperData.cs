#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace UltraPaste
{
    public class ReaperData
    {
        public List<ReaperTrack> Tracks { get; set; }
        public bool IsTrackData { get; set; }
        private string projectFilePath;
        private static readonly System.Reflection.Assembly assembly = typeof(Vegas).Assembly;
        private static readonly Type elastiqueStretchAttributes = assembly.GetType(string.Format("{0}.ElastiqueStretchAttributes", typeof(Vegas).Namespace)), timeStretchPitchShift = assembly.GetType(string.Format("{0}.TimeStretchPitchShift", typeof(Vegas).Namespace));
        private static readonly System.Reflection.PropertyInfo propertyElastiqueAttribute = typeof(AudioEvent).GetProperty("ElastiqueAttribute", elastiqueStretchAttributes),
                                                                       propertyPitchSemis = typeof(AudioEvent).GetProperty("PitchSemis", typeof(double)),
                                                                           propertyMethod = typeof(AudioEvent).GetProperty("Method", timeStretchPitchShift),
                                                                      propertyFormantLock = typeof(AudioEvent).GetProperty("FormantLock", typeof(bool));
        public string ProjectFilePath
        {
            get { return projectFilePath; }
            set
            {
                // relative paths may be used in project files
                projectFilePath = value;
                List<ReaperSource> sources = new List<ReaperSource>();
                foreach (ReaperTrack track in Tracks)
                {
                    foreach (ReaperItem item in track.Items)
                    {
                        sources.Add(item.Source);
                        foreach (ReaperTake take in item.Takes)
                        {
                            sources.Add(take.Source);
                        }
                    }
                }
                string folder = Path.GetDirectoryName(projectFilePath);
                foreach (ReaperSource source in sources)
                {
                    source.UpdateFullFilePath(folder);
                }
            }
        }

        public ReaperData()
        {
            Tracks = new List<ReaperTrack>();
        }

        public static ReaperData From(IEnumerable<Track> tracks)
        {
            List<TrackEvent> evs = new List<TrackEvent>();
            foreach (Track track in tracks)
            {
                evs.AddRange(track.Events);
            }
            ReaperData data = From(evs);
            data.IsTrackData = true;
            return data;
        }

        public static ReaperData From(IEnumerable<TrackEvent> evs)
        {
            ReaperData data = new ReaperData();
            Track lastTrack = null;
            ReaperTrack currentTrack = null;
            foreach (TrackEvent ev in evs)
            {
                int fadeInType = ev.FadeIn.Curve == CurveType.Fast ? 1 : ev.FadeIn.Curve == CurveType.Slow ? 2 : ev.FadeIn.Curve == CurveType.Smooth ? 5 : ev.FadeIn.Curve == CurveType.Sharp ? 6 : 0;
                int fadeOutType = ev.FadeOut.Curve == CurveType.Slow ? 1 : ev.FadeIn.Curve == CurveType.Fast ? 2 : ev.FadeIn.Curve == CurveType.Smooth ? 5 : ev.FadeIn.Curve == CurveType.Sharp ? 6 : 0;
                ReaperItem item = new ReaperItem()
                {
                    Position = ev.Start.ToMilliseconds() / 1000,
                    SnapOffs = ev.SnapOffset.ToMilliseconds() / 1000,
                    Length = ev.Length.ToMilliseconds() / 1000,
                    Loop = ev.Loop,
                    FadeIn = new double[] { 1, ev.FadeIn.Length.ToMilliseconds() / 1000, 0, fadeInType, 0, fadeInType == 2 ? 1 : 0, fadeInType == 2 ? 1 : 0 },
                    FadeOut = new double[] { 1, ev.FadeOut.Length.ToMilliseconds() / 1000, 0, fadeOutType, 0, fadeOutType == 2 ? -1 : 0, fadeOutType == 2 ? -1 : 0 },
                    Mute = new int[] { ev.Mute ? 1 : 0, 0 },
                    Sel = ev.Selected,
                    VolPan = new double[] { ev.FadeIn.Gain, 0, 1, -1 },
                    PlayRate = new double[] { ev.PlaybackRate, 1, 0, -1, 0, 0.0025 }
                };

                if (currentTrack == null || (lastTrack != ev.Track))
                {
                    currentTrack = new ReaperTrack();
                    data.Tracks.Add(currentTrack);
                    currentTrack.Name = ev.Track.Name;
                    currentTrack.VolPan = new double[] { 1, 0, -1, -1, 1 };
                    currentTrack.MuteSolo = new int[] { ev.Track.Mute ? 1 : 0, ev.Track.Solo ? 2 : 0, 0 };
                    if (ev.Track is AudioTrack aTrack)
                    {
                        currentTrack.VolPan = new double[] { aTrack.Volume, aTrack.PanX, -1, -1, 1 };
                        currentTrack.IPhase = aTrack.InvertPhase;

                        foreach (Envelope en in aTrack.Envelopes)
                        {
                            string type = en.Type == EnvelopeType.Volume ? "VOLENV2" : en.Type == EnvelopeType.Pan ? "PANENV2" : en.Type == EnvelopeType.Mute ? "MUTEENV" : null;
                            if (type == null)
                            {
                                continue;
                            }

                            ReaperEnvelope env = new ReaperEnvelope() { Type = type };
                            foreach (EnvelopePoint p in en.Points)
                            {
                                env.Points.Add(new double[] { p.X.ToMilliseconds() * 1000, p.Y, p.Curve == CurveType.None ? 1 : p.Curve == CurveType.Smooth ? 2 : p.Curve == CurveType.Fast ? 3: p.Curve == CurveType.Slow ? 4 : 0, 0, 0, 0, 0, p.X.ToMilliseconds() / 500 });
                            }
                            currentTrack.Envelopes.Add(env);
                        }
                    }
                }
                currentTrack.Items.Add(item);

                if (ev is AudioEvent)
                {
                    AudioEvent aEvent = ev as AudioEvent;
                    item.VolPan[2] = aEvent.Normalize ? (aEvent.NormalizeGain * (aEvent.InvertPhase ? -1 : 1)) : 1;

                    int methodType = -1;
                    double pitch = 0;
                    if (elastiqueStretchAttributes != null && timeStretchPitchShift != null)
                    {
                        if (propertyElastiqueAttribute != null && propertyPitchSemis != null && propertyMethod != null)
                        {
                            pitch = (double)propertyPitchSemis.GetValue(aEvent);
                            if (propertyMethod.GetValue(aEvent) == Enum.Parse(timeStretchPitchShift, "Elastique"))
                            {
                                object obj = propertyElastiqueAttribute.GetValue(aEvent);
                                if (obj == Enum.Parse(elastiqueStretchAttributes, "Pro"))
                                {
                                    methodType += 0x90000;
                                }
                                else if (obj == Enum.Parse(elastiqueStretchAttributes, "Efficient"))
                                {
                                    methodType += 0xA0000;
                                }
                                else if (obj == Enum.Parse(elastiqueStretchAttributes, "Soloist_Monophonic"))
                                {
                                    methodType += 0xB0000;
                                }
                                else if (obj == Enum.Parse(elastiqueStretchAttributes, "Soloist_Speech"))
                                {
                                    methodType += 0xB0002;
                                }
                            }
                        }
                    }
                    item.PlayRate = new double[] { ev.PlaybackRate, 1, pitch, methodType, 0, 0.0025 };
                    item.ChanMode = aEvent.Channels == ChannelRemapping.Swap ? 1 : aEvent.Channels == ChannelRemapping.Mono ? 2 : aEvent.Channels == ChannelRemapping.DisableRight || aEvent.Channels == ChannelRemapping.MuteRight ? 3 : aEvent.Channels == ChannelRemapping.DisableLeft || aEvent.Channels == ChannelRemapping.MuteLeft ? 4 : 0;
                }

                ReaperTake currentTake = item;
                foreach (Take tk in ev.Takes)
                {
                    currentTake.Name = tk.Name;
                    currentTake.SOffs = tk.Offset.ToMilliseconds() / 1000 * ev.PlaybackRate;
                    if (!File.Exists(tk.MediaPath))
                    {
                        continue;
                    }
                    if (tk.Media.HasVideo())
                    {
                        currentTake.Source.Type = "VIDEO";
                    }
                    else
                    {
                        switch (Path.GetExtension(tk.MediaPath).ToLower())
                        {
                            case ".wav":
                                currentTake.Source.Type = "WAVE";
                                break;

                            case ".mp3":
                                currentTake.Source.Type = "MP3";
                                break;

                            case ".flac":
                                currentTake.Source.Type = "FLAC";
                                break;

                            default:
                                currentTake.Source.Type = "VIDEO";
                                break;
                        }
                    }
                    currentTake.Source.FilePath = tk.MediaPath;
                    if (currentTake != item)
                    {
                        currentTake.ChanMode = item.ChanMode;
                        currentTake.Selected = tk.IsActive;
                        item.Takes.Add(currentTake);
                        currentTake = new ReaperTake();
                    }
                }
                lastTrack = ev.Track;
            }

            return data;
        }

        public List<TrackEvent> GenerateEventsToVegas(Timecode start, bool closeGap = true, bool addVideoStreams = true)
        {
            List<TrackEvent> l = new List<TrackEvent>();
            if (IsTrackData)
            {
                foreach (Track t in UltraPasteCommon.Vegas.Project.Tracks)
                {
                    t.Selected = false;
                }
            }

            if (start == null || start < new Timecode(0))
            {
                start = new Timecode(0);
            }

            if (closeGap)
            {
                Timecode offset = null;
                foreach (ReaperTrack track in Tracks)
                {
                    foreach (ReaperItem item in track.Items)
                    {
                        Timecode tmp = Timecode.FromSeconds(item.Position);
                        if (offset == null || tmp < offset)
                        {
                            offset = tmp;
                        }
                    }
                    foreach (ReaperEnvelope env in track.Envelopes)
                    {
                        if (env.Act[0] != 0 && env.Points.Count > 0)
                        {
                            Timecode tmp = Timecode.FromSeconds(env.Points[0][0]);
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

            Track lastTrack = null;
            foreach (ReaperTrack track in Tracks)
            {
                if (IsTrackData)
                {
                    AudioTrack trk = new AudioTrack(UltraPasteCommon.Vegas.Project, -1, track.Name);
                    UltraPasteCommon.Vegas.Project.Tracks.Add(trk);
                    if (track.VolPan?.Length > 0)
                    {
                        trk.Volume = (float)track.VolPan[0];
                    }
                    if (track.VolPan?.Length > 1)
                    {
                        trk.PanX = (float)track.VolPan[1];
                    }

                    trk.Mute = track.MuteSolo?.Length > 0 && track.MuteSolo[0] == 1;
                    trk.Solo = track.MuteSolo?.Length > 1 && track.MuteSolo[1] == 2;

                    trk.InvertPhase = track.IPhase;

                    foreach (ReaperEnvelope env in track.Envelopes)
                    {
                        if (env.Act[0] == 0 || env.SegRange != null)
                        {
                            continue;
                        }
                        Envelope en = null;
                        switch (env.Type)
                        {
                            case "VOLENV2":
                                en = new Envelope(EnvelopeType.Volume);
                                break;

                            case "PANENV2":
                                en = new Envelope(EnvelopeType.Pan);
                                break;

                            case "MUTEENV":
                                en = new Envelope(EnvelopeType.Mute);
                                break;

                            default:
                                continue;
                        }
                        if (!trk.Envelopes.Contains(en))
                        {
                            trk.Envelopes.Add(en);
                        }
                        foreach (double[] p in env.Points)
                        {
                            Timecode t = Timecode.FromSeconds(p[0]);
                            if (IsTrackData)
                            {
                                t += start;
                            }
                            CurveType curve = p[2] == 0 ? CurveType.Linear : p[2] == 1 ? CurveType.None : p[2] == 3 ? CurveType.Fast : p[2] == 4 ? CurveType.Slow : CurveType.Smooth;
                            EnvelopePoint point = en.Points.GetPointAtX(t);
                            double value = p[1];
                            if (point != null)
                            {
                                point.Y = value;
                            }
                            else
                            {
                                point = new EnvelopePoint(t, value);
                                en.Points.Add(point);
                            }
                            point.Curve = curve;
                        }
                        if (IsTrackData && (env.Points[0][0] != 0 || start.Nanos != 0))
                        {
                            en.Points[0].Curve = CurveType.None;
                            //if (en.Points.Count > 1) { en.Points[0].Y = en.Points[1].Y; }
                        }
                    }
                    trk.Selected = true;
                    if (track.Items.Count == 0)
                    {
                        lastTrack = trk;
                    }
                }

                foreach (ReaperItem item in track.Items)
                {
                    if (item == null || item.Source == null)
                    {
                        continue;
                    }

                    Media media = UltraPasteCommon.Vegas.GetValidMedia(item.Source.FilePath);

                    if (media != null && item.Source is ReaperSourceSection)
                    {
                        ReaperSourceSection section = item.Source as ReaperSourceSection;
                        bool reverse = section.Mode > 0;
                        Timecode clipStart = Timecode.FromSeconds(section.StartPos), clipLength = section.Mode == 3 ? media.Length : Timecode.FromSeconds(section.Length);
                        foreach (Media m in UltraPasteCommon.Vegas.Project.MediaPool)
                        {
                            if (m.IsSubclip())
                            {
                                Subclip clip = m as Subclip;
                                if (clip.ParentMedia == media && Math.Abs(clip.Start.ToMilliseconds() - clipStart.ToMilliseconds()) < 0.01 && Math.Abs(clip.Length.ToMilliseconds() - clipLength.ToMilliseconds()) < 0.01 && (clip.IsReversed == reverse))
                                {
                                    media = clip;
                                    break;
                                }
                            }
                        }
                        if (!media.IsSubclip())
                        {
                            media = new Subclip(UltraPasteCommon.Vegas.Project, section.FilePath, clipStart, clipLength, reverse, null);
                        }
                    }

                    List<AudioEvent> evs = UltraPasteCommon.Vegas.Project.GenerateEvents<AudioEvent>(media, start + Timecode.FromSeconds(item.Position), Timecode.FromSeconds(item.Length), false, lastTrack == null ? 0 : (lastTrack.Index + 1));
                    l.AddRange(evs);
                    foreach (AudioEvent ev in evs)
                    {
                        ev.Loop = item.Loop;
                        ev.Selected = item.Selected;
                        ev.SnapOffset = Timecode.FromSeconds(item.SnapOffs);
                        ev.Mute = item.Mute?.Length > 0 && item.Mute[0] != 0;
                        if (ev.ActiveTake != null)
                        {
                            ev.ActiveTake.Offset = Timecode.FromSeconds(item.SOffs);
                        }
                        if (item.PlayRate?.Length > 0)
                        {
                            ev.AdjustPlaybackRate(item.PlayRate[0], true);
                        }

                        ev.Channels = item.ChanMode == 1 ? ChannelRemapping.Swap : item.ChanMode == 2 ? ChannelRemapping.Mono : item.ChanMode == 3 ? ChannelRemapping.DisableRight : item.ChanMode == 4 ? ChannelRemapping.DisableLeft : ChannelRemapping.None;
                        if (item.FadeIn?.Length > 1)
                        {
                            ev.FadeIn.Length = Timecode.FromSeconds(item.FadeIn[1]);
                            if (item.FadeIn?.Length > 3)
                            {
                                ev.FadeIn.Curve = item.FadeIn[3] == 0 ? CurveType.Linear : (item.FadeIn[3] == 2 || item.FadeIn[3] == 4) ? CurveType.Slow : item.FadeIn[3] == 5 ? CurveType.Smooth : item.FadeIn[3] == 6 ? CurveType.Sharp : CurveType.Fast;
                            }
                        }
                        if (item.FadeOut?.Length > 1)
                        {
                            ev.FadeOut.Length = Timecode.FromSeconds(item.FadeOut[1]);
                            if (item.FadeOut?.Length > 3)
                            {
                                ev.FadeOut.Curve = item.FadeOut[3] == 0 ? CurveType.Linear : (item.FadeOut[3] == 2 || item.FadeOut[3] == 4) ? CurveType.Fast : item.FadeOut[3] == 5 ? CurveType.Smooth : item.FadeOut[3] == 6 ? CurveType.Sharp : CurveType.Slow;
                            }
                        }

                        if (item.VolPan?.Length > 0)
                        {
                            ev.FadeIn.Gain = (float)item.VolPan[0];
                        }
                        if (item.VolPan?.Length > 2)
                        {
                            ev.Normalize = item.VolPan[2] != 1;
                            if (ev.Normalize)
                            {
                                ev.NormalizeGain = Math.Abs(item.VolPan[2]);
                            }
                            ev.InvertPhase = item.VolPan[2] < 0;
                        }

                        if (elastiqueStretchAttributes != null && timeStretchPitchShift != null)
                        {
                            if (propertyElastiqueAttribute != null && propertyFormantLock != null && propertyPitchSemis != null && propertyMethod != null)
                            {
                                propertyPitchSemis.SetValue(ev, (item.PlayRate?.Length > 2 ? item.PlayRate[2] : 0) + ((item.PlayRate?.Length > 1 && (int)item.PlayRate[1] == 0) ? (Math.Log(ev.PlaybackRate, 2) * 12) : 0));

                                int methodType = item.PlayRate.Length > 3 ? (int)item.PlayRate[3] : 0;

                                switch (methodType / 0x10000)
                                {
                                    case 0x6:
                                    case 0x9:
                                        propertyMethod.SetValue(ev, Enum.Parse(timeStretchPitchShift, "Elastique"));
                                        propertyElastiqueAttribute.SetValue(ev, Enum.Parse(elastiqueStretchAttributes, "Pro"));
                                        propertyFormantLock.SetValue(ev, methodType - (methodType / 8 * 8) != 0);
                                        break;

                                    case 0x7:
                                    case 0xA:
                                        propertyMethod.SetValue(ev, Enum.Parse(timeStretchPitchShift, "Elastique"));
                                        propertyElastiqueAttribute.SetValue(ev, Enum.Parse(elastiqueStretchAttributes, "Efficient"));
                                        break;

                                    case 0x8:
                                    case 0xB:
                                        propertyMethod.SetValue(ev, Enum.Parse(timeStretchPitchShift, "Elastique"));
                                        propertyElastiqueAttribute.SetValue(ev, Enum.Parse(elastiqueStretchAttributes, (methodType - (methodType / 4 * 4)) / 2 == 0 ? "Soloist_Monophonic" : "Soloist_Speech"));
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }


                        if (item.Takes != null)
                        {
                            foreach (ReaperTake t in item.Takes)
                            {
                                Media m = UltraPasteCommon.Vegas.GetValidMedia(t.Source.FilePath);
                                if (m?.HasAudio() != true)
                                {
                                    continue;
                                }
                                AudioStream ms = m.GetAudioStreamByIndex(0);
                                Take tk = ev.AddTake(ms, false);
                                tk.Offset = Timecode.FromSeconds(t.SOffs);
                                if (t.Selected)
                                {
                                    ev.ActiveTake = tk;
                                }
                            }
                        }

                        if (item.StretchSegments.Count > 0)
                        {
                            ReaperStretchSegments segments = new ReaperStretchSegments();
                            segments.AddRange(item.StretchSegments.Where(s => s.OffsetStart < item.Length && s.OffsetEnd > 0));
                            if (segments.Count > 0)
                            {
                                ReaperStretchSegment seg = segments[0];
                                if (seg.OffsetStart < 0)
                                {
                                    seg.VelocityStart = Common.CalculatePointCoordinateInLine(seg.OffsetStart, seg.VelocityStart, seg.OffsetEnd, seg.VelocityEnd, 0);
                                    seg.OffsetStart = 0;
                                }
                                else if (seg.OffsetStart > 0)
                                {
                                    segments.Insert(0, new ReaperStretchSegment() { OffsetStart = 0, OffsetEnd = seg.OffsetStart, VelocityStart = 1, VelocityEnd = 1 });
                                }

                                seg = segments[segments.Count-1];
                                double playbackRateSave = ev.PlaybackRate;
                                double end = item.Length * playbackRateSave;
                                double compareEnd = seg.OffsetEnd - end;

                                if (compareEnd > 0.000001)
                                {
                                    seg.VelocityEnd = Common.CalculatePointCoordinateInLine(seg.OffsetStart, seg.VelocityStart, seg.OffsetEnd, seg.VelocityEnd, end);
                                    seg.OffsetEnd = end;
                                }
                                else if (compareEnd < -0.000001)
                                {
                                    segments.Add(new ReaperStretchSegment() { OffsetStart = seg.OffsetEnd, OffsetEnd = end, VelocityStart = 1, VelocityEnd = 1 });
                                    
                                }

                                if (addVideoStreams)
                                {
                                    UltraPasteCommon.Vegas.Project.AddMissingStreams(ev, out List<VideoEvent> vEvents);
                                    l.AddRange(vEvents);

                                    foreach (VideoEvent vEvent in vEvents)
                                    {
                                        Envelope en = new Envelope(EnvelopeType.Velocity);
                                        vEvent.Envelopes.Add(en);
                                        foreach (ReaperStretchSegment segment in segments)
                                        {
                                            Timecode t = Timecode.FromSeconds(segment.OffsetStart / playbackRateSave);
                                            double value = segment.VelocityStart;
                                            EnvelopePoint point = en.Points.GetPointAtX(t);
                                            if (segment.OffsetStart == 0)
                                            {
                                                point.Y = value;
                                            }
                                            else
                                            {
                                                    if (point != null)
                                                    {
                                                        t += Timecode.FromNanos(1);
                                                    }
                                                    point = en.Points.GetPointAtX(t);
                                                    if (point != null)
                                                    {
                                                        point.Y = value;
                                                    }
                                                    else
                                                    {
                                                        point = new EnvelopePoint(t, value);
                                                        en.Points.Add(point);
                                                    }

                                            }
                                            point.Curve = CurveType.Linear;

                                            t = Timecode.FromSeconds(segment.OffsetEnd / playbackRateSave);

                                            if (t > vEvent.End)
                                            {
                                                continue;
                                            }

                                            value = segment.VelocityEnd;
                                            point = en.Points.GetPointAtX(t);
                                            if (point != null)
                                            {
                                                point.Y = value;
                                            }
                                            else
                                            {
                                                point = new EnvelopePoint(t, value);
                                                en.Points.Add(point);
                                            }
                                            point.Curve = CurveType.Linear;
                                         }
                                    }
                                }

                                TrackEvent current = null;
                                Timecode lastLength = null;

                                List<TrackEvent> splitedEvents = new List<TrackEvent>();
                                foreach (ReaperStretchSegment segment in segments)
                                {
                                    if (current == null)
                                    {
                                        current = ev;

                                    }
                                    else
                                    {
                                        if (current.Length <= lastLength)
                                        {
                                            current.Length = lastLength + Timecode.FromSeconds(1);
                                        }
                                        current = current.Split(lastLength);
                                    }

                                    lastLength = Timecode.FromSeconds(segment.OffsetLength / playbackRateSave);
                                    current.AdjustPlaybackRate(playbackRateSave * segment.VelocityAverage, true);
                                    splitedEvents.Add(current);
                                }
                                current.End = ev.Start + Timecode.FromSeconds(item.Length);
                                Timecode extendTime = Timecode.FromMilliseconds(25);
                                foreach (TrackEvent se in splitedEvents)
                                {
                                    Timecode endSave = se.End;
                                    if (se != ev)
                                    {
                                        se.Start -= extendTime;
                                        l.Add(se);
                                    }
                                    se.End = endSave;
                                    if (se != current)
                                    {
                                        se.End += extendTime;
                                    }
                                }
                            }
                        }

                        List<ReaperEnvelope> envs = new List<ReaperEnvelope>();
                        foreach (ReaperEnvelope env in track.Envelopes)
                        {
                            if (env != null && env.SegRange != null)
                            {
                                envs.Add(env);
                            }
                        }
                        envs.AddRange(item.Envelopes);
                        foreach (ReaperEnvelope env in envs)
                        {
                            if (env.Act[0] == 0)
                            {
                                continue;
                            }
                            Envelope en = null;
                            switch (env.Type)
                            {
                                case "VOLENV":
                                case "VOLENV2":
                                    en = ev.Track.Envelopes.FindByType(EnvelopeType.Volume) ?? new Envelope(EnvelopeType.Volume);
                                    break;

                                // note that item pan envelopes ("PANENV", but not "PANENV2") in Reaper represent the actual pan taken from the source,
                                // and they're not really supposed to be converted into track pan envelopes...
                                // until there's a better way, we have to replace them with track pan envelopes
                                case "PANENV":
                                case "PANENV2":
                                    en = ev.Track.Envelopes.FindByType(EnvelopeType.Pan) ?? new Envelope(EnvelopeType.Pan);
                                    break;

                                case "MUTEENV":
                                    en = ev.Track.Envelopes.FindByType(EnvelopeType.Mute) ?? new Envelope(EnvelopeType.Mute);
                                    break;

                                default:
                                    continue;
                            }
                            if (!ev.Track.Envelopes.Contains(en))
                            {
                                ev.Track.Envelopes.Add(en);
                            }

                            EnvelopePoint pointPrev = en.Points[0];
                            List<EnvelopePoint> pointsNext = new List<EnvelopePoint>();
                            foreach (EnvelopePoint point in en.Points)
                            {
                                if (point.X < ev.Start)
                                {
                                    pointPrev = point;
                                }
                                else
                                {
                                    pointsNext.Add(point);
                                    if (point.X >= ev.End)
                                    {
                                        break;
                                    }
                                }
                            }
                            if (en.Type != EnvelopeType.Mute && (pointsNext.Count > 1 || (pointsNext.Count == 1 && pointPrev.Curve != CurveType.None && pointsNext[0].Y != pointPrev.Y)))
                            {
                                // unless there's a suitable way to handle both track envelope and item envelope, I'm not going to mix the two...
                                continue;
                            }
                            pointPrev.Curve = CurveType.None;
                            Timecode pointEndTime = ev.End;
                            if (env.SegRange == null)
                            {
                                pointEndTime -= Timecode.FromNanos(1);
                            }
                            EnvelopePoint pointStart = en.Points.GetPointAtX(ev.Start), pointEnd = en.Points.GetPointAtX(pointEndTime);
                            if (pointStart == null)
                            {
                                pointStart = new EnvelopePoint(ev.Start, en.ValueAt(ev.Start));
                                en.Points.Add(pointStart);
                            }
                            pointStart.Curve = CurveType.None;
                            if (pointEnd == null)
                            {
                                pointEnd = new EnvelopePoint(pointEndTime, en.ValueAt(pointEndTime));
                                en.Points.Add(pointEnd);
                            }
                            pointEnd.Curve = CurveType.None;

                            EnvelopePoint pointLast = null;
                            foreach (double[] p in env.Points)
                            {
                                Timecode t = Timecode.FromSeconds(p[0]) + ev.Start;
                                if (env.SegRange != null)
                                {
                                    t -= Timecode.FromSeconds(env.SegRange[0]);
                                    if (p == env.Points[env.Points.Count - 1])
                                    {
                                        t -= Timecode.FromNanos(1);
                                    }
                                }
                                CurveType curve = p[2] == 0 ? CurveType.Linear : p[2] == 1 ? CurveType.None : p[2] == 3 ? CurveType.Fast : p[2] == 4 ? CurveType.Slow : CurveType.Smooth;
                                EnvelopePoint point = en.Points.GetPointAtX(t);
                                double value = p[1];
                                if (env.SegRange == null)
                                {
                                    if (en.Type == EnvelopeType.Volume || en.Type == EnvelopeType.Mute)
                                    {
                                        value *= pointPrev.Y;
                                    }
                                    else if (en.Type == EnvelopeType.Pan)
                                    {
                                        value = (1 - Math.Sqrt((1 - Math.Abs(pointPrev.Y)) * (1 - Math.Abs(value)))) * (pointPrev.Y + value > 0 ? 1 : pointPrev.Y + value < 0 ? -1 : 0);
                                    }
                                }

                                if (point != null)
                                {
                                    point.Y = value;
                                }
                                else
                                {
                                    point = new EnvelopePoint(t, value);
                                    en.Points.Add(point);
                                }
                                point.Curve = curve;
                                pointLast = point;
                                if (p == env.Points[0] && point != pointStart)
                                {
                                    pointStart.Y = point.Y;
                                }
                            }
                            pointLast.Curve = CurveType.None;
                        }
                        if (lastTrack == null || ev.Track.Index > lastTrack.Index)
                        {
                            lastTrack = ev.Track;
                        }
                    }

                    if (addVideoStreams)
                    {
                        l.AddRange(UltraPasteCommon.Vegas.Project.AddMissingStreams(evs, MediaType.Video));
                    }
                }
                lastTrack.Selected = false;
            }

            foreach (TrackEvent ev in l)
            {
                ev.Track.Selected = true;
            }
            return l;
        }

        public class ReaperTrack
        {
            public List<ReaperItem> Items { get; set; }
            public string Name { get; set; }
            public double[] VolPan { get; set; }
            public int[] MuteSolo { get; set; }
            public bool IPhase { get; set; }
            public List<ReaperEnvelope> Envelopes { get; set; }

            public ReaperTrack()
            {
                Items = new List<ReaperItem>();
                VolPan = new double[] { 1, 0, -1, -1, 1 };
                MuteSolo = new int[] { 0, 0, 0 };
                Envelopes = new List<ReaperEnvelope>();
            }
        }

        public class ReaperEnvelope
        {
            public string Type { get; set; }
            public int[] Act { get; set; }
            public double[] SegRange { get; set; }
            public List<double[]> Points { get; set; }
            public ReaperEnvelope()
            {
                Act = new int[] { 1, -1 };
                SegRange = null;
                Points = new List<double[]>();
            }
        }


        public class ReaperItem : ReaperTake
        {
            public double Position { get; set; }
            public double SnapOffs { get; set; }
            public double Length { get; set; }
            public bool Loop { get; set; }
            public bool AllTakes { get; set; }
            public double[] FadeIn { get; set; }
            public double[] FadeOut { get; set; }
            public int[] Mute { get; set; }
            public bool Sel { get; set; }
            public List<ReaperEnvelope> Envelopes { get; set; }
            public List<ReaperTake> Takes { get; set; }

            public ReaperItem()
            {
                FadeIn = new double[7];
                FadeOut = new double[7];
                Mute = new int[2];
                Envelopes = new List<ReaperEnvelope>();
                Takes = new List<ReaperTake>();
            }
        }

        public class ReaperTake
        {
            public bool Selected { get; set; }
            public string Name { get; set; }
            public double[] VolPan { get; set; }
            public double SOffs { get; set; }
            public double[] PlayRate { get; set; }
            public int ChanMode { get; set; }
            public ReaperStretchSegments StretchSegments { get; set; }
            public ReaperSource Source { get; set; }

            public ReaperTake()
            {
                Selected = false;
                VolPan = new double[] { 1, 0, 1, -1 };
                PlayRate = new double[] { 1, 1, 0, -1, 0, 0.0025 };
                StretchSegments = new ReaperStretchSegments();
                Source = new ReaperSource();
            }
        }

        public class ReaperSource
        {
            public string Type { get; set; }
            private string filePath;
            private string filePathFull;
            public string FilePath
            {
                get
                {
                    return File.Exists(filePathFull) ? filePathFull : filePath;
                }
                set
                {
                    filePath = value;
                }
            }

            public void UpdateFullFilePath(string folder)
            {
                if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(filePath))
                {
                    return;
                }
                string path = Path.Combine(folder, filePath);
                if (File.Exists(path))
                {
                    filePathFull = path;
                }
            }
        }

        public class ReaperSourceSection : ReaperSource
        {
            public double Length { get; set; }
            public int Mode { get; set; }
            public double StartPos { get; set; }
            public double Overlap { get; set; }
            public ReaperSource Source { get; set; }
        }

        public class ReaperStretchSegments : List<ReaperStretchSegment>
        {
            public static ReaperStretchSegments GetFromMarkers(List<ReaperStretchMarker> markers)
            {
                ReaperStretchSegments segments = new ReaperStretchSegments();
                if (markers == null || markers.Count < 2)
                {
                    return segments;
                }
                ReaperStretchSegment currentSegment = null;
                ReaperStretchMarker lastMarker = null;
                foreach (ReaperStretchMarker marker in markers)
                {
                    if (currentSegment != null)
                    {
                        currentSegment.OffsetEnd = marker.Offset;
                        double velocityAverage = (marker.Position - lastMarker.Position) / currentSegment.OffsetLength; // (v_Start + v_End) / 2
                        double velocityHalf = lastMarker.VelocityChange * velocityAverage; // (v_End - v_Start) / (v_Start + v_End) * (v_Start + v_End) / 2 = (v_End - v_Start) / 2
                        currentSegment.VelocityStart = velocityAverage - velocityHalf;
                        currentSegment.VelocityEnd = velocityAverage + velocityHalf;
                        segments.Add(currentSegment);
                    }

                    currentSegment = new ReaperStretchSegment { OffsetStart = marker.Offset };
                    lastMarker = marker;
                }
                return segments;
            }
        }

        public class ReaperStretchSegment
        {
            public double OffsetStart { get; set; }
            public double OffsetEnd { get; set; }
            public double OffsetLength { get { return OffsetEnd - OffsetStart; } }
            public double VelocityStart { get; set; }
            public double VelocityEnd { get; set; }
            public double VelocityAverage { get { return (VelocityStart + VelocityEnd) / 2; } }
        }

        public class ReaperStretchMarker
        {
            public double Offset { get; set; }
            public double Position { get; set; }
            public double VelocityChange { get; set; } // (v_End - v_Start) / (v_Start + v_End)

            public ReaperStretchMarker(double offset, double position, double velocityChange)
            {
                Offset = offset;
                Position = position;
                VelocityChange = velocityChange;
            }
        }

        public static class Parser
        {
            public static string[] EnvelopeStrings = new string[] { "ENVSEG", "VOLENV", "VOLENV2", "PANENV", "PANENV2", "MUTEENV" };
            public static ReaperData Parse(string filePath)
            {
                ReaperData data = Parse(File.ReadAllBytes(filePath));
                if (data != null)
                {
                    data.ProjectFilePath = filePath;
                }
                return data;
            }

            public static ReaperData Parse(byte[] bytes)
            {
                List<string> lines = SplitLines(bytes);
                ReaperBlock masterBlock = new ReaperBlock(lines);
                ReaperData data = ParseFromBlock<ReaperData>(masterBlock);
                return data;
            }

            private static List<string> SplitLines(byte[] data)
            {
                List<string> lines = new List<string>();
                int start = 0;
                for (int i = 0; i < data.Length; i++)
                {
                    if (data[i] == 0x00)
                    {
                        if (i > start)
                        {
                            lines.Add(Encoding.UTF8.GetString(data, start, i - start).Trim());
                        }
                        start = i + 1;
                    }
                    else if (data[i] == 0x0D && i + 1 < data.Length && data[i + 1] == 0x0A)
                    {
                        if (i > start)
                        {
                            lines.Add(Encoding.UTF8.GetString(data, start, i - start).Trim());
                        }
                        start = i + 2;
                    }
                }
                if (start < data.Length)
                {
                    lines.Add(Encoding.UTF8.GetString(data, start, data.Length - start).Trim());
                }
                return lines;
            }
            private static T ParseFromBlock<T>(ReaperBlock block) where T : new()
            {
                object obj = null;
                if (typeof(T) == typeof(ReaperData))
                {
                    obj = ParseFromBlock(block, null);
                }
                else if (typeof(T) == typeof(ReaperTrack))
                {
                    obj = ParseFromBlock(block, "TRACK");
                }
                else if (typeof(T) == typeof(ReaperEnvelope))
                {
                    foreach (string str in EnvelopeStrings)
                    {
                        obj = ParseFromBlock(block, str);
                        if (obj != null)
                        {
                            break;
                        }
                    }
                }
                else if (typeof(T) == typeof(ReaperItem))
                {
                    obj = ParseFromBlock(block, "ITEM");
                }
                else if (typeof(T) == typeof(ReaperSource))
                {
                    obj = ParseFromBlock(block, "SOURCE");
                }
                return obj is T t ? t : default;
            }

            private static object ParseFromBlock(ReaperBlock block, string type)
            {
                object obj = null;
                if (block == null || (block.Type == null && block.Level > 0) || (type != null && (block.Type != type.ToUpper())))
                {
                    return obj;
                }
                else if (type == null)
                {
                    ReaperData data = new ReaperData();
                    ReaperTrack currentTrack = null;
                    foreach (ReaperBlock child in block.Children)
                    {
                        ReaperTrack track = ParseFromBlock<ReaperTrack>(child);
                        if (track != null)
                        {
                            data.IsTrackData = true;
                            data.Tracks.Add(track);
                        }
                        else
                        {
                            ReaperItem item = ParseFromBlock<ReaperItem>(child);
                            if (item != null)
                            {
                                if (currentTrack == null)
                                {
                                    currentTrack = new ReaperTrack();
                                    data.Tracks.Add(currentTrack);
                                }
                                currentTrack.Items.Add(item);
                            }
                            else if (child.Type == "ENVSEG")
                            {
                                ReaperEnvelope env = ParseFromBlock<ReaperEnvelope>(child);
                                if (env != null)
                                {
                                    currentTrack.Envelopes.Add(env);
                                }
                            }
                            else if (child.Lines?.Count > 0 && child.Lines[0]?.Split(' ')[0].ToUpper() == "TRACKSKIP")
                            {
                                currentTrack = new ReaperTrack();
                                data.Tracks.Add(currentTrack);
                            }
                        }
                    }
                    if (data.Tracks.Count == 0)
                    {
                        foreach (ReaperBlock child in block.Children)
                        {
                            data = ParseFromBlock<ReaperData>(child);
                            if (data?.Tracks.Count > 0)
                            {
                                break;
                            }
                        }
                    }
                    obj = data;
                    return obj;
                }
                else if (((IList<string>)EnvelopeStrings).Contains(block.Type))
                {
                    ReaperEnvelope env = new ReaperEnvelope { Type = block.Type };
                    foreach (string line in block.Lines)
                    {
                        string[] tokens = line.Split(' ');
                        if (tokens.Length == 0)
                        {
                            continue;
                        }
                        switch (tokens[0].ToUpper())
                        {
                            case "<ENVSEG":
                                env.Type = tokens[1];
                                break;
                            case "ACT":
                                env.Act = ParseIntArray(tokens);
                                break;
                            case "SEG_RANGE":
                                env.SegRange = ParseDoubleArray(tokens);
                                break;
                            case "PT":
                                env.Points.Add(ParseDoubleArray(tokens));
                                break;
                            default:
                                break;
                        }
                    }
                    obj = env;
                    return obj;
                }

                switch (block.Type)
                {
                    case "TRACK":
                        {
                            ReaperTrack track = new ReaperTrack();
                            foreach (string line in block.Lines)
                            {
                                string[] tokens = line.Split(' ');
                                if (tokens.Length == 0)
                                {
                                    continue;
                                }
                                switch (tokens[0].ToUpper())
                                {
                                    case "NAME":
                                        track.Name = ParsePathString(tokens);
                                        break;
                                    case "VOLPAN":
                                        track.VolPan = ParseDoubleArray(tokens);
                                        break;
                                    case "MUTESOLO":
                                        track.MuteSolo = ParseIntArray(tokens);
                                        break;
                                    case "IPHASE":
                                        track.IPhase = double.Parse(tokens[1]) != 0;
                                        break;
                                    default:
                                        break;
                                }
                            }
                            foreach (ReaperBlock child in block.Children)
                            {
                                ReaperItem item = ParseFromBlock<ReaperItem>(child);
                                if (item != null)
                                {
                                    track.Items.Add(item);
                                }
                                else
                                {
                                    ReaperEnvelope env = ParseFromBlock<ReaperEnvelope>(child);
                                    if (env != null)
                                    {
                                        track.Envelopes.Add(env);
                                    }
                                }
                            }
                            obj = track;
                            break;
                        }

                    case "ITEM":
                        {
                            ReaperItem item = new ReaperItem();
                            ReaperTake currentTake = item;
                            List<ReaperStretchMarker> stretchMarkers = new List<ReaperStretchMarker>();
                            foreach (string line in block.Lines)
                            {
                                string[] tokens = line.Split(' ');
                                if (tokens.Length == 0)
                                {
                                    continue;
                                }
                                switch (tokens[0].ToUpper())
                                {
                                    case "POSITION":
                                        item.Position = double.Parse(tokens[1]);
                                        break;
                                    case "SNAPOFFS":
                                        item.SnapOffs = double.Parse(tokens[1]);
                                        break;
                                    case "LENGTH":
                                        item.Length = double.Parse(tokens[1]);
                                        break;
                                    case "LOOP":
                                        item.Loop = int.Parse(tokens[1]) != 0;
                                        break;
                                    case "ALLTAKES":
                                        item.AllTakes = int.Parse(tokens[1]) != 0;
                                        break;
                                    case "FADEIN":
                                        item.FadeIn = ParseDoubleArray(tokens);
                                        break;
                                    case "FADEOUT":
                                        item.FadeOut = ParseDoubleArray(tokens);
                                        break;
                                    case "MUTE":
                                        item.Mute = ParseIntArray(tokens);
                                        break;
                                    case "SEL":
                                        item.Selected = int.Parse(tokens[1]) != 0;
                                        break;
                                    case "SM":
                                        foreach (double[] arr in ParseDoubleArrayWithSeparator(tokens, "+"))
                                        {
                                            stretchMarkers.Add(new ReaperStretchMarker(arr[0], arr[1], arr.Length > 2 ? arr[2] : 0));
                                        }
                                        break;
                                    case "TAKE":
                                        currentTake = new ReaperTake();
                                        item.Takes.Add(currentTake);
                                        if (tokens.Length > 1 && tokens[1].ToUpper() == "SEL")
                                        {
                                            currentTake.Selected = true;
                                        }
                                        break;
                                    case "NAME":
                                        currentTake.Name = ParsePathString(tokens);
                                        break;
                                    case "VOLPAN":
                                        currentTake.VolPan = ParseDoubleArray(tokens);
                                        break;
                                    case "SOFFS":
                                        currentTake.SOffs = double.Parse(tokens[1], CultureInfo.InvariantCulture);
                                        break;
                                    case "PLAYRATE":
                                        currentTake.PlayRate = ParseDoubleArray(tokens);
                                        break;
                                    case "CHANMODE":
                                        currentTake.ChanMode = int.Parse(tokens[1]);
                                        break;
                                    default:
                                        break;
                                }
                            }
                            item.StretchSegments = ReaperStretchSegments.GetFromMarkers(stretchMarkers);
                            currentTake = item;
                            int takeNum = -1;
                            foreach (ReaperBlock child in block.Children)
                            {
                                ReaperSource source = ParseFromBlock<ReaperSource>(child);
                                if (source != null)
                                {
                                    currentTake.Source = source;
                                    takeNum += 1;
                                    if (takeNum < item.Takes.Count)
                                    {
                                        currentTake = item.Takes[takeNum];
                                    }
                                }
                                else
                                {
                                    ReaperEnvelope env = ParseFromBlock<ReaperEnvelope>(child);
                                    if (env != null)
                                    {
                                        item.Envelopes.Add(env);
                                    }
                                }
                            }
                            obj = item;
                            break;
                        }

                    case "SOURCE":
                        {
                            ReaperSource source = new ReaperSource();
                            foreach (string line in block.Lines)
                            {
                                string[] tokens = line.Split(' ');
                                if (tokens.Length == 0)
                                {
                                    continue;
                                }
                                switch (tokens[0].ToUpper())
                                {
                                    case "<SOURCE":
                                        if (tokens[1].ToUpper() == "SECTION")
                                        {
                                            source = new ReaperSourceSection();
                                        }
                                        source.Type = tokens[1];
                                        break;
                                    case "FILE":
                                        source.FilePath = ParsePathString(tokens);
                                        break;
                                    default:
                                        break;
                                }
                            }

                            if (source is ReaperSourceSection)
                            {
                                ReaperSourceSection section = source as ReaperSourceSection;
                                foreach (string line in block.Lines)
                                {
                                    string[] tokens = line.Split(' ');
                                    if (tokens.Length == 0)
                                    {
                                        continue;
                                    }
                                    switch (tokens[0].ToUpper())
                                    {
                                        case "LENGTH":
                                            section.Length = double.Parse(tokens[1]);
                                            break;
                                        case "MODE":
                                            section.Mode = int.Parse(tokens[1]);
                                            break;
                                        case "STARTPOS":
                                            section.StartPos = double.Parse(tokens[1]);
                                            break;
                                        case "OVERLAP":
                                            section.Overlap = double.Parse(tokens[1]);
                                            break;
                                        default:
                                            break;
                                    }
                                }
                                foreach (ReaperBlock child in block.Children)
                                {
                                    ReaperSource childSource = ParseFromBlock<ReaperSource>(child);
                                    if (childSource != null)
                                    {
                                        section.Source = childSource;
                                        section.FilePath = childSource.FilePath;
                                        break;
                                    }
                                }
                                source = section;
                            }
                            obj = source;
                            break;
                        }

                    default:
                        break;
                }

                return obj;
            }

            private static double[] ParseDoubleArray(string[] tokens)
            {
                double[] values = new double[tokens.Length - 1];
                for (int i = 1; i < tokens.Length; i++)
                {
                    values[i - 1] = double.Parse(tokens[i], CultureInfo.InvariantCulture);
                }
                return values;
            }

            private static List<double[]> ParseDoubleArrayWithSeparator(string[] tokens, string separator)
            {
                List<double[]> l = new List<double[]>();
                List<double> doubles = new List<double>();
                for (int i = 1; i < tokens.Length; i++)
                {
                    if (tokens[i] != separator)
                    {
                        doubles.Add(double.Parse(tokens[i], CultureInfo.InvariantCulture));
                    }
                    else if(doubles.Count > 0)
                    {
                        l.Add(doubles.ToArray());
                        doubles = new List<double>();
                    }
                }
                if (doubles.Count > 0)
                {
                    l.Add(doubles.ToArray());
                }
                return l;
            }

            private static int[] ParseIntArray(string[] tokens)
            {
                int[] values = new int[tokens.Length - 1];
                for (int i = 1; i < tokens.Length; i++)
                {
                    values[i - 1] = int.Parse(tokens[i]);
                }
                return values;
            }

            private static string ParsePathString(string[] tokens)
            {
                string str = "";
                for (int i = 1; i < tokens.Length; i++)
                {
                    str += ' ' + tokens[i];
                    if (tokens[i][tokens[i].Length - 1] == '\"')
                    {
                        break;
                    }
                }
                return str.Trim(' ').Trim('\"');
            }

            public static string SerializeToString(ReaperData rd)
            {
                byte[] bytes = SerializeToBytes(rd);
                for (int i = 0; i < bytes.Length; i++)
                {
                    if (bytes[i] == 0x00)
                    {
                        bytes[i] = 0x20;
                    }
                }
                return Encoding.UTF8.GetString(bytes);
            }

            public static byte[] SerializeToBytes(ReaperData rd)
            {
                List<byte[]> tokens = CollectTokens(rd);
                return JoinTokensWithNullSeparator(tokens);
            }

            private static List<byte[]> CollectTokens(ReaperData data)
            {
                List<byte[]> tokens = new List<byte[]>();
                foreach (ReaperTrack track in data.Tracks)
                {
                    if (data.IsTrackData)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes("<TRACK"));
                        AddPropertyTokens(tokens, "NAME", track.Name);
                        AddPropertyTokens(tokens, "VOLPAN", track.VolPan);
                        AddPropertyTokens(tokens, "MUTESOLO", track.MuteSolo);
                        AddPropertyTokens(tokens, "IPHASE", track.IPhase);
                    }
                    foreach (ReaperItem item in track.Items)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes("<ITEM"));
                        AddPropertyTokens(tokens, "POSITION", item.Position);
                        AddPropertyTokens(tokens, "SNAPOFFS", item.SnapOffs);
                        AddPropertyTokens(tokens, "LENGTH", item.Length);
                        AddPropertyTokens(tokens, "LOOP", item.Loop);
                        AddPropertyTokens(tokens, "ALLTAKES", item.AllTakes);
                        AddPropertyTokens(tokens, "FADEIN", item.FadeIn);
                        AddPropertyTokens(tokens, "FADEOUT", item.FadeOut);
                        AddPropertyTokens(tokens, "MUTE", item.Mute);
                        AddPropertyTokens(tokens, "SEL", item.Sel);
                        CollectTakeTokens(item, tokens);

                        if (item.Takes != null)
                        {
                            foreach (ReaperTake t in item.Takes)
                            {
                                CollectTakeTokens(t, tokens);
                            }
                        }
                        foreach (ReaperEnvelope env in item.Envelopes)
                        {
                            tokens.Add(Encoding.UTF8.GetBytes(string.Format("<{0}", env.Type)));
                            foreach (double[] p in env.Points)
                            {
                                AddPropertyTokens(tokens, "PT", p);
                            }
                            tokens.Add(Encoding.UTF8.GetBytes(">"));
                        }
                        tokens.Add(Encoding.UTF8.GetBytes(">"));
                    }
                    foreach (ReaperEnvelope env in track.Envelopes)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes(string.Format(data.IsTrackData ? "<{0}" : "<ENVSEG {0}", env.Type)));
                        if (!data.IsTrackData && env.SegRange != null)
                        {
                            AddPropertyTokens(tokens, "SEG_RANGE", env.SegRange);
                        }
                        foreach (double[] p in env.Points)
                        {
                            AddPropertyTokens(tokens, "PT", p);
                        }
                        tokens.Add(Encoding.UTF8.GetBytes(">"));
                    }
                    if (data.IsTrackData)
                    {
                        tokens.Add(Encoding.UTF8.GetBytes(">"));
                    }
                    else /* if (track != data.Tracks[data.Tracks.Count - 1]) */
                    {
                        AddPropertyTokens(tokens, "TRACKSKIP", new int[] { 1, 1 });
                    }
                }

                return tokens;
            }

            private static List<byte[]> CollectTakeTokens<T>(T t, List<byte[]> tokens = null) where T : ReaperTake
            {
                if (tokens == null)
                {
                    tokens = new List<byte[]>();
                }
                if (t == null)
                {
                    return tokens;
                }
                if (!(t is ReaperItem))
                {
                    AddPropertyTokens(tokens, "TAKE", t.Selected ? "SEL" : null);
                }
                AddPropertyTokens(tokens, "NAME", t.Name, true);
                AddPropertyTokens(tokens, t is ReaperItem ? "VOLPAN" : "TAKEVOLPAN", t.VolPan);
                AddPropertyTokens(tokens, "SOFFS", t.SOffs);
                AddPropertyTokens(tokens, "PLAYRATE", t.PlayRate);
                AddPropertyTokens(tokens, "CHANMODE", t.ChanMode);
                if (t.Source != null)
                {
                    AddPropertyTokens(tokens, "<SOURCE", t.Source.Type);
                    AddPropertyTokens(tokens, "FILE", t.Source.FilePath, true);
                    tokens.Add(Encoding.UTF8.GetBytes(">"));
                }
                return tokens;
            }

            private static void AddPropertyTokens<T>(List<byte[]> tokens, string key, T value, bool isQuoted = false)
            {
                if (value == null) return;
                string valueStr = ConvertValueToString(value, isQuoted);
                tokens.Add(Encoding.UTF8.GetBytes(string.Format("{0} {1}", key, valueStr)));
            }

            private static string ConvertValueToString<T>(T value, bool isQuoted)
            {
                if (value is Array)
                {
                    Array array = value as Array;
                    List<string> elements = new List<string>();
                    foreach (object item in array)
                    {
                        elements.Add(Convert.ToString(item, CultureInfo.InvariantCulture));
                    }
                    return string.Join(" ", elements);
                }
                else if (value is bool?)
                {
                    return (value as bool?) == true ? "1" : "0";
                }
                else if (isQuoted)
                {
                    return string.Format("\"{0}\"", value);
                }
                return value.ToString();
            }

            private static byte[] JoinTokensWithNullSeparator(List<byte[]> tokens)
            {
                byte[] separator = new byte[] { 0x00 };
                using (MemoryStream ms = new MemoryStream())
                {
                    foreach (byte[] token in tokens)
                    {
                        ms.Write(token, 0, token.Length);
                        ms.Write(separator, 0, 1);
                    }
                    return ms.ToArray();
                }
            }
        }
    }
}