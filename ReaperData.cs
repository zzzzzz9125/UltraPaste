#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;

public class ReaperData
{
    public List<ReaperTrack> Tracks { get; set; }
    public bool IsFromTrackData { get; set; }
    private string projectFilePath;
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

    public static ReaperData FromVegasEvents(List<TrackEvent> evs)
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
                Position = new double[] { ev.Start.ToMilliseconds() / 1000, ev.Start.ToMilliseconds() / 500 },
                SnapOffs = new double[] { ev.SnapOffset.ToMilliseconds() / 1000, ev.SnapOffset.ToMilliseconds() / 500 },
                Length = new double[] { ev.Length.ToMilliseconds() / 1000, ev.Length.ToMilliseconds() / 500 },
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
            }
            currentTrack.Items.Add(item);

            if (ev is AudioEvent)
            {
                AudioEvent aEvent = ev as AudioEvent;
                item.VolPan[2] = aEvent.Normalize ? (aEvent.NormalizeGain * (aEvent.InvertPhase ? -1 : 1)) : 1;
                int methodType = 0;
                if (aEvent.Method != TimeStretchPitchShift.Elastique)
                {
                    methodType = -1;
                }
                else
                {
                    switch (aEvent.ElastiqueAttribute)
                    {
                        case ElastiqueStretchAttributes.Pro:
                            methodType += 0x90000;
                            break;

                        case ElastiqueStretchAttributes.Efficient:
                            methodType += 0xA0000;
                            break;

                        case ElastiqueStretchAttributes.Soloist_Monophonic:
                            methodType += 0xB0000;
                            break;

                        case ElastiqueStretchAttributes.Soloist_Speech:
                            methodType += 0xB0002;
                            break;

                        default:
                            methodType = -1;
                            break;
                    }
                }
                item.PlayRate = new double[] { ev.PlaybackRate, 1, aEvent.PitchSemis, methodType, 0, 0.0025 };
                item.ChanMode = aEvent.Channels == ChannelRemapping.Swap ? 1 : aEvent.Channels == ChannelRemapping.Mono ? 2 : aEvent.Channels == ChannelRemapping.DisableRight || aEvent.Channels == ChannelRemapping.MuteRight ? 3 : aEvent.Channels == ChannelRemapping.DisableLeft || aEvent.Channels == ChannelRemapping.MuteLeft ? 4 : 0;
            }

            ReaperTake currentTake = item;
            foreach (Take tk in ev.Takes)
            {
                currentTake.Name = tk.Name;
                currentTake.SOffs = tk.Offset.ToMilliseconds() / 1000 * ev.PlaybackRate;
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

    public List<TrackEvent> GenerateEventsToVegas(Timecode start, bool closeBegin = true)
    {
        List<TrackEvent> l = new List<TrackEvent>();
        if (IsFromTrackData)
        {
            foreach (Track t in UltraPasteCommon.myVegas.Project.Tracks)
            {
                t.Selected = false;
            }
        }

        if (start == null || start < new Timecode(0))
        {
            start = new Timecode(0);
        }

        if (closeBegin)
        {
            Timecode offset = null;
            foreach (ReaperTrack track in Tracks)
            {
                foreach (ReaperItem item in track.Items)
                {
                    Timecode tmp = Timecode.FromSeconds(item.Position[0]);
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
            if (IsFromTrackData)
            {
                AudioTrack trk = new AudioTrack(UltraPasteCommon.myVegas.Project, -1, track.Name);
                UltraPasteCommon.myVegas.Project.Tracks.Add(trk);
                if (track.VolPan != null)
                {
                    if (track.VolPan.Length > 0)
                    {
                        trk.Volume = (float)track.VolPan[0];
                    }
                    if (track.VolPan.Length > 1)
                    {
                        trk.PanX = (float)track.VolPan[1];
                    }
                }
                if (track.MuteSolo != null)
                {
                    trk.Mute = track.MuteSolo.Length > 0 && track.MuteSolo[0] == 1;
                    trk.Solo = track.MuteSolo.Length > 1 && track.MuteSolo[1] == 2;
                }
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
                        if (IsFromTrackData)
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
                    if (IsFromTrackData && (env.Points[0][0] != 0 || start.Nanos != 0))
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

                Media media = UltraPasteCommon.GetValidMedia(item.Source.FilePath);

                if (item.Source is ReaperSourceSection)
                {
                    ReaperSourceSection section = item.Source as ReaperSourceSection;
                    bool reverse = section.Mode > 0;
                    Timecode clipStart = Timecode.FromSeconds(section.StartPos), clipLength = section.Mode == 3 ? media.Length : Timecode.FromSeconds(section.Length);
                    foreach (Media m in UltraPasteCommon.myVegas.Project.MediaPool)
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
                        media = new Subclip(UltraPasteCommon.myVegas.Project, section.FilePath, clipStart, clipLength, reverse, null);
                    }
                }

                List<TrackEvent> evs = UltraPasteCommon.GenerateEvents<TrackEvent>(media, start + Timecode.FromSeconds(item.Position[0]), Timecode.FromSeconds(item.Length[0]), false, lastTrack == null ? 0 : (lastTrack.Index + 1));
                foreach (TrackEvent ev in evs)
                {
                    ev.Loop = item.Loop;
                    ev.Selected = item.Selected;
                    ev.SnapOffset = Timecode.FromSeconds(item.SnapOffs != null && item.SnapOffs.Length > 0 ? item.SnapOffs[0] : 0);
                    ev.Mute = item.Mute != null && item.Mute.Length > 0 && item.Mute[0] != 0;
                    if (ev.ActiveTake != null)
                    {
                        ev.ActiveTake.Offset = Timecode.FromSeconds(item.SOffs);
                    }
                    if (item.PlayRate != null && item.PlayRate.Length > 0)
                    {
                        ev.AdjustPlaybackRate(item.PlayRate[0], true);
                    }
                    if (ev is AudioEvent)
                    {
                        AudioEvent aEvent = ev as AudioEvent;
                        aEvent.Channels = item.ChanMode == 1 ? ChannelRemapping.Swap : item.ChanMode == 2 ? ChannelRemapping.Mono : item.ChanMode == 3 ? ChannelRemapping.DisableRight : item.ChanMode == 4 ? ChannelRemapping.DisableLeft : ChannelRemapping.None;
                        if (item.FadeIn != null && item.FadeIn.Length > 1)
                        {
                            ev.FadeIn.Length = Timecode.FromSeconds(item.FadeIn[1]);
                            if (item.FadeIn.Length > 3)
                            {
                                ev.FadeIn.Curve = item.FadeIn[3] == 0 ? CurveType.Linear : (item.FadeIn[3] == 2 || item.FadeIn[3] == 4) ? CurveType.Slow : item.FadeIn[3] == 5 ? CurveType.Smooth : item.FadeIn[3] == 6 ? CurveType.Sharp : CurveType.Fast;
                            }
                        }
                        if (item.FadeOut != null && item.FadeOut.Length > 1)
                        {
                            ev.FadeOut.Length = Timecode.FromSeconds(item.FadeOut[1]);
                            if (item.FadeOut.Length > 3)
                            {
                                ev.FadeOut.Curve = item.FadeOut[3] == 0 ? CurveType.Linear : (item.FadeOut[3] == 2 || item.FadeOut[3] == 4) ? CurveType.Fast : item.FadeOut[3] == 5 ? CurveType.Smooth : item.FadeOut[3] == 6 ? CurveType.Sharp : CurveType.Slow;
                            }
                        }
                        if (item.VolPan != null)
                        {
                            if (item.VolPan.Length > 0)
                            {
                                ev.FadeIn.Gain = (float)item.VolPan[0];
                            }
                            if (item.VolPan.Length > 2)
                            {
                                aEvent.Normalize = item.VolPan[2] != 1;
                                if (aEvent.Normalize)
                                {
                                    aEvent.NormalizeGain = Math.Abs(item.VolPan[2]);
                                }
                                aEvent.InvertPhase = item.VolPan[2] < 0;
                            }
                        }
                        if (item.PlayRate != null)
                        {
                            aEvent.PitchSemis = item.PlayRate.Length > 2 ? (int)item.PlayRate[2] : 0;
                            if (item.PlayRate.Length > 1 && (int)item.PlayRate[1] == 0)
                            {
                                aEvent.PitchSemis += Math.Log(aEvent.PlaybackRate, 2) * 12;
                            }
                            int methodType = item.PlayRate.Length > 3 ? (int)item.PlayRate[3] : 0;
                            switch (methodType / 0x10000)
                            {
                                case 0x6:
                                case 0x9:
                                    aEvent.Method = TimeStretchPitchShift.Elastique;
                                    aEvent.ElastiqueAttribute = ElastiqueStretchAttributes.Pro;
                                    aEvent.FormantLock = methodType - (methodType / 8 * 8) != 0;
                                    break;

                                case 0x7:
                                case 0xA:
                                    aEvent.Method = TimeStretchPitchShift.Elastique;
                                    aEvent.ElastiqueAttribute = ElastiqueStretchAttributes.Efficient;
                                    break;

                                case 0x8:
                                case 0xB:
                                    aEvent.Method = TimeStretchPitchShift.Elastique;
                                    aEvent.ElastiqueAttribute = (methodType - (methodType / 4 * 4)) / 2 == 0 ? ElastiqueStretchAttributes.Soloist_Monophonic : ElastiqueStretchAttributes.Soloist_Speech;
                                    break;

                                default:
                                    break;
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
                                    en = aEvent.Track.Envelopes.FindByType(EnvelopeType.Volume) ?? new Envelope(EnvelopeType.Volume);
                                    break;

                                // note that item pan envelopes ("PANENV", but not "PANENV2") in Reaper represent the actual pan taken from the source,
                                // and they're not really supposed to be converted into track pan envelopes...
                                // until there's a better way, we have to replace them with track pan envelopes
                                case "PANENV":
                                case "PANENV2":
                                    en = aEvent.Track.Envelopes.FindByType(EnvelopeType.Pan) ?? new Envelope(EnvelopeType.Pan);
                                    break;

                                case "MUTEENV":
                                    en = aEvent.Track.Envelopes.FindByType(EnvelopeType.Mute) ?? new Envelope(EnvelopeType.Mute);
                                    break;

                                default:
                                    continue;
                            }
                            if (!aEvent.Track.Envelopes.Contains(en))
                            {
                                aEvent.Track.Envelopes.Add(en);
                            }

                            EnvelopePoint pointPrev = en.Points[0];
                            List<EnvelopePoint> pointsNext = new List<EnvelopePoint>();
                            foreach (EnvelopePoint point in en.Points)
                            {
                                if (point.X < aEvent.Start)
                                {
                                    pointPrev = point;
                                }
                                else
                                {
                                    pointsNext.Add(point);
                                    if (point.X >= aEvent.End)
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
                            Timecode pointEndTime = aEvent.End;
                            if (env.SegRange == null)
                            {
                                pointEndTime -= Timecode.FromNanos(1);
                            }
                            EnvelopePoint pointStart = en.Points.GetPointAtX(aEvent.Start), pointEnd = en.Points.GetPointAtX(pointEndTime);
                            if (pointStart == null)
                            {
                                pointStart = new EnvelopePoint(aEvent.Start, en.ValueAt(aEvent.Start));
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
                                Timecode t = Timecode.FromSeconds(p[0]) + aEvent.Start;
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
                    }
                    if (item.Takes != null)
                    {
                        foreach (ReaperTake t in item.Takes)
                        {
                            Media m = UltraPasteCommon.GetValidMedia(t.Source.FilePath);
                            if (m == null)
                            {
                                continue;
                            }
                            MediaStream ms = ev is VideoEvent && m.HasVideo() ? (MediaStream)m.GetVideoStreamByIndex(0) : (MediaStream)m.GetAudioStreamByIndex(0);
                            Take tk = ev.AddTake(ms, false);
                            tk.Offset = Timecode.FromSeconds(t.SOffs);
                            if (t.Selected)
                            {
                                ev.ActiveTake = tk;
                            }
                        }
                    }
                    if (lastTrack == null || ev.Track.Index > lastTrack.Index)
                    {
                        lastTrack = ev.Track;
                    }
                }
                l.AddRange(evs);
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


    public class ReaperItem : ReaperTake, IComparable<ReaperItem>
    {
        public double[] Position { get; set; }
        public double[] SnapOffs { get; set; }
        public double[] Length { get; set; }
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
            Position = new double[2];
            SnapOffs = new double[2];
            Length = new double[2];
            FadeIn = new double[7];
            FadeOut = new double[7];
            Mute = new int[2];
            Envelopes = new List<ReaperEnvelope>();
            Takes = new List<ReaperTake>();
        }

        public int CompareTo(ReaperItem other)
        {
            return (this.Position == null || other.Position == null || this.Position.Length == 0 || other.Position.Length == 0 || this.Position[0] == other.Position[0]) ? 0
                    : this.Position[0] > other.Position[0] ? 1 : -1;
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
        public ReaperSource Source { get; set; }

        public ReaperTake()
        {
            Selected = false;
            VolPan = new double[] { 1, 0, 1, -1 };
            PlayRate = new double[] { 1, 1, 0, -1, 0, 0.0025 };
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
                        data.IsFromTrackData = true;
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
                        else if (child.Lines != null && child.Lines.Count > 0 && child.Lines[0] != null && child.Lines[0].Split(' ')[0].ToUpper() == "TRACKSKIP")
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
                        if (data != null && data.Tracks.Count > 0)
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
                        ReaperTake currentTake = (ReaperTake)item;
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
                                    item.Position = ParseDoubleArray(tokens);
                                    break;
                                case "SNAPOFFS":
                                    item.SnapOffs = ParseDoubleArray(tokens);
                                    break;
                                case "LENGTH":
                                    item.Length = ParseDoubleArray(tokens);
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
                        currentTake = item as ReaperTake;
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
                if (data.IsFromTrackData)
                {
                    tokens.Add(Encoding.UTF8.GetBytes("<TRACK"));
                    AddPropertyTokens(tokens, "NAME", track.Name);
                    AddPropertyTokens(tokens, "VOLPAN", track.VolPan);
                    AddPropertyTokens(tokens, "MUTESOLO", track.MuteSolo);
                    AddPropertyTokens(tokens, "IPHASE", track.IPhase);
                }
                foreach (ReaperEnvelope env in track.Envelopes)
                {
                    tokens.Add(Encoding.UTF8.GetBytes(string.Format(data.IsFromTrackData ? "<ENVSEG {0}" : "<{0}", env.Type)));
                    foreach (double[] p in env.Points)
                    {
                        AddPropertyTokens(tokens, "PT", p);
                    }
                    tokens.Add(Encoding.UTF8.GetBytes(">"));
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
                if (data.IsFromTrackData)
                {
                    tokens.Add(Encoding.UTF8.GetBytes("<TRACK"));
                    AddPropertyTokens(tokens, "NAME", track.Name);
                    AddPropertyTokens(tokens, "VOLPAN", track.VolPan);
                    AddPropertyTokens(tokens, "MUTESOLO", track.MuteSolo);
                    AddPropertyTokens(tokens, "IPHASE", track.IPhase);
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
                foreach (var item in array)
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