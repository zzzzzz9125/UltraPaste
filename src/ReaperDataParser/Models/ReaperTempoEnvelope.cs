using System.Collections.Generic;

namespace ReaperDataParser.Models
{
    public class ReaperTempoEnvelope
    {
        public string EGUID { get; set; }
        public int[] Act { get; set; }
        public int[] Vis { get; set; }
        public int[] LaneHeight { get; set; }
        public int Arm { get; set; }
        public int[] DefShape { get; set; }
        public List<double[]> Points { get; set; }

        public ReaperTempoEnvelope()
        {
            Act = new int[] { 1, -1 };
            Vis = new int[] { 1, 0, 1 };
            LaneHeight = new int[] { 0, 0 };
            Arm = 0;
            DefShape = new int[] { 1, -1, -1 };
            Points = new List<double[]>();
        }
    }
}
