namespace RevitAI.Models.Domain
{
    /// <summary>
    /// POCO representing a wall segment in a room boundary.
    /// Layer 1 SIL pattern - NO Revit API dependencies.
    /// </summary>
    public class WallSegmentInfo
    {
        /// <summary>
        /// Segment start point (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) StartPoint { get; set; }

        /// <summary>
        /// Segment end point (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) EndPoint { get; set; }

        /// <summary>
        /// Wall normal vector (perpendicular to wall, points outward from room).
        /// Used for dimension offset direction.
        /// </summary>
        public (double X, double Y, double Z) Normal { get; set; }

        /// <summary>
        /// Wall orientation in degrees (0 = East, 90 = North, 180 = West, 270 = South).
        /// </summary>
        public double OrientationDegrees { get; set; }

        /// <summary>
        /// Length of wall segment in feet.
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// True if this wall segment is curved (arc geometry).
        /// </summary>
        public bool IsCurved { get; set; }

        /// <summary>
        /// True if this is a room separator (non-physical boundary).
        /// </summary>
        public bool IsRoomSeparator { get; set; }

        /// <summary>
        /// Curve radius in feet (only for curved walls).
        /// </summary>
        public double? CurveRadius { get; set; }

        /// <summary>
        /// Wall thickness in feet.
        /// </summary>
        public double Thickness { get; set; }

        /// <summary>
        /// Factory method: Creates a horizontal wall segment.
        /// </summary>
        public static WallSegmentInfo CreateHorizontal(
            (double X, double Y, double Z) startPoint,
            (double X, double Y, double Z) endPoint,
            (double X, double Y, double Z) normal)
        {
            double dx = endPoint.X - startPoint.X;
            double length = System.Math.Abs(dx);

            // Determine orientation: 0° if going east, 180° if going west
            double orientation = dx > 0 ? 0 : 180;

            return new WallSegmentInfo
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Normal = normal,
                OrientationDegrees = orientation,
                Length = length,
                IsCurved = false,
                IsRoomSeparator = false,
                Thickness = 0.5  // Default 6 inches
            };
        }

        /// <summary>
        /// Factory method: Creates a vertical wall segment.
        /// </summary>
        public static WallSegmentInfo CreateVertical(
            (double X, double Y, double Z) startPoint,
            (double X, double Y, double Z) endPoint,
            (double X, double Y, double Z) normal)
        {
            double dy = endPoint.Y - startPoint.Y;
            double length = System.Math.Abs(dy);

            // Determine orientation: 90° if going north, 270° if going south
            double orientation = dy > 0 ? 90 : 270;

            return new WallSegmentInfo
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Normal = normal,
                OrientationDegrees = orientation,
                Length = length,
                IsCurved = false,
                IsRoomSeparator = false,
                Thickness = 0.5  // Default 6 inches
            };
        }

        /// <summary>
        /// Factory method: Creates a curved wall segment (for testing).
        /// </summary>
        public static WallSegmentInfo CreateCurved(
            (double X, double Y, double Z) startPoint,
            (double X, double Y, double Z) endPoint,
            double radius)
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            return new WallSegmentInfo
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Normal = (0, 0, 0),  // Normal varies along curve
                OrientationDegrees = 0,  // Not applicable for curves
                Length = length,
                IsCurved = true,
                IsRoomSeparator = false,
                CurveRadius = radius,
                Thickness = 0.5
            };
        }

        /// <summary>
        /// Factory method: Creates an angled wall segment (non-orthogonal).
        /// </summary>
        public static WallSegmentInfo CreateAngled(
            (double X, double Y, double Z) startPoint,
            (double X, double Y, double Z) endPoint,
            double angleInDegrees)
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            // Calculate normal perpendicular to wall direction
            double angleRad = angleInDegrees * System.Math.PI / 180.0;
            double normalX = -System.Math.Sin(angleRad);
            double normalY = System.Math.Cos(angleRad);

            return new WallSegmentInfo
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Normal = (normalX, normalY, 0),
                OrientationDegrees = angleInDegrees,
                Length = length,
                IsCurved = false,
                IsRoomSeparator = false,
                Thickness = 0.5
            };
        }

        /// <summary>
        /// Factory method: Creates a room separator (non-physical boundary).
        /// </summary>
        public static WallSegmentInfo CreateRoomSeparator(
            (double X, double Y, double Z) startPoint,
            (double X, double Y, double Z) endPoint)
        {
            double dx = endPoint.X - startPoint.X;
            double dy = endPoint.Y - startPoint.Y;
            double length = System.Math.Sqrt(dx * dx + dy * dy);

            return new WallSegmentInfo
            {
                StartPoint = startPoint,
                EndPoint = endPoint,
                Normal = (0, 0, 0),
                OrientationDegrees = 0,
                Length = length,
                IsCurved = false,
                IsRoomSeparator = true,  // Flagged as separator
                Thickness = 0
            };
        }
    }
}
