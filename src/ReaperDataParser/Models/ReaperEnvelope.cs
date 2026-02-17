using System.Collections.Generic;

namespace ReaperDataParser.Models
{
    public class ReaperEnvelope
    {
        public string Type { get; set; }
        public int[] Act { get; set; }
        public double[] SegRange { get; set; }
        public List<double[]> Points { get; set; }

        public ReaperEnvelope()
        {
            Act = new int[] { 1, -1 };
            SegRange = null;
            Points = new List<double[]>();
        }
    }
}
