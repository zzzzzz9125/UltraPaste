namespace ReaperDataParser
{
    public class ReaperStretchSegment
    {
        public double OffsetStart { get; set; }
        public double OffsetEnd { get; set; }
        public double OffsetLength { get { return OffsetEnd - OffsetStart; } }
        public double VelocityStart { get; set; }
        public double VelocityEnd { get; set; }
        public double VelocityAverage { get { return (VelocityStart + VelocityEnd) / 2; } }
    }
}
