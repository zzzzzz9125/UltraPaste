#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

#if TEST
public static class S
{
    public static object s
    {
        set => MessageBox.Show(value is null ? "null" : value.ToString());
    }
}
#endif

public static class Common
{
    public static int VegasVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileMajorPart;

    public static double CalculatePointCoordinateInLine(double x1, double y1, double x2, double y2, double x3)
    {
        double k = (y2 - y1) / (x2 - x1);
        double b = y1 - k * x1;
        return k * x3 + b;
    }


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

    public static string GetShortCutTarget(string lnk)
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

    // get all valid paths, and output the extension when all files have a uniform extension
    public static List<string> GetFilePathsFromPathList(this System.Collections.Specialized.StringCollection pathList, out string uniformExtension)
    {
        List<string> filePaths = new List<string>();
        foreach (string path in pathList)
        {
            string filePath = path;
            if (Path.GetExtension(filePath).ToLower() == ".lnk")
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
        uniformExtension = null;
        if (filePaths.Count > 0)
        {
            uniformExtension = Path.GetExtension(filePaths[0]).ToLower();
            foreach (string path in filePaths)
            {
                if (Path.GetExtension(path).ToLower() != uniformExtension)
                {
                    uniformExtension = null;
                    break;
                }
            }
        }
        return filePaths;
    }

    public static bool IsSameTo(this System.Drawing.Image img1, System.Drawing.Image img2)
    {
        if (img2 == null || img1.RawFormat.Guid != img2.RawFormat.Guid || img1.Size != img2.Size)
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

    public static T DeserializeXml<T>(this string path) where T : new()
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

    // get a mix of modern VEGAS data and Sony one, allowing users to paste between the two
    public static void UnifyVegasClipboardData()
    {
        string vegasDataStr = "Vegas Data 5.0", sonyVegasDataStr = "Sony Vegas Data 5.0", vegasMetaDataStr = "Vegas Meta-Data 5.0", sonyVegasMetaDataStr = "Sony Vegas Meta-Data 5.0";

        if (!Clipboard.ContainsData(vegasDataStr) && !Clipboard.ContainsData(sonyVegasDataStr) && !Clipboard.ContainsData(vegasMetaDataStr) && !Clipboard.ContainsData(sonyVegasMetaDataStr))
        {
            return;
        }

        IDataObject oldData = Clipboard.GetDataObject();
        DataObject newData = new DataObject();


        Dictionary<string, object> dic = new Dictionary<string, object>
        {
            { vegasDataStr, null }, { sonyVegasDataStr, null }, { vegasMetaDataStr, null }, { sonyVegasMetaDataStr, null }
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

        if (dic[vegasDataStr] != null || dic[sonyVegasDataStr] != null)
        {
            dic[vegasDataStr] = dic[vegasDataStr] ?? dic[sonyVegasDataStr];
            dic[sonyVegasDataStr] = dic[sonyVegasDataStr] ?? dic[vegasDataStr];

            // trying to convert the new VideoFX GUIDs to the old Sony ones, but failed...

            /*if (dic[sonyVegasDataStr] is MemoryStream ms)
            {
                byte[] magixBytes = new byte[] { 0x76, 0x00, 0x65, 0x00, 0x67, 0x00, 0x61, 0x00, 0x73, 0x00,   // v e g a s 
                                                             0x63, 0x00, 0x72, 0x00, 0x65, 0x00, 0x61, 0x00,   //   c r e a 
                                                             0x74, 0x00, 0x69, 0x00, 0x76, 0x00, 0x65, 0x00,   //   t i v e 
                                                             0x73, 0x00, 0x6F, 0x00, 0x66, 0x00, 0x74, 0x00,   //   s o f t 
                                                             0x77, 0x00, 0x61, 0x00, 0x72, 0x00, 0x65, 0x00 }; //   w a r e 

                byte[] sonyBytes = new byte[] {              0x73, 0x00, 0x6F, 0x00, 0x6E, 0x00, 0x79, 0x00,   //   s o n y 
                                                             0x63, 0x00, 0x72, 0x00, 0x65, 0x00, 0x61, 0x00,   //   c r e a 
                                                             0x74, 0x00, 0x69, 0x00, 0x76, 0x00, 0x65, 0x00,   //   t i v e 
                                                             0x73, 0x00, 0x6F, 0x00, 0x66, 0x00, 0x74, 0x00,   //   s o f t 
                                                             0x77, 0x00, 0x61, 0x00, 0x72, 0x00, 0x65, 0x00 }; //   w a r e 
                byte[] oldBytes = ms.ToArray();
                byte[] sonyVegasBytes = ReplaceBytes(oldBytes, magixBytes, sonyBytes);
                MemoryStream ms2 = new MemoryStream();
                ms2.Write(sonyVegasBytes, 0, sonyVegasBytes.Length);
                dic[sonyVegasDataStr] = ms2;
                dic[vegasDataStr] = ms2;
            }*/

            newData.SetData(vegasDataStr, dic[vegasDataStr]);
            newData.SetData(sonyVegasDataStr, dic[sonyVegasDataStr]);
        }

        if (dic[vegasMetaDataStr] != null || dic[sonyVegasMetaDataStr] != null)
        {
            dic[vegasMetaDataStr] = dic[vegasMetaDataStr] ?? dic[sonyVegasMetaDataStr];
            dic[sonyVegasMetaDataStr] = dic[sonyVegasMetaDataStr] ?? dic[vegasMetaDataStr];
            newData.SetData(vegasMetaDataStr, dic[vegasMetaDataStr]);
            newData.SetData(sonyVegasMetaDataStr, dic[sonyVegasMetaDataStr]);
        }

        Clipboard.SetDataObject(newData, true);
    }

    public static byte[] ReplaceBytes(byte[] bytes1, byte[] bytes2, byte[] bytes3)
    {
        if (bytes2 == null || bytes2.Length == 0)
            return bytes1;

        List<int> occurrences = new List<int>();
        int current = 0;
        int patternLength = bytes2.Length;
        int maxIndex = bytes1.Length - patternLength;

        while (current <= maxIndex)
        {
            bool match = true;
            for (int i = 0; i < patternLength; i++)
            {
                if (bytes1[current + i] != bytes2[i])
                {
                    match = false;
                    break;
                }
            }
            if (match)
            {
                occurrences.Add(current);
                current += patternLength;
            }
            else
            {
                current++;
            }
        }

        using (MemoryStream ms = new MemoryStream())
        {
            int previous = 0;
            foreach (int index in occurrences)
            {
                if (index > previous)
                {
                    ms.Write(bytes1, previous, index - previous);
                }
                ms.Write(bytes3, 0, bytes3.Length);
                previous = index + patternLength;
            }
            if (previous < bytes1.Length)
            {
                ms.Write(bytes1, previous, bytes1.Length - previous);
            }
            return ms.ToArray();
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

    public static T GetFirstSelectedEvent<T>(this Project project) where T : TrackEvent
    {
        List<T> l = project.GetSelectedEvents<T>();
        return l.Count > 0 ? l[0] : null;
    }

    public static Timecode GetEndTimeFromMarkers<T>(this IEnumerable<T> markers) where T : Marker
    {
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

    public static Timecode GetEndTimeFromEvents<T>(this IEnumerable<T> evs) where T : TrackEvent
    {
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

    public static Media GetValidMedia(this Vegas vegas, string path)
    {
        if (!File.Exists(path))
        {
            return null;
        }
        try
        {
            System.Reflection.MethodInfo method;
            if ((method = typeof(Vegas).GetMethod("MediaInfo", new Type[] { typeof(string) })) != null)
            {
                // VEGAS Pro 18+
                method.Invoke(vegas, new object[] { path });
            }
            else if ((method = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool), typeof(bool) })) != null)
            {
                // VEGAS Pro 22 Build 122+ (not recommended, for compatibility only)
                method.Invoke(vegas, new object[] { path, true, false });
            }
            else if ((method = typeof(Vegas).GetMethod("ImportFile", new Type[] { typeof(string), typeof(bool) })) != null)
            {
                // VEGAS Pro 22 Build 93-
                method.Invoke(vegas, new object[] { path, true });
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