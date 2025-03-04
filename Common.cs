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

    public static void SetIconFile(this CustomCommand cmd, string fileName)
    {
        string path = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), fileName);
        if (!File.Exists(path))
        {
            cmd.IconFile = path;
        }
    }

    public static string SerializeXml(object data)
    {
        using (StringWriter sw = new StringWriter())
        {
            System.Xml.Serialization.XmlSerializer xz = new System.Xml.Serialization.XmlSerializer(data.GetType());
            xz.Serialize(sw, data);
            return sw.ToString();
        }
    }

    public static T DeserializeXml<T>(string path) where T : new()
    {
        T t = default;
        if (string.IsNullOrEmpty(path))
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