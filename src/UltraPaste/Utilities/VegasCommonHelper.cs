#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
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

namespace UltraPaste.Utilities
{
    internal static class VegasCommonHelper
    {
        public static int VegasVersion = FileVersionInfo.GetVersionInfo(Application.ExecutablePath).FileMajorPart;
        public static string RoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

#if !Sony
        public static System.Drawing.Color[] UIColors = new System.Drawing.Color[] { ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonFace, ScriptPortal.MediaSoftware.Skins.Skins.Colors.ButtonText };
#else
    public static System.Drawing.Color[] UIColors = new System.Drawing.Color[] { Sony.MediaSoftware.Skins.Skins.Colors.ButtonFace, Sony.MediaSoftware.Skins.Skins.Colors.ButtonText };
#endif

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

        // get a mix of modern VEGAS data and Sony one, allowing users to paste between the two
        public static void GenerateMixedVegasClipboardData()
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
    }
}