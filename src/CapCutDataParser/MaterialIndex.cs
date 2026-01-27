using System;
using System.Globalization;
using System.Collections.Generic;

namespace CapCutDataParser
{
    using static CapCutJsonUtilities;
    internal sealed class MaterialIndex
    {
        private readonly Dictionary<string, VideoMaterialInfo> videos;
        private readonly Dictionary<string, AudioMaterialInfo> audios;
        private readonly Dictionary<string, TextMaterialInfo> texts;

        private MaterialIndex(Dictionary<string, VideoMaterialInfo> videos,
                              Dictionary<string, AudioMaterialInfo> audios,
                              Dictionary<string, TextMaterialInfo> texts)
        {
            this.videos = videos;
            this.audios = audios;
            this.texts = texts;
        }

        public static MaterialIndex Create(Dictionary<string, object> materials)
        {
            var videoIndex = new Dictionary<string, VideoMaterialInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var video in EnumerateObjects(GetList(materials, "videos")))
            {
                var aliases = new[]
                {
                    GetString(video, "id"),
                    GetString(video, "material_id"),
                    GetString(video, "local_material_id")
                };

                var info = new VideoMaterialInfo
                {
                    MaterialId = FirstNonEmpty(aliases),
                    Name = GetString(video, "material_name"),
                    Path = GetString(video, "path"),
                    HasSoundSeparated = GetBool(video, "has_sound_separated", false),
                    Width = (int)GetLong(video, "width"),
                    Height = (int)GetLong(video, "height"),
                    FrameRate = ParseFrameRate(video)
                };

                if (string.IsNullOrWhiteSpace(info.MaterialId))
                {
                    continue;
                }

                AddMaterialAliases(videoIndex, info, aliases);
            }

            var audioIndex = new Dictionary<string, AudioMaterialInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var audio in EnumerateObjects(GetList(materials, "audios")))
            {
                var aliases = new[]
                {
                    GetString(audio, "id"),
                    GetString(audio, "material_id"),
                    GetString(audio, "local_material_id"),
                    GetString(audio, "music_id")
                };

                var info = new AudioMaterialInfo
                {
                    MaterialId = FirstNonEmpty(aliases),
                    Name = GetString(audio, "name"),
                    Path = GetString(audio, "path")
                };

                if (string.IsNullOrWhiteSpace(info.MaterialId))
                {
                    continue;
                }

                AddMaterialAliases(audioIndex, info, aliases);
            }

            var textIndex = new Dictionary<string, TextMaterialInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var text in EnumerateObjects(GetList(materials, "texts")))
            {
                var key = FirstNonEmpty(GetString(text, "id"));
                if (string.IsNullOrWhiteSpace(key) || textIndex.ContainsKey(key))
                {
                    continue;
                }

                textIndex[key] = new TextMaterialInfo
                {
                    MaterialId = key,
                    Content = GetString(text, "content")
                };
            }

            return new MaterialIndex(videoIndex, audioIndex, textIndex);
        }

        public bool TryGetVideo(string materialId, out VideoMaterialInfo material)
        {
            material = null;
            return !string.IsNullOrWhiteSpace(materialId) && videos.TryGetValue(materialId, out material);
        }

        public bool TryGetAudio(string materialId, out AudioMaterialInfo material)
        {
            material = null;
            return !string.IsNullOrWhiteSpace(materialId) && audios.TryGetValue(materialId, out material);
        }

        public bool TryGetText(string materialId, out TextMaterialInfo material)
        {
            material = null;
            return !string.IsNullOrWhiteSpace(materialId) && texts.TryGetValue(materialId, out material);
        }

        private static string FirstNonEmpty(params string[] values)
        {
            if (values == null)
            {
                return null;
            }

            foreach (var value in values)
            {
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value;
                }
            }

            return null;
        }

        private static void AddMaterialAliases<T>(Dictionary<string, T> index, T material, params string[] aliases)
        {
            if (index == null || material == null || aliases == null)
            {
                return;
            }

            foreach (var alias in aliases)
            {
                if (string.IsNullOrWhiteSpace(alias) || index.ContainsKey(alias))
                {
                    continue;
                }

                index[alias] = material;
            }
        }

        private static double? ParseFrameRate(Dictionary<string, object> video)
        {
            double fps = GetDouble(video, "fps", 0d);
            if (fps > 0)
            {
                return fps;
            }

            var fpsObject = GetObject(video, "fps");
            if (fpsObject != null)
            {
                double numerator = GetDouble(fpsObject, "num", 0d);
                double denominator = GetDouble(fpsObject, "den", 0d);
                if (numerator > 0 && denominator > 0)
                {
                    return numerator / denominator;
                }
            }

            string frameRateText = GetString(video, "frame_rate") ?? GetString(video, "frameRate");
            if (!string.IsNullOrWhiteSpace(frameRateText))
            {
                if (frameRateText.Contains("/"))
                {
                    string[] parts = frameRateText.Split('/');
                    if (parts.Length == 2 && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out double num) &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double den) && den != 0)
                    {
                        return num / den;
                    }
                }
                else if (double.TryParse(frameRateText, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsed) && parsed > 0)
                {
                    return parsed;
                }
            }

            return null;
        }

        internal sealed class VideoMaterialInfo
        {
            public string MaterialId { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public bool HasSoundSeparated { get; set; } = false;
            public int Width { get; set; }
            public int Height { get; set; }
            public double? FrameRate { get; set; }
        }

        internal sealed class AudioMaterialInfo
        {
            public string MaterialId { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
        }

        internal sealed class TextMaterialInfo
        {
            public string MaterialId { get; set; }
            public string Content { get; set; }
        }
    }
}
