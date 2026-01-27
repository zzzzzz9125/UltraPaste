#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using System.Collections.Generic;

namespace UltraPaste.Utilities
{
    internal static class PlugInHelper
    {
        public static string FxPresetsPath = Path.Combine(VegasCommonHelper.VegasVersion > 13 ? Path.Combine(VegasCommonHelper.RoamingPath, "VEGAS", "FX Presets") : Path.Combine(VegasCommonHelper.RoamingPath, "Sony", "VEGAS", "FX Presets"));
        public static RegistryKey DxtReg = Registry.CurrentUser.CreateSubKey(Path.Combine("Software", "DXTransform", "Presets"));

        public static void SaveDxtEffectPresetXml(this PlugInNode plugIn, string presetName, string xmlString)
        {
            if (plugIn == null || plugIn.IsOFX)
            {
                return;
            }

            RegistryKey myReg = DxtReg.CreateSubKey(plugIn.UniqueID);
            string filePath = (string)myReg.GetValue(presetName) ?? Path.Combine(FxPresetsPath, plugIn.UniqueID, presetName + ".dxp");
            if (myReg.GetValue(presetName) == null)
            {
                myReg.SetValue(presetName, filePath);
            }
            if (!Directory.Exists(Path.GetDirectoryName(filePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(filePath));
            }
            byte[] data = Encoding.UTF8.GetBytes(xmlString);

            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            if (!BitConverter.IsLittleEndian)
            {
                Array.Reverse(lengthBytes);
            }

            List<byte> l = new List<byte>(lengthBytes);
            l.AddRange(data);
            data = l.ToArray();

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        public static void SaveDxtEffectPreset(this PlugInNode plugIn, string presetName, byte[] data)
        {
            if (plugIn == null || plugIn.IsOFX || string.IsNullOrEmpty(presetName))
            {
                return;
            }

            DxtReg.CreateSubKey(plugIn.UniqueID).SetValue(presetName, data);
        }

        public static void DeleteDxtEffectPreset(this PlugInNode plugIn, string presetName)
        {
            if (plugIn == null || plugIn.IsOFX || string.IsNullOrEmpty(presetName))
            {
                return;
            }

            DxtReg.CreateSubKey(plugIn.UniqueID).DeleteValue(presetName);
        }

        public static byte[] LoadDxtEffectPreset(this PlugInNode plugIn, string presetName)
        {
            if (plugIn == null || plugIn.IsOFX || string.IsNullOrEmpty(presetName))
            {
                return new byte[0];
            }

            RegistryKey myReg = DxtReg.CreateSubKey(plugIn.UniqueID);
            string filePath = myReg.GetValue(presetName) as string;
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                
                return myReg.GetValue(presetName) as byte[] ?? new byte[0];
            }

            byte[] data = null;
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                byte[] bytes = new byte[fileStream.Length];
                fileStream.Read(bytes, 0, bytes.Length);
                data = bytes;
            }

            if (data == null || data.Length < 4)
            {
                return new byte[0];
            }

            byte[] newData = new byte[data.Length - 4];
            Array.Copy(data, 4, newData, 0, newData.Length);
            return newData;
        }

        public static string[] GetAvailableDxtPresets(this PlugInNode plugIn)
        {
            if (plugIn == null || plugIn.IsOFX)
            {
                return new string [0];
            }

            return DxtReg.OpenSubKey(plugIn.UniqueID)?.GetValueNames() ?? new string[0];
        }

        public static bool IsGenerator(this PlugInNode p)
        {
            if (p == null)
            {
                return false;
            }
            return UltraPasteCommon.Vegas?.Generators.FindChildByUniqueID(p.UniqueID) != null;
        }

        public static string[] GetAvailablePresets(this PlugInNode plug)
        {
            if (plug == null)
            {
                return new string[0];
            }
            else if (plug.IsOFX)
            {
                List<string> l = new List<string>();
                foreach (EffectPreset p in plug.Presets)
                {
                    l.Add(p.Name);
                }
                return l.ToArray();
            }
            else
            {
                return plug.GetAvailableDxtPresets();
            }
        }


    }
}
