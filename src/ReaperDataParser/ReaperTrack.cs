using System.Collections.Generic;

namespace ReaperDataParser
{
    public class ReaperTrack
    {
        public List<ReaperItem> Items { get; set; }
        public string Name { get; set; }
        public double[] VolPan { get; set; }
        public int[] MuteSolo { get; set; }
        public bool IPhase { get; set; }
        public List<ReaperEnvelope> Envelopes { get; set; }

        public ReaperTrack()
        {
            Items = new List<ReaperItem>();
            VolPan = new double[] { 1, 0, -1, -1, 1 };
            MuteSolo = new int[] { 0, 0, 0 };
            Envelopes = new List<ReaperEnvelope>();
        }
    }
}
