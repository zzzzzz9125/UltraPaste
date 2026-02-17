using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace UltraPaste.Utilities
{
    internal class VegasFxPackagesHelper
    {
        private const string FX_PACKAGES_REGISTRY_PATH = @"Software\DirectShow\Presets\FX Packages\";

        /// <summary>
        /// Finds an FX Package by its GUID and returns the Stream value as a byte array.
        /// </summary>
        /// <param name="guid">The GUID as a Guid object</param>
        /// <returns>The Stream value as a byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when guid is null</exception>
        /// <exception cref="FileNotFoundException">Thrown when the FX package is not found in the registry</exception>
        public static byte[] FindFxPackageByGuid(Guid guid)
        {
            return FindFxPackageByGuid(guid.ToString("B").ToUpperInvariant());
        }

        /// <summary>
        /// Finds an FX Package by its GUID string and returns the Stream value as a byte array.
        /// </summary>
        /// <param name="guidString">The GUID as a string in the format {XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}</param>
        /// <returns>The Stream value as a byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when guidString is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the FX package is not found in the registry</exception>
        public static byte[] FindFxPackageByGuid(string guidString)
        {
            if (string.IsNullOrEmpty(guidString))
                throw new ArgumentNullException(nameof(guidString), "GUID string cannot be null or empty.");

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(FX_PACKAGES_REGISTRY_PATH + guidString))
                {
                    if (key == null)
                        throw new FileNotFoundException($"FX package with GUID '{guidString}' not found in the registry.");

                    // Ensure Type == 5 (REG_DWORD)
                    object typeObj = key.GetValue("Type");
                    int typeVal = -1;
                    if (typeObj is int)
                    {
                        typeVal = (int)typeObj;
                    }
                    else if (typeObj is long)
                    {
                        typeVal = Convert.ToInt32(typeObj);
                    }

                    if (typeVal != 5)
                    {
                        throw new FileNotFoundException($"FX package with GUID '{guidString}' is not available (Type != 5).");
                    }

                    return ReadStreamValue(key, guidString);
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to find FX package by GUID '{guidString}'.", ex);
            }
        }

        /// <summary>
        /// Finds an FX Package by name and returns the Stream value as a byte array.
        /// Searches through all GUIDs in the FX Packages registry and matches by the Name value.
        /// Only packages with Type == 5 are considered available.
        /// </summary>
        /// <param name="fxPackageName">The name of the FX Package to find</param>
        /// <returns>The Stream value as a byte array</returns>
        /// <exception cref="ArgumentNullException">Thrown when fxPackageName is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the FX package is not found</exception>
        public static byte[] FindFxPackageByName(string fxPackageName)
        {
            if (string.IsNullOrEmpty(fxPackageName))
                throw new ArgumentNullException(nameof(fxPackageName), "FX package name cannot be null or empty.");

            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(FX_PACKAGES_REGISTRY_PATH))
                {
                    if (key == null)
                        throw new FileNotFoundException("FX Packages registry path not found.");

                    string[] subKeyNames = key.GetSubKeyNames();

                    foreach (string subKeyName in subKeyNames)
                    {
                        using (RegistryKey subKey = key.OpenSubKey(subKeyName))
                        {
                            if (subKey == null)
                                continue;

                            // Ensure Type == 5 (REG_DWORD)
                            object typeObj = subKey.GetValue("Type");
                            int typeVal = -1;
                            if (typeObj is int)
                            {
                                typeVal = (int)typeObj;
                            }
                            else if (typeObj is long)
                            {
                                typeVal = Convert.ToInt32(typeObj);
                            }

                            if (typeVal != 5)
                            {
                                continue;
                            }

                            object nameValue = subKey.GetValue("Name");
                            if (nameValue == null || !(nameValue is string))
                                continue;

                            string registryName = (string)nameValue;
                            if (string.Equals(registryName, fxPackageName, StringComparison.OrdinalIgnoreCase))
                            {
                                byte[] bytes = ReadStreamValue(subKey, fxPackageName);
                                return bytes;
                            }
                        }
                    }

                    throw new FileNotFoundException($"FX package '{fxPackageName}' not found in the registry.");
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to find FX package by name '{fxPackageName}'.", ex);
            }
        }

        /// <summary>
        /// Reads the Stream value from a registry key and returns it as a byte array.
        /// If Stream is REG_BINARY, reads it directly.
        /// If Stream is REG_SZ, treats it as a file path and reads the file.
        /// </summary>
        /// <param name="key">The registry key to read from</param>
        /// <param name="packageIdentifier">The package identifier for error messages</param>
        /// <returns>The Stream value as a byte array</returns>
        /// <exception cref="InvalidOperationException">Thrown when Stream value cannot be read or processed</exception>
        private static byte[] ReadStreamValue(RegistryKey key, string packageIdentifier)
        {
            try
            {
                object streamValue = key.GetValue("Stream") ?? throw new InvalidOperationException($"Stream value not found for package '{packageIdentifier}'.");
                if (streamValue is byte[] v) // REG_BINARY type
                {
                    return v;
                }
                else if (streamValue is string filePath) // REG_SZ type - treat as file path
                {
                    if (!File.Exists(filePath))
                        throw new FileNotFoundException($"Stream file path does not exist: {filePath}");

                    try
                    {
                        return File.ReadAllBytes(filePath);
                    }
                    catch (FileNotFoundException)
                    {
                        throw;
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException($"Failed to read stream file: {filePath}", ex);
                    }
                }
                else
                {
                    throw new InvalidOperationException($"Stream value has unsupported type: {streamValue.GetType().Name}");
                }
            }
            catch (FileNotFoundException)
            {
                throw;
            }
            catch (InvalidOperationException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to read stream value for package '{packageIdentifier}'.", ex);
            }
        }

        public class VideoFxData
        {
            /// <summary>
            /// Gets or sets the unique identifier string of the video effect.
            /// </summary>
            public string UniqueId { get; set; }

            /// <summary>
            /// Gets or sets the raw binary parameter data of the video effect.
            /// </summary>
            public byte[] Parameters { get; set; }

            /// <summary>
            /// Converts this VideoFxData instance into its binary representation.
            /// The binary format is:
            /// [16-byte fixed header] + [4-byte ID length (UInt32, little-endian)] + [ID bytes (Encoding.Default)] +
            /// [4-byte parameter length (UInt32, little-endian)] + [parameter bytes].
            /// </summary>
            /// <returns>A byte array containing the binary data of this video effect.</returns>
            public byte[] ToBytes()
            {
                // Fixed header for all video effects.
                byte[] header = new byte[]
                {
                    0x41, 0x1A, 0xFC, 0xA2, 0xF6, 0x01, 0x0E, 0x46,
                    0xAE, 0x5A, 0x2B, 0xE4, 0xAF, 0x57, 0xB9, 0x2E
                };

                byte[] idBytes = Encoding.Default.GetBytes(UniqueId ?? string.Empty);
                byte[] paramBytes = Parameters ?? new byte[0];

                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(ms))
                    {
                        writer.Write(header);
                        writer.Write((uint)idBytes.Length); // ID length, little-endian
                        writer.Write(idBytes);
                        writer.Write((uint)paramBytes.Length); // Parameter length, little-endian
                        writer.Write(paramBytes);
                        return ms.ToArray();
                    }
                }
            }

            /// <summary>
            /// Parses a single video effect from its complete binary representation (including the header).
            /// </summary>
            /// <param name="data">Byte array containing exactly one video effect's data.</param>
            /// <returns>A VideoFxData object populated from the binary data.</returns>
            /// <exception cref="ArgumentNullException">Thrown when data is null</exception>
            /// <exception cref="ArgumentException">Thrown when the data is too short or invalid.</exception>
            /// <exception cref="InvalidDataException">Thrown when the fixed header does not match the expected value.</exception>
            public static VideoFxData Parse(byte[] data)
            {
                if (data == null)
                    throw new ArgumentNullException(nameof(data), "Data array cannot be null.");
                if (data.Length < 16 + 4)
                {
                    throw new ArgumentException("Data is too short to contain a valid video effect. Minimum 20 bytes required.", nameof(data));
                }

                int offset = 0;

                // Read and verify the fixed header.
                byte[] header = new byte[16];
                Array.Copy(data, offset, header, 0, 16);
                byte[] expectedHeader = new byte[]
                {
                    0x41, 0x1A, 0xFC, 0xA2, 0xF6, 0x01, 0x0E, 0x46,
                    0xAE, 0x5A, 0x2B, 0xE4, 0xAF, 0x57, 0xB9, 0x2E
                };
                if (!header.SequenceEqual(expectedHeader))
                {
                    throw new InvalidDataException("Fixed header mismatch. The data may be corrupted or not a video effect.");
                }

                offset += 16;

                // Read ID length (UInt32, little-endian)
                uint idLength = BitConverter.ToUInt32(data, offset);
                offset += 4;

                if (offset + idLength > data.Length)
                    throw new InvalidDataException("ID length exceeds available data.");

                string uniqueId = Encoding.Default.GetString(data, offset, (int)idLength);
                offset += (int)idLength;

                // Read parameter length (UInt32, little-endian)
                uint paramLength = BitConverter.ToUInt32(data, offset);
                offset += 4;

                if (offset + paramLength > data.Length)
                    throw new InvalidDataException("Parameter length exceeds available data.");

                byte[] parameters = new byte[paramLength];
                Array.Copy(data, offset, parameters, 0, paramLength);

                return new VideoFxData
                {
                    UniqueId = uniqueId,
                    Parameters = parameters
                };
            }
        }

        public class VideoFxPackageData : List<VideoFxData>
        {
            /// <summary>
            /// Parses an FX package byte array into a list of VideoFxData objects.
            /// The first 8 bytes and the last 8 bytes of the input are ignored.
            /// The remaining bytes are expected to contain one or more video effect blocks.
            /// </summary>
            /// <param name="fxPackageBytes">The full FX package binary data.</param>
            /// <returns>A list of VideoFxData objects parsed from the package.</returns>
            /// <exception cref="ArgumentNullException">Thrown when fxPackageBytes is null</exception>
            /// <exception cref="ArgumentException">Thrown when the input is too short.</exception>
            /// <exception cref="InvalidDataException">Thrown when a header mismatch or incomplete data is encountered.</exception>
            public static VideoFxPackageData Parse(byte[] fxPackageBytes)
            {
                if (fxPackageBytes == null)
                    throw new ArgumentNullException(nameof(fxPackageBytes), "FX package bytes array cannot be null.");
                if (fxPackageBytes.Length < 16)
                    throw new ArgumentException("FX package data is too short. Must be at least 16 bytes.", nameof(fxPackageBytes));

                // Skip the first 8 bytes and the last 8 bytes.
                int innerLength = fxPackageBytes.Length - 16;
                byte[] innerData = new byte[innerLength];
                Array.Copy(fxPackageBytes, 8, innerData, 0, innerLength);

                VideoFxPackageData package = new VideoFxPackageData();
                int index = 0;

                while (index < innerData.Length)
                {
                    // Ensure we have at least the header and the next 4 bytes (ID length) available.
                    if (index + 16 + 4 > innerData.Length)
                    {
                        break; // Not enough data for another effect; stop parsing.
                    }

                    // Verify the fixed header at the current position.
                    byte[] header = new byte[16];
                    Array.Copy(innerData, index, header, 0, 16);
                    byte[] expectedHeader = new byte[]
                    {
                        0x41, 0x1A, 0xFC, 0xA2, 0xF6, 0x01, 0x0E, 0x46,
                        0xAE, 0x5A, 0x2B, 0xE4, 0xAF, 0x57, 0xB9, 0x2E
                    };
                    if (!header.SequenceEqual(expectedHeader))
                    {
                        throw new InvalidDataException($"Fixed header mismatch at position {index} in the inner package data.");
                    }

                    index += 16;

                    // Read ID length.
                    uint idLength = BitConverter.ToUInt32(innerData, index);
                    index += 4;

                    if (index + idLength > innerData.Length)
                    {
                        throw new InvalidDataException("ID length exceeds available data in package.");
                    }

                    string uniqueId = Encoding.Default.GetString(innerData, index, (int)idLength);
                    index += (int)idLength;

                    // Read parameter length.
                    uint paramLength = BitConverter.ToUInt32(innerData, index);
                    index += 4;

                    if (index + paramLength > innerData.Length)
                    {
                        throw new InvalidDataException("Parameter length exceeds available data in package.");
                    }

                    byte[] parameters = new byte[paramLength];
                    Array.Copy(innerData, index, parameters, 0, paramLength);
                    index += (int)paramLength;

                    package.Add(new VideoFxData
                    {
                        UniqueId = uniqueId,
                        Parameters = parameters
                    });
                }

                return package;
            }

            /// <summary>
            /// Converts this VideoFxPackageData into clipboard-compatible binary format.
            /// The format is: [4-byte Count (UInt32, little-endian)] + [concatenated Parameters from each VideoFxData].
            /// </summary>
            /// <returns>A byte array containing the clipboard format of this FX package.</returns>
            public byte[] ToClipboardBytes()
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    using (BinaryWriter writer = new BinaryWriter(ms))
                    {
                        // Write the count of effects (UInt32, little-endian)
                        writer.Write((uint)this.Count);

                        // Concatenate parameters from each VideoFxData
                        foreach (VideoFxData fxData in this)
                        {
                            if (fxData.Parameters != null && fxData.Parameters.Length > 0)
                            {
                                writer.Write(fxData.Parameters);
                            }
                        }

                        return ms.ToArray();
                    }
                }
            }
        }
    }
}
