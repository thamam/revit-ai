using System.Collections.Generic;
using System.Linq;
using RevitAI.Models.Domain;

namespace RevitAI.Services
{
    /// <summary>
    /// Pure business logic service for planning dimension placements.
    ///
    /// LAYER 1 (SIL Architecture):
    /// This service operates on POCOs only - NO Revit API dependencies.
    /// Can be unit tested in milliseconds without Revit installed.
    ///
    /// Responsibilities:
    /// - Determine which walls need dimensions
    /// - Calculate dimension line offsets
    /// - Plan dimension chains
    /// - Validate dimension feasibility
    /// </summary>
    public class DimensionPlanningService
    {
        /// <summary>
        /// Default offset for dimension lines from walls (in feet).
        /// </summary>
        public double DefaultOffsetFeet { get; set; } = 2.0;

        /// <summary>
        /// Maximum number of dimensions allowed per operation.
        /// </summary>
        public int MaxDimensionsPerOperation { get; set; } = 1000;

        /// <summary>
        /// Plans dimensions for room boundaries.
        /// </summary>
        /// <param name="rooms">Rooms to dimension.</param>
        /// <param name="walls">Available walls in the model.</param>
        /// <returns>Planned dimensions with feasibility status.</returns>
        public DimensionPlanResult PlanRoomDimensions(
            IEnumerable<RoomInfo> rooms,
            IEnumerable<WallInfo> walls)
        {
            var result = new DimensionPlanResult();
            var wallDict = walls.ToDictionary(w => w.ElementId);

            foreach (var room in rooms)
            {
                if (!room.IsPlaced)
                {
                    result.Warnings.Add($"Room '{room.Name}' is not placed - skipping");
                    continue;
                }

                foreach (var wallId in room.BoundingWallIds)
                {
                    if (!wallDict.TryGetValue(wallId, out var wall))
                    {
                        result.Warnings.Add($"Wall {wallId} not found for room '{room.Name}'");
                        continue;
                    }

                    if (wall.IsCurved)
                    {
                        result.Warnings.Add($"Wall {wallId} is curved - cannot dimension with standard tools");
                        continue;
                    }

                    // Plan dimensions for each dimensionable segment
                    foreach (var segment in wall.DimensionableSegments)
                    {
                        var dimInfo = new DimensionInfo
                        {
                            Value = segment.Length,
                            DimensionType = "Linear",
                            Direction = GetSegmentDirection(segment),
                            Offset = DefaultOffsetFeet,
                            ReferencedElementIds = new[] { wallId },
                            DimensionLineLocation = CalculateOffsetLocation(segment, DefaultOffsetFeet),
                            IsValid = true
                        };

                        result.PlannedDimensions.Add(dimInfo);
                    }
                }
            }

            // Validate total count
            if (result.PlannedDimensions.Count > MaxDimensionsPerOperation)
            {
                result.IsFeasible = false;
                result.Errors.Add(
                    $"Operation would create {result.PlannedDimensions.Count} dimensions, " +
                    $"exceeding limit of {MaxDimensionsPerOperation}");
            }
            else
            {
                result.IsFeasible = true;
            }

            result.TotalDimensionCount = result.PlannedDimensions.Count;
            return result;
        }

        /// <summary>
        /// Plans a continuous dimension chain across multiple walls.
        /// </summary>
        /// <param name="walls">Ordered list of walls to chain together.</param>
        /// <returns>Single dimension info representing the chain.</returns>
        public DimensionInfo PlanDimensionChain(IEnumerable<WallInfo> walls)
        {
            var wallList = walls.ToList();

            if (wallList.Count < 2)
            {
                return new DimensionInfo
                {
                    IsValid = false,
                    ErrorMessage = "Dimension chain requires at least 2 walls"
                };
            }

            // Check all walls are linear (not curved)
            var curvedWalls = wallList.Where(w => w.IsCurved).ToList();
            if (curvedWalls.Any())
            {
                return new DimensionInfo
                {
                    IsValid = false,
                    ErrorMessage = $"Cannot create chain with curved walls: {string.Join(", ", curvedWalls.Select(w => w.ElementId))}"
                };
            }

            double totalLength = wallList.Sum(w => w.Length);
            var wallIds = wallList.Select(w => w.ElementId).ToArray();

            return DimensionInfo.CreateChain(wallIds, totalLength);
        }

        /// <summary>
        /// Validates that rooms can be dimensioned.
        /// </summary>
        /// <param name="rooms">Rooms to check.</param>
        /// <returns>Validation result with any issues.</returns>
        public DimensionPlanResult ValidateRoomsForDimensioning(IEnumerable<RoomInfo> rooms)
        {
            var result = new DimensionPlanResult { IsFeasible = true };
            var roomList = rooms.ToList();

            if (!roomList.Any())
            {
                result.IsFeasible = false;
                result.Errors.Add("No rooms provided for dimensioning");
                return result;
            }

            foreach (var room in roomList)
            {
                if (!room.IsPlaced)
                {
                    result.Warnings.Add($"Room '{room.Name}' is not placed");
                }

                if (room.Area <= 0)
                {
                    result.Warnings.Add($"Room '{room.Name}' has zero or negative area");
                }

                if (!room.BoundingWallIds.Any())
                {
                    result.Errors.Add($"Room '{room.Name}' has no bounding walls");
                    result.IsFeasible = false;
                }
            }

            return result;
        }

        /// <summary>
        /// Determines if a line segment is horizontal or vertical.
        /// </summary>
        private string GetSegmentDirection(LineSegment segment)
        {
            double dx = System.Math.Abs(segment.EndPoint.X - segment.StartPoint.X);
            double dy = System.Math.Abs(segment.EndPoint.Y - segment.StartPoint.Y);

            return dx > dy ? "Horizontal" : "Vertical";
        }

        /// <summary>
        /// Calculates the dimension line location offset from a wall segment.
        /// </summary>
        private (double X, double Y, double Z) CalculateOffsetLocation(LineSegment segment, double offset)
        {
            var mid = segment.Midpoint;
            string direction = GetSegmentDirection(segment);

            // Offset perpendicular to wall direction
            if (direction == "Horizontal")
            {
                return (mid.X, mid.Y + offset, mid.Z);
            }
            else
            {
                return (mid.X + offset, mid.Y, mid.Z);
            }
        }
    }

    /// <summary>
    /// Result of dimension planning operation.
    /// </summary>
    public class DimensionPlanResult
    {
        /// <summary>
        /// Whether the operation is feasible (within limits, no blocking errors).
        /// </summary>
        public bool IsFeasible { get; set; }

        /// <summary>
        /// Total number of dimensions that would be created.
        /// </summary>
        public int TotalDimensionCount { get; set; }

        /// <summary>
        /// Planned dimensions to create.
        /// </summary>
        public List<DimensionInfo> PlannedDimensions { get; set; } = new();

        /// <summary>
        /// Non-blocking warnings (e.g., skipped rooms).
        /// </summary>
        public List<string> Warnings { get; set; } = new();

        /// <summary>
        /// Blocking errors that prevent operation.
        /// </summary>
        public List<string> Errors { get; set; } = new();
    }
}
