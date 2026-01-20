#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using ReaperDataParser;


namespace UltraPaste.ExtensionMethods
{
    using Utilities;
    using static Utilities.ReaperDataHelper;
    internal static class ReaperDataExtensions
    {
        public static List<TrackEvent> GenerateEventsToVegas(this ReaperData data, Timecode start, bool closeGap = true, bool addVideoStreams = true)
        {
            List<TrackEvent> l = new List<TrackEvent>();
            if (data.IsTrackData)
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
            foreach (ReaperTrack track in data.Tracks)
            {
                if (data.IsTrackData)
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
                            if (data.IsTrackData)
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
                        if (data.IsTrackData && (env.Points[0][0] != 0 || start.Nanos != 0))
                        {
                            en.Points[0].Curve = CurveType.None;
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
                            ev.FadeIn.Gain = item.VolPan[0] < 1 ? (float)item.VolPan[0] : 1;
                        }
                        if (item.VolPan?.Length > 2)
                        {
                            double normalize = item.VolPan[0] * (item.VolPan[1] > 1 ? item.VolPan[1] : 1);
                            ev.Normalize = normalize != 1;
                            if (ev.Normalize)
                            {
                                ev.NormalizeGain = Math.Abs(normalize);
                            }
                            ev.InvertPhase = item.VolPan[2] < 0;
                        }

                        if (Type_ElastiqueStretchAttributes != null && Type_TimeStretchPitchShift != null)
                        {
                            if (Property_ElastiqueAttribute != null && Property_FormantLock != null && Property_PitchSemis != null && Property_Method != null)
                            {
                                Property_PitchSemis.SetValue(ev, (item.PlayRate?.Length > 2 ? item.PlayRate[2] : 0) + ((item.PlayRate?.Length > 1 && (int)item.PlayRate[1] == 0) ? (Math.Log(ev.PlaybackRate, 2) * 12) : 0));

                                int methodType = item.PlayRate.Length > 3 ? (int)item.PlayRate[3] : 0;

                                switch (methodType / 0x10000)
                                {
                                    case 0x6:
                                    case 0x9:
                                        Property_Method.SetValue(ev, Enum.Parse(Type_TimeStretchPitchShift, "Elastique"));
                                        Property_ElastiqueAttribute.SetValue(ev, Enum.Parse(Type_ElastiqueStretchAttributes, "Pro"));
                                        Property_FormantLock.SetValue(ev, methodType - (methodType / 8 * 8) != 0);
                                        break;

                                    case 0x7:
                                    case 0xA:
                                        Property_Method.SetValue(ev, Enum.Parse(Type_TimeStretchPitchShift, "Elastique"));
                                        Property_ElastiqueAttribute.SetValue(ev, Enum.Parse(Type_ElastiqueStretchAttributes, "Efficient"));
                                        break;

                                    case 0x8:
                                    case 0xB:
                                        Property_Method.SetValue(ev, Enum.Parse(Type_TimeStretchPitchShift, "Elastique"));
                                        Property_ElastiqueAttribute.SetValue(ev, Enum.Parse(Type_ElastiqueStretchAttributes, (methodType - (methodType / 4 * 4)) / 2 == 0 ? "Soloist_Monophonic" : "Soloist_Speech"));
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
                                    seg.VelocityStart = VegasCommonHelper.CalculatePointCoordinateInLine(seg.OffsetStart, seg.VelocityStart, seg.OffsetEnd, seg.VelocityEnd, 0);
                                    seg.OffsetStart = 0;
                                }
                                else if (seg.OffsetStart > 0)
                                {
                                    segments.Insert(0, new ReaperStretchSegment() { OffsetStart = 0, OffsetEnd = seg.OffsetStart, VelocityStart = 1, VelocityEnd = 1 });
                                }

                                seg = segments[segments.Count - 1];
                                double playbackRateSave = ev.PlaybackRate;
                                double end = item.Length * playbackRateSave;
                                double compareEnd = seg.OffsetEnd - end;

                                if (compareEnd > 0.000001)
                                {
                                    seg.VelocityEnd = VegasCommonHelper.CalculatePointCoordinateInLine(seg.OffsetStart, seg.VelocityStart, seg.OffsetEnd, seg.VelocityEnd, end);
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
    }
}
