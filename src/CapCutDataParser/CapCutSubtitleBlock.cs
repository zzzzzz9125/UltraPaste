using System;

namespace CapCutDataParser
{
    public sealed class CapCutSubtitleBlock
    {
        public string MaterialId { get; set; }
        public string Text { get; set; }
        public TimeSpan Start { get; set; }
        public TimeSpan End { get; set; }
    }
}