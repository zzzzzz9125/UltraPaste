using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Xml;

namespace UltraPaste.Utilities
{
    /// <summary>
    /// Represents a media file with absolute and relative paths.
    /// </summary>
    public class MediaFile
    {
        /// <summary>
        /// Gets or sets the absolute path of the media file.
        /// </summary>
        public string AbsolutePath { get; set; }

        /// <summary>
        /// Gets or sets the relative path of the media file within the archive.
        /// </summary>
        public string RelativePath { get; set; }

        /// <summary>
        /// Initializes a new instance of the MediaFile class.
        /// </summary>
        public MediaFile()
        {
        }

        /// <summary>
        /// Initializes a new instance of the MediaFile class with the specified paths.
        /// </summary>
        public MediaFile(string absolutePath, string relativePath)
        {
            AbsolutePath = absolutePath;
            RelativePath = relativePath;
        }
    }

    /// <summary>
    /// Represents Vegas clipboard data that can be serialized and deserialized as a .vegclb package.
    /// </summary>
    public class VegasClipboardData
    {
        /// <summary>
        /// Gets or sets the Vegas data bytes.
        /// </summary>
        public byte[] DataBytes { get; set; }

        /// <summary>
        /// Gets or sets the Vegas metadata bytes.
        /// </summary>
        public byte[] MetaDataBytes { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the clipboard data.
        /// </summary>
        public DateTime Time { get; set; }

        /// <summary>
        /// Gets or sets the Vegas version.
        /// </summary>
        public string VegasVersion { get; set; }

        /// <summary>
        /// Gets or sets the display name for the clipboard data.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the comment for the clipboard data.
        /// </summary>
        public string Comment { get; set; }

        /// <summary>
        /// Gets or sets the list of media files associated with this clipboard data.
        /// </summary>
        public List<MediaFile> MediaFiles { get; set; }

        /// <summary>
        /// Initializes a new instance of the VegasClipboardData class.
        /// </summary>
        public VegasClipboardData()
        {
            MediaFiles = new List<MediaFile>();
            Time = DateTime.Now;
            VegasVersion = $"{VegasCommonHelper.VegasVersionInfo.FileMajorPart}.{VegasCommonHelper.VegasVersionInfo.FileMinorPart}.{VegasCommonHelper.VegasVersionInfo.FileBuildPart}.{VegasCommonHelper.VegasVersionInfo.FilePrivatePart}";
        }

        /// <summary>
        /// Saves the clipboard data to a .vegclb file (zip format).
        /// </summary>
        /// <param name="filePath">The path where the file will be saved</param>
        /// <param name="includeMediaFiles">Whether to include media files in the archive</param>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
        /// <exception cref="InvalidOperationException">Thrown when data or metadata bytes are null</exception>
        public void Save(string filePath, bool includeMediaFiles = false)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");

            if (DataBytes == null || DataBytes.Length == 0)
                throw new InvalidOperationException("DataBytes cannot be null or empty.");

            if (MetaDataBytes == null || MetaDataBytes.Length == 0)
                throw new InvalidOperationException("MetaDataBytes cannot be null or empty.");

            // Ensure the directory exists
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (FileStream fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write))
            {
                using (ZipArchive archive = new ZipArchive(fileStream, ZipArchiveMode.Create, true))
                {
                    // Write DataBytes to Vegas_Data_5.0.bin
                    ZipArchiveEntry dataEntry = archive.CreateEntry("Vegas Data 5.0.bin");
                    using (Stream stream = dataEntry.Open())
                    {
                        stream.Write(DataBytes, 0, DataBytes.Length);
                    }

                    // Write MetaDataBytes to Vegas_Meta-Data_5.0.bin
                    ZipArchiveEntry metadataEntry = archive.CreateEntry("Vegas Meta-Data 5.0.bin");
                    using (Stream stream = metadataEntry.Open())
                    {
                        stream.Write(MetaDataBytes, 0, MetaDataBytes.Length);
                    }

                    // Create info.xml
                    XmlDocument infoDoc = CreateInfoXml(includeMediaFiles);
                    ZipArchiveEntry infoEntry = archive.CreateEntry("info.xml");
                    using (Stream stream = infoEntry.Open())
                    {
                        infoDoc.Save(stream);
                    }

                    // Add media files if requested
                    if (includeMediaFiles && MediaFiles != null && MediaFiles.Count > 0)
                    {
                        for (int i = 0; i < MediaFiles.Count; i++)
                        {
                            MediaFile mediaFile = MediaFiles[i];
                            if (File.Exists(mediaFile.AbsolutePath))
                            {
                                string extension = Path.GetExtension(mediaFile.AbsolutePath);
                                string archiveFileName = $"Media/Media_{i:D6}{extension}";
                                ZipArchiveEntry mediaEntry = archive.CreateEntry(archiveFileName);
                                using (FileStream sourceStream = File.OpenRead(mediaFile.AbsolutePath))
                                {
                                    using (Stream archiveStream = mediaEntry.Open())
                                    {
                                        sourceStream.CopyTo(archiveStream);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Loads clipboard data from a .vegclb stream (zip format).
        /// </summary>
        /// <param name="stream">The stream containing the .vegclb data.</param>
        /// <param name="loadMediaFiles">Whether to load media files from the archive.</param>
        /// <returns>A new VegasClipboardData instance with loaded data.</returns>
        /// <exception cref="ArgumentNullException">Thrown when stream is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when required files are missing from the archive.</exception>
        public static VegasClipboardData Load(Stream stream, bool loadMediaFiles = false)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream), "Stream cannot be null.");

            VegasClipboardData data = new VegasClipboardData();

            using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read, true))
            {
                ZipArchiveEntry dataEntry = archive.GetEntry("Vegas Data 5.0.bin");
                if (dataEntry == null)
                    throw new InvalidOperationException("Required file 'Vegas Data 5.0.bin' not found in archive.");

                using (Stream entryStream = dataEntry.Open())
                using (MemoryStream memory = new MemoryStream())
                {
                    entryStream.CopyTo(memory);
                    data.DataBytes = memory.ToArray();
                }

                ZipArchiveEntry metadataEntry = archive.GetEntry("Vegas Meta-Data 5.0.bin");
                if (metadataEntry == null)
                    throw new InvalidOperationException("Required file 'Vegas Meta-Data 5.0.bin' not found in archive.");

                using (Stream entryStream = metadataEntry.Open())
                using (MemoryStream memory = new MemoryStream())
                {
                    entryStream.CopyTo(memory);
                    data.MetaDataBytes = memory.ToArray();
                }

                ZipArchiveEntry infoEntry = archive.GetEntry("info.xml");
                if (infoEntry != null)
                {
                    using (Stream entryStream = infoEntry.Open())
                    {
                        XmlDocument infoDoc = new XmlDocument();
                        infoDoc.Load(entryStream);
                        ParseInfoXml(data, infoDoc, loadMediaFiles);
                    }
                }

                if (loadMediaFiles)
                {
                    LoadMediaFilesFromArchive(data, archive);
                }
            }

            return data;
        }

        /// <summary>
        /// Loads clipboard data from a .vegclb file (zip format).
        /// </summary>
        /// <param name="filePath">The path of the .vegclb file to load</param>
        /// <param name="loadMediaFiles">Whether to load media files from the archive</param>
        /// <returns>A new VegasClipboardData instance with loaded data</returns>
        /// <exception cref="ArgumentNullException">Thrown when filePath is null or empty</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist</exception>
        /// <exception cref="InvalidOperationException">Thrown when required files are missing from the archive</exception>
        public static VegasClipboardData Load(string filePath, bool loadMediaFiles = false)
        {
            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentNullException(nameof(filePath), "File path cannot be null or empty.");

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File '{filePath}' not found.");

            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                return Load(fileStream, loadMediaFiles);
            }
        }

        /// <summary>
        /// Creates the info.xml document containing metadata about the clipboard data.
        /// </summary>
        private XmlDocument CreateInfoXml(bool includeMediaFiles)
        {
            XmlDocument doc = new XmlDocument();
            XmlElement root = doc.CreateElement("VegasClipboardData");
            doc.AppendChild(root);

            XmlElement timeElement = doc.CreateElement("Time");
            timeElement.InnerText = new DateTimeOffset(Time).ToUnixTimeMilliseconds().ToString();
            root.AppendChild(timeElement);

            XmlElement versionElement = doc.CreateElement("VegasVersion");
            versionElement.InnerText = VegasVersion ?? string.Empty;
            root.AppendChild(versionElement);

            XmlElement nameElement = doc.CreateElement("Name");
            nameElement.InnerText = Name ?? string.Empty;
            root.AppendChild(nameElement);

            XmlElement commentElement = doc.CreateElement("Comment");
            commentElement.InnerText = Comment ?? string.Empty;
            root.AppendChild(commentElement);

            XmlElement includesMediaElement = doc.CreateElement("IncludesMediaFiles");
            includesMediaElement.InnerText = includeMediaFiles.ToString();
            root.AppendChild(includesMediaElement);

            // Add media file mappings if media files are included
            if (includeMediaFiles && MediaFiles != null && MediaFiles.Count > 0)
            {
                XmlElement mediaFilesElement = doc.CreateElement("MediaFiles");
                for (int i = 0; i < MediaFiles.Count; i++)
                {
                    MediaFile mediaFile = MediaFiles[i];
                    string extension = Path.GetExtension(mediaFile.AbsolutePath);
                    
                    XmlElement mediaElement = doc.CreateElement("MediaFile");
                    mediaElement.SetAttribute("index", i.ToString());
                    mediaElement.SetAttribute("archiveName", $"Media_{i:D6}{extension}");

                    XmlElement absolutePathElement = doc.CreateElement("AbsolutePath");
                    absolutePathElement.InnerText = mediaFile.AbsolutePath ?? string.Empty;
                    mediaElement.AppendChild(absolutePathElement);

                    XmlElement relativePathElement = doc.CreateElement("RelativePath");
                    relativePathElement.InnerText = mediaFile.RelativePath ?? string.Empty;
                    mediaElement.AppendChild(relativePathElement);

                    mediaFilesElement.AppendChild(mediaElement);
                }
                root.AppendChild(mediaFilesElement);
            }

            return doc;
        }

        /// <summary>
        /// Parses the info.xml document and updates the clipboard data instance.
        /// </summary>
        private static void ParseInfoXml(VegasClipboardData data, XmlDocument doc, bool loadMediaFiles)
        {
            XmlElement root = doc.DocumentElement;
            if (root == null)
                return;

            // Parse Time
            XmlElement timeElement = root.SelectSingleNode("Time") as XmlElement;
            if (timeElement != null)
            {
                long unixTime;
                if (long.TryParse(timeElement.InnerText, out unixTime))
                {
                    data.Time = DateTimeOffset.FromUnixTimeMilliseconds(unixTime).DateTime;
                }
                else if (DateTime.TryParse(timeElement.InnerText, out DateTime time))
                {
                    data.Time = time;
                }
            }

            // Parse VegasVersion
            XmlElement versionElement = root.SelectSingleNode("VegasVersion") as XmlElement;
            if (versionElement != null)
            {
                data.VegasVersion = versionElement.InnerText;
            }

            XmlElement nameElement = root.SelectSingleNode("Name") as XmlElement;
            if (nameElement != null)
            {
                data.Name = nameElement.InnerText;
            }

            XmlElement commentElement = root.SelectSingleNode("Comment") as XmlElement;
            if (commentElement != null)
            {
                data.Comment = commentElement.InnerText;
            }

            // Parse MediaFiles metadata if needed
            if (loadMediaFiles)
            {
                XmlElement mediaFilesElement = root.SelectSingleNode("MediaFiles") as XmlElement;
                if (mediaFilesElement != null)
                {
                    XmlNodeList mediaNodes = mediaFilesElement.SelectNodes("MediaFile");
                    foreach (XmlElement mediaElement in mediaNodes)
                    {
                        MediaFile mediaFile = new MediaFile();
                        XmlElement absolutePathElement = mediaElement.SelectSingleNode("AbsolutePath") as XmlElement;
                        XmlElement relativePathElement = mediaElement.SelectSingleNode("RelativePath") as XmlElement;

                        if (absolutePathElement != null)
                            mediaFile.AbsolutePath = absolutePathElement.InnerText;
                        if (relativePathElement != null)
                            mediaFile.RelativePath = relativePathElement.InnerText;

                        data.MediaFiles.Add(mediaFile);
                    }
                }
            }
        }

        /// <summary>
        /// Loads media files from the archive into the clipboard data instance.
        /// </summary>
        private static void LoadMediaFilesFromArchive(VegasClipboardData data, ZipArchive archive)
        {
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.FullName.StartsWith("Media/") && !entry.FullName.EndsWith("/"))
                {
                    using (Stream stream = entry.Open())
                    {
                        MemoryStream memoryStream = new MemoryStream();
                        stream.CopyTo(memoryStream);
                        memoryStream.Seek(0, SeekOrigin.Begin);

                        // Store the loaded media data in RelativePath for now
                        // The actual file would need to be extracted to disk if needed
                        int mediaIndex = -1;
                        if (data.MediaFiles != null && data.MediaFiles.Count > 0)
                        {
                            // Try to match based on archive name from info.xml
                            string fileName = Path.GetFileName(entry.FullName);
                            for (int i = 0; i < data.MediaFiles.Count; i++)
                            {
                                string extension = Path.GetExtension(data.MediaFiles[i].AbsolutePath);
                                if (fileName == $"Media_{i:D6}{extension}")
                                {
                                    mediaIndex = i;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
