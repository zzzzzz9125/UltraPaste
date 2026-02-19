#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

namespace UltraPaste.Utilities
{
    /// <summary>
    /// Extension methods providing format compatibility operations for VEGAS Pro projects.
    /// Integrates CommonFormatHelper capabilities with VEGAS Pro project objects.
    /// </summary>
    internal static class ProjectFormatHelper
    {
        /// <summary>
        /// Exports the project to DaVinci Resolve XML format.
        /// </summary>
        /// <param name="project">The project to export.</param>
        /// <param name="outputPath">Optional file path to save the export.</param>
        /// <returns>XML content as string.</returns>
        public static string ExportAsResolveXml(this Project project, string outputPath = null)
        {
            string content = CommonFormatHelper.ExportProjectToResolveXml(project);
            if (!string.IsNullOrEmpty(outputPath))
            {
                CommonFormatHelper.ExportProjectToResolveXmlFile(project, outputPath);
            }
            return content;
        }

        /// <summary>
        /// Exports the project to CMX 3600 EDL format.
        /// </summary>
        /// <param name="project">The project to export.</param>
        /// <param name="outputPath">Optional file path to save the export.</param>
        /// <returns>EDL content as string.</returns>
        public static string ExportAsEdl(this Project project, string outputPath = null)
        {
            string content = CommonFormatHelper.ExportProjectToEdl(project);
            if (!string.IsNullOrEmpty(outputPath))
            {
                CommonFormatHelper.ExportProjectToEdlFile(project, outputPath);
            }
            return content;
        }

        /// <summary>
        /// Exports the project to Adobe Premiere Pro PRPROJ format.
        /// </summary>
        /// <param name="project">The project to export.</param>
        /// <param name="outputPath">Optional file path to save the export.</param>
        /// <returns>PRPROJ content as string.</returns>
        public static string ExportAsPrproj(this Project project, string outputPath = null)
        {
            string content = CommonFormatHelper.ExportProjectToPrproj(project);
            if (!string.IsNullOrEmpty(outputPath))
            {
                CommonFormatHelper.ExportProjectToPrprojFile(project, outputPath);
            }
            return content;
        }

        /// <summary>
        /// Exports the project to the specified format.
        /// </summary>
        /// <param name="project">The project to export.</param>
        /// <param name="format">The target format.</param>
        /// <param name="outputPath">Optional file path to save the export.</param>
        /// <returns>Exported content as string.</returns>
        public static string ExportAsFormat(this Project project, FormatCompatibilityHelper.SupportedFormat format, 
            string outputPath = null)
        {
            return FormatCompatibilityHelper.ExportProject(project, format, outputPath);
        }

        /// <summary>
        /// Imports timeline data from DaVinci Resolve XML format.
        /// </summary>
        /// <param name="project">The target project.</param>
        /// <param name="xmlContent">The XML content or file path.</param>
        /// <param name="isFilePath">If true, xmlContent is a file path; otherwise it's content.</param>
        public static void ImportFromResolveXml(this Project project, string xmlContent, bool isFilePath = false)
        {
            if (isFilePath)
                CommonFormatHelper.ImportFromResolveXmlFile(project, xmlContent);
            else
                CommonFormatHelper.ImportFromResolveXml(project, xmlContent);
        }

        /// <summary>
        /// Imports timeline data from CMX 3600 EDL format.
        /// </summary>
        /// <param name="project">The target project.</param>
        /// <param name="edlContent">The EDL content or file path.</param>
        /// <param name="isFilePath">If true, edlContent is a file path; otherwise it's content.</param>
        public static void ImportFromEdl(this Project project, string edlContent, bool isFilePath = false)
        {
            if (isFilePath)
                CommonFormatHelper.ImportFromEdlFile(project, edlContent);
            else
                CommonFormatHelper.ImportFromEdl(project, edlContent);
        }

        /// <summary>
        /// Imports timeline data from Adobe Premiere Pro PRPROJ format.
        /// </summary>
        /// <param name="project">The target project.</param>
        /// <param name="prprojContent">The PRPROJ content or file path.</param>
        /// <param name="isFilePath">If true, prprojContent is a file path; otherwise it's content.</param>
        public static void ImportFromPrproj(this Project project, string prprojContent, bool isFilePath = false)
        {
            if (isFilePath)
                CommonFormatHelper.ImportFromPrprojFile(project, prprojContent);
            else
                CommonFormatHelper.ImportFromPrproj(project, prprojContent);
        }

        /// <summary>
        /// Imports timeline data from the specified format.
        /// </summary>
        /// <param name="project">The target project.</param>
        /// <param name="format">The source format.</param>
        /// <param name="contentOrPath">The content or file path.</param>
        /// <param name="isFilePath">If true, contentOrPath is a file path; otherwise it's content.</param>
        public static void ImportFromFormat(this Project project, FormatCompatibilityHelper.SupportedFormat format, 
            string contentOrPath, bool isFilePath = false)
        {
            FormatCompatibilityHelper.ImportProject(project, format, contentOrPath, isFilePath);
        }

        /// <summary>
        /// Converts the project from one format to another.
        /// </summary>
        /// <param name="project">The source project.</param>
        /// <param name="sourceFormat">The source format (for documentation).</param>
        /// <param name="targetFormat">The target format to convert to.</param>
        /// <param name="outputPath">Optional file path to save the converted file.</param>
        /// <returns>Converted content in target format.</returns>
        public static string ConvertToFormat(this Project project, FormatCompatibilityHelper.SupportedFormat sourceFormat,
            FormatCompatibilityHelper.SupportedFormat targetFormat, string outputPath = null)
        {
            return FormatCompatibilityHelper.ConvertFormat(project, sourceFormat, targetFormat, outputPath);
        }

        /// <summary>
        /// Gets a list of compatible software for the format.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>Array of compatible software names.</returns>
        public static string[] GetCompatibleSoftware(this FormatCompatibilityHelper.SupportedFormat format)
        {
            var metadata = FormatCompatibilityHelper.GetFormatMetadata(format);
            return metadata.SupportedSoftware;
        }

        /// <summary>
        /// Gets the file extension for the format.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>File extension including dot (e.g., ".edl").</returns>
        public static string GetFileExtension(this FormatCompatibilityHelper.SupportedFormat format)
        {
            var metadata = FormatCompatibilityHelper.GetFormatMetadata(format);
            return metadata.FileExtension;
        }

        /// <summary>
        /// Checks if the format supports effect information.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>True if effects are supported.</returns>
        public static bool SupportsEffects(this FormatCompatibilityHelper.SupportedFormat format)
        {
            return FormatCompatibilityHelper.SupportsEffects(format);
        }

        /// <summary>
        /// Checks if the format supports full project information.
        /// </summary>
        /// <param name="format">The format to check.</param>
        /// <returns>True if full project is supported.</returns>
        public static bool SupportsFullProject(this FormatCompatibilityHelper.SupportedFormat format)
        {
            return FormatCompatibilityHelper.SupportsFullProject(format);
        }

        /// <summary>
        /// Gets a human-readable description of the format.
        /// </summary>
        /// <param name="format">The format to describe.</param>
        /// <returns>Formatted description string.</returns>
        public static string GetDescription(this FormatCompatibilityHelper.SupportedFormat format)
        {
            return FormatCompatibilityHelper.GetFormatDescription(format);
        }
    }
}
