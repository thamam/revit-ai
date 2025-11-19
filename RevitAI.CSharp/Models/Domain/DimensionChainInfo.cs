using System.Collections.Generic;

namespace RevitAI.Models.Domain
{
    /// <summary>
    /// POCO representing a planned dimension chain for a wall segment.
    /// Layer 1 SIL pattern - NO Revit API dependencies.
    /// Contains all information needed to create a dimension chain in Revit (Layer 2).
    /// </summary>
    public class DimensionChainInfo
    {
        /// <summary>
        /// Wall segment being dimensioned.
        /// </summary>
        public WallSegmentInfo WallSegment { get; set; } = new();

        /// <summary>
        /// Reference points along the wall where dimensions should measure.
        /// Ordered left-to-right (horizontal walls) or bottom-to-top (vertical walls).
        /// Includes start point, end point, and intermediate points (openings, corners).
        /// </summary>
        public List<(double X, double Y, double Z)> ReferencePoints { get; set; } = new();

        /// <summary>
        /// Offset vector indicating where dimension line should be placed.
        /// Perpendicular to wall (uses wall normal), magnitude = offset distance.
        /// </summary>
        public (double X, double Y, double Z) OffsetVector { get; set; }

        /// <summary>
        /// Dimension style/type name to apply (e.g., "Linear - 3/32\" Arial").
        /// Retrieved from DimensionParameters or firm defaults.
        /// </summary>
        public string DimensionStyle { get; set; } = string.Empty;

        /// <summary>
        /// Start point of the dimension line in 3D space.
        /// Calculated as wall start point + offset vector.
        /// </summary>
        public (double X, double Y, double Z) DimensionLineStart { get; set; }

        /// <summary>
        /// End point of the dimension line in 3D space.
        /// Calculated as wall end point + offset vector.
        /// </summary>
        public (double X, double Y, double Z) DimensionLineEnd { get; set; }

        /// <summary>
        /// Indices of openings (doors/windows) that create gaps in this dimension chain.
        /// References OpeningInfo objects in RoomBoundaryInfo.Openings list.
        /// </summary>
        public List<int> OpeningIndices { get; set; } = new();

        /// <summary>
        /// Factory method: Creates a simple dimension chain for a horizontal wall.
        /// </summary>
        /// <param name="wallLength">Wall length in feet</param>
        /// <param name="offsetDistance">Dimension offset from wall in feet (default 200mm = 0.656ft)</param>
        /// <param name="dimensionStyle">Dimension style name</param>
        /// <returns>DimensionChainInfo with 2 reference points (start/end)</returns>
        public static DimensionChainInfo CreateSimple(
            double wallLength = 20.0,
            double offsetDistance = 0.656,  // 200mm in feet
            string dimensionStyle = "Linear - 3/32\" Arial")
        {
            var wallSegment = WallSegmentInfo.CreateHorizontal(
                startPoint: (0, 0, 0),
                endPoint: (wallLength, 0, 0),
                normal: (0, -1, 0)  // Points south
            );

            // Offset vector = normal * offset distance
            var offsetVector = (
                X: wallSegment.Normal.X * offsetDistance,
                Y: wallSegment.Normal.Y * offsetDistance,
                Z: 0.0
            );

            return new DimensionChainInfo
            {
                WallSegment = wallSegment,
                ReferencePoints = new List<(double, double, double)>
                {
                    wallSegment.StartPoint,  // Start reference
                    wallSegment.EndPoint     // End reference
                },
                OffsetVector = offsetVector,
                DimensionStyle = dimensionStyle,
                DimensionLineStart = (
                    X: wallSegment.StartPoint.X + offsetVector.X,
                    Y: wallSegment.StartPoint.Y + offsetVector.Y,
                    Z: wallSegment.StartPoint.Z
                ),
                DimensionLineEnd = (
                    X: wallSegment.EndPoint.X + offsetVector.X,
                    Y: wallSegment.EndPoint.Y + offsetVector.Y,
                    Z: wallSegment.EndPoint.Z
                ),
                OpeningIndices = new List<int>()
            };
        }

        /// <summary>
        /// Factory method: Creates a dimension chain with openings (doors/windows).
        /// Reference points include start, opening edges, and end.
        /// </summary>
        /// <param name="wallLength">Wall length in feet</param>
        /// <param name="doorCenterX">Door center X position in feet</param>
        /// <param name="doorWidth">Door width in feet</param>
        /// <param name="offsetDistance">Dimension offset from wall in feet</param>
        /// <returns>DimensionChainInfo with 4 reference points (start, door edges, end)</returns>
        public static DimensionChainInfo CreateWithOpenings(
            double wallLength = 20.0,
            double doorCenterX = 10.0,
            double doorWidth = 3.0,
            double offsetDistance = 0.656)
        {
            var wallSegment = WallSegmentInfo.CreateHorizontal(
                startPoint: (0, 0, 0),
                endPoint: (wallLength, 0, 0),
                normal: (0, -1, 0)
            );

            var offsetVector = (
                X: wallSegment.Normal.X * offsetDistance,
                Y: wallSegment.Normal.Y * offsetDistance,
                Z: 0.0
            );

            // Calculate door opening edges
            double doorLeftX = doorCenterX - doorWidth / 2.0;
            double doorRightX = doorCenterX + doorWidth / 2.0;

            return new DimensionChainInfo
            {
                WallSegment = wallSegment,
                ReferencePoints = new List<(double, double, double)>
                {
                    (0, 0, 0),                        // Wall start
                    (doorLeftX, 0, 0),                // Door left edge
                    (doorRightX, 0, 0),               // Door right edge
                    (wallLength, 0, 0)                // Wall end
                },
                OffsetVector = offsetVector,
                DimensionStyle = "Linear - 3/32\" Arial",
                DimensionLineStart = (
                    X: 0 + offsetVector.X,
                    Y: 0 + offsetVector.Y,
                    Z: 0
                ),
                DimensionLineEnd = (
                    X: wallLength + offsetVector.X,
                    Y: 0 + offsetVector.Y,
                    Z: 0
                ),
                OpeningIndices = new List<int> { 0 }  // First opening
            };
        }

        /// <summary>
        /// Factory method: Creates dimension chains for an L-shaped room.
        /// Returns 2 dimension chains (one for each wall segment).
        /// </summary>
        /// <param name="horizontalLength">Horizontal wall length in feet</param>
        /// <param name="verticalLength">Vertical wall length in feet</param>
        /// <param name="offsetDistance">Dimension offset from wall in feet</param>
        /// <returns>List of 2 DimensionChainInfo objects</returns>
        public static List<DimensionChainInfo> CreateLShaped(
            double horizontalLength = 20.0,
            double verticalLength = 15.0,
            double offsetDistance = 0.656)
        {
            // Horizontal wall (bottom of L)
            var horizontalWall = WallSegmentInfo.CreateHorizontal(
                startPoint: (0, 0, 0),
                endPoint: (horizontalLength, 0, 0),
                normal: (0, -1, 0)
            );

            var horizontalOffset = (
                X: horizontalWall.Normal.X * offsetDistance,
                Y: horizontalWall.Normal.Y * offsetDistance,
                Z: 0.0
            );

            var horizontalChain = new DimensionChainInfo
            {
                WallSegment = horizontalWall,
                ReferencePoints = new List<(double, double, double)>
                {
                    (0, 0, 0),                    // Start
                    (horizontalLength, 0, 0)      // End (corner)
                },
                OffsetVector = horizontalOffset,
                DimensionStyle = "Linear - 3/32\" Arial",
                DimensionLineStart = (
                    X: 0 + horizontalOffset.X,
                    Y: 0 + horizontalOffset.Y,
                    Z: 0
                ),
                DimensionLineEnd = (
                    X: horizontalLength + horizontalOffset.X,
                    Y: 0 + horizontalOffset.Y,
                    Z: 0
                ),
                OpeningIndices = new List<int>()
            };

            // Vertical wall (vertical part of L)
            var verticalWall = WallSegmentInfo.CreateVertical(
                startPoint: (horizontalLength, 0, 0),
                endPoint: (horizontalLength, verticalLength, 0),
                normal: (1, 0, 0)
            );

            var verticalOffset = (
                X: verticalWall.Normal.X * offsetDistance,
                Y: verticalWall.Normal.Y * offsetDistance,
                Z: 0.0
            );

            var verticalChain = new DimensionChainInfo
            {
                WallSegment = verticalWall,
                ReferencePoints = new List<(double, double, double)>
                {
                    (horizontalLength, 0, 0),                    // Start (corner)
                    (horizontalLength, verticalLength, 0)        // End
                },
                OffsetVector = verticalOffset,
                DimensionStyle = "Linear - 3/32\" Arial",
                DimensionLineStart = (
                    X: horizontalLength + verticalOffset.X,
                    Y: 0 + verticalOffset.Y,
                    Z: 0
                ),
                DimensionLineEnd = (
                    X: horizontalLength + verticalOffset.X,
                    Y: verticalLength + verticalOffset.Y,
                    Z: 0
                ),
                OpeningIndices = new List<int>()
            };

            return new List<DimensionChainInfo> { horizontalChain, verticalChain };
        }
    }
}
