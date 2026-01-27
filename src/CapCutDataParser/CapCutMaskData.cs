using System;
using System.IO;
using System.Drawing;
using System.Globalization;
using System.Drawing.Imaging;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CapCutDataParser
{
    using static CapCutDataParser.CapCutJsonUtilities;

    public sealed class CapCutMaskData
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ResourceId { get; set; }
        public string ResourceType { get; set; }
        public string LoaderWorkSpace { get; set; }
        public string LoaderWorkSpaceFull { get; private set; }
        public CapCutMaskConfig Config { get; set; }
        public int SourceWidth { get; set; }
        public int SourceHeight { get; set; }
        public double? SourceFrameRate { get; set; }

        public void UpdateLoaderWorkspace(string draftPath)
        {
            if (string.IsNullOrEmpty(LoaderWorkSpace))
            {
                return;
            }

            if (Directory.Exists(LoaderWorkSpace))
            {
                LoaderWorkSpaceFull = LoaderWorkSpace;
                return;
            }

            string sanitized = CapCutData.DraftPathRegex.Replace(LoaderWorkSpace, string.Empty)
                .TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            string combined = sanitized;
            if (!string.IsNullOrEmpty(draftPath) && !Path.IsPathRooted(sanitized))
            {
                combined = Path.Combine(draftPath, sanitized);
            }

            if (!string.IsNullOrEmpty(combined) && Directory.Exists(combined))
            {
                LoaderWorkSpaceFull = combined;
            }
        }

        public IReadOnlyList<CapCutMaskFrame> EnumerateFrames()
        {
            string workspace = GetWorkspacePath();
            if (string.IsNullOrEmpty(workspace) || !Directory.Exists(workspace))
            {
                return Array.Empty<CapCutMaskFrame>();
            }

            List<CapCutMaskFrame> frames = new List<CapCutMaskFrame>();
            foreach (string file in Directory.EnumerateFiles(workspace))
            {
                string name = Path.GetFileNameWithoutExtension(file);
                if (string.IsNullOrEmpty(name) || !IsNumeric(name))
                {
                    continue;
                }

                if (long.TryParse(name, NumberStyles.Integer, CultureInfo.InvariantCulture, out long timestamp))
                {
                    frames.Add(new CapCutMaskFrame(timestamp, file));
                }
            }

            frames.Sort((left, right) => left.Timestamp.CompareTo(right.Timestamp));
            return frames;
        }

        public IReadOnlyList<string> ConvertToPngSequence(string destinationFolder, double? overrideFrameRate = null)
        {
            if (string.IsNullOrWhiteSpace(destinationFolder))
            {
                throw new ArgumentException("Destination folder is required.", nameof(destinationFolder));
            }

            IReadOnlyList<CapCutMaskFrame> frames = EnumerateFrames();
            if (frames.Count == 0)
            {
                return Array.Empty<string>();
            }

            if (SourceWidth <= 0 || SourceHeight <= 0)
            {
                throw new InvalidOperationException("Mask source dimensions are not available.");
            }

            long byteCount = new FileInfo(frames[0].FilePath).Length;
            if (byteCount <= 0)
            {
                return Array.Empty<string>();
            }

            Size frameSize = CalculateMaskSize(byteCount, SourceWidth, SourceHeight);
            if (frameSize.IsEmpty)
            {
                throw new InvalidDataException("Unable to infer mask frame size.");
            }

            double fps = (overrideFrameRate ?? SourceFrameRate) ?? 0d;
            if (fps <= 0)
            {
                fps = 30d;
            }

            Directory.CreateDirectory(destinationFolder);
            List<string> outputFiles = new List<string>(frames.Count);
            foreach (CapCutMaskFrame frame in frames)
            {
                byte[] gray = File.ReadAllBytes(frame.FilePath);
                if (gray.LongLength != byteCount)
                {
                    continue;
                }

                using (Bitmap bitmap = CreateMaskBitmap(gray, frameSize.Width, frameSize.Height))
                {
                    long frameIndex = (long)Math.Round(frame.Timestamp / 1_000_000d * fps, MidpointRounding.AwayFromZero);
                    if (frameIndex < 0)
                    {
                        frameIndex = 0;
                    }

                    string outputPath = Path.Combine(destinationFolder, string.Format("mask_{0:D6}.png", frameIndex));
                    bitmap.Save(outputPath, ImageFormat.Png);
                    outputFiles.Add(outputPath);
                }
            }

            return outputFiles;
        }

        public CapCutMaskData Clone()
        {
            return new CapCutMaskData
            {
                Id = Id,
                Name = Name,
                ResourceId = ResourceId,
                ResourceType = ResourceType,
                LoaderWorkSpace = LoaderWorkSpace,
                LoaderWorkSpaceFull = LoaderWorkSpaceFull,
                Config = Config?.Clone(),
                SourceFrameRate = SourceFrameRate,
                SourceHeight = SourceHeight,
                SourceWidth = SourceWidth
            };
        }

        private string GetWorkspacePath()
        {
            if (!string.IsNullOrEmpty(LoaderWorkSpaceFull) && Directory.Exists(LoaderWorkSpaceFull))
            {
                return LoaderWorkSpaceFull;
            }

            if (!string.IsNullOrEmpty(LoaderWorkSpace) && Directory.Exists(LoaderWorkSpace))
            {
                LoaderWorkSpaceFull = LoaderWorkSpace;
                return LoaderWorkSpaceFull;
            }

            return LoaderWorkSpaceFull;
        }

        private static bool IsNumeric(string value)
        {
            foreach (char c in value)
            {
                if (!char.IsDigit(c))
                {
                    return false;
                }
            }

            return value.Length > 0;
        }

        private static Size CalculateMaskSize(long byteCount, int sourceWidth, int sourceHeight)
        {
            if (byteCount <= 0 || sourceWidth <= 0 || sourceHeight <= 0)
            {
                return Size.Empty;
            }

            double totalPixels = sourceWidth * (double)sourceHeight;
            double scale = Math.Sqrt(totalPixels / byteCount);
            if (double.IsNaN(scale) || double.IsInfinity(scale) || scale <= 0)
            {
                return Size.Empty;
            }

            int width = Math.Max(1, (int)Math.Round(sourceWidth / scale));
            int height = Math.Max(1, (int)Math.Round(sourceHeight / scale));

            if ((long)width * height != byteCount)
            {
                double aspect = sourceHeight == 0 ? 1d : sourceWidth / (double)sourceHeight;
                width = Math.Max(1, (int)Math.Round(Math.Sqrt(byteCount * aspect)));
                height = (int)Math.Max(1, byteCount / Math.Max(width, 1));

                if ((long)width * height != byteCount)
                {
                    width = Math.Max(1, (int)Math.Round(Math.Sqrt(byteCount)));
                    height = (int)Math.Max(1, byteCount / Math.Max(width, 1));
                }
            }

            return (long)width * height == byteCount ? new Size(width, height) : Size.Empty;
        }

        private static Bitmap CreateMaskBitmap(byte[] data, int width, int height)
        {
            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (data.Length != width * height)
            {
                throw new InvalidDataException("Mask frame payload size does not match inferred dimensions.");
            }

            Bitmap bitmap = new Bitmap(width, height, PixelFormat.Format32bppArgb);
            BitmapData bitmapData = bitmap.LockBits(new Rectangle(0, 0, width, height), ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            try
            {
                int stride = bitmapData.Stride;
                byte[] buffer = new byte[stride * height];
                int src = 0;
                for (int y = 0; y < height; y++)
                {
                    int dest = y * stride;
                    for (int x = 0; x < width; x++)
                    {
                        byte value = data[src++];
                        buffer[dest++] = 255; // B
                        buffer[dest++] = 255; // G
                        buffer[dest++] = 255; // R
                        buffer[dest++] = value; // A
                    }
                }

                Marshal.Copy(buffer, 0, bitmapData.Scan0, buffer.Length);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }

            return bitmap;
        }

        internal static CapCutMaskData FromDictionary(Dictionary<string, object> source)
        {
            if (source == null)
            {
                return null;
            }

            return new CapCutMaskData
            {
                Id = GetString(source, "id"),
                Name = GetString(source, "name"),
                ResourceId = GetString(source, "resource_id"),
                ResourceType = GetString(source, "resource_type"),
                LoaderWorkSpace = GetString(source, "loader_work_space"),
                Config = CapCutMaskConfig.FromDictionary(GetObject(source, "config"))
            };
        }
    }

    public sealed class CapCutMaskConfig
    {
        public double AspectRatio { get; set; }
        public double CenterX { get; set; }
        public double CenterY { get; set; }
        public double Feather { get; set; }
        public double Height { get; set; }
        public bool Invert { get; set; }
        public double Rotation { get; set; }
        public double RoundCorner { get; set; }
        public double Width { get; set; }

        public CapCutMaskConfig Clone()
        {
            return (CapCutMaskConfig)MemberwiseClone();
        }

        internal static CapCutMaskConfig FromDictionary(Dictionary<string, object> config)
        {
            if (config == null)
            {
                return null;
            }

            return new CapCutMaskConfig
            {
                AspectRatio = GetDouble(config, "aspectRatio", 0d),
                CenterX = GetDouble(config, "centerX", 0d),
                CenterY = GetDouble(config, "centerY", 0d),
                Feather = GetDouble(config, "feather", 0d),
                Height = GetDouble(config, "height", 0d),
                Invert = GetBool(config, "invert", false),
                Rotation = GetDouble(config, "rotation", 0d),
                RoundCorner = GetDouble(config, "roundCorner", 0d),
                Width = GetDouble(config, "width", 0d)
            };
        }
    }

    public sealed class CapCutMaskFrame
    {
        internal CapCutMaskFrame(long timestamp, string filePath)
        {
            Timestamp = timestamp;
            FilePath = filePath;
        }

        public long Timestamp { get; }
        public string FilePath { get; }
    }
}
