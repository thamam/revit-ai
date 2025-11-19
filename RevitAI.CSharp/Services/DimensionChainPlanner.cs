using System;
using System.Collections.Generic;
using System.Linq;
using RevitAI.Models.Domain;
using RevitAI.Models.Commands;

namespace RevitAI.Services
{
    /// <summary>
    /// Layer 1 service for planning dimension chains from room boundaries.
    /// Pure business logic - NO Revit API dependencies.
    /// Consumes RoomBoundaryInfo from RoomBoundaryAnalyzer.
    /// Produces DimensionChainInfo for RevitDimensionCreator (Layer 2).
    /// </summary>
    public class DimensionChainPlanner
    {
        private readonly LoggingService? _logger;

        /// <summary>
        /// Constructor with optional logging service injection.
        /// </summary>
        /// <param name="logger">Optional logging service for operation tracking</param>
        public DimensionChainPlanner(LoggingService? logger = null)
        {
            _logger = logger;
        }

        /// <summary>
        /// Plans dimension chains for all walls in a room boundary.
        /// </summary>
        /// <param name="boundary">Analyzed room boundary from RoomBoundaryAnalyzer</param>
        /// <param name="parameters">Dimension parameters (offset, style)</param>
        /// <returns>List of dimension chains, one per wall segment</returns>
        /// <exception cref="ArgumentNullException">Thrown if boundary or parameters is null</exception>
        public List<DimensionChainInfo> PlanDimensions(RoomBoundaryInfo boundary, DimensionParameters parameters)
        {
            if (boundary == null)
                throw new ArgumentNullException(nameof(boundary));
            if (parameters == null)
                throw new ArgumentNullException(nameof(parameters));

            _logger?.LogOperation("dimension_planning", "STARTED",
                $"Planning dimensions for room '{boundary.Room.Name}' with {boundary.WallSegments.Count} walls");

            var dimensionChains = new List<DimensionChainInfo>();

            // Handle empty wall segments
            if (boundary.WallSegments.Count == 0)
            {
                _logger?.Warning("No wall segments to dimension", "DIMENSION_PLANNING");
                return dimensionChains;
            }

            // Create one dimension chain per wall segment
            for (int i = 0; i < boundary.WallSegments.Count; i++)
            {
                var wall = boundary.WallSegments[i];

                // Skip curved walls (deferred to Story 2.5: Edge Case Handling)
                if (wall.IsCurved)
                {
                    _logger?.Warning($"Skipping curved wall segment {i} (not yet supported)", "DIMENSION_PLANNING");
                    continue;
                }

                // Skip room separators (non-physical boundaries)
                if (wall.IsRoomSeparator)
                {
                    _logger?.Debug($"Skipping room separator segment {i}", "DIMENSION_PLANNING");
                    continue;
                }

                // Calculate offset vector from wall normal
                double offsetDistance = ConvertMillimetersToFeet(parameters.OffsetMm);
                var offsetVector = CalculateOffsetVector(wall.Normal, offsetDistance);

                // Generate reference points for this wall
                var referencePoints = GenerateReferencePoints(wall, boundary.Openings, i, boundary.Corners);

                // Calculate dimension line endpoints
                var dimLineStart = (
                    X: wall.StartPoint.X + offsetVector.X,
                    Y: wall.StartPoint.Y + offsetVector.Y,
                    Z: wall.StartPoint.Z
                );

                var dimLineEnd = (
                    X: wall.EndPoint.X + offsetVector.X,
                    Y: wall.EndPoint.Y + offsetVector.Y,
                    Z: wall.EndPoint.Z
                );

                // Find opening indices for this wall segment
                var openingIndices = boundary.Openings
                    .Select((opening, index) => new { opening, index })
                    .Where(x => x.opening.WallSegmentIndex == i)
                    .Select(x => x.index)
                    .ToList();

                var chain = new DimensionChainInfo
                {
                    WallSegment = wall,
                    ReferencePoints = referencePoints,
                    OffsetVector = offsetVector,
                    DimensionStyle = parameters.DimensionStyle ?? "Linear - 3/32\" Arial",
                    DimensionLineStart = dimLineStart,
                    DimensionLineEnd = dimLineEnd,
                    OpeningIndices = openingIndices
                };

                dimensionChains.Add(chain);

                _logger?.Debug($"Planned dimension chain for wall {i}: {referencePoints.Count} reference points, {openingIndices.Count} openings",
                    "DIMENSION_PLANNING");
            }

            _logger?.LogOperation("dimension_planning", "SUCCESS",
                $"Planned {dimensionChains.Count} dimension chains for room '{boundary.Room.Name}'");

            return dimensionChains;
        }

        /// <summary>
        /// Calculates offset vector from wall normal and offset distance.
        /// Offset vector = normal unit vector * offset distance.
        /// </summary>
        /// <param name="normal">Wall normal vector (perpendicular to wall)</param>
        /// <param name="offsetDistance">Offset distance in feet</param>
        /// <returns>Offset vector pointing perpendicular to wall</returns>
        private (double X, double Y, double Z) CalculateOffsetVector(
            (double X, double Y, double Z) normal,
            double offsetDistance)
        {
            // Normalize the normal vector (in case it's not unit length)
            double magnitude = Math.Sqrt(normal.X * normal.X + normal.Y * normal.Y + normal.Z * normal.Z);

            if (magnitude < 0.0001)  // Avoid division by zero
            {
                _logger?.Warning("Normal vector has near-zero magnitude, using default offset", "DIMENSION_PLANNING");
                return (0, offsetDistance, 0);  // Default: offset in Y direction
            }

            double unitX = normal.X / magnitude;
            double unitY = normal.Y / magnitude;
            double unitZ = normal.Z / magnitude;

            return (
                X: unitX * offsetDistance,
                Y: unitY * offsetDistance,
                Z: unitZ * offsetDistance
            );
        }

        /// <summary>
        /// Generates reference points for a wall segment.
        /// Includes: wall start/end, opening edges, corner references.
        /// </summary>
        /// <param name="wall">Wall segment to generate references for</param>
        /// <param name="allOpenings">All openings in the room boundary</param>
        /// <param name="wallIndex">Index of this wall segment</param>
        /// <param name="corners">Corner points from room boundary</param>
        /// <returns>Ordered list of reference points (left-to-right or bottom-to-top)</returns>
        private List<(double X, double Y, double Z)> GenerateReferencePoints(
            WallSegmentInfo wall,
            List<OpeningInfo> allOpenings,
            int wallIndex,
            List<(double X, double Y, double Z)> corners)
        {
            var referencePoints = new List<(double X, double Y, double Z)>();

            // Start with wall start point
            referencePoints.Add(wall.StartPoint);

            // Add opening edges if this wall has openings
            var wallOpenings = allOpenings
                .Where(o => o.WallSegmentIndex == wallIndex)
                .OrderBy(o => GetDistanceAlongWall(wall, o.CenterPosition))
                .ToList();

            foreach (var opening in wallOpenings)
            {
                // Calculate opening edges based on wall orientation
                var (leftEdge, rightEdge) = CalculateOpeningEdges(wall, opening);

                // Add opening edges as separate reference points
                referencePoints.Add(leftEdge);
                referencePoints.Add(rightEdge);
            }

            // Add corner references if wall endpoint is a corner
            // (Corner references help ensure dimension chains connect properly at junctions)
            if (IsCornerPoint(wall.EndPoint, corners))
            {
                referencePoints.Add(wall.EndPoint);
            }
            else
            {
                // If not a corner, still add wall endpoint
                referencePoints.Add(wall.EndPoint);
            }

            // Sort reference points in correct order (left-to-right or bottom-to-top)
            referencePoints = SortReferencePoints(referencePoints, wall.OrientationDegrees);

            return referencePoints;
        }

        /// <summary>
        /// Calculates opening edge points along a wall.
        /// </summary>
        /// <param name="wall">Wall segment containing the opening</param>
        /// <param name="opening">Opening (door/window) information</param>
        /// <returns>Tuple of (left edge point, right edge point)</returns>
        private ((double X, double Y, double Z) LeftEdge, (double X, double Y, double Z) RightEdge) CalculateOpeningEdges(
            WallSegmentInfo wall,
            OpeningInfo opening)
        {
            double halfWidth = opening.Width / 2.0;

            // Determine wall direction vector
            double dx = wall.EndPoint.X - wall.StartPoint.X;
            double dy = wall.EndPoint.Y - wall.StartPoint.Y;
            double wallLength = Math.Sqrt(dx * dx + dy * dy);

            if (wallLength < 0.0001)  // Degenerate wall
            {
                return (opening.CenterPosition, opening.CenterPosition);
            }

            // Unit direction vector along wall
            double dirX = dx / wallLength;
            double dirY = dy / wallLength;

            // Calculate left and right edge points
            var leftEdge = (
                X: opening.CenterPosition.X - dirX * halfWidth,
                Y: opening.CenterPosition.Y - dirY * halfWidth,
                Z: opening.CenterPosition.Z
            );

            var rightEdge = (
                X: opening.CenterPosition.X + dirX * halfWidth,
                Y: opening.CenterPosition.Y + dirY * halfWidth,
                Z: opening.CenterPosition.Z
            );

            return (leftEdge, rightEdge);
        }

        /// <summary>
        /// Calculates distance along wall from start point.
        /// </summary>
        /// <param name="wall">Wall segment</param>
        /// <param name="point">Point to measure distance to</param>
        /// <returns>Distance along wall in feet</returns>
        private double GetDistanceAlongWall(WallSegmentInfo wall, (double X, double Y, double Z) point)
        {
            double dx = point.X - wall.StartPoint.X;
            double dy = point.Y - wall.StartPoint.Y;
            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        /// Checks if a point is a corner point.
        /// </summary>
        /// <param name="point">Point to check</param>
        /// <param name="corners">List of corner points</param>
        /// <returns>True if point matches a corner (within tolerance)</returns>
        private bool IsCornerPoint((double X, double Y, double Z) point, List<(double X, double Y, double Z)> corners)
        {
            const double tolerance = 0.01;  // 1/100 ft tolerance

            return corners.Any(corner =>
                Math.Abs(corner.X - point.X) < tolerance &&
                Math.Abs(corner.Y - point.Y) < tolerance &&
                Math.Abs(corner.Z - point.Z) < tolerance
            );
        }

        /// <summary>
        /// Sorts reference points in correct order based on wall orientation.
        /// Horizontal walls: left-to-right (increasing X).
        /// Vertical walls: bottom-to-top (increasing Y).
        /// </summary>
        /// <param name="points">Unsorted reference points</param>
        /// <param name="orientationDegrees">Wall orientation (0=E, 90=N, 180=W, 270=S)</param>
        /// <returns>Sorted reference points</returns>
        private List<(double X, double Y, double Z)> SortReferencePoints(
            List<(double X, double Y, double Z)> points,
            double orientationDegrees)
        {
            // Horizontal walls (0° or 180°): sort by X coordinate
            if (Math.Abs(orientationDegrees % 180) < 10)  // Tolerance for near-horizontal
            {
                return points.OrderBy(p => p.X).ToList();
            }

            // Vertical walls (90° or 270°): sort by Y coordinate
            if (Math.Abs((orientationDegrees - 90) % 180) < 10)  // Tolerance for near-vertical
            {
                return points.OrderBy(p => p.Y).ToList();
            }

            // Angled walls: sort by distance from origin
            _logger?.Warning($"Angled wall ({orientationDegrees}°) - sorting by distance", "DIMENSION_PLANNING");
            return points.OrderBy(p => Math.Sqrt(p.X * p.X + p.Y * p.Y)).ToList();
        }

        /// <summary>
        /// Converts millimeters to feet (1mm = 0.00328084 ft).
        /// </summary>
        /// <param name="millimeters">Distance in millimeters</param>
        /// <returns>Distance in feet</returns>
        private double ConvertMillimetersToFeet(double millimeters)
        {
            return millimeters * 0.00328084;
        }
    }
}
