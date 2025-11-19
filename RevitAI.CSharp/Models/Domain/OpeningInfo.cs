namespace RevitAI.Models.Domain
{
    /// <summary>
    /// Type of opening in a wall.
    /// </summary>
    public enum OpeningType
    {
        Door,
        Window,
        Other
    }

    /// <summary>
    /// POCO representing an opening (door/window) in a wall segment.
    /// Layer 1 SIL pattern - NO Revit API dependencies.
    /// </summary>
    public class OpeningInfo
    {
        /// <summary>
        /// Type of opening (door, window, etc.).
        /// </summary>
        public OpeningType Type { get; set; }

        /// <summary>
        /// Index of the wall segment containing this opening.
        /// References WallSegmentInfo in RoomBoundaryInfo.WallSegments list.
        /// </summary>
        public int WallSegmentIndex { get; set; }

        /// <summary>
        /// Center position of opening (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) CenterPosition { get; set; }

        /// <summary>
        /// Opening width in feet.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Opening height in feet.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Sill height in feet (for windows).
        /// </summary>
        public double SillHeight { get; set; }

        /// <summary>
        /// Factory method: Creates a door opening.
        /// </summary>
        public static OpeningInfo CreateDoor(
            int wallSegmentIndex,
            (double X, double Y, double Z) centerPosition,
            double width = 3.0)
        {
            return new OpeningInfo
            {
                Type = OpeningType.Door,
                WallSegmentIndex = wallSegmentIndex,
                CenterPosition = centerPosition,
                Width = width,
                Height = 7.0,  // Standard door height
                SillHeight = 0
            };
        }

        /// <summary>
        /// Factory method: Creates a window opening.
        /// </summary>
        public static OpeningInfo CreateWindow(
            int wallSegmentIndex,
            (double X, double Y, double Z) centerPosition,
            double width = 4.0,
            double sillHeight = 3.0)
        {
            return new OpeningInfo
            {
                Type = OpeningType.Window,
                WallSegmentIndex = wallSegmentIndex,
                CenterPosition = centerPosition,
                Width = width,
                Height = 4.0,  // Standard window height
                SillHeight = sillHeight
            };
        }
    }
}
