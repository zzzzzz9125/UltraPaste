using System.Collections.Generic;

namespace ReaperDataParser
{
    public class ReaperStretchSegments : List<ReaperStretchSegment>
    {
        public static ReaperStretchSegments GetFromMarkers(List<ReaperStretchMarker> markers)
        {
            ReaperStretchSegments segments = new ReaperStretchSegments();
            if (markers == null || markers.Count < 2)
            {
                return segments;
            }
            ReaperStretchSegment currentSegment = null;
            ReaperStretchMarker lastMarker = null;
            foreach (ReaperStretchMarker marker in markers)
            {
                if (currentSegment != null)
                {
                    currentSegment.OffsetEnd = marker.Offset;
                    double velocityAverage = (marker.Position - lastMarker.Position) / currentSegment.OffsetLength;
                    double velocityHalf = lastMarker.VelocityChange * velocityAverage;
                    currentSegment.VelocityStart = velocityAverage - velocityHalf;
                    currentSegment.VelocityEnd = velocityAverage + velocityHalf;
                    segments.Add(currentSegment);
                }

                currentSegment = new ReaperStretchSegment { OffsetStart = marker.Offset };
                lastMarker = marker;
            }
            return segments;
        }
    }
}
