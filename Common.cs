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
    public static string AppFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
    public static string SettingsFolder = Path.Combine(VegasVersion < 14 ? Path.Combine(AppFolder, "Sony") : AppFolder, "VEGAS Pro", VegasVersion + ".0");

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

    // get all valid paths
    public static List<string> GetFilePathsFromPathList(System.Collections.Specialized.StringCollection pathList)
    {
        return GetFilePathsFromPathList(pathList, out _);
    }

    // get all valid paths, and output the extension when all files have a uniform extension
    public static List<string> GetFilePathsFromPathList(System.Collections.Specialized.StringCollection pathList, out string uniformExtension)
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
        using (StringWriter sw = new StringWriter())
        {
            XmlSerializer xz = new XmlSerializer(data.GetType());
            xz.Serialize(sw, data);
            return sw.ToString();
        }
    }

    public static T DeserializeXml<T>(string path) where T : new()
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
}