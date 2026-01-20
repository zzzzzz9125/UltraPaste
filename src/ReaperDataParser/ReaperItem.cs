using System.Collections.Generic;

namespace ReaperDataParser
{
    public class ReaperItem : ReaperTake
    {
        public double Position { get; set; }
        public double SnapOffs { get; set; }
        public double Length { get; set; }
        public bool Loop { get; set; }
        public bool AllTakes { get; set; }
        public double[] FadeIn { get; set; }
        public double[] FadeOut { get; set; }
        public int[] Mute { get; set; }
        public bool Sel { get; set; }
        public List<ReaperEnvelope> Envelopes { get; set; }
        public List<ReaperTake> Takes { get; set; }

        public ReaperItem()
        {
            FadeIn = new double[7];
            FadeOut = new double[7];
            Mute = new int[2];
            Envelopes = new List<ReaperEnvelope>();
            Takes = new List<ReaperTake>();
        }
    }
}
