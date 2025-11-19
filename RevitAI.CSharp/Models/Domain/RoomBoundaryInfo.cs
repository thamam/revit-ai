using System.Collections.Generic;

namespace RevitAI.Models.Domain
{
    /// <summary>
    /// POCO representing analyzed room boundary geometry.
    /// Layer 1 SIL pattern - NO Revit API dependencies, enabling millisecond unit tests.
    /// </summary>
    public class RoomBoundaryInfo
    {
        /// <summary>
        /// Reference to the source room.
        /// </summary>
        public RoomInfo Room { get; set; } = new();

        /// <summary>
        /// Wall segments forming the room boundary.
        /// Multiple segments for complex shapes (L-shaped, U-shaped rooms).
        /// </summary>
        public List<WallSegmentInfo> WallSegments { get; set; } = new();

        /// <summary>
        /// Corner/junction points where walls meet.
        /// </summary>
        public List<(double X, double Y, double Z)> Corners { get; set; } = new();

        /// <summary>
        /// Openings (doors/windows) detected in walls.
        /// </summary>
        public List<OpeningInfo> Openings { get; set; } = new();

        /// <summary>
        /// Total perimeter length in feet.
        /// </summary>
        public double Perimeter { get; set; }

        /// <summary>
        /// Number of room separators that were filtered out.
        /// </summary>
        public int FilteredSeparatorCount { get; set; }

        /// <summary>
        /// Factory method: Creates a simple rectangular room boundary for testing.
        /// </summary>
        /// <param name="width">Room width in feet</param>
        /// <param name="height">Room height (depth) in feet</param>
        /// <param name="roomName">Room name</param>
        /// <returns>Fully populated RoomBoundaryInfo with 4 walls</returns>
        public static RoomBoundaryInfo CreateRectangular(double width = 20.0, double height = 15.0, string roomName = "Test Room")
        {
            var room = new RoomInfo
            {
                ElementId = 1001,
                Name = roomName,
                Number = "101",
                Area = width * height,
                LevelName = "Level 1",
                BoundingMin = (0, 0, 0),
                BoundingMax = (width, height, 10)
            };

            var boundary = new RoomBoundaryInfo
            {
                Room = room,
                WallSegments = new List<WallSegmentInfo>
                {
                    // Bottom wall (South) - horizontal
                    WallSegmentInfo.CreateHorizontal(
                        startPoint: (0, 0, 0),
                        endPoint: (width, 0, 0),
                        normal: (0, -1, 0)  // Points south (outward)
                    ),
                    // Right wall (East) - vertical
                    WallSegmentInfo.CreateVertical(
                        startPoint: (width, 0, 0),
                        endPoint: (width, height, 0),
                        normal: (1, 0, 0)  // Points east (outward)
                    ),
                    // Top wall (North) - horizontal
                    WallSegmentInfo.CreateHorizontal(
                        startPoint: (width, height, 0),
                        endPoint: (0, height, 0),
                        normal: (0, 1, 0)  // Points north (outward)
                    ),
                    // Left wall (West) - vertical
                    WallSegmentInfo.CreateVertical(
                        startPoint: (0, height, 0),
                        endPoint: (0, 0, 0),
                        normal: (-1, 0, 0)  // Points west (outward)
                    )
                },
                Corners = new List<(double, double, double)>
                {
                    (0, 0, 0),          // Bottom-left
                    (width, 0, 0),      // Bottom-right
                    (width, height, 0), // Top-right
                    (0, height, 0)      // Top-left
                },
                Perimeter = 2 * (width + height),
                FilteredSeparatorCount = 0
            };

            return boundary;
        }

        /// <summary>
        /// Factory method: Creates an L-shaped room boundary for testing.
        /// </summary>
        /// <returns>RoomBoundaryInfo with 6 walls forming an L-shape</returns>
        public static RoomBoundaryInfo CreateLShaped()
        {
            var room = new RoomInfo
            {
                ElementId = 1002,
                Name = "L-Shaped Room",
                Number = "102",
                Area = 300,
                LevelName = "Level 1",
                BoundingMin = (0, 0, 0),
                BoundingMax = (20, 20, 10)
            };

            var boundary = new RoomBoundaryInfo
            {
                Room = room,
                WallSegments = new List<WallSegmentInfo>
                {
                    // Bottom horizontal segment
                    WallSegmentInfo.CreateHorizontal((0, 0, 0), (20, 0, 0), (0, -1, 0)),
                    // Right vertical segment (short)
                    WallSegmentInfo.CreateVertical((20, 0, 0), (20, 10, 0), (1, 0, 0)),
                    // Inner horizontal segment
                    WallSegmentInfo.CreateHorizontal((20, 10, 0), (10, 10, 0), (0, 1, 0)),
                    // Inner vertical segment
                    WallSegmentInfo.CreateVertical((10, 10, 0), (10, 20, 0), (1, 0, 0)),
                    // Top horizontal segment
                    WallSegmentInfo.CreateHorizontal((10, 20, 0), (0, 20, 0), (0, 1, 0)),
                    // Left vertical segment
                    WallSegmentInfo.CreateVertical((0, 20, 0), (0, 0, 0), (-1, 0, 0))
                },
                Corners = new List<(double, double, double)>
                {
                    (0, 0, 0), (20, 0, 0), (20, 10, 0), (10, 10, 0), (10, 20, 0), (0, 20, 0)
                },
                Perimeter = 80,
                FilteredSeparatorCount = 0
            };

            return boundary;
        }

        /// <summary>
        /// Factory method: Creates a rectangular room with door and window openings.
        /// </summary>
        /// <returns>RoomBoundaryInfo with openings detected</returns>
        public static RoomBoundaryInfo CreateWithOpenings()
        {
            var boundary = CreateRectangular(width: 20, height: 15, roomName: "Room with Openings");

            // Add door opening on bottom wall (South)
            boundary.Openings.Add(new OpeningInfo
            {
                Type = OpeningType.Door,
                WallSegmentIndex = 0,  // Bottom wall
                CenterPosition = (10, 0, 0),
                Width = 3.0,
                Height = 7.0
            });

            // Add window opening on right wall (East)
            boundary.Openings.Add(new OpeningInfo
            {
                Type = OpeningType.Window,
                WallSegmentIndex = 1,  // Right wall
                CenterPosition = (20, 7.5, 4),
                Width = 4.0,
                Height = 3.0
            });

            return boundary;
        }
    }
}
