using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CapCutDataParser
{
    public static class CapCutMediaDecryptor
    {
        public static bool TryEnsureDecryptedMedia(string filePath, out string newFilePath)
        {
            newFilePath = null;
            try
            {
                if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
                {
                    return false;
                }

                string extension = Path.GetExtension(filePath)?.ToLowerInvariant();
                if (string.IsNullOrEmpty(extension))
                {
                    return false;
                }

                List<FileSignature> signatures = GetSignatures(extension).ToList();
                if (signatures.Count == 0)
                {
                    return false;
                }

                string decryptedPath = BuildDecryptedPath(filePath);
                if (IsExistingDecryptionUpToDate(filePath, decryptedPath))
                {
                    newFilePath = decryptedPath;
                    return true;
                }

                foreach (FileSignature signature in signatures)
                {
                    if (!TryDeriveXorKey(filePath, signature, out byte key))
                    {
                        continue;
                    }

                    if (TryDecryptFile(filePath, decryptedPath, key))
                    {
                        newFilePath = decryptedPath;
                        return true;
                    }
                }
            }
            catch
            {
                // ignored ¨C caller will fall back to the original path
            }

            return false;
        }

        private static IEnumerable<FileSignature> GetSignatures(string extension)
        {
            foreach (FileSignature signature in FileSignatures)
            {
                if (signature.Extensions.Contains(extension))
                {
                    yield return signature;
                }
            }
        }

        private static bool TryDeriveXorKey(string path, FileSignature signature, out byte key)
        {
            key = 0;
            byte[] plain = signature.Pattern;
            byte[] buffer = new byte[plain.Length];

            using (FileStream stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                if (stream.Length < signature.Offset + plain.Length)
                {
                    return false;
                }

                stream.Seek(signature.Offset, SeekOrigin.Begin);
                int totalRead = 0;
                while (totalRead < plain.Length)
                {
                    int read = stream.Read(buffer, totalRead, plain.Length - totalRead);
                    if (read == 0)
                    {
                        return false;
                    }
                    totalRead += read;
                }
            }

            byte? candidate = null;
            for (int i = 0; i < plain.Length; i++)
            {
                byte xor = (byte)(buffer[i] ^ plain[i]);
                if (xor == 0)
                {
                    return false; // already matches plain header
                }

                if (!candidate.HasValue)
                {
                    candidate = xor;
                }
                else if (candidate.Value != xor)
                {
                    return false;
                }
            }

            if (!candidate.HasValue)
            {
                return false;
            }

            key = candidate.Value;
            return true;
        }

        private static bool TryDecryptFile(string sourcePath, string destinationPath, byte key)
        {
            string tempPath = destinationPath + ".tmp";
            const int bufferSize = 64 * 1024;

            try
            {
                using (FileStream input = File.Open(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (FileStream output = File.Open(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    byte[] buffer = new byte[bufferSize];
                    int read;
                    while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        for (int i = 0; i < read; i++)
                        {
                            buffer[i] = (byte)(buffer[i] ^ key);
                        }

                        output.Write(buffer, 0, read);
                    }
                }

                if (File.Exists(destinationPath))
                {
                    File.Delete(destinationPath);
                }
                File.Move(tempPath, destinationPath);
                return true;
            }
            catch
            {
                try
                {
                    if (File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                    }
                }
                catch
                {
                    // ignored
                }
                return false;
            }
        }

        private static bool IsExistingDecryptionUpToDate(string sourcePath, string destinationPath)
        {
            if (!File.Exists(destinationPath))
            {
                return false;
            }

            FileInfo srcInfo = new FileInfo(sourcePath);
            FileInfo dstInfo = new FileInfo(destinationPath);
            return dstInfo.LastWriteTimeUtc >= srcInfo.LastWriteTimeUtc && dstInfo.Length == srcInfo.Length;
        }

        private static string BuildDecryptedPath(string originalPath)
        {
            string directory = Path.GetDirectoryName(originalPath);
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(originalPath);
            string extension = Path.GetExtension(originalPath);
            return Path.Combine(directory ?? string.Empty, string.Format("{0}_decrypted{1}", fileNameWithoutExtension, extension));
        }

        private sealed class FileSignature
        {
            public FileSignature(IEnumerable<string> extensions, long offset, byte[] pattern)
            {
                Extensions = extensions?.Select(e => e?.ToLowerInvariant()).Where(e => !string.IsNullOrWhiteSpace(e)).ToArray() ?? Array.Empty<string>();
                Offset = offset;
                Pattern = pattern ?? Array.Empty<byte>();
            }

            public string[] Extensions { get; }
            public long Offset { get; }
            public byte[] Pattern { get; }
        }

        private static readonly FileSignature[] FileSignatures = new[]
        {
            new FileSignature(new[]{".mp4", ".mov", ".m4v", ".m4a"}, 4, new byte[]{0x66, 0x74, 0x79, 0x70}),
            new FileSignature(new[]{".jpg", ".jpeg", ".jfif"}, 0, new byte[]{0xFF, 0xD8}),
            new FileSignature(new[]{".png"}, 0, new byte[]{0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A}),
            new FileSignature(new[]{".mp3"}, 0, new byte[]{0x49, 0x44, 0x33}),
            new FileSignature(new[]{".mp3"}, 0, new byte[]{0xFF, 0xFB}),
            new FileSignature(new[]{".flac"}, 0, new byte[]{0x66, 0x4C, 0x61, 0x43}),
            new FileSignature(new[]{".wav"}, 0, new byte[]{0x52, 0x49, 0x46, 0x46})
        };
    }
}
