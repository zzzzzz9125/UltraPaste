namespace ReaperDataParser
{
    public class ReaperSourceSection : ReaperSource
    {
        public double Length { get; set; }
        public int Mode { get; set; }
        public double StartPos { get; set; }
        public double Overlap { get; set; }
        public ReaperSource Source { get; set; }
    }
}
