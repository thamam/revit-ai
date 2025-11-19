using System;
using System.Collections.Generic;
using System.Linq;
using RevitAI.Models.Domain;

namespace RevitAI.Services
{
    /// <summary>
    /// Layer 1 service for analyzing room boundaries and extracting wall geometry.
    /// NO Revit API dependencies - uses POCOs only for millisecond testing.
    /// </summary>
    public class RoomBoundaryAnalyzer
    {
        private readonly LoggingService _logger;

        /// <summary>
        /// Constructor with optional logging service (for dependency injection in tests).
        /// </summary>
        public RoomBoundaryAnalyzer(LoggingService logger = null)
        {
            _logger = logger ?? LoggingService.Instance;
        }
        /// <summary>
        /// Analyzes a room's boundary and extracts wall segments with geometry.
        /// </summary>
        /// <param name="roomInfo">Room to analyze</param>
        /// <returns>Complete boundary analysis with wall segments, corners, openings</returns>
        public RoomBoundaryInfo AnalyzeBoundary(RoomInfo roomInfo)
        {
            if (roomInfo == null)
            {
                _logger.Error("AnalyzeBoundary called with null room", "BOUNDARY_ANALYSIS");
                throw new ArgumentNullException(nameof(roomInfo));
            }

            _logger.Info($"Starting boundary analysis for room: {roomInfo.Name} (ID: {roomInfo.ElementId})", "BOUNDARY_ANALYSIS");

            // For Layer 1 testing: Analyze room geometry from bounding box
            // In Layer 2, this will receive actual boundary curves from Revit API
            var boundary = new RoomBoundaryInfo
            {
                Room = roomInfo,
                WallSegments = ExtractWallSegmentsFromBounds(roomInfo),
                Corners = ExtractCornersFromBounds(roomInfo),
                Openings = new List<OpeningInfo>(),  // Will be populated by Layer 2
                FilteredSeparatorCount = 0
            };

            // Calculate perimeter
            boundary.Perimeter = boundary.WallSegments.Sum(w => w.Length);

            _logger.Info($"Boundary analysis complete: {boundary.WallSegments.Count} walls, {boundary.Corners.Count} corners, perimeter={boundary.Perimeter:F2}ft", "BOUNDARY_ANALYSIS");

            return boundary;
        }

        /// <summary>
        /// Extracts wall segments from room bounding box (simple rectangular approximation).
        /// Layer 2 will use actual Room.GetBoundarySegments() for real geometry.
        /// </summary>
        private List<WallSegmentInfo> ExtractWallSegmentsFromBounds(RoomInfo room)
        {
            var (minX, minY, minZ) = room.BoundingMin;
            var (maxX, maxY, maxZ) = room.BoundingMax;

            var segments = new List<WallSegmentInfo>
            {
                // Bottom wall (South) - min Y
                WallSegmentInfo.CreateHorizontal(
                    startPoint: (minX, minY, minZ),
                    endPoint: (maxX, minY, minZ),
                    normal: (0, -1, 0)
                ),
                // Right wall (East) - max X
                WallSegmentInfo.CreateVertical(
                    startPoint: (maxX, minY, minZ),
                    endPoint: (maxX, maxY, minZ),
                    normal: (1, 0, 0)
                ),
                // Top wall (North) - max Y
                WallSegmentInfo.CreateHorizontal(
                    startPoint: (maxX, maxY, minZ),
                    endPoint: (minX, maxY, minZ),
                    normal: (0, 1, 0)
                ),
                // Left wall (West) - min X
                WallSegmentInfo.CreateVertical(
                    startPoint: (minX, maxY, minZ),
                    endPoint: (minX, minY, minZ),
                    normal: (-1, 0, 0)
                )
            };

            return segments;
        }

        /// <summary>
        /// Extracts corner points from room bounding box.
        /// </summary>
        private List<(double X, double Y, double Z)> ExtractCornersFromBounds(RoomInfo room)
        {
            var (minX, minY, minZ) = room.BoundingMin;
            var (maxX, maxY, _) = room.BoundingMax;

            return new List<(double, double, double)>
            {
                (minX, minY, minZ),  // Bottom-left
                (maxX, minY, minZ),  // Bottom-right
                (maxX, maxY, minZ),  // Top-right
                (minX, maxY, minZ)   // Top-left
            };
        }

        /// <summary>
        /// Calculates wall normal vector perpendicular to wall direction.
        /// </summary>
        /// <param name="start">Wall start point</param>
        /// <param name="end">Wall end point</param>
        /// <returns>Normalized normal vector pointing outward from room</returns>
        public (double X, double Y, double Z) CalculateWallNormal(
            (double X, double Y, double Z) start,
            (double X, double Y, double Z) end)
        {
            // Wall direction vector
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            // Perpendicular vector (rotate 90° counterclockwise)
            double normalX = -dy;
            double normalY = dx;

            // Normalize
            double length = Math.Sqrt(normalX * normalX + normalY * normalY);
            if (length > 0.0001)
            {
                normalX /= length;
                normalY /= length;
            }

            return (normalX, normalY, 0);
        }

        /// <summary>
        /// Identifies corner/junction points where walls meet.
        /// </summary>
        /// <param name="wallSegments">List of wall segments</param>
        /// <returns>List of corner points</returns>
        public List<(double X, double Y, double Z)> IdentifyCorners(List<WallSegmentInfo> wallSegments)
        {
            var corners = new List<(double, double, double)>();

            foreach (var segment in wallSegments)
            {
                // Start point is a corner where previous wall ends
                if (!corners.Contains(segment.StartPoint))
                {
                    corners.Add(segment.StartPoint);
                }

                // End point is a corner where next wall begins
                if (!corners.Contains(segment.EndPoint))
                {
                    corners.Add(segment.EndPoint);
                }
            }

            return corners;
        }

        /// <summary>
        /// Filters out room separators from wall segments list.
        /// </summary>
        /// <param name="segments">All boundary segments including separators</param>
        /// <returns>Only physical walls (separators removed)</returns>
        public (List<WallSegmentInfo> PhysicalWalls, int FilteredCount) FilterRoomSeparators(
            List<WallSegmentInfo> segments)
        {
            var physicalWalls = segments.Where(s => !s.IsRoomSeparator).ToList();
            int filteredCount = segments.Count - physicalWalls.Count;

            if (filteredCount > 0)
            {
                _logger.Info($"Filtered {filteredCount} room separator(s) from {segments.Count} total segments", "BOUNDARY_ANALYSIS");
            }

            return (physicalWalls, filteredCount);
        }

        /// <summary>
        /// Detects curved wall segments.
        /// </summary>
        /// <param name="segment">Wall segment to check</param>
        /// <returns>True if wall is curved (arc geometry)</returns>
        public bool IsCurvedWall(WallSegmentInfo segment)
        {
            bool isCurved = segment.IsCurved && segment.CurveRadius.HasValue;

            if (isCurved)
            {
                _logger.Warning($"Curved wall detected: radius={segment.CurveRadius:F2}ft, length={segment.Length:F2}ft", "BOUNDARY_ANALYSIS");
            }

            return isCurved;
        }

        /// <summary>
        /// Calculates wall angle relative to project coordinates.
        /// </summary>
        /// <param name="start">Wall start point</param>
        /// <param name="end">Wall end point</param>
        /// <returns>Angle in degrees (0 = East, 90 = North, 180 = West, 270 = South)</returns>
        public double CalculateWallAngle(
            (double X, double Y, double Z) start,
            (double X, double Y, double Z) end)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;

            // Calculate angle using atan2 (returns radians, then convert to degrees)
            double angleRad = Math.Atan2(dy, dx);
            double angleDeg = angleRad * 180.0 / Math.PI;

            // Normalize to 0-360 range
            if (angleDeg < 0)
            {
                angleDeg += 360;
            }

            // Warn if wall is not orthogonal (not at 0, 90, 180, or 270 degrees)
            double[] orthogonalAngles = { 0, 90, 180, 270, 360 };
            bool isOrthogonal = orthogonalAngles.Any(oa => Math.Abs(angleDeg - oa) < 0.1);

            if (!isOrthogonal)
            {
                _logger.Warning($"Angled (non-orthogonal) wall detected: angle={angleDeg:F2}°", "BOUNDARY_ANALYSIS");
            }

            return angleDeg;
        }
    }
}
