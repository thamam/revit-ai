using System.Collections.Generic;

namespace RevitAI.Models.Domain
{
    /// <summary>
    /// Plain C# representation of a Revit Room for Layer 1 testing.
    /// This POCO decouples business logic from Revit API, enabling
    /// millisecond unit tests without Revit installed.
    /// </summary>
    public class RoomInfo
    {
        /// <summary>
        /// Unique identifier (maps to Revit Element.Id).
        /// </summary>
        public long ElementId { get; set; }

        /// <summary>
        /// Room name from Revit.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Room number from Revit.
        /// </summary>
        public string Number { get; set; } = string.Empty;

        /// <summary>
        /// Area in square feet.
        /// </summary>
        public double Area { get; set; }

        /// <summary>
        /// Level name where room is located.
        /// </summary>
        public string LevelName { get; set; } = string.Empty;

        /// <summary>
        /// Bounding box minimum point (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) BoundingMin { get; set; }

        /// <summary>
        /// Bounding box maximum point (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) BoundingMax { get; set; }

        /// <summary>
        /// IDs of walls that bound this room.
        /// </summary>
        public List<long> BoundingWallIds { get; set; } = new();

        /// <summary>
        /// Room perimeter in feet.
        /// </summary>
        public double Perimeter { get; set; }

        /// <summary>
        /// Whether room is placed in a valid location.
        /// </summary>
        public bool IsPlaced { get; set; } = true;

        /// <summary>
        /// Creates a rectangular room for testing purposes.
        /// </summary>
        /// <param name="width">Width in feet.</param>
        /// <param name="length">Length in feet.</param>
        /// <param name="name">Room name.</param>
        /// <returns>RoomInfo with calculated area and perimeter.</returns>
        public static RoomInfo CreateRectangular(double width, double length, string name = "Test Room")
        {
            return new RoomInfo
            {
                ElementId = System.DateTime.Now.Ticks,
                Name = name,
                Number = "100",
                Area = width * length,
                Perimeter = 2 * (width + length),
                BoundingMin = (0, 0, 0),
                BoundingMax = (width, length, 10), // 10ft ceiling
                LevelName = "Level 1",
                BoundingWallIds = new List<long> { 1, 2, 3, 4 }, // 4 walls
                IsPlaced = true
            };
        }

        /// <summary>
        /// Creates an L-shaped room for testing purposes.
        /// </summary>
        /// <param name="name">Room name.</param>
        /// <returns>RoomInfo representing an L-shaped room with 6 walls.</returns>
        public static RoomInfo CreateLShaped(string name = "L-Shaped Room")
        {
            // L-shape: 10x20 with 5x10 cut out
            double area = (10 * 20) - (5 * 10); // 150 sq ft
            double perimeter = 10 + 20 + 5 + 10 + 5 + 10; // 60 ft

            return new RoomInfo
            {
                ElementId = System.DateTime.Now.Ticks,
                Name = name,
                Number = "101",
                Area = area,
                Perimeter = perimeter,
                BoundingMin = (0, 0, 0),
                BoundingMax = (10, 20, 10),
                LevelName = "Level 1",
                BoundingWallIds = new List<long> { 1, 2, 3, 4, 5, 6 }, // 6 walls
                IsPlaced = true
            };
        }

        /// <summary>
        /// Creates a room with a door opening.
        /// </summary>
        /// <param name="name">Room name.</param>
        /// <returns>RoomInfo with door opening metadata.</returns>
        public static RoomInfo CreateWithDoorOpening(string name = "Room with Door")
        {
            var room = CreateRectangular(12, 15, name);
            room.Number = "102";
            // Door doesn't change wall count, but affects dimensioning logic
            return room;
        }
    }
}
