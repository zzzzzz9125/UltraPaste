namespace ReaperDataParser
{
    public class ReaperTake
    {
        public bool Selected { get; set; }
        public string Name { get; set; }
        public double[] VolPan { get; set; }
        public double SOffs { get; set; }
        public double[] PlayRate { get; set; }
        public int ChanMode { get; set; }
        public ReaperStretchSegments StretchSegments { get; set; }
        public ReaperSource Source { get; set; }

        public ReaperTake()
        {
            Selected = false;
            VolPan = new double[] { 1, 0, 1, -1 };
            PlayRate = new double[] { 1, 1, 0, -1, 0, 0.0025 };
            StretchSegments = new ReaperStretchSegments();
            Source = new ReaperSource();
        }
    }
}
