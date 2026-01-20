#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace UltraPaste.ExtensionMethods
{
    internal static class VegasCommonExtensions
    {
        // get all valid paths, and output the extension when all files have a uniform extension
        public static List<string> GetFilePathsFromPathList(this System.Collections.Specialized.StringCollection pathList)
        {
            List<string> filePaths = new List<string>();
            foreach (string path in pathList)
            {
                string filePath = path;
                if (Path.GetExtension(filePath).ToLowerInvariant() == ".lnk")
                {
                    filePath = GetShortCutTarget(filePath);
                }
                if (File.Exists(filePath))
                {
                    filePaths.Add(filePath);
                }
                else if (Directory.Exists(filePath))
                {
                    foreach (string child in Directory.GetFiles(filePath, "*.*", SearchOption.AllDirectories))
                    {
                        filePaths.Add(child);
                    }
                }
            }

            return filePaths;
        }

        private static string GetShortCutTarget(string lnk)
        {
            if (lnk != null && File.Exists(lnk))
            {
                dynamic objWshShell = null, objShortcut = null;
                try
                {
                    objWshShell = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
                    objShortcut = objWshShell.CreateShortcut(lnk);
                    Console.WriteLine(objShortcut.TargetPath);
                    return objShortcut.TargetPath;
                }
                finally
                {
                    Marshal.ReleaseComObject(objShortcut);
                    Marshal.ReleaseComObject(objWshShell);
                }
            }
            return string.Empty;
        }

        public static string GetUniformExtention(this List<string> paths)
        {
            string uniformExtension = null;
            if (paths?.Count > 0)
            {
                uniformExtension = Path.GetExtension(paths[0]).ToLowerInvariant();
                foreach (string path in paths)
                {
                    if (Path.GetExtension(path).ToLowerInvariant() != uniformExtension)
                    {
                        uniformExtension = null;
                        break;
                    }
                }
            }
            return uniformExtension;
        }

        public static bool IsSameTo(this System.Drawing.Image img1, System.Drawing.Image img2)
        {
            if (img1 == null || img2 == null || img1.RawFormat.Guid != img2.RawFormat.Guid || img1.Size != img2.Size)
            {
                return false;
            }
            using (MemoryStream ms1 = new MemoryStream(), ms2 = new MemoryStream())
            {
                img1.Save(ms1, System.Drawing.Imaging.ImageFormat.Png);
                img2.Save(ms2, System.Drawing.Imaging.ImageFormat.Png);
                return ms1.IsSameTo(ms2);
            }
        }

        public static bool IsSameTo(this System.Drawing.Image img, byte[] bytes)
        {
            if (img == null || bytes == null)
            {
                return false;
            }
            using (MemoryStream ms = new MemoryStream())
            {
                img.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                return ms.IsSameTo(bytes);
            }
        }

        public static bool IsSameTo(this Stream stream, byte[] bytes)
        {
            byte[] streamBytes;
            if (stream is MemoryStream ms)
            {
                streamBytes = ms.ToArray();
            }
            else
            {
                streamBytes = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(streamBytes, 0, streamBytes.Length);
                stream.Seek(0, SeekOrigin.Begin);
            }
            return Convert.ToBase64String(streamBytes) == Convert.ToBase64String(bytes);
        }

        public static bool IsSameTo(this Stream stream1, Stream stream2)
        {
            if (stream1.GetType() != stream2.GetType() || stream1.Length != stream2.Length)
            {
                return false;
            }
            byte[] bytes1, bytes2;
            if (stream1 is MemoryStream ms1 && stream2 is MemoryStream ms2)
            {
                bytes1 = ms1.ToArray();
                bytes2 = ms2.ToArray();
            }
            else
            {
                bytes1 = new byte[stream1.Length];
                stream1.Seek(0, SeekOrigin.Begin);
                stream1.Read(bytes1, 0, bytes1.Length);
                stream1.Seek(0, SeekOrigin.Begin);
                bytes2 = new byte[stream2.Length];
                stream2.Seek(0, SeekOrigin.Begin);
                stream2.Read(bytes2, 0, bytes2.Length);
                stream2.Seek(0, SeekOrigin.Begin);
            }
            return Convert.ToBase64String(bytes1) == Convert.ToBase64String(bytes2);
        }

        public static void SetIconFile(this CustomCommand cmd, string fileName)
        {
            cmd.IconFile = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), fileName);
        }

        public static string SerializeXml(this object data)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (StreamWriter textWriter = new StreamWriter(ms, new UTF8Encoding()))
                {
                    XmlSerializer serializer = new XmlSerializer(data.GetType());
                    serializer.Serialize(textWriter, data);
                    return Encoding.UTF8.GetString(ms.GetBuffer(), 0, (int)ms.Length);
                }
            }
        }

        public static T DeserializeXml<T>(this string context) where T : new()
        {
            T t = default;

            using (StringReader sr = new StringReader(context))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                t = (T)new XmlSerializer(typeof(T)).Deserialize(sr);
            }

            return t;
        }

        public static T DeserializeFromFile<T>(this string path) where T : new()
        {
            T t = default;
            if (!File.Exists(path))
            {
                return t;
            }
            FileStream fs = File.Open(path, FileMode.Open);
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                t = (T)new XmlSerializer(typeof(T)).Deserialize(sr);
            }
            return t;
        }

        public static T DeepClone<T>(this T t) where T : new()
        {
            return t.SerializeXml().DeserializeXml<T>();
        }

        public static List<T> GetSelectedTracks<T>(this Project project, int maxCount = 0) where T : Track
        {
            List<T> l = new List<T>();
            foreach (Track myTrack in project.Tracks)
            {
                if (myTrack.Selected)
                {
                    if ((typeof(T) == typeof(VideoTrack) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioTrack) && !myTrack.IsAudio()))
                    {
                        continue;
                    }
                    l.Add((T)myTrack);
                }
            }
            if (maxCount > 0 && maxCount < l.Count)
            {
                l = l.GetRange(0, maxCount);
            }
            return l;
        }

        public static List<T> GetSelectedEvents<T>(this Project project, int maxCount = 0) where T : TrackEvent
        {
            List<T> l = new List<T>();
            foreach (Track myTrack in project.Tracks)
            {
                if ((typeof(T) == typeof(VideoEvent) && !myTrack.IsVideo()) || (typeof(T) == typeof(AudioEvent) && !myTrack.IsAudio()))
                {
                    continue;
                }
                foreach (TrackEvent ev in myTrack.Events)
                {
                    if (ev.Selected)
                    {
                        l.Add((T)ev);
                    }
                }
            }
            if (maxCount > 0 && maxCount < l.Count)
            {
                l = l.GetRange(0, maxCount);
            }
            return l;
        }

        public static T GetFirstSelectedEvent<T>(this Project project) where T : TrackEvent
        {
            List<T> l = project.GetSelectedEvents<T>();
            return l.Count > 0 ? l[0] : null;
        }

        public static Timecode GetEndTimeFromMarkers<T>(this List<T> markers) where T : Marker
        {
            if (markers == null || markers.Count == 0)
            {
                return null;
            }
            Timecode end = new Timecode(0);
            foreach (T m in markers)
            {
                Region r = m as Region;
                Timecode t = r != null ? r.End : m.Position;
                if (end < t)
                {
                    end = t;
                }
            }
            return end;
        }

        public static Timecode GetEndTimeFromEvents<T>(this List<T> evs) where T : TrackEvent
        {
            if (evs == null || evs.Count == 0)
            {
                return null;
            }
            Timecode end = new Timecode(0);
            foreach (T ev in evs)
            {
                if (end < ev.End)
                {
                    end = ev.End;
                }
            }
            return end;
        }

        public static void RefreshCursorPosition(this Vegas vegas, Timecode time)
        {
            if (time != null && vegas.Transport.CursorPosition != time)
            {
                vegas.UpdateUI();
                vegas.Transport.CursorPosition = time;
                vegas.Transport.ViewCursor(false);
            }
        }

        public static List<Media> GetValidMedia(this Vegas vegas, IEnumerable<string> paths)
        {
            List<Media> mediaList = new List<Media>();
            foreach (string path in paths)
            {
                Media media;
                if ((media = vegas.GetValidMedia(path)) != null)
                {
                    mediaList.Add(media);
                }
            }
            return mediaList;
        }

        private static readonly System.Reflection.MethodInfo method_MediaInfo = typeof(Vegas).GetMethod("MediaInfo", new Type[] { typeof(string) }), method_ImportFile_New = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool), typeof(bool) }), method_ImportFile = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool) });

        public static Media GetValidMedia(this Vegas vegas, string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }
            try
            {
                if (method_MediaInfo != null)
                {
                    // VEGAS Pro 18+
                    method_MediaInfo.Invoke(vegas, new object[] { path });
                }
                else if (method_ImportFile_New != null)
                {
                    // VEGAS Pro 22 Build 122+ (not recommended, for compatibility only)
                    method_ImportFile_New.Invoke(vegas, new object[] { path, true, false });
                }
                else
                {
                    // VEGAS Pro 22 Build 93-
                    method_ImportFile?.Invoke(vegas, new object[] { path, true });
                }
                Media media = Media.CreateInstance(vegas.Project, path);
                return media;
            }
            catch
            {
                return null;
            }
        }

        public static System.Drawing.Color ConvertToColor(this OFXColor ofxColor)
        {
            return System.Drawing.Color.FromArgb((int)(ofxColor.A * 255), (int)(ofxColor.R * 255), (int)(ofxColor.G * 255), (int)(ofxColor.B * 255));
        }

        public static string RemoveMatchedPart(this string str1, string str2)
        {
            List<int> nonWhitespaceIndices = new List<int>();
            for (int i = 0; i < str1.Length; i++)
            {
                if (!char.IsWhiteSpace(str1[i]))
                {
                    nonWhitespaceIndices.Add(i);
                }
            }

            if (nonWhitespaceIndices.Count < str2.Length)
            {
                return str1;
            }

            bool isMatched = true;
            for (int i = 0; i < str2.Length; i++)
            {
                if (str1[nonWhitespaceIndices[i]] != str2[i])
                {
                    isMatched = false;
                    break;
                }
            }

            if (!isMatched)
            {
                return str1;
            }

            HashSet<int> removeIndices = new HashSet<int>(nonWhitespaceIndices.Take(str2.Length));

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < str1.Length; i++)
            {
                if (!removeIndices.Contains(i))
                {
                    sb.Append(str1[i]);
                }
            }

            return sb.ToString();
        }
    }
}