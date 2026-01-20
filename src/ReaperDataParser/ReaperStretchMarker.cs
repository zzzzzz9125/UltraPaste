namespace ReaperDataParser
{
    public class ReaperStretchMarker
    {
        public double Offset { get; set; }
        public double Position { get; set; }
        public double VelocityChange { get; set; }

        public ReaperStretchMarker(double offset, double position, double velocityChange)
        {
            Offset = offset;
            Position = position;
            VelocityChange = velocityChange;
        }
    }
}
