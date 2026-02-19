using System;
using System.IO;
using System.Collections.Generic;

namespace UltraPaste.Utilities
{
    /// <summary>
    /// Compatibility layer providing easy access to CommonFormatHelper export/import functionality.
    /// Simplifies format conversion operations across the UltraPaste application.
    /// </summary>
    internal static class FormatCompatibilityHelper
    {
        /// <summary>
        /// Defines supported export/import formats and their characteristics.
        /// </summary>
        public enum SupportedFormat
        {
            /// <summary>DaVinci Resolve XML format - Full project structure with effects and metadata.</summary>
            DaVinciResolveXml,

            /// <summary>CMX 3600 EDL format - Universal timeline interchange format.</summary>
            EdlCmx3600,

            /// <summary>Adobe Premiere Pro PRPROJ format - Native Premiere Pro project format.</summary>
            PremierePrproj
        }

        /// <summary>
        /// Format metadata containing file extensions, descriptions, and compatibility information.
        /// </summary>
        private static readonly Dictionary<SupportedFormat, FormatMetadata> FormatMetadata = 
            new Dictionary<SupportedFormat, FormatMetadata>
            {
                {
                    SupportedFormat.DaVinciResolveXml,
                    new FormatMetadata
                    {
                        FileExtension = ".xml",
                        Description = "DaVinci Resolve XML",
                        Mnemonic = "resolve",
                        IsTextBased = true,
                        SupportsFullProject = true,
                        SupportsEffects = true,
                        SupportedSoftware = new[] { "DaVinci Resolve", "VEGAS Pro" }
                    }
                },
                {
                    SupportedFormat.EdlCmx3600,
                    new FormatMetadata
                    {
                        FileExtension = ".edl",
                        Description = "CMX 3600 EDL",
                        Mnemonic = "edl",
                        IsTextBased = true,
                        SupportsFullProject = false,
                        SupportsEffects = false,
                        SupportedSoftware = new[] { "All Major Editors", "Avid", "Premiere Pro", "Final Cut Pro", "VEGAS Pro" }
                    }
                },
                {
                    SupportedFormat.PremierePrproj,
                    new FormatMetadata
                    {
                        FileExtension = ".prproj",
                        Description = "Adobe Premiere Pro PRPROJ",
                        Mnemonic = "prproj",
                        IsTextBased = true,
                        SupportsFullProject = true,
                        SupportsEffects = false,
                        SupportedSoftware = new[] { "Adobe Premiere Pro 2020+", "VEGAS Pro" }
                    }
                }
            };

        /// <summary>
        /// Gets metadata for a specific format.
        /// </summary>
        /// <param name="format">The format to query.</param>
        /// <returns>Format metadata including extension, description, and compatibility info.</returns>
        public static FormatMetadata GetFormatMetadata(SupportedFormat format)
        {
            if (FormatMetadata.ContainsKey(format))
            {
                return FormatMetadata[format];
            }
            throw new ArgumentException($"Format {format} is not supported.", nameof(format));
        }

        /// <summary>
        /// Determines the format based on file extension.
        /// </summary>
        /// <param name="filePath">The file path to analyze.</param>
        /// <returns>The detected format, or null if unrecognized.</returns>
        public static SupportedFormat? DetectFormatFromFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return null;

            string extension = Path.GetExtension(filePath).ToLowerInvariant();

            foreach (var kvp in FormatMetadata)
            {
                if (kvp.Value.FileExtension.Equals(extension, StringComparison.OrdinalIgnoreCase))
                {
                    return kvp.Key;
                }
            }

            return null;
        }

        /// <summary>
        /// Exports a VEGAS Pro project to the specified format.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <param name="format">The target format.</param>
        /// <param name="outputPath">Optional file path to save the export.</param>
        /// <returns>Exported content as string. If outputPath is provided, also saves to file.</returns>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        /// <exception cref="NotSupportedException">Thrown for unsupported formats.</exception>
        public static string ExportProject(ScriptPortal.Vegas.Project project, SupportedFormat format, string outputPath = null)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            string content;

            try
            {
                switch (format)
                {
                    case SupportedFormat.DaVinciResolveXml:
                        content = CommonFormatHelper.ExportProjectToResolveXml(project);
                        if (!string.IsNullOrEmpty(outputPath))
                            CommonFormatHelper.ExportProjectToResolveXmlFile(project, outputPath);
                        break;

                    case SupportedFormat.EdlCmx3600:
                        content = CommonFormatHelper.ExportProjectToEdl(project);
                        if (!string.IsNullOrEmpty(outputPath))
                            CommonFormatHelper.ExportProjectToEdlFile(project, outputPath);
                        break;

                    case SupportedFormat.PremierePrproj:
                        content = CommonFormatHelper.ExportProjectToPrproj(project);
                        if (!string.IsNullOrEmpty(outputPath))
                            CommonFormatHelper.ExportProjectToPrprojFile(project, outputPath);
                        break;

                    default:
                        throw new NotSupportedException($"Format {format} is not supported for export.");
                }

                return content;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to export project to {format} format.", ex);
            }
        }

        /// <summary>
        /// Imports timeline data from the specified format into a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="format">The source format.</param>
        /// <param name="contentOrPath">Either the file content (string) or file path.</param>
        /// <param name="isFilePath">If true, contentOrPath is treated as a file path; otherwise as content.</param>
        /// <exception cref="ArgumentNullException">Thrown when required parameters are null.</exception>
        /// <exception cref="NotSupportedException">Thrown for unsupported formats.</exception>
        public static void ImportProject(ScriptPortal.Vegas.Project project, SupportedFormat format, string contentOrPath, bool isFilePath = false)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(contentOrPath))
                throw new ArgumentException("Content or file path cannot be null or empty.", nameof(contentOrPath));

            try
            {
                switch (format)
                {
                    case SupportedFormat.DaVinciResolveXml:
                        if (isFilePath)
                            CommonFormatHelper.ImportFromResolveXmlFile(project, contentOrPath);
                        else
                            CommonFormatHelper.ImportFromResolveXml(project, contentOrPath);
                        break;

                    case SupportedFormat.EdlCmx3600:
                        if (isFilePath)
                            CommonFormatHelper.ImportFromEdlFile(project, contentOrPath);
                        else
                            CommonFormatHelper.ImportFromEdl(project, contentOrPath);
                        break;

                    case SupportedFormat.PremierePrproj:
                        if (isFilePath)
                            CommonFormatHelper.ImportFromPrprojFile(project, contentOrPath);
                        else
                            CommonFormatHelper.ImportFromPrproj(project, contentOrPath);
                        break;

                    default:
                        throw new NotSupportedException($"Format {format} is not supported for import.");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to import project from {format} format.", ex);
            }
        }

        /// <summary>
        /// Performs format conversion between any two supported formats.
        /// </summary>
        /// <param name="sourceProject">The source VEGAS Pro project.</param>
        /// <param name="sourceFormat">The source format.</param>
        /// <param name="targetFormat">The target format.</param>
        /// <param name="outputPath">Optional file path to save the converted file.</param>
        /// <returns>Converted content in target format.</returns>
        public static string ConvertFormat(ScriptPortal.Vegas.Project sourceProject, SupportedFormat sourceFormat, 
            SupportedFormat targetFormat, string outputPath = null)
        {
            if (sourceProject == null)
                throw new ArgumentNullException(nameof(sourceProject), "Source project cannot be null.");

            if (sourceFormat == targetFormat)
                return ExportProject(sourceProject, sourceFormat, outputPath);

            try
            {
                // Export from source format
                string sourceContent = ExportProject(sourceProject, sourceFormat);

                // Import to temporary project (if needed for intermediate processing)
                // For direct conversion, we can export the source project in target format
                string targetContent = ExportProject(sourceProject, targetFormat, outputPath);

                return targetContent;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to convert from {sourceFormat} to {targetFormat}.", ex);
            }
        }

        /// <summary>
        /// Gets all supported formats with their metadata.
        /// </summary>
        /// <returns>Dictionary of formats and their metadata.</returns>
        public static IReadOnlyDictionary<SupportedFormat, FormatMetadata> GetAllFormats()
        {
            return new Dictionary<SupportedFormat, FormatMetadata>(FormatMetadata);
        }

        /// <summary>
        /// Checks if a format supports full project information (including effects, markers, etc.).
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>True if full project is supported; false if timeline-only.</returns>
        public static bool SupportsFullProject(SupportedFormat format)
        {
            var metadata = GetFormatMetadata(format);
            return metadata.SupportsFullProject;
        }

        /// <summary>
        /// Checks if a format supports effect information.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>True if effects are supported; false otherwise.</returns>
        public static bool SupportsEffects(SupportedFormat format)
        {
            var metadata = GetFormatMetadata(format);
            return metadata.SupportsEffects;
        }

        /// <summary>
        /// Gets a user-friendly description of the format.
        /// </summary>
        /// <param name="format">The format to describe.</param>
        /// <returns>Formatted string describing the format and its capabilities.</returns>
        public static string GetFormatDescription(SupportedFormat format)
        {
            var metadata = GetFormatMetadata(format);
            var capabilities = new List<string>();

            if (metadata.SupportsFullProject)
                capabilities.Add("full project");
            if (metadata.SupportsEffects)
                capabilities.Add("effects");
            if (metadata.IsTextBased)
                capabilities.Add("text-based");

            string capsText = capabilities.Count > 0 ? $" ({string.Join(", ", capabilities)})" : "";
            return $"{metadata.Description}{capsText}";
        }
    }

    /// <summary>
    /// Metadata for a format, containing technical and compatibility information.
    /// </summary>
    public class FormatMetadata
    {
        /// <summary>File extension including the dot (e.g., ".xml", ".edl").</summary>
        public string FileExtension { get; set; }

        /// <summary>Human-readable format name.</summary>
        public string Description { get; set; }

        /// <summary>Short mnemonic for quick reference (e.g., "resolve", "edl").</summary>
        public string Mnemonic { get; set; }

        /// <summary>Whether the format is text-based (vs binary).</summary>
        public bool IsTextBased { get; set; }

        /// <summary>Whether the format supports full project information.</summary>
        public bool SupportsFullProject { get; set; }

        /// <summary>Whether the format supports effect information.</summary>
        public bool SupportsEffects { get; set; }

        /// <summary>Array of compatible software products.</summary>
        public string[] SupportedSoftware { get; set; }
    }
}
