#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Text;
using Microsoft.Win32;

namespace UltraPaste
{
    public static class DxtPresetModifier
    {
        public static string RoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        public static RegistryKey DxtReg = Registry.CurrentUser.CreateSubKey(Path.Combine("Software", "DXTransform", "Presets"));

        public static void SaveDxtEffectPreset(this PlugInNode plugIn, string presetName, string xmlString)
        {
            if (plugIn == null || plugIn.IsOFX)
            {
                return;
            }

            RegistryKey myReg = DxtReg.CreateSubKey(plugIn.UniqueID);
            string filePath = (string)myReg.GetValue(presetName) ?? Path.Combine(Common.VegasVersion > 13 ? Path.Combine(RoamingPath, "VEGAS", "FX Presets") : Path.Combine(RoamingPath, "Sony", "VEGAS", "FX Presets"), plugIn.UniqueID, presetName + ".dxp");
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

            data = data.InsertBytes(lengthBytes, 0);

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                fileStream.Write(data, 0, data.Length);
            }
        }

        public static byte[] InsertBytes(this byte[] data1, byte[] data2, int insertPos, int extendLength = 0)
        {
            byte[] newData = new byte[data1.Length + data2.Length + extendLength];
            Array.Copy(data1, 0, newData, 0, insertPos);
            Array.Copy(data2, 0, newData, insertPos, data2.Length);
            Array.Copy(data1, insertPos, newData, data2.Length + insertPos, data1.Length - insertPos);
            return newData;
        }

        public static string LoadDxtEffectPreset(this PlugInNode plugIn, string presetName)
        {
            if (plugIn == null || plugIn.IsOFX || string.IsNullOrEmpty(presetName))
            {
                return null;
            }

            RegistryKey myReg = DxtReg.CreateSubKey(plugIn.UniqueID);
            string filePath = (string)myReg.GetValue(presetName);
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return null;
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
                return null;
            }

            byte[] newData = new byte[data.Length - 4];
            Array.Copy(data, 4, newData, 0, newData.Length);
            return Encoding.UTF8.GetString(newData);
        }
    }
}