namespace ReaperDataParser.Models
{
    public class ReaperSourceMidi : ReaperSource
    {
        public bool HasData { get; set; }
        public int PPQ { get; set; }
        public string TimeBase { get; set; }
        public int CCInterp { get; set; }
        public string PooledEvents { get; set; }
        public string IgnoreTempo { get; set; }
        public int SourceColor { get; set; }
        public string EventFilter { get; set; }
        public string VelLane { get; set; }
        public string ConfigEditView { get; set; }
        public int KeySnap { get; set; }
        public int TrackSel { get; set; }
        public string ConfigEdit { get; set; }
        public string MIDIData { get; set; }

        public ReaperSourceMidi()
        {
            Type = "MIDI";
            HasData = false;
            PPQ = 480;
            TimeBase = "QN";
            CCInterp = 32;
            KeySnap = 0;
            TrackSel = 0;
            MIDIData = string.Empty;
        }
    }
}
