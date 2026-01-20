using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
using System.Text.RegularExpressions;

namespace CapCutDataParser
{
    using static CapCutJsonUtilities;
    public class CapCutData
    {
        public static Regex DraftPathRegex = new Regex(@"##_draftpath_placeholder_.+?##[/\\]?", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private CapCutData(List<CapCutMediaUsage> mediaUsages, List<CapCutSubtitleBlock> subtitles)
        {
            MediaUsages = mediaUsages;
            Subtitles = subtitles;
        }
        private string draftPath;
        public string DraftPath
        {
            get { return draftPath; }
            set
            {
                draftPath = value;

                if (MediaUsages != null)
                {
                    foreach (CapCutMediaUsage usage in MediaUsages)
                    {
                        usage.UpdateFullFilePath(draftPath);
                    }
                }
            }
        }

        public IReadOnlyList<CapCutMediaUsage> MediaUsages { get; }

        public IReadOnlyList<CapCutSubtitleBlock> Subtitles { get; }

        public static CapCutData ParseFile(string file)
        {
            CapCutData data = Parse(File.ReadAllText(file));
            if (data != null)
            {
                data.DraftPath = Path.GetDirectoryName(file);
            }
            return data;
        }

        public static CapCutData Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("CapCut draft content is empty.", nameof(input));
            }

            string json = input;

            try
            {
                JsonParser.Parse(json);
            }
            catch (FormatException)
            {
                string decrypted = DecryptString(input);
                if (decrypted != null)
                {
                    json = decrypted;
                }
            }

            Dictionary<string, object> root;
            try
            {
                root = JsonParser.Parse(json) as Dictionary<string, object>;
            }
            catch (FormatException ex)
            {
                throw new InvalidDataException("Unable to parse CapCut draft json.", ex);
            }

            if (root == null)
            {
                throw new InvalidDataException("CapCut draft content is invalid.");
            }

            var materialIndex = MaterialIndex.Create(GetObject(root, "materials"));
            var mediaUsages = new List<CapCutMediaUsage>();
            var subtitles = new List<CapCutSubtitleBlock>();

            foreach (var track in EnumerateObjects(GetList(root, "tracks")))
            {
                var trackType = GetString(track, "type");
                if (string.IsNullOrWhiteSpace(trackType))
                {
                    continue;
                }

                foreach (var segment in EnumerateObjects(GetList(track, "segments")))
                {
                    var timerange = GetObject(segment, "target_timerange");
                    if (timerange == null)
                    {
                        continue;
                    }

                    var start = GetLong(timerange, "start");
                    var duration = GetLong(timerange, "duration");
                    if (duration <= 0)
                    {
                        continue;
                    }

                    var startTime = ToTimeSpan(start);
                    var endTime = ToTimeSpan(start + duration);
                    var materialId = GetString(segment, "material_id");

                    switch (trackType.ToLowerInvariant())
                    {
                        case "video":
                            if (materialIndex.TryGetVideo(materialId, out var video))
                            {
                                mediaUsages.Add(new CapCutMediaUsage
                                {
                                    MediaType = CapCutMediaType.Video,
                                    MaterialId = video.MaterialId,
                                    Name = video.Name,
                                    Path = video.Path,
                                    Start = startTime,
                                    End = endTime
                                });
                            }
                            break;

                        case "audio":
                            if (materialIndex.TryGetAudio(materialId, out var audio))
                            {
                                mediaUsages.Add(new CapCutMediaUsage
                                {
                                    MediaType = CapCutMediaType.Audio,
                                    MaterialId = audio.MaterialId,
                                    Name = audio.Name,
                                    Path = audio.Path,
                                    Start = startTime,
                                    End = endTime
                                });
                            }
                            break;

                        case "text":
                            if (materialIndex.TryGetText(materialId, out var textMaterial))
                            {
                                var text = ParseTextContent(textMaterial.Content);
                                if (!string.IsNullOrWhiteSpace(text))
                                {
                                    subtitles.Add(new CapCutSubtitleBlock
                                    {
                                        MaterialId = textMaterial.MaterialId,
                                        Text = text,
                                        Start = startTime,
                                        End = endTime
                                    });
                                }
                            }
                            break;
                    }
                }
            }

            return new CapCutData(mediaUsages, subtitles);
        }

        public static string Encrypt(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                throw new ArgumentException("CapCut draft content is empty.", nameof(input));
            }

            var encrypted = EncryptString(input);
            return encrypted ?? throw new InvalidOperationException("Unable to encrypt CapCut draft content.");
        }

        private static string ParseTextContent(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return string.Empty;
            }

            try
            {
                var obj = JsonParser.Parse(content) as Dictionary<string, object>;
                var text = GetString(obj, "text");
                if (!string.IsNullOrWhiteSpace(text))
                {
                    return NormalizeNewLines(text);
                }
            }
            catch (FormatException)
            {
                // fallback to raw content when payload is not JSON
            }

            return NormalizeNewLines(content);
        }

        private static string NormalizeNewLines(string input)
        {
            return input?.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private static TimeSpan ToTimeSpan(long microseconds)
        {
            return TimeSpan.FromTicks(microseconds * 10);
        }

        private static string DecryptString(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return null;
            }

            if (!TryExtractKeyAndIv(input, out var keyIv, out var payload))
            {
                return null;
            }

            if (!TrySplitCipherAndTag(payload, out var cipher, out var tag))
            {
                return null;
            }

            var keyBytes = Encoding.ASCII.GetBytes(keyIv.Substring(0, KeyLength));
            var ivBytes = Encoding.ASCII.GetBytes(keyIv.Substring(KeyLength, IvLength));

            if (!AesGcmCng.TryDecrypt(keyBytes, ivBytes, cipher, tag, out var plaintext))
            {
                return null;
            }

            return Encoding.UTF8.GetString(plaintext);
        }

        private static string EncryptString(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return null;
            }

            var plaintext = Encoding.UTF8.GetBytes(input);
            var key = GenerateRandomToken(KeyLength);
            var iv = GenerateRandomToken(IvLength);
            var keyBytes = Encoding.ASCII.GetBytes(key);
            var ivBytes = Encoding.ASCII.GetBytes(iv);

            if (!AesGcmCng.TryEncrypt(keyBytes, ivBytes, plaintext, out var cipher, out var tag))
            {
                return null;
            }

            var encoded = BuildEncryptedPayload(cipher, tag);
            return InsertKeyAndIv(encoded, key + iv);
        }

        private static bool TrySplitCipherAndTag(string payload, out byte[] cipher, out byte[] tag)
        {
            cipher = null;
            tag = null;

            if (string.IsNullOrEmpty(payload))
            {
                return false;
            }

            byte[] raw;
            try
            {
                raw = Convert.FromBase64String(payload);
            }
            catch (FormatException)
            {
                return false;
            }

            if (raw.Length < AuthTagSize)
            {
                return false;
            }

            var cipherLength = raw.Length - AuthTagSize;
            cipher = new byte[cipherLength];
            if (cipherLength > 0)
            {
                Buffer.BlockCopy(raw, 0, cipher, 0, cipherLength);
            }

            tag = new byte[AuthTagSize];
            Buffer.BlockCopy(raw, raw.Length - AuthTagSize, tag, 0, AuthTagSize);
            return true;
        }

        private static string BuildEncryptedPayload(byte[] cipher, byte[] tag)
        {
            var cipherLength = cipher?.Length ?? 0;
            var combined = new byte[cipherLength + AuthTagSize];
            if (cipherLength > 0)
            {
                Buffer.BlockCopy(cipher, 0, combined, 0, cipherLength);
            }

            Buffer.BlockCopy(tag, 0, combined, cipherLength, AuthTagSize);
            return Convert.ToBase64String(combined);
        }

        private static bool TryExtractKeyAndIv(string input, out string keyIv, out string payload)
        {
            keyIv = null;
            payload = null;

            if (string.IsNullOrEmpty(input) || input.Length < MinimumKeyCarrierLength)
            {
                return false;
            }

            var chars = new List<char>(input);
            var segments = new List<string>(KeyIvOffsets.Length);

            for (int i = KeyIvOffsets.Length - 1; i >= 0; i--)
            {
                int offset = KeyIvOffsets[i];
                if (offset + KeyIvSegmentLength > chars.Count)
                {
                    return false;
                }

                var slice = chars.GetRange(offset, KeyIvSegmentLength);
                chars.RemoveRange(offset, KeyIvSegmentLength);
                segments.Insert(0, new string(slice.ToArray()));
            }

            var builder = new StringBuilder(KeyLength + IvLength);
            foreach (var segment in segments)
            {
                builder.Append(segment);
            }

            keyIv = builder.ToString();
            payload = new string(chars.ToArray());
            return keyIv.Length == KeyLength + IvLength;
        }

        private static string InsertKeyAndIv(string payload, string keyIv)
        {
            if (string.IsNullOrEmpty(payload) || string.IsNullOrEmpty(keyIv) || keyIv.Length != KeyLength + IvLength)
            {
                return null;
            }

            if (payload.Length < MinimumPayloadLength)
            {
                return null;
            }

            var chars = new List<char>(payload);
            int index = 0;
            foreach (var offset in KeyIvOffsets)
            {
                var segment = keyIv.Substring(index, KeyIvSegmentLength);
                int insertIndex = offset <= chars.Count ? offset : chars.Count;
                chars.InsertRange(insertIndex, segment);
                index += KeyIvSegmentLength;
            }

            return new string(chars.ToArray());
        }

        private static string GenerateRandomToken(int length)
        {
            var buffer = new char[length];
            var randomBytes = new byte[length];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }

            for (int i = 0; i < length; i++)
            {
                buffer[i] = RandomTable[randomBytes[i] % RandomTable.Length];
            }

            return new string(buffer);
        }

        private const int KeyLength = 32;
        private const int IvLength = 16;
        private const int KeyIvSegmentLength = 4;
        private const int AuthTagSize = 16;
        private const int MinimumKeyCarrierLength = 131;
        private const int MinimumPayloadLength = 90;
        private const string RandomTable = "0123456789abcdefghijABCDEFGHIJ";
        private static readonly int[] KeyIvOffsets = { 0, 7, 20, 33, 40, 47, 59, 66, 76, 89, 99, 127 };

        [StructLayout(LayoutKind.Sequential)]
        private struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO
        {
            public int cbSize;
            public int dwInfoVersion;
            public IntPtr pbNonce;
            public int cbNonce;
            public IntPtr pbAuthData;
            public int cbAuthData;
            public IntPtr pbTag;
            public int cbTag;
            public IntPtr pbMacContext;
            public int cbMacContext;
            public int cbAAD;
            public long cbData;
            public int dwFlags;

            public void Init()
            {
                cbSize = Marshal.SizeOf(typeof(BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO));
                dwInfoVersion = 1;
                pbAuthData = IntPtr.Zero;
                pbMacContext = IntPtr.Zero;
            }
        }

        private sealed class PinnedBuffer : IDisposable
        {
            private readonly GCHandle _handle;

            public PinnedBuffer(byte[] buffer)
            {
                Buffer = buffer;
                if (buffer != null && buffer.Length > 0)
                {
                    _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
                }
            }

            public byte[] Buffer { get; }

            public IntPtr Pointer => Buffer != null && Buffer.Length > 0 ? _handle.AddrOfPinnedObject() : IntPtr.Zero;

            public void Dispose()
            {
                if (_handle.IsAllocated)
                {
                    _handle.Free();
                }
            }
        }

        private static class AesGcmCng
        {
            internal static bool TryEncrypt(byte[] key, byte[] nonce, byte[] plaintext, out byte[] ciphertext, out byte[] tag)
            {
                tag = new byte[AuthTagSize];
                return TryCrypt(true, key, nonce, plaintext, tag, out ciphertext);
            }

            internal static bool TryDecrypt(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag, out byte[] plaintext)
            {
                return TryCrypt(false, key, nonce, ciphertext, tag, out plaintext);
            }

            private static bool TryCrypt(bool encrypt, byte[] key, byte[] nonce, byte[] input, byte[] tag, out byte[] output)
            {
                output = null;

                if (key == null || (key.Length != 16 && key.Length != 24 && key.Length != 32))
                {
                    return false;
                }

                if (nonce == null || nonce.Length == 0 || tag == null || tag.Length != AuthTagSize)
                {
                    return false;
                }

                input = input ?? Array.Empty<byte>();

                try
                {
                    using (var algorithm = OpenAlgorithm())
                    using (var keyHandle = CreateKeyHandle(algorithm, key))
                    {
                        var authInfo = CreateAuthInfo(nonce, tag, input.Length);
                        using (var noncePin = new PinnedBuffer(nonce))
                        using (var tagPin = new PinnedBuffer(tag))
                        {
                            authInfo.pbNonce = noncePin.Pointer;
                            authInfo.pbTag = tagPin.Pointer;

                            var buffer = new byte[input.Length];
                            int resultLength;

                            if (encrypt)
                            {
                                EnsureSuccess(NativeMethods.BCryptEncrypt(keyHandle, input, input.Length, ref authInfo, null, 0, buffer, buffer.Length, out resultLength, 0));
                            }
                            else
                            {
                                EnsureSuccess(NativeMethods.BCryptDecrypt(keyHandle, input, input.Length, ref authInfo, null, 0, buffer, buffer.Length, out resultLength, 0));
                            }

                            if (resultLength != buffer.Length)
                            {
                                Array.Resize(ref buffer, resultLength);
                            }

                            output = buffer;
                            return true;
                        }
                    }
                }
                catch (CryptographicException)
                {
                    output = null;
                    return false;
                }
            }

            private static SafeAlgorithmHandle OpenAlgorithm()
            {
                EnsureSuccess(NativeMethods.BCryptOpenAlgorithmProvider(out var handle, NativeMethods.BCRYPT_AES_ALGORITHM, null, 0));
                try
                {
                    SetChainingMode(handle);
                    return handle;
                }
                catch
                {
                    handle.Dispose();
                    throw;
                }
            }

            private static void SetChainingMode(SafeAlgorithmHandle handle)
            {
                var value = Encoding.Unicode.GetBytes(NativeMethods.BCRYPT_CHAIN_MODE_GCM + "\0");
                EnsureSuccess(NativeMethods.BCryptSetProperty(handle, NativeMethods.BCRYPT_CHAINING_MODE, value, value.Length, 0));
            }

            private static SafeKeyHandle CreateKeyHandle(SafeAlgorithmHandle algorithm, byte[] key)
            {
                int objectLength = GetIntProperty(algorithm, NativeMethods.BCRYPT_OBJECT_LENGTH);
                var keyObject = new byte[objectLength];
                var keyObjectHandle = GCHandle.Alloc(keyObject, GCHandleType.Pinned);
                try
                {
                    EnsureSuccess(NativeMethods.BCryptGenerateSymmetricKey(algorithm, out var keyHandle, keyObjectHandle.AddrOfPinnedObject(), objectLength, key, key.Length, 0));
                    keyHandle.SetKeyObjectHandle(keyObjectHandle);
                    return keyHandle;
                }
                catch
                {
                    keyObjectHandle.Free();
                    throw;
                }
            }

            private static BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO CreateAuthInfo(byte[] nonce, byte[] tag, int dataLength)
            {
                var info = new BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO();
                info.Init();
                info.cbNonce = nonce.Length;
                info.cbAuthData = 0;
                info.cbTag = tag.Length;
                info.cbMacContext = 0;
                info.cbAAD = 0;
                info.cbData = dataLength;
                info.dwFlags = 0;
                return info;
            }

            private static int GetIntProperty(SafeHandle handle, string propertyName)
            {
                var buffer = new byte[sizeof(int)];
                EnsureSuccess(NativeMethods.BCryptGetProperty(handle, propertyName, buffer, buffer.Length, out _, 0));
                return BitConverter.ToInt32(buffer, 0);
            }

            private static void EnsureSuccess(int status)
            {
                if (status != NativeMethods.STATUS_SUCCESS)
                {
                    throw new CryptographicException(string.Format("BCrypt error 0x{0:X8}.", status));
                }
            }
        }

        private sealed class SafeAlgorithmHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            public SafeAlgorithmHandle() : base(true)
            {
            }

            protected override bool ReleaseHandle()
            {
                return NativeMethods.BCryptCloseAlgorithmProvider(handle, 0) == NativeMethods.STATUS_SUCCESS;
            }
        }

        private sealed class SafeKeyHandle : SafeHandleZeroOrMinusOneIsInvalid
        {
            private GCHandle _keyObjectHandle;

            public SafeKeyHandle() : base(true)
            {
            }

            internal void SetKeyObjectHandle(GCHandle handle)
            {
                _keyObjectHandle = handle;
            }

            protected override bool ReleaseHandle()
            {
                if (_keyObjectHandle.IsAllocated)
                {
                    _keyObjectHandle.Free();
                }

                return NativeMethods.BCryptDestroyKey(handle) == NativeMethods.STATUS_SUCCESS;
            }
        }

        private static class NativeMethods
        {
            internal const int STATUS_SUCCESS = 0;
            internal const string BCRYPT_AES_ALGORITHM = "AES";
            internal const string BCRYPT_CHAINING_MODE = "ChainingMode";
            internal const string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";
            internal const string BCRYPT_OBJECT_LENGTH = "ObjectLength";

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            internal static extern int BCryptOpenAlgorithmProvider(out SafeAlgorithmHandle phAlgorithm, string pszAlgId, string pszImplementation, int dwFlags);

            [DllImport("bcrypt.dll")]
            internal static extern int BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, int dwFlags);

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            internal static extern int BCryptSetProperty(SafeAlgorithmHandle hObject, string pszProperty, byte[] pbInput, int cbInput, int dwFlags);

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            internal static extern int BCryptGetProperty(SafeHandle hObject, string pszProperty, byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);

            [DllImport("bcrypt.dll")]
            internal static extern int BCryptGenerateSymmetricKey(SafeAlgorithmHandle hAlgorithm, out SafeKeyHandle phKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbSecret, int cbSecret, int dwFlags);

            [DllImport("bcrypt.dll")]
            internal static extern int BCryptDestroyKey(IntPtr hKey);

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            internal static extern int BCryptEncrypt(SafeKeyHandle hKey, byte[] pbInput, int cbInput, ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);

            [DllImport("bcrypt.dll", CharSet = CharSet.Unicode)]
            internal static extern int BCryptDecrypt(SafeKeyHandle hKey, byte[] pbInput, int cbInput, ref BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);
        }
    }
}