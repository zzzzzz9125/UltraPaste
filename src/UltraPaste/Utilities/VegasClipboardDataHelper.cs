#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using WindowsInput;
using WindowsInput.Native;

namespace UltraPaste.Utilities
{
    using UltraPaste.Core;
    internal class VegasClipboardDataHelper
    {
        private const string VEGAS_DATA_FORMAT = "Vegas Data 5.0";
        private const string SONY_VEGAS_DATA_FORMAT = "Sony Vegas Data 5.0";
        private const string VEGAS_METADATA_FORMAT = "Vegas Meta-Data 5.0";
        private const string SONY_VEGAS_METADATA_FORMAT = "Sony Vegas Meta-Data 5.0";

        /// <summary>
        /// Extracts Vegas clipboard data from the current clipboard and saves it as a .vegclb file.
        /// Prioritizes non-Sony formats if both Sony and non-Sony formats are present.
        /// </summary>
        /// <param name="filePath">The path where the .vegclb file will be saved</param>
        /// <param name="includeMediaFiles">Whether to include media files in the package</param>
        /// <returns>A VegasClipboardData instance if successful, null otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when clipboard data cannot be extracted</exception>
        public static VegasClipboardData ExtractFromClipboardAndSave(string filePath, bool includeMediaFiles = false)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");

            IDataObject dataObject = Clipboard.GetDataObject();
            if (dataObject == null)
                throw new InvalidOperationException("Failed to get clipboard data.");

            VegasClipboardData vegasData = ExtractFromDataObject(dataObject);
            if (vegasData == null || vegasData.DataBytes == null || vegasData.MetaDataBytes == null)
                throw new InvalidOperationException("Failed to extract Vegas clipboard data from clipboard.");

            vegasData.Name = Path.GetFileNameWithoutExtension(filePath);

            vegasData.Save(filePath, includeMediaFiles);
            return vegasData;
        }

        /// <summary>
        /// Extracts Vegas clipboard data from a data object.
        /// Prioritizes non-Sony formats if both Sony and non-Sony formats are present.
        /// </summary>
        /// <param name="dataObject">The data object to extract from</param>
        /// <returns>A VegasClipboardData instance if successful, null otherwise</returns>
        public static VegasClipboardData ExtractFromDataObject(IDataObject dataObject)
        {
            if (dataObject == null)
                return null;

            VegasClipboardData vegasData = new VegasClipboardData();

            // Extract DataBytes with priority: Vegas Data 5.0 > Sony Vegas Data 5.0
            object vegasDataObj = dataObject.GetData(VEGAS_DATA_FORMAT);
            if (vegasDataObj == null)
            {
                vegasDataObj = dataObject.GetData(SONY_VEGAS_DATA_FORMAT);
            }

            if (vegasDataObj is MemoryStream ms)
            {
                vegasData.DataBytes = ms.ToArray();
            }
            else if (vegasDataObj != null)
            {
                // Handle other stream types
                vegasData.DataBytes = ConvertToByteArray(vegasDataObj);
            }

            // Extract MetaDataBytes with priority: Vegas Meta-Data 5.0 > Sony Vegas Meta-Data 5.0
            object vegasMetadataObj = dataObject.GetData(VEGAS_METADATA_FORMAT);
            if (vegasMetadataObj == null)
            {
                vegasMetadataObj = dataObject.GetData(SONY_VEGAS_METADATA_FORMAT);
            }

            if (vegasMetadataObj is MemoryStream metaMs)
            {
                vegasData.MetaDataBytes = metaMs.ToArray();
            }
            else if (vegasMetadataObj != null)
            {
                // Handle other stream types
                vegasData.MetaDataBytes = ConvertToByteArray(vegasMetadataObj);
            }

            // Validate that we have both required data
            if (vegasData.DataBytes == null || vegasData.DataBytes.Length == 0 ||
                vegasData.MetaDataBytes == null || vegasData.MetaDataBytes.Length == 0)
            {
                return null;
            }

            return vegasData;
        }

        /// <summary>
        /// Converts an object to a byte array.
        /// </summary>
        private static byte[] ConvertToByteArray(object obj)
        {
            if (obj == null)
                return null;

            if (obj is byte[] bytes)
                return bytes;

            if (obj is MemoryStream ms)
                return ms.ToArray();

            if (obj is Stream stream)
            {
                byte[] buffer = new byte[stream.Length];
                stream.Seek(0, SeekOrigin.Begin);
                stream.Read(buffer, 0, buffer.Length);
                return buffer;
            }

            return null;
        }

        // get a mix of modern VEGAS data and Sony one, allowing users to paste to both versions
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

        /// <summary>
        /// Generates FX package clipboard event bytes by embedding the FX package data into a Vegas event structure.
        /// </summary>
        /// <param name="fxPackageName">The name of the FX package to retrieve and embed</param>
        /// <returns>A byte array representing the FX package clipboard event</returns>
        /// <exception cref="ArgumentNullException">Thrown when fxPackageName is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the FX package is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when the template file cannot be loaded or the pattern is not found</exception>
        public static byte[] GenerateFxPackageClipboardEventBytes(string fxPackageName)
        {
            if (string.IsNullOrEmpty(fxPackageName))
                throw new ArgumentNullException(nameof(fxPackageName), "FX package name cannot be null or empty.");

            byte[] packageBytes = VegasFxPackageHelper.FindFxPackageByName(fxPackageName) ?? throw new FileNotFoundException($"FX package '{fxPackageName}' not found in the registry.");
            var fxPackageData = VegasFxPackageHelper.VideoFxPackageData.Parse(packageBytes);
            byte[] clipboardPackageBytes = fxPackageData.ToClipboardBytes();

            byte[] templateBytes = GetEmptyVideoEventTemplateBytes() ?? throw new FileNotFoundException("Template file 'Vegas_Data_5.0.bin' not found or cannot be loaded.");

            // Search for the pattern: 84 00 00 00
            byte[] searchPattern = new byte[] { 0x84, 0x00, 0x00, 0x00 };
            int offset = FindBytePattern(templateBytes, searchPattern);
            if (offset < 0)
                throw new InvalidOperationException("Required byte pattern '84 00 00 00' not found in the template data.");

            // Create the result by:
            // 1. Copy everything before (offset - 4)
            // 2. Insert clipboardPackageBytes
            // 3. Copy everything from offset onwards
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(templateBytes, 0, offset - 4);
                ms.Write(clipboardPackageBytes, 0, clipboardPackageBytes.Length);
                ms.Write(templateBytes, offset, templateBytes.Length - offset);
                return ms.ToArray();
            }
        }

        /// <summary>
        /// Applies an FX package to the clipboard by generating clipboard data for both Vegas Data and Meta-Data formats.
        /// </summary>
        /// <param name="fxPackageName">The name of the FX package to apply</param>
        /// <param name="length">Optional event length to apply to the generated clipboard bytes.</param>
        /// <exception cref="ArgumentNullException">Thrown when fxPackageName is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the FX package is not found</exception>
        /// <exception cref="InvalidOperationException">Thrown when clipboard data cannot be generated</exception>
        public static void ApplyFxPackageToClipboard(string fxPackageName, Timecode length = null)
        {
            if (string.IsNullOrEmpty(fxPackageName))
                throw new ArgumentNullException(nameof(fxPackageName), "FX package name cannot be null or empty.");

            byte[] fxPackageBytes = GenerateFxPackageClipboardEventBytes(fxPackageName);
            if (fxPackageBytes == null || fxPackageBytes.Length == 0)
                throw new InvalidOperationException("Failed to generate FX package clipboard bytes.");

            if (length != null && fxPackageBytes?.Length > 0x6A)
            {
                long nanosValue = Math.Abs(length.Nanos);
                if (nanosValue > int.MaxValue)
                {
                    nanosValue = int.MaxValue;
                }

                byte[] nanosBytes = BitConverter.GetBytes((int)nanosValue);
                if (!BitConverter.IsLittleEndian)
                {
                    Array.Reverse(nanosBytes);
                }

                Array.Copy(nanosBytes, 0, fxPackageBytes, 0x20, 4);
                Array.Copy(nanosBytes, 0, fxPackageBytes, 0x68, 4);
            }

            // Clear clipboard data
            DataObject dataObject = new DataObject();

            // Set FX package bytes for Vegas Data formats
            MemoryStream dataStream = new MemoryStream(fxPackageBytes);
            dataObject.SetData(VEGAS_DATA_FORMAT, dataStream);
            dataObject.SetData(SONY_VEGAS_DATA_FORMAT, dataStream);

            // Load and set metadata bytes for Vegas Meta-Data formats
            byte[] metadataBytes = GetEmptyVideoEventMetadataTemplateBytes();
            if (metadataBytes == null || metadataBytes.Length == 0)
                throw new FileNotFoundException("Template file 'Vegas_Meta-Data_5.0.bin' not found or cannot be loaded.");

            MemoryStream metadataStream = new MemoryStream(metadataBytes);
            dataObject.SetData(VEGAS_METADATA_FORMAT, metadataStream);
            dataObject.SetData(SONY_VEGAS_METADATA_FORMAT, metadataStream);

            // Set the clipboard data
            Clipboard.SetDataObject(dataObject, true);
        }

        /// <summary>
        /// Finds the first occurrence of a byte pattern within a byte array.
        /// </summary>
        /// <param name="haystack">The byte array to search in</param>
        /// <param name="needle">The byte pattern to find</param>
        /// <returns>The offset of the first match</returns>
        /// <exception cref="ArgumentNullException">Thrown when haystack or needle is null</exception>
        /// <exception cref="ArgumentException">Thrown when needle is empty or longer than haystack</exception>
        private static int FindBytePattern(byte[] haystack, byte[] needle)
        {
            if (haystack == null)
                throw new ArgumentNullException(nameof(haystack), "Haystack array cannot be null.");
            if (needle == null)
                throw new ArgumentNullException(nameof(needle), "Needle array cannot be null.");
            if (needle.Length == 0)
                throw new ArgumentException("Needle array cannot be empty.", nameof(needle));
            if (haystack.Length < needle.Length)
                throw new ArgumentException("Haystack length cannot be less than needle length.", nameof(haystack));

            for (int i = 0; i <= haystack.Length - needle.Length; i++)
            {
                bool match = true;
                for (int j = 0; j < needle.Length; j++)
                {
                    if (haystack[i + j] != needle[j])
                    {
                        match = false;
                        break;
                    }
                }
                if (match)
                    return i;
            }
            throw new InvalidOperationException("Byte pattern not found in the haystack array.");
        }

        /// <summary>
        /// Gets the template data from the embedded vegclb resource.
        /// </summary>
        /// <returns>The parsed <see cref="VegasClipboardData"/> template.</returns>
        /// <exception cref="FileNotFoundException">Thrown when the template file cannot be found.</exception>
        private static VegasClipboardData GetEmptyVideoEventTemplateData()
        {
            string resourcePath = string.Format("Templates.{0}.vegclb", VegasCommonHelper.VegasVersionInfo.FileMajorPart > 20 ? "AdjustmentEvent" : "EmptyVideoEvent");
            byte[] templateBytes = ReadSourceBytes(resourcePath);
            using (MemoryStream stream = new MemoryStream(templateBytes))
            {
                return VegasClipboardData.Load(stream);
            }
        }

        /// <summary>
        /// Gets the template bytes from the embedded EmptyVideoEvent metadata resource.
        /// </summary>
        /// <returns>The metadata template bytes</returns>
        /// <exception cref="FileNotFoundException">Thrown when the metadata template file cannot be found</exception>
        private static byte[] GetEmptyVideoEventMetadataTemplateBytes()
        {
            try
            {
                VegasClipboardData template = GetEmptyVideoEventTemplateData();
                return template?.MetaDataBytes;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load metadata template file.", ex);
            }
        }

        /// <summary>
        /// Gets the template bytes from the embedded EmptyVideoEvent resource.
        /// </summary>
        /// <returns>The template bytes</returns>
        /// <exception cref="FileNotFoundException">Thrown when the template file cannot be found</exception>
        private static byte[] GetEmptyVideoEventTemplateBytes()
        {
            try
            {
                VegasClipboardData template = GetEmptyVideoEventTemplateData();
                return template?.DataBytes;
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to load template file.", ex);
            }
        }

        public static byte[] ReadSourceBytes(string resourceName)
        {
            string fullResourceName = $"UltraPaste.Resources.{resourceName}";
            using (Stream stream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(fullResourceName))
            {
                if (stream == null)
                {
                    throw new FileNotFoundException($"Resource '{fullResourceName}' not found.");
                }
                byte[] data = new byte[stream.Length];
                stream.Read(data, 0, data.Length);
                return data;
            }
        }

        public static void DoPasteEventAttributes(params bool[] predefinedOptions)
        {
            DoPasteEventAttributes(predefinedOptions.ToList());
        }

        public static void DoPasteEventAttributes(IList<bool> predefinedOptions = null)
        {
            UltraPasteCommon.Vegas.FocusToVegasMainWindow();

            InputSimulator inputSimulator = new InputSimulator();

            ReleaseModifierKeys(inputSimulator);

            if (VegasCommonHelper.VegasVersionInfo.FileMajorPart > 22)
            {
                // Ctrl + Alt + Shift + V (Selectively Paste Event Attributes, only for VP23+)
                inputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.LCONTROL, VirtualKeyCode.LMENU, VirtualKeyCode.LSHIFT }, VirtualKeyCode.VK_V);
            }
            else
            {
                // Alt + E ("Edit" Menu)
                inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LMENU, VirtualKeyCode.VK_E);

                // V (Paste Event Attributes)
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.VK_V);

                if (VegasCommonHelper.VegasVersionInfo.FileMajorPart > 14)
                {
                    // Down (Selectively Paste Event Attributes)
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);
                }

                // Enter
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.RETURN);
            }

            if (VegasCommonHelper.VegasVersionInfo.FileMajorPart < 15 || predefinedOptions == null || predefinedOptions.Count == 0)
            {
                return;
            }

            // Focus on "Clear All"
            for (int i = 0; i < 4; i++)
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            }

            // Space
            inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);

            // Focus on options list
            for (int i = 0; i < 3; i++)
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            }

            int maxCount = Math.Min(predefinedOptions.Count(), VegasCommonHelper.VegasVersionInfo.FileMajorPart > 19 ? 9 : 5);

            for (int i = 0; i < maxCount; i++)
            {
                // Down
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.DOWN);

                if (predefinedOptions[i])
                {
                    // Space
                    inputSimulator.Keyboard.KeyPress(VirtualKeyCode.SPACE);
                }
            }

            // Focus on "OK"
            for (int i = 0; i < 6; i++)
            {
                inputSimulator.Keyboard.KeyPress(VirtualKeyCode.TAB);
            }
        }

        public static void DoNormalCopy()
        {
            UltraPasteCommon.Vegas.FocusToVegasMainWindow();

            InputSimulator inputSimulator = new InputSimulator();

            ReleaseModifierKeys(inputSimulator);

            // Ctrl + C (Copy)
            inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_C);
        }

        public static void DoNormalPaste()
        {
            UltraPasteCommon.Vegas.FocusToVegasMainWindow();

            InputSimulator inputSimulator = new InputSimulator();

            ReleaseModifierKeys(inputSimulator);

            // Ctrl + V (Paste)
            inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.LCONTROL, VirtualKeyCode.VK_V);
        }

        private static void ReleaseModifierKeys(InputSimulator inputSimulator)
        {
            VirtualKeyCode[] commonKeys = new[]
            {
                VirtualKeyCode.LCONTROL, VirtualKeyCode.RCONTROL,
                VirtualKeyCode.LSHIFT, VirtualKeyCode.RSHIFT,
                VirtualKeyCode.LMENU, VirtualKeyCode.RMENU
            };

            foreach (VirtualKeyCode keyCode in commonKeys)
            {
                inputSimulator.Keyboard.KeyUp(keyCode);
            }
        }
    }
}