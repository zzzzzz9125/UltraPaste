#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using CapCutDataParser;
using System.Collections.Generic;

namespace UltraPaste.Utilities
{
    internal static class CapCutDataHelper
    {
        public static List<TrackEvent> GenerateEventsToVegas(this CapCutData data, ref Timecode start, bool closeGap, bool subtitlesOnly, out SubtitlesData subtitles)
        {
            subtitles = null;
            List<TrackEvent> evs = new List<TrackEvent>();

            if (data == null)
            {
                return evs;
            }

            if (start == null || start < new Timecode(0))
            {
                start = new Timecode(0);
            }

            Timecode offset = null;

            List<CapCutMediaUsage> usageList = null;
            if (data.MediaUsages != null && !subtitlesOnly)
            {
                usageList = data.MediaUsages.Where(u => u != null && !string.IsNullOrWhiteSpace(u.Path)).ToList();
                usageList.Sort(CompareByTrackAndStart);

                if (closeGap)
                {
                    foreach (CapCutMediaUsage usage in usageList)
                    {
                        Timecode tmp = ToTimecode(usage.Start);
                        if (offset == null || tmp < offset)
                        {
                            offset = tmp;
                        }
                    }
                }
            }

            List<CapCutSubtitleBlock> blocks = null;

            if (data.Subtitles != null && data.Subtitles.Count > 0)
            {
                SubtitlesData subtitlesData = new SubtitlesData { IsFromStrings = false };
                blocks = new List<CapCutSubtitleBlock>(data.Subtitles);
                blocks.Sort(CompareByStart);

                if (closeGap)
                {
                    foreach (CapCutSubtitleBlock block in blocks)
                    {
                        Timecode tmp = ToTimecode(block.Start);
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

            if (usageList?.Count > 0)
            {
                Dictionary<string, Track> trackCache = new Dictionary<string, Track>(StringComparer.OrdinalIgnoreCase);
                Dictionary<string, Media> mediaCache = new Dictionary<string, Media>(StringComparer.OrdinalIgnoreCase);

                foreach (CapCutMediaUsage usage in usageList)
                {
                    TrackEvent ev = CreateEventFromUsage(UltraPasteCommon.Vegas, usage, start, trackCache, mediaCache);
                    if (ev != null)
                    {
                        evs.Add(ev);

                        if (usage.MediaType == CapCutMediaType.Video && !usage.HasSoundSeparated)
                        {
                            List<TrackEvent> addedAudio = UltraPasteCommon.Vegas.Project.AddMissingStreams(new[] { ev }, MediaType.Audio);
                            if (addedAudio != null && addedAudio.Count > 0)
                            {
                                evs.AddRange(addedAudio);
                            }
                        }
                    }
                }
            }

            if (blocks != null)
            {
                foreach(TrackEvent ev in evs)
                {
                    ev.Track.Selected = false;
                }

                SubtitlesData subtitlesData = new SubtitlesData { IsFromStrings = false };

                foreach (CapCutSubtitleBlock block in blocks)
                {
                    if (block == null)
                    {
                        continue;
                    }

                    TimeSpan length = block.End - block.Start;
                    if (length <= TimeSpan.Zero)
                    {
                        continue;
                    }

                    SubtitlesData.Subtitle subtitle = new SubtitlesData.Subtitle
                    {
                        Start = block.Start,
                        Length = length
                    };

                    foreach (string line in SplitTextLines(block.Text))
                    {
                        subtitle.TextLines.Add(line);
                    }

                    subtitlesData.Subtitles.Add(subtitle);
                }

                if (subtitlesData.Subtitles.Count > 0)
                {
                    subtitles = subtitlesData;
                }
            }

            return evs;
        }

        private static TrackEvent CreateEventFromUsage(Vegas vegas, CapCutMediaUsage usage, Timecode timelineOffset, Dictionary<string, Track> trackCache, Dictionary<string, Media> mediaCache)
        {
            if (vegas?.Project == null || usage == null)
            {
                return null;
            }

            TimeSpan usageDuration = usage.End - usage.Start;
            if (usageDuration <= TimeSpan.Zero)
            {
                return null;
            }

            Media media = ResolveMedia(vegas, usage.Path, mediaCache);
            if (media == null)
            {
                return null;
            }

            Track track = EnsureTrackForUsage(vegas.Project, usage, trackCache);
            if (track == null)
            {
                return null;
            }

            Timecode eventStart = timelineOffset + ToTimecode(usage.Start);
            Timecode eventLength = ToTimecode(usageDuration);
            TrackEvent ev = CreateTrackEvent(vegas.Project, track, usage, media, eventStart, eventLength);
            if (ev == null)
            {
                return null;
            }

            ApplyUsageMetadata(ev, usage, usageDuration);
            return ev;
        }

        private static Media ResolveMedia(Vegas vegas, string path, Dictionary<string, Media> cache)
        {
            if (vegas == null || string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            string normalizedPath = NormalizePath(path);
            Media media = TryGetOrLoadMedia(vegas, normalizedPath, cache);

            if (media == null)
            {
                string fallbackPath = TryResolveOnlineMaterialPath(normalizedPath);
                if (!string.IsNullOrEmpty(fallbackPath))
                {
                    string normalizedFallback = NormalizePath(fallbackPath);
                    media = TryGetOrLoadMedia(vegas, normalizedFallback, cache);
                }
            }

            return media;
        }

        private static Media TryGetOrLoadMedia(Vegas vegas, string normalizedPath, Dictionary<string, Media> cache)
        {
            if (vegas == null || string.IsNullOrWhiteSpace(normalizedPath))
            {
                return null;
            }

            if (cache.TryGetValue(normalizedPath, out Media cachedValue))
            {
                return cachedValue;
            }

            Media media = vegas.GetValidMedia(normalizedPath);
            if (media == null)
            {
                if (CapCutMediaDecryptor.TryEnsureDecryptedMedia(normalizedPath, out string decryptedPath) && !string.IsNullOrEmpty(decryptedPath))
                {
                    string normalizedDecrypted = NormalizePath(decryptedPath);
                    if (!cache.TryGetValue(normalizedDecrypted, out media))
                    {
                        media = vegas.GetValidMedia(normalizedDecrypted);
                        if (media != null)
                        {
                            cache[normalizedDecrypted] = media;
                        }
                    }
                }
            }

            if (media != null)
            {
                cache[normalizedPath] = media;
            }

            return media;
        }

        private static Track EnsureTrackForUsage(Project project, CapCutMediaUsage usage, Dictionary<string, Track> cache)
        {
            if (project == null || usage == null)
            {
                return null;
            }

            string key = !string.IsNullOrWhiteSpace(usage.TrackId)
                ? usage.TrackId
                : string.Format("{0}_{1}_{2}", usage.TrackType ?? usage.MediaType.ToString(), usage.TrackOrder, usage.MediaType);

            if (cache.TryGetValue(key, out Track existing))
            {
                return existing;
            }

            string defaultName = !string.IsNullOrWhiteSpace(usage.TrackName)
                ? usage.TrackName
                : string.Empty;

            Track newTrack = usage.MediaType == CapCutMediaType.Audio
                ? (Track)new AudioTrack(project, -1, defaultName)
                : new VideoTrack(project, -1, defaultName);

            project.Tracks.Add(newTrack);

            if (string.IsNullOrWhiteSpace(newTrack.Name))
            {
                newTrack.Name = defaultName;
            }
            cache[key] = newTrack;
            return newTrack;
        }

        private static TrackEvent CreateTrackEvent(Project project, Track track, CapCutMediaUsage usage, Media media, Timecode start, Timecode length)
        {
            TrackEvent ev = usage.MediaType == CapCutMediaType.Audio
                ? (TrackEvent)new AudioEvent(project, start, length, null)
                : new VideoEvent(project, start, length, null);

            track.Events.Add(ev);
            if (ev.AddTake(media, true, usage.Name) == null)
            {
                track.Events.Remove(ev);
                return null;
            }

            return ev;
        }

        private static void ApplyUsageMetadata(TrackEvent ev, CapCutMediaUsage usage, TimeSpan usageDuration)
        {
            if (ev == null || usage == null)
            {
                return;
            }

            if (ev.ActiveTake != null && usage.SourceStart.HasValue)
            {
                ev.ActiveTake.Offset = ToTimecode(usage.SourceStart.Value);
            }

            if (Math.Abs(usage.PlaybackRate - 1d) > 0.0001d)
            {
                ev.AdjustPlaybackRate(usage.PlaybackRate, true);
            }

            ApplyFadeLength(ev.FadeIn, usage.FadeIn, usageDuration);
            ApplyFadeLength(ev.FadeOut, usage.FadeOut, usageDuration);

            if (usage.MediaType == CapCutMediaType.Audio && ev is AudioEvent audioEvent)
            {
                ApplyAudioVolume(audioEvent, usage.Volume);
            }
        }

        private static void ApplyFadeLength(Fade fade, TimeSpan? value, TimeSpan usageDuration)
        {
            if (fade == null || !value.HasValue || value.Value <= TimeSpan.Zero || usageDuration <= TimeSpan.Zero)
            {
                return;
            }

            TimeSpan length = value.Value;
            if (length >= usageDuration)
            {
                length = usageDuration - TimeSpan.FromMilliseconds(1);
                if (length <= TimeSpan.Zero)
                {
                    return;
                }
            }

            fade.Length = ToTimecode(length);
        }

        private static void ApplyAudioVolume(AudioEvent audioEvent, double volume)
        {
            if (audioEvent == null)
            {
                return;
            }

            if (volume > 0 && volume < 1)
            {
                audioEvent.FadeIn.Gain = (float)volume;
            }
        }

        private static Timecode ToTimecode(TimeSpan span)
        {
            return Timecode.FromMilliseconds(span.TotalMilliseconds);
        }

        private static int CompareByTrackAndStart(CapCutMediaUsage left, CapCutMediaUsage right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            int leftOrder = left.TrackOrder < 0 ? int.MaxValue : left.TrackOrder;
            int rightOrder = right.TrackOrder < 0 ? int.MaxValue : right.TrackOrder;
            int compare = leftOrder.CompareTo(rightOrder);
            if (compare != 0)
            {
                return compare;
            }

            compare = left.Start.CompareTo(right.Start);
            if (compare != 0)
            {
                return compare;
            }

            return string.Compare(left.MaterialId, right.MaterialId, StringComparison.OrdinalIgnoreCase);
        }

        private static int CompareByStart(CapCutSubtitleBlock left, CapCutSubtitleBlock right)
        {
            if (ReferenceEquals(left, right))
            {
                return 0;
            }

            if (left == null)
            {
                return 1;
            }

            if (right == null)
            {
                return -1;
            }

            return left.Start.CompareTo(right.Start);
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return path;
            }

            string normalized = path.Replace('/', Path.DirectorySeparatorChar).Replace('\\', Path.DirectorySeparatorChar);
            try
            {
                if (Path.IsPathRooted(normalized))
                {
                    normalized = Path.GetFullPath(normalized);
                }
            }
            catch
            {
                // ignored ¨C fall back to best effort path
            }

            return normalized;
        }

        private static string TryResolveOnlineMaterialPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            if (path.IndexOf("onlinematerial", StringComparison.OrdinalIgnoreCase) < 0)
            {
                return null;
            }

            string fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            string cacheFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CapCut", "User Data", "Cache", "onlineMaterial");
            if (!Directory.Exists(cacheFolder))
            {
                return null;
            }

            string candidate = Path.Combine(cacheFolder, fileName);
            return File.Exists(candidate) ? candidate : null;
        }

        private static IEnumerable<string> SplitTextLines(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                yield return string.Empty;
                yield break;
            }

            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            if (lines.Length == 0)
            {
                yield return string.Empty;
                yield break;
            }

            foreach (string line in lines)
            {
                yield return line;
            }
        }
    }
}
