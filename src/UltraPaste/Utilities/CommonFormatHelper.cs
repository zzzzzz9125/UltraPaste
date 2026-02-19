#if !Sony
using ScriptPortal.Vegas;
#else
using Sony.Vegas;
#endif

using System;
using System.Xml;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace UltraPaste.Utilities
{
    /// <summary>
    /// Helper class for converting between VEGAS Pro projects and DaVinci Resolve compatible XML formats.
    /// </summary>
    internal class CommonFormatHelper
    {
        private const string XML_VERSION = "1.0";
        private const string XML_ENCODING = "utf-8";

        /// <summary>
        /// Exports a VEGAS Pro project to DaVinci Resolve compatible XML format.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <returns>An XML string representing the project in DaVinci Resolve format.</returns>
        /// <exception cref="ArgumentNullException">Thrown when project is null.</exception>
        public static string ExportProjectToResolveXml(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            try
            {
                XmlDocument doc = new XmlDocument();
                XmlDeclaration declaration = doc.CreateXmlDeclaration(XML_VERSION, XML_ENCODING, null);
                doc.AppendChild(declaration);

                XmlElement root = CreateResolveProjectElement(doc, project);
                doc.AppendChild(root);

                using (StringWriter sw = new StringWriter())
                {
                    using (XmlTextWriter writer = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
                    {
                        doc.WriteTo(writer);
                        return sw.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export project to Resolve XML format.", ex);
            }
        }

        /// <summary>
        /// Saves a VEGAS Pro project as DaVinci Resolve compatible XML to a file.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <param name="filePath">The file path where the XML will be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        public static void ExportProjectToResolveXmlFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string xmlContent = ExportProjectToResolveXml(project);
                File.WriteAllText(filePath, xmlContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save project XML to file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Imports timeline data from DaVinci Resolve compatible XML and applies it to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="xmlContent">The XML content in DaVinci Resolve format.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or xmlContent is null.</exception>
        /// <exception cref="ArgumentException">Thrown when xmlContent is empty.</exception>
        public static void ImportFromResolveXml(Project project, string xmlContent)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(xmlContent))
                throw new ArgumentException("XML content cannot be null or empty.", nameof(xmlContent));

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xmlContent);
                ParseResolveXmlToProject(project, doc.DocumentElement);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Failed to parse Resolve XML format.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to import project from Resolve XML format.", ex);
            }
        }

        /// <summary>
        /// Imports timeline data from a DaVinci Resolve compatible XML file and applies it to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="filePath">The file path of the Resolve XML file.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public static void ImportFromResolveXmlFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            try
            {
                string xmlContent = File.ReadAllText(filePath, Encoding.UTF8);
                ImportFromResolveXml(project, xmlContent);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException) && !(ex is ArgumentException) && !(ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"Failed to import project XML from file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Creates the root project element in DaVinci Resolve XML format.
        /// </summary>
        private static XmlElement CreateResolveProjectElement(XmlDocument doc, Project project)
        {
            XmlElement projectElement = doc.CreateElement("Project");
            projectElement.SetAttribute("name", GetProjectName(project));
            projectElement.SetAttribute("framerate", GetFrameRate(project).ToString());
            projectElement.SetAttribute("width", project.Video.Width.ToString());
            projectElement.SetAttribute("height", project.Video.Height.ToString());
            projectElement.SetAttribute("sampleRate", project.Audio.SampleRate.ToString());
            projectElement.SetAttribute("bitDepth", project.Audio.BitDepth.ToString());

            projectElement.AppendChild(CreateProjectMetadataElement(doc, project));
            projectElement.AppendChild(CreateTimecodeElement(doc, project));
            projectElement.AppendChild(CreateTracksElement(doc, project));
            projectElement.AppendChild(CreateMarkersElement(doc, project));
            projectElement.AppendChild(CreateRegionsElement(doc, project));

            return projectElement;
        }

        /// <summary>
        /// Creates project metadata element.
        /// </summary>
        private static XmlElement CreateProjectMetadataElement(XmlDocument doc, Project project)
        {
            XmlElement metadataElement = doc.CreateElement("Metadata");

            XmlElement durationElement = doc.CreateElement("Duration");
            durationElement.InnerText = GetProjectDuration(project);
            metadataElement.AppendChild(durationElement);

            return metadataElement;
        }

        /// <summary>
        /// Creates timecode element with project timecode properties.
        /// </summary>
        private static XmlElement CreateTimecodeElement(XmlDocument doc, Project project)
        {
            XmlElement timecodeElement = doc.CreateElement("Timecode");
            Timecode startTime = new Timecode(0);
            timecodeElement.SetAttribute("start", startTime.ToString());
            timecodeElement.SetAttribute("format", project.Ruler.Format.ToString());
            timecodeElement.SetAttribute("frameRate", GetFrameRate(project).ToString());

            return timecodeElement;
        }

        /// <summary>
        /// Creates all tracks element containing audio and video tracks.
        /// </summary>
        private static XmlElement CreateTracksElement(XmlDocument doc, Project project)
        {
            XmlElement tracksElement = doc.CreateElement("Tracks");

            foreach (Track track in project.Tracks)
            {
                if (track.IsVideo())
                {
                    tracksElement.AppendChild(CreateVideoTrackElement(doc, track as VideoTrack));
                }
            }

            foreach (Track track in project.Tracks)
            {
                if (track.IsAudio())
                {
                    tracksElement.AppendChild(CreateAudioTrackElement(doc, track as AudioTrack));
                }
            }

            return tracksElement;
        }

        /// <summary>
        /// Creates a video track element with all its events.
        /// </summary>
        private static XmlElement CreateVideoTrackElement(XmlDocument doc, VideoTrack track)
        {
            XmlElement trackElement = doc.CreateElement("VideoTrack");
            trackElement.SetAttribute("index", track.Index.ToString());
            trackElement.SetAttribute("name", track.Name ?? $"Video {track.Index}");
            trackElement.SetAttribute("muted", track.Mute.ToString());
            trackElement.SetAttribute("solo", track.Solo.ToString());

            foreach (TrackEvent trackEvent in track.Events)
            {
                if (trackEvent is VideoEvent videoEvent)
                {
                    trackElement.AppendChild(CreateVideoEventElement(doc, videoEvent));
                }
            }

            return trackElement;
        }

        /// <summary>
        /// Creates an audio track element with all its events.
        /// </summary>
        private static XmlElement CreateAudioTrackElement(XmlDocument doc, AudioTrack track)
        {
            XmlElement trackElement = doc.CreateElement("AudioTrack");
            trackElement.SetAttribute("index", track.Index.ToString());
            trackElement.SetAttribute("name", track.Name ?? $"Audio {track.Index}");
            trackElement.SetAttribute("muted", track.Mute.ToString());
            trackElement.SetAttribute("solo", track.Solo.ToString());
            trackElement.SetAttribute("volume", track.Volume.ToString("F4"));
            trackElement.SetAttribute("panX", track.PanX.ToString("F4"));

            foreach (TrackEvent trackEvent in track.Events)
            {
                if (trackEvent is AudioEvent audioEvent)
                {
                    trackElement.AppendChild(CreateAudioEventElement(doc, audioEvent));
                }
            }

            return trackElement;
        }

        /// <summary>
        /// Creates a video event element with media and effect information.
        /// </summary>
        private static XmlElement CreateVideoEventElement(XmlDocument doc, VideoEvent videoEvent)
        {
            XmlElement eventElement = doc.CreateElement("VideoEvent");
            eventElement.SetAttribute("name", videoEvent.Name ?? string.Empty);
            eventElement.SetAttribute("start", videoEvent.Start.ToString());
            eventElement.SetAttribute("length", videoEvent.Length.ToString());
            eventElement.SetAttribute("startInMedia", videoEvent.Takes[0]?.Offset.ToString() ?? "0");

            if (videoEvent.Takes.Count > 0 && videoEvent.Takes[0].Media != null)
            {
                eventElement.AppendChild(CreateMediaElement(doc, videoEvent.Takes[0].Media));
            }

            if (videoEvent.Effects.Count > 0)
            {
                eventElement.AppendChild(CreateEffectsElement(doc, videoEvent.Effects));
            }

            return eventElement;
        }

        /// <summary>
        /// Creates an audio event element with media and effect information.
        /// </summary>
        private static XmlElement CreateAudioEventElement(XmlDocument doc, AudioEvent audioEvent)
        {
            XmlElement eventElement = doc.CreateElement("AudioEvent");
            eventElement.SetAttribute("name", audioEvent.Name ?? string.Empty);
            eventElement.SetAttribute("start", audioEvent.Start.ToString());
            eventElement.SetAttribute("length", audioEvent.Length.ToString());
            eventElement.SetAttribute("startInMedia", audioEvent.Takes[0]?.Offset.ToString() ?? "0");

            if (audioEvent.Takes.Count > 0 && audioEvent.Takes[0].Media != null)
            {
                eventElement.AppendChild(CreateMediaElement(doc, audioEvent.Takes[0].Media));
            }

            if (audioEvent.Effects.Count > 0)
            {
                eventElement.AppendChild(CreateEffectsElement(doc, audioEvent.Effects));
            }

            return eventElement;
        }

        /// <summary>
        /// Creates a media element representing a media file.
        /// </summary>
        private static XmlElement CreateMediaElement(XmlDocument doc, Media media)
        {
            XmlElement mediaElement = doc.CreateElement("Media");
            mediaElement.SetAttribute("path", media.FilePath ?? string.Empty);
            mediaElement.SetAttribute("duration", media.Length?.ToString() ?? "0");

            if (!media.IsGenerated())
            {
                mediaElement.SetAttribute("filename", Path.GetFileName(media.FilePath));
            }
            else
            {
                mediaElement.SetAttribute("type", "generated");
            }

            return mediaElement;
        }

        /// <summary>
        /// Creates effects element containing all effects applied to an event or track.
        /// </summary>
        private static XmlElement CreateEffectsElement(XmlDocument doc, Effects effects)
        {
            XmlElement effectsElement = doc.CreateElement("Effects");

            foreach (Effect effect in effects)
            {
                XmlElement effectElement = doc.CreateElement("Effect");
                effectElement.SetAttribute("name", effect.Description ?? "Unknown");
                effectElement.SetAttribute("bypass", effect.Bypass.ToString());
                effectElement.SetAttribute("index", effect.Index.ToString());
                effectsElement.AppendChild(effectElement);
            }

            return effectsElement;
        }

        /// <summary>
        /// Creates markers element containing all project markers.
        /// </summary>
        private static XmlElement CreateMarkersElement(XmlDocument doc, Project project)
        {
            XmlElement markersElement = doc.CreateElement("Markers");

            foreach (Marker marker in project.Markers)
            {
                XmlElement markerElement = doc.CreateElement("Marker");
                markerElement.SetAttribute("position", marker.Position.ToString());
                markerElement.SetAttribute("label", marker.Label ?? string.Empty);
                markersElement.AppendChild(markerElement);
            }

            return markersElement;
        }

        /// <summary>
        /// Creates regions element containing all project regions.
        /// </summary>
        private static XmlElement CreateRegionsElement(XmlDocument doc, Project project)
        {
            XmlElement regionsElement = doc.CreateElement("Regions");

            foreach (Region region in project.Regions)
            {
                XmlElement regionElement = doc.CreateElement("Region");
                regionElement.SetAttribute("start", region.Position.ToString());
                regionElement.SetAttribute("end", region.End.ToString());
                regionElement.SetAttribute("label", region.Label ?? string.Empty);
                regionsElement.AppendChild(regionElement);
            }

            return regionsElement;
        }

        /// <summary>
        /// Parses DaVinci Resolve XML and applies timeline data to the VEGAS Pro project.
        /// </summary>
        private static void ParseResolveXmlToProject(Project project, XmlElement root)
        {
            if (root == null)
                return;

            XmlElement tracksElement = root.SelectSingleNode("Tracks") as XmlElement;
            if (tracksElement != null)
            {
                ParseTracksFromXml(project, tracksElement);
            }

            XmlElement markersElement = root.SelectSingleNode("Markers") as XmlElement;
            if (markersElement != null)
            {
                ParseMarkersFromXml(project, markersElement);
            }

            XmlElement regionsElement = root.SelectSingleNode("Regions") as XmlElement;
            if (regionsElement != null)
            {
                ParseRegionsFromXml(project, regionsElement);
            }
        }

        /// <summary>
        /// Parses tracks from XML and creates them in the project.
        /// </summary>
        private static void ParseTracksFromXml(Project project, XmlElement tracksElement)
        {
            XmlNodeList videoTracks = tracksElement.SelectNodes("VideoTrack");
            foreach (XmlElement trackElement in videoTracks)
            {
                ParseVideoTrackFromXml(project, trackElement);
            }

            XmlNodeList audioTracks = tracksElement.SelectNodes("AudioTrack");
            foreach (XmlElement trackElement in audioTracks)
            {
                ParseAudioTrackFromXml(project, trackElement);
            }
        }

        /// <summary>
        /// Parses a video track from XML.
        /// </summary>
        private static void ParseVideoTrackFromXml(Project project, XmlElement trackElement)
        {
            try
            {
                string trackName = trackElement.GetAttribute("name") ?? "Imported Video Track";
                bool isMuted = ParseBoolAttribute(trackElement, "muted", false);
                bool isSolo = ParseBoolAttribute(trackElement, "solo", false);

                VideoTrack track = new VideoTrack(project, -1, trackName);
                project.Tracks.Add(track);

                if (isMuted) track.Mute = true;
                if (isSolo) track.Solo = true;

                XmlNodeList eventNodes = trackElement.SelectNodes("VideoEvent");
                foreach (XmlElement eventElement in eventNodes)
                {
                    ParseVideoEventFromXml(project, track, eventElement);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing video track: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses an audio track from XML.
        /// </summary>
        private static void ParseAudioTrackFromXml(Project project, XmlElement trackElement)
        {
            try
            {
                string trackName = trackElement.GetAttribute("name") ?? "Imported Audio Track";
                bool isMuted = ParseBoolAttribute(trackElement, "muted", false);
                bool isSolo = ParseBoolAttribute(trackElement, "solo", false);
                float volume = ParseFloatAttribute(trackElement, "volume", 1.0f);
                float panX = ParseFloatAttribute(trackElement, "panX", 0.0f);

                AudioTrack track = new AudioTrack(project, -1, trackName);
                project.Tracks.Add(track);

                track.Mute = isMuted;
                track.Solo = isSolo;
                track.Volume = volume;
                track.PanX = panX;

                XmlNodeList eventNodes = trackElement.SelectNodes("AudioEvent");
                foreach (XmlElement eventElement in eventNodes)
                {
                    ParseAudioEventFromXml(project, track, eventElement);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing audio track: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a video event from XML.
        /// </summary>
        private static void ParseVideoEventFromXml(Project project, VideoTrack track, XmlElement eventElement)
        {
            try
            {
                Timecode start = ParseTimecodeAttribute(eventElement, "start");
                Timecode length = ParseTimecodeAttribute(eventElement, "length");
                string eventName = eventElement.GetAttribute("name") ?? string.Empty;

                if (start != null && length != null)
                {
                    VideoEvent videoEvent = new VideoEvent(project, start, length, eventName);
                    track.Events.Add(videoEvent);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing video event: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses an audio event from XML.
        /// </summary>
        private static void ParseAudioEventFromXml(Project project, AudioTrack track, XmlElement eventElement)
        {
            try
            {
                Timecode start = ParseTimecodeAttribute(eventElement, "start");
                Timecode length = ParseTimecodeAttribute(eventElement, "length");
                string eventName = eventElement.GetAttribute("name") ?? string.Empty;

                if (start != null && length != null)
                {
                    AudioEvent audioEvent = new AudioEvent(project, start, length, eventName);
                    track.Events.Add(audioEvent);
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing audio event: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses markers from XML.
        /// </summary>
        private static void ParseMarkersFromXml(Project project, XmlElement markersElement)
        {
            XmlNodeList markerNodes = markersElement.SelectNodes("Marker");
            foreach (XmlElement markerElement in markerNodes)
            {
                try
                {
                    Timecode position = ParseTimecodeAttribute(markerElement, "position");
                    string label = markerElement.GetAttribute("label") ?? string.Empty;

                    if (position != null)
                    {
                        Marker marker = new Marker(position, label);
                        project.Markers.Add(marker);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"Error parsing marker: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Parses regions from XML.
        /// </summary>
        private static void ParseRegionsFromXml(Project project, XmlElement regionsElement)
        {
            XmlNodeList regionNodes = regionsElement.SelectNodes("Region");
            foreach (XmlElement regionElement in regionNodes)
            {
                try
                {
                    Timecode start = ParseTimecodeAttribute(regionElement, "start");
                    Timecode end = ParseTimecodeAttribute(regionElement, "end");
                    string label = regionElement.GetAttribute("label") ?? string.Empty;

                    if (start != null && end != null)
                    {
                        Timecode length = end - start;
                        Region region = new Region(start, length, label);
                        project.Regions.Add(region);
                    }
                }
                catch (Exception ex)
                {
                    System.Windows.Forms.MessageBox.Show($"Error parsing region: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Gets the project name from the file path or returns a default name.
        /// </summary>
        private static string GetProjectName(Project project)
        {
            if (!string.IsNullOrEmpty(project.FilePath))
            {
                return Path.GetFileNameWithoutExtension(project.FilePath);
            }
            return "Untitled Project";
        }

        /// <summary>
        /// Gets the frame rate from the project video properties.
        /// </summary>
        private static double GetFrameRate(Project project)
        {
            if (project.Video.FrameRate > 0)
            {
                return project.Video.FrameRate;
            }
            return 30.0;
        }

        /// <summary>
        /// Gets the project duration.
        /// </summary>
        private static string GetProjectDuration(Project project)
        {
            Timecode maxTime = new Timecode(0);

            foreach (Track track in project.Tracks)
            {
                foreach (TrackEvent trackEvent in track.Events)
                {
                    if (trackEvent.End > maxTime)
                    {
                        maxTime = trackEvent.End;
                    }
                }
            }

            return maxTime.ToString();
        }

        /// <summary>
        /// Parses a timecode attribute from an XML element.
        /// </summary>
        private static Timecode ParseTimecodeAttribute(XmlElement element, string attributeName)
        {
            try
            {
                string value = element.GetAttribute(attributeName);
                if (!string.IsNullOrEmpty(value))
                {
                    return new Timecode(value);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Parses a boolean attribute from an XML element.
        /// </summary>
        private static bool ParseBoolAttribute(XmlElement element, string attributeName, bool defaultValue)
        {
            try
            {
                string value = element.GetAttribute(attributeName);
                if (!string.IsNullOrEmpty(value) && bool.TryParse(value, out bool result))
                {
                    return result;
                }
            }
            catch { }

            return defaultValue;
        }

        /// <summary>
        /// Parses a float attribute from an XML element.
        /// </summary>
        private static float ParseFloatAttribute(XmlElement element, string attributeName, float defaultValue)
        {
            try
            {
                string value = element.GetAttribute(attributeName);
                if (!string.IsNullOrEmpty(value) && float.TryParse(value, out float result))
                {
                    return result;
                }
            }
            catch { }

            return defaultValue;
        }

        #region EDL Format Support

        /// <summary>
        /// Exports a VEGAS Pro project to CMX 3600 EDL (Edit Decision List) format.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <returns>An EDL string in CMX 3600 format representing the project.</returns>
        /// <exception cref="ArgumentNullException">Thrown when project is null.</exception>
        public static string ExportProjectToEdl(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            try
            {
                StringBuilder edl = new StringBuilder();
                
                // EDL Header
                edl.AppendLine("TITLE: " + GetProjectName(project));
                edl.AppendLine("FCM: DROP FRAME" + (IsDropFrame(project) ? "" : "NCF"));
                edl.AppendLine();

                // Track all video and audio events
                int eventNumber = 1;
                Dictionary<string, int> mediaIndexMap = new Dictionary<string, int>();
                int mediaIndex = 1;

                foreach (Track track in project.Tracks)
                {
                    if (!track.IsVideo() && !track.IsAudio())
                        continue;

                    foreach (TrackEvent trackEvent in track.Events.OrderBy(e => e.Start))
                    {
                        string mediaKey = GetMediaKey(trackEvent);
                        if (!string.IsNullOrEmpty(mediaKey) && !mediaIndexMap.ContainsKey(mediaKey))
                        {
                            mediaIndexMap[mediaKey] = mediaIndex++;
                        }

                        edl.Append(CreateEdlEventLine(eventNumber++, trackEvent, mediaIndexMap, project));
                        edl.AppendLine();
                    }
                }

                // Add media file locator information
                if (mediaIndexMap.Count > 0)
                {
                    edl.AppendLine();
                    edl.AppendLine("* MEDIA LOCATORS");
                    foreach (Track track in project.Tracks)
                    {
                        foreach (TrackEvent trackEvent in track.Events)
                        {
                            string mediaKey = GetMediaKey(trackEvent);
                            if (!string.IsNullOrEmpty(mediaKey) && mediaIndexMap.ContainsKey(mediaKey))
                            {
                                Media media = GetMediaFromEvent(trackEvent);
                                if (media != null)
                                {
                                    edl.AppendLine($"LOCATOR {mediaIndexMap[mediaKey]:D2} RED     {media.FilePath}");
                                }
                            }
                        }
                    }
                }

                return edl.ToString();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export project to EDL format.", ex);
            }
        }

        /// <summary>
        /// Saves a VEGAS Pro project as CMX 3600 EDL format to a file.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <param name="filePath">The file path where the EDL will be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        public static void ExportProjectToEdlFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string edlContent = ExportProjectToEdl(project);
                File.WriteAllText(filePath, edlContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save project to EDL file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Imports timeline events from a CMX 3600 EDL file and applies them to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="filePath">The file path of the EDL file to import.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public static void ImportFromEdlFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            try
            {
                string edlContent = File.ReadAllText(filePath, Encoding.UTF8);
                ImportFromEdl(project, edlContent);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException) && !(ex is ArgumentException) && !(ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"Failed to import EDL from file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Imports timeline events from CMX 3600 EDL format and applies them to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="edlContent">The EDL content string in CMX 3600 format.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or edlContent is null.</exception>
        /// <exception cref="ArgumentException">Thrown when edlContent is empty.</exception>
        public static void ImportFromEdl(Project project, string edlContent)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(edlContent))
                throw new ArgumentException("EDL content cannot be null or empty.", nameof(edlContent));

            try
            {
                string[] lines = edlContent.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                ParseEdlLines(project, lines);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to import EDL format.", ex);
            }
        }

        /// <summary>
        /// Creates an EDL event line for a track event.
        /// </summary>
        private static string CreateEdlEventLine(int eventNumber, TrackEvent trackEvent, Dictionary<string, int> mediaIndexMap, Project project)
        {
            string mediaKey = GetMediaKey(trackEvent);
            int mediaNumber = string.IsNullOrEmpty(mediaKey) ? 0 : mediaIndexMap[mediaKey];
            
            string reel = mediaNumber > 0 ? $"{mediaNumber:D3}" : "BL";
            string channel = trackEvent is VideoEvent ? "V" : "A";
            
            Timecode inTime = GetMediaInTime(trackEvent);
            Timecode outTime = GetMediaOutTime(trackEvent, inTime);
            Timecode recInTime = trackEvent.Start;
            Timecode recOutTime = trackEvent.End;

            string transType = "CUT";
            double transRate = 0;

            string line = string.Format("{0:D3}  {1}  {2}     {3}    {4} {5} {6} {7}",
                eventNumber,
                reel,
                channel,
                transType,
                inTime.ToString(),
                outTime.ToString(),
                recInTime.ToString(),
                recOutTime.ToString()
            );

            if (transRate > 0)
            {
                line += string.Format("\n* TRANSITION RATE {0:F2}", transRate);
            }

            return line;
        }

        /// <summary>
        /// Parses EDL lines and creates events in the project.
        /// </summary>
        private static void ParseEdlLines(Project project, string[] lines)
        {
            AudioTrack currentAudioTrack = null;
            VideoTrack currentVideoTrack = null;
            Dictionary<int, string> mediaMap = new Dictionary<int, string>();

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (string.IsNullOrEmpty(trimmed) || trimmed.StartsWith("*"))
                    continue;

                // Parse media locator lines
                if (trimmed.StartsWith("LOCATOR"))
                {
                    ParseEdlMediaLocator(trimmed, mediaMap);
                    continue;
                }

                // Parse event lines
                if (IsEdlEventLine(trimmed))
                {
                    try
                    {
                        ParseEdlEventLine(project, trimmed, ref currentAudioTrack, ref currentVideoTrack, mediaMap);
                    }
                    catch (Exception ex)
                    {
                        System.Windows.Forms.MessageBox.Show($"Error parsing EDL event: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Parses a single EDL event line.
        /// </summary>
        private static void ParseEdlEventLine(Project project, string line, ref AudioTrack audioTrack, ref VideoTrack videoTrack, Dictionary<int, string> mediaMap)
        {
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 8)
                return;

            string channel = parts[2];
            string inTimeStr = parts[4];
            string outTimeStr = parts[5];
            string recInTimeStr = parts[6];
            string recOutTimeStr = parts[7];

            Timecode recInTime = ParseEdlTimecode(recInTimeStr);
            Timecode recOutTime = ParseEdlTimecode(recOutTimeStr);

            if (recInTime == null || recOutTime == null)
                return;

            Timecode length = recOutTime - recInTime;

            if (channel.Contains("V"))
            {
                if (videoTrack == null)
                {
                    videoTrack = new VideoTrack(project, -1, "Imported Video");
                    project.Tracks.Add(videoTrack);
                }
                VideoEvent videoEvent = new VideoEvent(project, recInTime, length, "EDL Clip");
                videoTrack.Events.Add(videoEvent);
            }
            else if (channel.Contains("A"))
            {
                if (audioTrack == null)
                {
                    audioTrack = new AudioTrack(project, -1, "Imported Audio");
                    project.Tracks.Add(audioTrack);
                }
                AudioEvent audioEvent = new AudioEvent(project, recInTime, length, "EDL Clip");
                audioTrack.Events.Add(audioEvent);
            }
        }

        /// <summary>
        /// Parses an EDL media locator line.
        /// </summary>
        private static void ParseEdlMediaLocator(string line, Dictionary<int, string> mediaMap)
        {
            // Format: LOCATOR 01 RED     /path/to/file
            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 4 && int.TryParse(parts[1], out int mediaNumber))
            {
                string filePath = string.Join(" ", parts.Skip(3));
                mediaMap[mediaNumber] = filePath;
            }
        }

        /// <summary>
        /// Checks if a line is an EDL event line.
        /// </summary>
        private static bool IsEdlEventLine(string line)
        {
            if (string.IsNullOrEmpty(line) || line.Length < 3)
                return false;

            string[] parts = line.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            return parts.Length >= 8 && int.TryParse(parts[0], out _);
        }

        /// <summary>
        /// Parses an EDL timecode string to a VEGAS Timecode.
        /// </summary>
        private static Timecode ParseEdlTimecode(string timecodeStr)
        {
            try
            {
                return new Timecode(timecodeStr);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a media key for a track event to identify unique media files.
        /// </summary>
        private static string GetMediaKey(TrackEvent trackEvent)
        {
            Media media = GetMediaFromEvent(trackEvent);
            return media?.FilePath ?? string.Empty;
        }

        /// <summary>
        /// Gets the Media object from a track event.
        /// </summary>
        private static Media GetMediaFromEvent(TrackEvent trackEvent)
        {
            if (trackEvent.Takes.Count > 0 && trackEvent.Takes[0].Media != null)
            {
                return trackEvent.Takes[0].Media;
            }
            return null;
        }

        /// <summary>
        /// Gets the in-time (start time in source media) for a track event.
        /// </summary>
        private static Timecode GetMediaInTime(TrackEvent trackEvent)
        {
            if (trackEvent.Takes.Count > 0)
            {
                return trackEvent.Takes[0].Offset ?? new Timecode(0);
            }
            return new Timecode(0);
        }

        /// <summary>
        /// Gets the out-time (end time in source media) for a track event.
        /// </summary>
        private static Timecode GetMediaOutTime(TrackEvent trackEvent, Timecode inTime)
        {
            Timecode mediaOffset = inTime;
            Timecode eventLength = trackEvent.Length ?? new Timecode(0);
            return mediaOffset + eventLength;
        }

        /// <summary>
        /// Determines if the project uses drop frame timecode.
        /// </summary>
        private static bool IsDropFrame(Project project)
        {
            return project.Video.FrameRate == 29.97 || project.Video.FrameRate == 59.94;
        }

        #endregion

        #region Premiere Pro PRPROJ Format Support

        /// <summary>
        /// Exports a VEGAS Pro project to Premiere Pro compatible PRPROJ format (XML-based).
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <returns>An XML string representing the project in Premiere Pro PRPROJ format.</returns>
        /// <exception cref="ArgumentNullException">Thrown when project is null.</exception>
        public static string ExportProjectToPrproj(Project project)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            try
            {
                XmlDocument doc = new XmlDocument();
                XmlDeclaration declaration = doc.CreateXmlDeclaration(XML_VERSION, XML_ENCODING, null);
                doc.AppendChild(declaration);

                // Create root PremiereProject element
                XmlElement root = doc.CreateElement("PremiereProject");
                root.SetAttribute("Version", GetPremiereMajorVersion(project));
                doc.AppendChild(root);

                // Add project metadata
                XmlElement metadata = doc.CreateElement("ProjectMetadata");
                metadata.AppendChild(CreateProjElement(doc, "ProjectName", GetProjectName(project)));
                metadata.AppendChild(CreateProjElement(doc, "ProjectFrameRate", GetFrameRate(project).ToString("F2")));
                metadata.AppendChild(CreateProjElement(doc, "ProjectWidth", project.Video.Width.ToString()));
                metadata.AppendChild(CreateProjElement(doc, "ProjectHeight", project.Video.Height.ToString()));
                metadata.AppendChild(CreateProjElement(doc, "AudioSampleRate", project.Audio.SampleRate.ToString()));
                metadata.AppendChild(CreateProjElement(doc, "AudioBitDepth", project.Audio.BitDepth.ToString()));
                root.AppendChild(metadata);

                // Add sequences (timelines)
                XmlElement sequences = doc.CreateElement("Sequences");
                sequences.AppendChild(CreateProjSequence(doc, project));
                root.AppendChild(sequences);

                // Add media pool
                XmlElement mediaPool = doc.CreateElement("MediaPool");
                mediaPool.AppendChild(CreateProjMediaPool(doc, project));
                root.AppendChild(mediaPool);

                // Format output
                using (StringWriter sw = new StringWriter())
                {
                    using (XmlTextWriter writer = new XmlTextWriter(sw) { Formatting = Formatting.Indented })
                    {
                        doc.WriteTo(writer);
                        return sw.ToString();
                    }
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to export project to Premiere Pro PRPROJ format.", ex);
            }
        }

        /// <summary>
        /// Saves a VEGAS Pro project as Premiere Pro PRPROJ format to a file.
        /// </summary>
        /// <param name="project">The VEGAS Pro project to export.</param>
        /// <param name="filePath">The file path where the PRPROJ will be saved.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        public static void ExportProjectToPrprojFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            try
            {
                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                string prprojContent = ExportProjectToPrproj(project);
                File.WriteAllText(filePath, prprojContent, Encoding.UTF8);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to save project to PRPROJ file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Imports timeline data from Premiere Pro PRPROJ format and applies it to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="prprojContent">The PRPROJ content string in Premiere Pro format.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or prprojContent is null.</exception>
        /// <exception cref="ArgumentException">Thrown when prprojContent is empty.</exception>
        public static void ImportFromPrproj(Project project, string prprojContent)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(prprojContent))
                throw new ArgumentException("PRPROJ content cannot be null or empty.", nameof(prprojContent));

            try
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(prprojContent);
                ParsePrprojToProject(project, doc.DocumentElement);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Failed to parse Premiere Pro PRPROJ format.", ex);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Failed to import project from PRPROJ format.", ex);
            }
        }

        /// <summary>
        /// Imports timeline data from a Premiere Pro PRPROJ file and applies it to a VEGAS Pro project.
        /// </summary>
        /// <param name="project">The target VEGAS Pro project.</param>
        /// <param name="filePath">The file path of the PRPROJ file to import.</param>
        /// <exception cref="ArgumentNullException">Thrown when project or filePath is null.</exception>
        /// <exception cref="ArgumentException">Thrown when filePath is empty.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the file does not exist.</exception>
        public static void ImportFromPrprojFile(Project project, string filePath)
        {
            if (project == null)
                throw new ArgumentNullException(nameof(project), "Project cannot be null.");

            if (string.IsNullOrEmpty(filePath))
                throw new ArgumentException("File path cannot be null or empty.", nameof(filePath));

            if (!File.Exists(filePath))
                throw new FileNotFoundException($"File not found: {filePath}");

            try
            {
                string prprojContent = File.ReadAllText(filePath, Encoding.UTF8);
                ImportFromPrproj(project, prprojContent);
            }
            catch (Exception ex) when (!(ex is ArgumentNullException) && !(ex is ArgumentException) && !(ex is FileNotFoundException))
            {
                throw new InvalidOperationException($"Failed to import project from PRPROJ file: {filePath}", ex);
            }
        }

        /// <summary>
        /// Creates a Premiere Pro XML element with value.
        /// </summary>
        private static XmlElement CreateProjElement(XmlDocument doc, string name, string value)
        {
            XmlElement element = doc.CreateElement(name);
            element.InnerText = value ?? string.Empty;
            return element;
        }

        /// <summary>
        /// Creates a Premiere Pro sequence element for the project.
        /// </summary>
        private static XmlElement CreateProjSequence(XmlDocument doc, Project project)
        {
            XmlElement sequence = doc.CreateElement("Sequence");
            sequence.SetAttribute("ObjectID", "1");
            sequence.SetAttribute("Name", GetProjectName(project) + " Sequence");

            // Add sequence settings
            XmlElement settings = doc.CreateElement("Settings");
            settings.AppendChild(CreateProjElement(doc, "FrameRate", GetFrameRate(project).ToString("F2")));
            settings.AppendChild(CreateProjElement(doc, "Width", project.Video.Width.ToString()));
            settings.AppendChild(CreateProjElement(doc, "Height", project.Video.Height.ToString()));
            sequence.AppendChild(settings);

            // Add video tracks
            XmlElement videoTracks = doc.CreateElement("VideoTracks");
            int videoTrackIndex = 0;
            foreach (Track track in project.Tracks)
            {
                if (track.IsVideo())
                {
                    videoTracks.AppendChild(CreateProjVideoTrack(doc, track as VideoTrack, videoTrackIndex++));
                }
            }
            sequence.AppendChild(videoTracks);

            // Add audio tracks
            XmlElement audioTracks = doc.CreateElement("AudioTracks");
            int audioTrackIndex = 0;
            foreach (Track track in project.Tracks)
            {
                if (track.IsAudio())
                {
                    audioTracks.AppendChild(CreateProjAudioTrack(doc, track as AudioTrack, audioTrackIndex++));
                }
            }
            sequence.AppendChild(audioTracks);

            return sequence;
        }

        /// <summary>
        /// Creates a Premiere Pro video track element.
        /// </summary>
        private static XmlElement CreateProjVideoTrack(XmlDocument doc, VideoTrack track, int index)
        {
            XmlElement videoTrack = doc.CreateElement("VideoTrack");
            videoTrack.SetAttribute("ObjectID", (100 + index).ToString());
            videoTrack.SetAttribute("Name", track.Name ?? $"Video {index + 1}");

            // Add track properties
            XmlElement properties = doc.CreateElement("Properties");
            properties.AppendChild(CreateProjElement(doc, "Locked", "false"));
            properties.AppendChild(CreateProjElement(doc, "Muted", track.Mute.ToString().ToLower()));
            properties.AppendChild(CreateProjElement(doc, "Solo", track.Solo.ToString().ToLower()));
            videoTrack.AppendChild(properties);

            // Add clips
            XmlElement clips = doc.CreateElement("Clips");
            foreach (TrackEvent trackEvent in track.Events.OrderBy(e => e.Start))
            {
                if (trackEvent is VideoEvent videoEvent)
                {
                    clips.AppendChild(CreateProjVideoClip(doc, videoEvent));
                }
            }
            videoTrack.AppendChild(clips);

            return videoTrack;
        }

        /// <summary>
        /// Creates a Premiere Pro audio track element.
        /// </summary>
        private static XmlElement CreateProjAudioTrack(XmlDocument doc, AudioTrack track, int index)
        {
            XmlElement audioTrack = doc.CreateElement("AudioTrack");
            audioTrack.SetAttribute("ObjectID", (200 + index).ToString());
            audioTrack.SetAttribute("Name", track.Name ?? $"Audio {index + 1}");

            // Add track properties
            XmlElement properties = doc.CreateElement("Properties");
            properties.AppendChild(CreateProjElement(doc, "Locked", "false"));
            properties.AppendChild(CreateProjElement(doc, "Muted", track.Mute.ToString().ToLower()));
            properties.AppendChild(CreateProjElement(doc, "Solo", track.Solo.ToString().ToLower()));
            properties.AppendChild(CreateProjElement(doc, "Volume", track.Volume.ToString("F4")));
            properties.AppendChild(CreateProjElement(doc, "Pan", track.PanX.ToString("F4")));
            audioTrack.AppendChild(properties);

            // Add clips
            XmlElement clips = doc.CreateElement("Clips");
            foreach (TrackEvent trackEvent in track.Events.OrderBy(e => e.Start))
            {
                if (trackEvent is AudioEvent audioEvent)
                {
                    clips.AppendChild(CreateProjAudioClip(doc, audioEvent));
                }
            }
            audioTrack.AppendChild(clips);

            return audioTrack;
        }

        /// <summary>
        /// Creates a Premiere Pro video clip element.
        /// </summary>
        private static XmlElement CreateProjVideoClip(XmlDocument doc, VideoEvent videoEvent)
        {
            XmlElement clip = doc.CreateElement("Clip");
            clip.SetAttribute("ObjectID", videoEvent.GetHashCode().ToString());
            clip.SetAttribute("Name", videoEvent.Name ?? "Video Clip");

            // Add clip timing
            XmlElement timing = doc.CreateElement("Timing");
            timing.AppendChild(CreateProjElement(doc, "Start", videoEvent.Start.ToString()));
            timing.AppendChild(CreateProjElement(doc, "Duration", videoEvent.Length.ToString()));
            timing.AppendChild(CreateProjElement(doc, "End", videoEvent.End.ToString()));
            clip.AppendChild(timing);

            // Add media reference
            if (videoEvent.Takes.Count > 0 && videoEvent.Takes[0].Media != null)
            {
                clip.AppendChild(CreateProjMediaReference(doc, videoEvent.Takes[0].Media, videoEvent.Takes[0].Offset));
            }

            return clip;
        }

        /// <summary>
        /// Creates a Premiere Pro audio clip element.
        /// </summary>
        private static XmlElement CreateProjAudioClip(XmlDocument doc, AudioEvent audioEvent)
        {
            XmlElement clip = doc.CreateElement("Clip");
            clip.SetAttribute("ObjectID", audioEvent.GetHashCode().ToString());
            clip.SetAttribute("Name", audioEvent.Name ?? "Audio Clip");

            // Add clip timing
            XmlElement timing = doc.CreateElement("Timing");
            timing.AppendChild(CreateProjElement(doc, "Start", audioEvent.Start.ToString()));
            timing.AppendChild(CreateProjElement(doc, "Duration", audioEvent.Length.ToString()));
            timing.AppendChild(CreateProjElement(doc, "End", audioEvent.End.ToString()));
            clip.AppendChild(timing);

            // Add media reference
            if (audioEvent.Takes.Count > 0 && audioEvent.Takes[0].Media != null)
            {
                clip.AppendChild(CreateProjMediaReference(doc, audioEvent.Takes[0].Media, audioEvent.Takes[0].Offset));
            }

            return clip;
        }

        /// <summary>
        /// Creates a Premiere Pro media reference element.
        /// </summary>
        private static XmlElement CreateProjMediaReference(XmlDocument doc, Media media, Timecode offset)
        {
            XmlElement mediaRef = doc.CreateElement("MediaReference");
            mediaRef.AppendChild(CreateProjElement(doc, "FilePath", media.FilePath ?? string.Empty));
            mediaRef.AppendChild(CreateProjElement(doc, "MediaType", media.IsGenerated() ? "Generated" : "File"));
            mediaRef.AppendChild(CreateProjElement(doc, "Offset", offset?.ToString() ?? "0"));
            mediaRef.AppendChild(CreateProjElement(doc, "Duration", media.Length?.ToString() ?? "0"));

            return mediaRef;
        }

        /// <summary>
        /// Creates a Premiere Pro media pool element.
        /// </summary>
        private static XmlElement CreateProjMediaPool(XmlDocument doc, Project project)
        {
            XmlElement pool = doc.CreateElement("Pool");

            HashSet<string> addedMedia = new HashSet<string>();

            foreach (Track track in project.Tracks)
            {
                foreach (TrackEvent trackEvent in track.Events)
                {
                    if (trackEvent.Takes.Count > 0 && trackEvent.Takes[0].Media != null)
                    {
                        Media media = trackEvent.Takes[0].Media;
                        if (!addedMedia.Contains(media.FilePath ?? string.Empty))
                        {
                            pool.AppendChild(CreateProjMediaItem(doc, media));
                            addedMedia.Add(media.FilePath ?? string.Empty);
                        }
                    }
                }
            }

            return pool;
        }

        /// <summary>
        /// Creates a Premiere Pro media item element.
        /// </summary>
        private static XmlElement CreateProjMediaItem(XmlDocument doc, Media media)
        {
            XmlElement mediaItem = doc.CreateElement("MediaItem");
            mediaItem.SetAttribute("ObjectID", media.FilePath?.GetHashCode().ToString() ?? "0");

            mediaItem.AppendChild(CreateProjElement(doc, "Name", Path.GetFileName(media.FilePath) ?? "Media"));
            mediaItem.AppendChild(CreateProjElement(doc, "FilePath", media.FilePath ?? string.Empty));
            mediaItem.AppendChild(CreateProjElement(doc, "Duration", media.Length?.ToString() ?? "0"));
            mediaItem.AppendChild(CreateProjElement(doc, "IsGenerated", media.IsGenerated().ToString().ToLower()));

            return mediaItem;
        }

        /// <summary>
        /// Parses Premiere Pro PRPROJ format and applies it to a VEGAS Pro project.
        /// </summary>
        private static void ParsePrprojToProject(Project project, XmlElement root)
        {
            if (root == null)
                return;

            try
            {
                // Parse sequences
                XmlElement sequences = root.SelectSingleNode("Sequences") as XmlElement;
                if (sequences != null)
                {
                    XmlNodeList sequenceNodes = sequences.SelectNodes("Sequence");
                    foreach (XmlElement sequenceElement in sequenceNodes)
                    {
                        ParseProjSequence(project, sequenceElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro PRPROJ: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro sequence and creates tracks/events in the project.
        /// </summary>
        private static void ParseProjSequence(Project project, XmlElement sequenceElement)
        {
            try
            {
                // Parse video tracks
                XmlElement videoTracksElement = sequenceElement.SelectSingleNode("VideoTracks") as XmlElement;
                if (videoTracksElement != null)
                {
                    XmlNodeList videoTrackNodes = videoTracksElement.SelectNodes("VideoTrack");
                    foreach (XmlElement trackElement in videoTrackNodes)
                    {
                        ParseProjVideoTrack(project, trackElement);
                    }
                }

                // Parse audio tracks
                XmlElement audioTracksElement = sequenceElement.SelectSingleNode("AudioTracks") as XmlElement;
                if (audioTracksElement != null)
                {
                    XmlNodeList audioTrackNodes = audioTracksElement.SelectNodes("AudioTrack");
                    foreach (XmlElement trackElement in audioTrackNodes)
                    {
                        ParseProjAudioTrack(project, trackElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro sequence: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro video track and creates it in the project.
        /// </summary>
        private static void ParseProjVideoTrack(Project project, XmlElement trackElement)
        {
            try
            {
                string trackName = trackElement.GetAttribute("Name") ?? "Imported Video Track";
                VideoTrack track = new VideoTrack(project, -1, trackName);

                // Parse track properties
                XmlElement properties = trackElement.SelectSingleNode("Properties") as XmlElement;
                if (properties != null)
                {
                    track.Mute = ParseProjBoolElement(properties, "Muted", false);
                    track.Solo = ParseProjBoolElement(properties, "Solo", false);
                }

                project.Tracks.Add(track);

                // Parse clips
                XmlElement clipsElement = trackElement.SelectSingleNode("Clips") as XmlElement;
                if (clipsElement != null)
                {
                    XmlNodeList clipNodes = clipsElement.SelectNodes("Clip");
                    foreach (XmlElement clipElement in clipNodes)
                    {
                        ParseProjVideoClip(project, track, clipElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro video track: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro audio track and creates it in the project.
        /// </summary>
        private static void ParseProjAudioTrack(Project project, XmlElement trackElement)
        {
            try
            {
                string trackName = trackElement.GetAttribute("Name") ?? "Imported Audio Track";
                AudioTrack track = new AudioTrack(project, -1, trackName);

                // Parse track properties
                XmlElement properties = trackElement.SelectSingleNode("Properties") as XmlElement;
                if (properties != null)
                {
                    track.Mute = ParseProjBoolElement(properties, "Muted", false);
                    track.Solo = ParseProjBoolElement(properties, "Solo", false);
                    track.Volume = ParseProjFloatElement(properties, "Volume", 1.0f);
                    track.PanX = ParseProjFloatElement(properties, "Pan", 0.0f);
                }

                project.Tracks.Add(track);

                // Parse clips
                XmlElement clipsElement = trackElement.SelectSingleNode("Clips") as XmlElement;
                if (clipsElement != null)
                {
                    XmlNodeList clipNodes = clipsElement.SelectNodes("Clip");
                    foreach (XmlElement clipElement in clipNodes)
                    {
                        ParseProjAudioClip(project, track, clipElement);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro audio track: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro video clip and creates it in the project.
        /// </summary>
        private static void ParseProjVideoClip(Project project, VideoTrack track, XmlElement clipElement)
        {
            try
            {
                string clipName = clipElement.GetAttribute("Name") ?? "Video Clip";

                XmlElement timingElement = clipElement.SelectSingleNode("Timing") as XmlElement;
                if (timingElement != null)
                {
                    Timecode start = ParseProjTimecodeElement(timingElement, "Start");
                    Timecode duration = ParseProjTimecodeElement(timingElement, "Duration");

                    if (start != null && duration != null)
                    {
                        VideoEvent videoEvent = new VideoEvent(project, start, duration, clipName);
                        track.Events.Add(videoEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro video clip: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro audio clip and creates it in the project.
        /// </summary>
        private static void ParseProjAudioClip(Project project, AudioTrack track, XmlElement clipElement)
        {
            try
            {
                string clipName = clipElement.GetAttribute("Name") ?? "Audio Clip";

                XmlElement timingElement = clipElement.SelectSingleNode("Timing") as XmlElement;
                if (timingElement != null)
                {
                    Timecode start = ParseProjTimecodeElement(timingElement, "Start");
                    Timecode duration = ParseProjTimecodeElement(timingElement, "Duration");

                    if (start != null && duration != null)
                    {
                        AudioEvent audioEvent = new AudioEvent(project, start, duration, clipName);
                        track.Events.Add(audioEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show($"Error parsing Premiere Pro audio clip: {ex.Message}");
            }
        }

        /// <summary>
        /// Parses a Premiere Pro timecode element.
        /// </summary>
        private static Timecode ParseProjTimecodeElement(XmlElement element, string elementName)
        {
            try
            {
                XmlElement child = element.SelectSingleNode(elementName) as XmlElement;
                if (child != null && !string.IsNullOrEmpty(child.InnerText))
                {
                    return new Timecode(child.InnerText);
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Parses a Premiere Pro boolean element.
        /// </summary>
        private static bool ParseProjBoolElement(XmlElement element, string elementName, bool defaultValue)
        {
            try
            {
                XmlElement child = element.SelectSingleNode(elementName) as XmlElement;
                if (child != null && bool.TryParse(child.InnerText, out bool result))
                {
                    return result;
                }
            }
            catch { }

            return defaultValue;
        }

        /// <summary>
        /// Parses a Premiere Pro float element.
        /// </summary>
        private static float ParseProjFloatElement(XmlElement element, string elementName, float defaultValue)
        {
            try
            {
                XmlElement child = element.SelectSingleNode(elementName) as XmlElement;
                if (child != null && float.TryParse(child.InnerText, out float result))
                {
                    return result;
                }
            }
            catch { }

            return defaultValue;
        }

        /// <summary>
        /// Gets the Premiere Pro major version number based on frame rate.
        /// </summary>
        private static string GetPremiereMajorVersion(Project project)
        {
            // Premiere Pro 2024 = version 24, 2023 = 23, etc.
            // Default to recent version
            return "24";
        }

        #endregion
    }
}
