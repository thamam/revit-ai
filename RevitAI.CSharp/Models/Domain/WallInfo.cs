using System.Collections.Generic;

namespace RevitAI.Models.Domain
{
    /// <summary>
    /// Plain C# representation of a Revit Wall for Layer 1 testing.
    /// Contains the geometric data needed for dimension placement logic.
    /// </summary>
    public class WallInfo
    {
        /// <summary>
        /// Unique identifier (maps to Revit Element.Id).
        /// </summary>
        public long ElementId { get; set; }

        /// <summary>
        /// Wall type name (e.g., "Basic Wall - Interior").
        /// </summary>
        public string TypeName { get; set; } = string.Empty;

        /// <summary>
        /// Length of wall in feet.
        /// </summary>
        public double Length { get; set; }

        /// <summary>
        /// Height of wall in feet.
        /// </summary>
        public double Height { get; set; }

        /// <summary>
        /// Width/thickness of wall in feet.
        /// </summary>
        public double Width { get; set; }

        /// <summary>
        /// Start point of wall centerline (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) StartPoint { get; set; }

        /// <summary>
        /// End point of wall centerline (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) EndPoint { get; set; }

        /// <summary>
        /// Level name where wall is located.
        /// </summary>
        public string LevelName { get; set; } = string.Empty;

        /// <summary>
        /// Whether the wall is curved (not dimensionable with standard dimensions).
        /// </summary>
        public bool IsCurved { get; set; }

        /// <summary>
        /// Whether wall is structural (may affect dimension placement rules).
        /// </summary>
        public bool IsStructural { get; set; }

        /// <summary>
        /// Segments that can be dimensioned (linear portions).
        /// For straight walls: single segment matching wall length.
        /// For walls with openings: multiple segments around openings.
        /// </summary>
        public List<LineSegment> DimensionableSegments { get; set; } = new();

        /// <summary>
        /// Creates a straight wall for testing.
        /// </summary>
        /// <param name="length">Wall length in feet.</param>
        /// <param name="startX">Start X coordinate.</param>
        /// <param name="startY">Start Y coordinate.</param>
        /// <param name="direction">Direction: "horizontal" or "vertical".</param>
        /// <returns>WallInfo for testing.</returns>
        public static WallInfo CreateStraight(double length, double startX, double startY, string direction = "horizontal")
        {
            var endX = direction == "horizontal" ? startX + length : startX;
            var endY = direction == "vertical" ? startY + length : startY;

            var wall = new WallInfo
            {
                ElementId = System.DateTime.Now.Ticks,
                TypeName = "Basic Wall - Interior",
                Length = length,
                Height = 10, // Standard 10ft
                Width = 0.5, // 6" wall
                StartPoint = (startX, startY, 0),
                EndPoint = (endX, endY, 0),
                LevelName = "Level 1",
                IsCurved = false,
                IsStructural = false,
                DimensionableSegments = new List<LineSegment>
                {
                    new LineSegment
                    {
                        StartPoint = (startX, startY, 0),
                        EndPoint = (endX, endY, 0),
                        Length = length
                    }
                }
            };

            return wall;
        }

        /// <summary>
        /// Creates a wall with a door opening (multiple dimensionable segments).
        /// </summary>
        /// <param name="totalLength">Total wall length in feet.</param>
        /// <param name="doorWidth">Door opening width in feet.</param>
        /// <returns>WallInfo with two dimensionable segments (before and after door).</returns>
        public static WallInfo CreateWithDoorOpening(double totalLength, double doorWidth = 3)
        {
            var wall = CreateStraight(totalLength, 0, 0, "horizontal");
            wall.ElementId = System.DateTime.Now.Ticks;

            // Door centered in wall
            double doorStart = (totalLength - doorWidth) / 2;
            double doorEnd = doorStart + doorWidth;

            wall.DimensionableSegments = new List<LineSegment>
            {
                new LineSegment
                {
                    StartPoint = (0, 0, 0),
                    EndPoint = (doorStart, 0, 0),
                    Length = doorStart
                },
                new LineSegment
                {
                    StartPoint = (doorEnd, 0, 0),
                    EndPoint = (totalLength, 0, 0),
                    Length = totalLength - doorEnd
                }
            };

            return wall;
        }

        /// <summary>
        /// Creates a curved wall (not dimensionable with standard tools).
        /// </summary>
        /// <returns>WallInfo representing a curved wall.</returns>
        public static WallInfo CreateCurved()
        {
            return new WallInfo
            {
                ElementId = System.DateTime.Now.Ticks,
                TypeName = "Basic Wall - Curved",
                Length = 15.7, // Arc length
                Height = 10,
                Width = 0.5,
                StartPoint = (0, 0, 0),
                EndPoint = (10, 10, 0),
                LevelName = "Level 1",
                IsCurved = true,
                IsStructural = false,
                DimensionableSegments = new List<LineSegment>() // Empty - can't dimension curved
            };
        }
    }

    /// <summary>
    /// Represents a linear segment that can be dimensioned.
    /// </summary>
    public class LineSegment
    {
        public (double X, double Y, double Z) StartPoint { get; set; }
        public (double X, double Y, double Z) EndPoint { get; set; }
        public double Length { get; set; }

        /// <summary>
        /// Calculates the midpoint of the segment.
        /// </summary>
        public (double X, double Y, double Z) Midpoint =>
            ((StartPoint.X + EndPoint.X) / 2,
             (StartPoint.Y + EndPoint.Y) / 2,
             (StartPoint.Z + EndPoint.Z) / 2);
    }
}
