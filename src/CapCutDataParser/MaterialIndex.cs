using System;
using System.Collections.Generic;
using static CapCutDataParser.CapCutJsonUtilities;

namespace CapCutDataParser
{
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
                var key = FirstNonEmpty(GetString(video, "material_id"), GetString(video, "id"), GetString(video, "local_material_id"));
                if (string.IsNullOrWhiteSpace(key) || videoIndex.ContainsKey(key))
                {
                    continue;
                }

                videoIndex[key] = new VideoMaterialInfo
                {
                    MaterialId = key,
                    Name = GetString(video, "material_name"),
                    Path = GetString(video, "path"),
                    HasSoundSeparated = GetBool(video, "has_sound_separated", false)
                };
            }

            var audioIndex = new Dictionary<string, AudioMaterialInfo>(StringComparer.OrdinalIgnoreCase);
            foreach (var audio in EnumerateObjects(GetList(materials, "audios")))
            {
                var key = FirstNonEmpty(GetString(audio, "id"), GetString(audio, "local_material_id"), GetString(audio, "music_id"));
                if (string.IsNullOrWhiteSpace(key) || audioIndex.ContainsKey(key))
                {
                    continue;
                }

                audioIndex[key] = new AudioMaterialInfo
                {
                    MaterialId = key,
                    Name = GetString(audio, "name"),
                    Path = GetString(audio, "path")
                };
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

        internal sealed class VideoMaterialInfo
        {
            public string MaterialId { get; set; }
            public string Name { get; set; }
            public string Path { get; set; }
            public bool HasSoundSeparated { get; set; } = false;
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
