#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

#if TEST
public static class S
{
    public static object s
    {
        set => MessageBox.Show(value is null ? "null" : value.ToString());
    }
}
#endif

namespace UltraPaste.Utilities
{
    internal static class VegasCommonHelper
    {
        private const string VEGAS_DATA_FORMAT = "Vegas Data 5.0";
        private const string SONY_VEGAS_DATA_FORMAT = "Sony Vegas Data 5.0";
        private const string VEGAS_METADATA_FORMAT = "Vegas Meta-Data 5.0";
        private const string SONY_VEGAS_METADATA_FORMAT = "Sony Vegas Meta-Data 5.0";

        public static int VegasVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileMajorPart;
        public static string RoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

#if !Sony
        public static System.Drawing.Color[] UIColors = new System.Drawing.Color[] { ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonFace, ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonText };
#else
    public static System.Drawing.Color[] UIColors = new System.Drawing.Color[] { Sony.MediaSoftware.Skins.Skins.Colors.ButtonFace, Sony.MediaSoftware.Skins.Skins.Colors.ButtonText };
#endif

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern void ILFree(IntPtr pidlList);

        [DllImport("shell32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern IntPtr ILCreateFromPathW(string pszPath);

        [DllImport("shell32.dll", ExactSpelling = true)]
        private static extern int SHOpenFolderAndSelectItems(IntPtr pidlList, uint cild, IntPtr children, uint dwFlags);

        public static void ExplorerFile(string filePath)
        {
            if (!File.Exists(filePath) && !Directory.Exists(filePath))
                return;

            if (Directory.Exists(filePath))
                Process.Start(@"explorer.exe", "/select,\"" + filePath + "\"");
            else
            {
                IntPtr pidlList = ILCreateFromPathW(filePath);
                if (pidlList != IntPtr.Zero)
                {
                    try
                    {
                        Marshal.ThrowExceptionForHR(SHOpenFolderAndSelectItems(pidlList, 0, IntPtr.Zero, 0));
                    }
                    finally
                    {
                        ILFree(pidlList);
                    }
                }
            }
        }

        public static bool IsPathMatch(string path, string dosExpression)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(dosExpression))
            {
                return false;
            }

            string fileName = Path.GetFileName(path);
            if (string.IsNullOrEmpty(fileName))
            {
                return false;
            }

            List<string> validPatterns = new List<string>();
            foreach (string rawPattern in dosExpression.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string trimmedPattern = rawPattern.Trim();
                if (!string.IsNullOrEmpty(trimmedPattern))
                {
                    validPatterns.Add(trimmedPattern);
                }
            }

            foreach (string pattern in validPatterns)
            {
                string regexPattern = string.Format("^{0}$", Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", "."));
                if (Regex.IsMatch(fileName, regexPattern, RegexOptions.IgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        // get a mix of modern VEGAS data and Sony one, allowing users to paste between the two
        public static void GenerateMixedVegasClipboardData()
        {
            if (!Clipboard.ContainsData(VEGAS_DATA_FORMAT) && !Clipboard.ContainsData(SONY_VEGAS_DATA_FORMAT) && 
                !Clipboard.ContainsData(VEGAS_METADATA_FORMAT) && !Clipboard.ContainsData(SONY_VEGAS_METADATA_FORMAT))
            {
                return;
            }

            IDataObject oldData = Clipboard.GetDataObject();
            DataObject newData = new DataObject();

            Dictionary<string, object> dic = new Dictionary<string, object>
            {
                { VEGAS_DATA_FORMAT, null }, 
                { SONY_VEGAS_DATA_FORMAT, null }, 
                { VEGAS_METADATA_FORMAT, null }, 
                { SONY_VEGAS_METADATA_FORMAT, null }
            };

            if (oldData != null)
            {
                foreach (string existingFormat in oldData.GetFormats())
                {
                    object obj = oldData.GetData(existingFormat);
                    if (dic.ContainsKey(existingFormat))
                    {
                        dic[existingFormat] = obj;
                    }
                    else
                    {
                        newData.SetData(existingFormat, obj);
                    }
                }
            }

            if (dic[VEGAS_DATA_FORMAT] != null || dic[SONY_VEGAS_DATA_FORMAT] != null)
            {
                dic[VEGAS_DATA_FORMAT] = dic[VEGAS_DATA_FORMAT] ?? dic[SONY_VEGAS_DATA_FORMAT];
                dic[SONY_VEGAS_DATA_FORMAT] = dic[SONY_VEGAS_DATA_FORMAT] ?? dic[VEGAS_DATA_FORMAT];

                newData.SetData(VEGAS_DATA_FORMAT, dic[VEGAS_DATA_FORMAT]);
                newData.SetData(SONY_VEGAS_DATA_FORMAT, dic[SONY_VEGAS_DATA_FORMAT]);
            }

            if (dic[VEGAS_METADATA_FORMAT] != null || dic[SONY_VEGAS_METADATA_FORMAT] != null)
            {
                dic[VEGAS_METADATA_FORMAT] = dic[VEGAS_METADATA_FORMAT] ?? dic[SONY_VEGAS_METADATA_FORMAT];
                dic[SONY_VEGAS_METADATA_FORMAT] = dic[SONY_VEGAS_METADATA_FORMAT] ?? dic[VEGAS_METADATA_FORMAT];
                newData.SetData(VEGAS_METADATA_FORMAT, dic[VEGAS_METADATA_FORMAT]);
                newData.SetData(SONY_VEGAS_METADATA_FORMAT, dic[SONY_VEGAS_METADATA_FORMAT]);
            }

            Clipboard.SetDataObject(newData, true);
        }

        // get all valid paths
        public static List<string> GetFilePathsFromPathList(this System.Collections.Specialized.StringCollection pathList)
        {
            List<string> filePaths = new List<string>();
            foreach (string path in pathList)
            {
                CollectFilePaths(path, filePaths);
            }
            return filePaths;
        }

        private static void CollectFilePaths(string path, List<string> filePaths)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            string resolvedPath = path;

            if (Path.GetExtension(resolvedPath).ToLowerInvariant() == ".lnk")
            {
                resolvedPath = GetShortCutTarget(resolvedPath);
                if (string.IsNullOrEmpty(resolvedPath))
                {
                    return;
                }
            }

            if (File.Exists(resolvedPath))
            {
                if (Path.GetExtension(resolvedPath).ToLowerInvariant() == ".lnk")
                {
                    CollectFilePaths(resolvedPath, filePaths);
                }
                else
                {
                    filePaths.Add(resolvedPath);
                }
            }
            else if (Directory.Exists(resolvedPath))
            {
                try
                {
                    foreach (string child in Directory.GetFiles(resolvedPath, "*.*", SearchOption.AllDirectories))
                    {
                        CollectFilePaths(child, filePaths);
                    }
                }
                catch
                {

                }
            }
        }

        private static string GetShortCutTarget(string lnk)
        {
            if (string.IsNullOrEmpty(lnk) || !File.Exists(lnk))
            {
                return string.Empty;
            }

            dynamic objWshShell = null, objShortcut = null;
            try
            {
                objWshShell = Activator.CreateInstance(Type.GetTypeFromCLSID(new Guid("72C24DD5-D70A-438B-8A42-98424B88AFB8")));
                objShortcut = objWshShell.CreateShortcut(lnk);
                return objShortcut.TargetPath ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
            finally
            {
                if (objShortcut != null)
                {
                    Marshal.ReleaseComObject(objShortcut);
                }
                if (objWshShell != null)
                {
                    Marshal.ReleaseComObject(objWshShell);
                }
            }
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
            return streamBytes.IsSameTo(bytes);
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
            return bytes1.IsSameTo(bytes2);
        }

        public static bool IsSameTo(this byte[] bytes1, byte[] bytes2)
        {
            if (ReferenceEquals(bytes1, bytes2))
            {
                return true;
            }

            if (bytes1 == null || bytes2 == null || bytes1.Length != bytes2.Length)
            {
                return false;
            }

            for (int i = 0; i < bytes1.Length; i++)
            {
                if (bytes1[i] != bytes2[i])
                {
                    return false;
                }
            }

            return true;
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
            using (StringReader sr = new StringReader(context))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(sr);
            }
        }

        public static T DeserializeFromFile<T>(this string path) where T : new()
        {
            if (!File.Exists(path))
            {
                return default;
            }

            using (FileStream fs = File.Open(path, FileMode.Open))
            using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(sr);
            }
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

        public static T GetSelectedTrack<T>(this Project project, int index) where T : Track
        {
            List<T> l = project.GetSelectedTracks<T>();
            return l.Count > index ? l[index] : null;
        }

        public static T GetSelectedEvent<T>(this Project project, int index) where T : TrackEvent
        {
            List<T> l = project.GetSelectedEvents<T>();
            return l.Count > index ? l[index] : null;
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
    }
}