#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;

using ReaperDataParser;

namespace UltraPaste.Utilities
{
    internal static class ReaperDataHelper
    {
        internal static readonly Assembly VegasAssembly = typeof(Vegas).Assembly;
        internal static readonly Type Type_ElastiqueStretchAttributes = VegasAssembly.GetType(string.Format("{0}.ElastiqueStretchAttributes", typeof(Vegas).Namespace));
        internal static readonly Type Type_TimeStretchPitchShift = VegasAssembly.GetType(string.Format("{0}.TimeStretchPitchShift", typeof(Vegas).Namespace));
        internal static readonly PropertyInfo Property_ElastiqueAttribute = Type_ElastiqueStretchAttributes != null ? typeof(AudioEvent).GetProperty("ElastiqueAttribute", Type_ElastiqueStretchAttributes) : null;
        internal static readonly PropertyInfo Property_PitchSemis = typeof(AudioEvent).GetProperty("PitchSemis", typeof(double));
        internal static readonly PropertyInfo Property_Method = Type_TimeStretchPitchShift != null ? typeof(AudioEvent).GetProperty("Method", Type_TimeStretchPitchShift) : null;
        internal static readonly PropertyInfo Property_FormantLock = typeof(AudioEvent).GetProperty("FormantLock", typeof(bool));

        public static ReaperData ConvertToReaperData(IEnumerable<Track> tracks)
        {
            List<TrackEvent> evs = new List<TrackEvent>();
            foreach (Track track in tracks)
            {
                evs.AddRange(track.Events);
            }
            ReaperData data = ConvertToReaperData(evs);
            data.IsTrackData = true;
            return data;
        }

        public static ReaperData ConvertToReaperData(IEnumerable<TrackEvent> evs)
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
                                env.Points.Add(new double[] { p.X.ToMilliseconds() * 1000, p.Y, p.Curve == CurveType.None ? 1 : p.Curve == CurveType.Smooth ? 2 : p.Curve == CurveType.Fast ? 3 : p.Curve == CurveType.Slow ? 4 : 0, 0, 0, 0, 0, p.X.ToMilliseconds() / 500 });
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
                    if (Type_ElastiqueStretchAttributes != null && Type_TimeStretchPitchShift != null)
                    {
                        if (Property_ElastiqueAttribute != null && Property_PitchSemis != null && Property_Method != null)
                        {
                            pitch = (double)Property_PitchSemis.GetValue(aEvent);
                            if (Property_Method.GetValue(aEvent) == Enum.Parse(Type_TimeStretchPitchShift, "Elastique"))
                            {
                                object obj = Property_ElastiqueAttribute.GetValue(aEvent);
                                if (obj == Enum.Parse(Type_ElastiqueStretchAttributes, "Pro"))
                                {
                                    methodType += 0x90000;
                                }
                                else if (obj == Enum.Parse(Type_ElastiqueStretchAttributes, "Efficient"))
                                {
                                    methodType += 0xA0000;
                                }
                                else if (obj == Enum.Parse(Type_ElastiqueStretchAttributes, "Soloist_Monophonic"))
                                {
                                    methodType += 0xB0000;
                                }
                                else if (obj == Enum.Parse(Type_ElastiqueStretchAttributes, "Soloist_Speech"))
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
                        switch (Path.GetExtension(tk.MediaPath).ToLowerInvariant())
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
    }
}
