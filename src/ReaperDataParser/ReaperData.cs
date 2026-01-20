using System.Collections.Generic;
using System.IO;

namespace ReaperDataParser
{
    public class ReaperData
    {
        public List<ReaperTrack> Tracks { get; set; }
        public bool IsTrackData { get; set; }
        private string projectFilePath;

        public string ProjectFilePath
        {
            get { return projectFilePath; }
            set
            {
                projectFilePath = value;
                List<ReaperSource> sources = new List<ReaperSource>();
                foreach (ReaperTrack track in Tracks)
                {
                    foreach (ReaperItem item in track.Items)
                    {
                        sources.Add(item.Source);
                        foreach (ReaperTake take in item.Takes)
                        {
                            sources.Add(take.Source);
                        }
                    }
                }
                string folder = Path.GetDirectoryName(projectFilePath);
                foreach (ReaperSource source in sources)
                {
                    source.UpdateFullFilePath(folder);
                }
            }
        }

        public ReaperData()
        {
            Tracks = new List<ReaperTrack>();
        }
    }
}
