using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Models.Commands;
using RevitAI.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAI.UnitTests.Services
{
    [TestFixture]
    [Category("Unit")]
    [Category("Layer1")]
    public class DimensionChainPlannerTests
    {
        private DimensionChainPlanner _planner = null!;
        private DimensionParameters _defaultParameters = null!;

        [SetUp]
        public void Setup()
        {
            _planner = new DimensionChainPlanner();
            _defaultParameters = new DimensionParameters
            {
                OffsetMm = 200,  // Default 200mm offset
                DimensionStyle = "Linear - 3/32\" Arial"
            };
        }

        #region AC-2.3.1: Dimension Chain Creation

        [Test]
        public void PlanDimensions_RectangularRoom_Returns4DimensionChains()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular(width: 20.0, height: 15.0, roomName: "Conference Room");

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            Assert.AreEqual(4, chains.Count, "Rectangular room should have 4 dimension chains (one per wall)");
        }

        [Test]
        public void PlanDimensions_ValidBoundary_ChainsHaveCorrectDimensionStyle()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();
            var customParameters = new DimensionParameters
            {
                OffsetMm = 200,
                DimensionStyle = "Custom Style - 1/8\" Arial"
            };

            // Act
            var chains = _planner.PlanDimensions(boundary, customParameters);

            // Assert
            Assert.IsTrue(chains.All(c => c.DimensionStyle == "Custom Style - 1/8\" Arial"),
                "All chains should use the specified dimension style");
        }

        [Test]
        public void PlanDimensions_ValidBoundary_ChainsHaveStartAndEndReferences()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            foreach (var chain in chains)
            {
                Assert.IsNotNull(chain.WallSegment, "Chain should reference its wall segment");
                Assert.IsTrue(chain.ReferencePoints.Contains(chain.WallSegment.StartPoint),
                    "Reference points should include wall start point");
                Assert.IsTrue(chain.ReferencePoints.Contains(chain.WallSegment.EndPoint),
                    "Reference points should include wall end point");
            }
        }

        #endregion

        #region AC-2.3.2: Dimension Offset and Alignment

        [Test]
        public void PlanDimensions_DefaultOffset_CalculatesCorrectOffsetVector()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();
            double expectedOffsetFeet = 200 * 0.00328084;  // 200mm to feet = 0.656ft

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            foreach (var chain in chains)
            {
                double offsetMagnitude = Math.Sqrt(
                    chain.OffsetVector.X * chain.OffsetVector.X +
                    chain.OffsetVector.Y * chain.OffsetVector.Y +
                    chain.OffsetVector.Z * chain.OffsetVector.Z
                );

                Assert.AreEqual(expectedOffsetFeet, offsetMagnitude, 0.001,
                    "Offset vector magnitude should match 200mm in feet");
            }
        }

        [Test]
        public void PlanDimensions_HorizontalWall_OffsetDirectionMatchesNormal()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Bottom wall (South) - normal should point south (0, -1, 0)
            var bottomChain = chains[0];

            // Assert
            Assert.IsTrue(bottomChain.OffsetVector.Y < 0, "Bottom wall offset should point south (negative Y)");
            Assert.AreEqual(0, bottomChain.OffsetVector.X, 0.001, "Horizontal wall offset should have zero X component");
        }

        [Test]
        public void PlanDimensions_VerticalWall_OffsetDirectionMatchesNormal()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Right wall (East) - normal should point east (1, 0, 0)
            var rightChain = chains[1];

            // Assert
            Assert.IsTrue(rightChain.OffsetVector.X > 0, "Right wall offset should point east (positive X)");
            Assert.AreEqual(0, rightChain.OffsetVector.Y, 0.001, "Vertical wall offset should have zero Y component");
        }

        [Test]
        public void PlanDimensions_DimensionLineParallelToWall()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular(width: 20.0, height: 15.0);

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            foreach (var chain in chains)
            {
                // Dimension line direction
                double dimDx = chain.DimensionLineEnd.X - chain.DimensionLineStart.X;
                double dimDy = chain.DimensionLineEnd.Y - chain.DimensionLineStart.Y;

                // Wall direction
                double wallDx = chain.WallSegment.EndPoint.X - chain.WallSegment.StartPoint.X;
                double wallDy = chain.WallSegment.EndPoint.Y - chain.WallSegment.StartPoint.Y;

                // Normalize both vectors
                double dimLength = Math.Sqrt(dimDx * dimDx + dimDy * dimDy);
                double wallLength = Math.Sqrt(wallDx * wallDx + wallDy * wallDy);

                if (dimLength > 0.001 && wallLength > 0.001)
                {
                    double dimUnitX = dimDx / dimLength;
                    double dimUnitY = dimDy / dimLength;
                    double wallUnitX = wallDx / wallLength;
                    double wallUnitY = wallDy / wallLength;

                    // Check parallel (dot product should be ±1)
                    double dotProduct = dimUnitX * wallUnitX + dimUnitY * wallUnitY;
                    Assert.AreEqual(1.0, Math.Abs(dotProduct), 0.01,
                        "Dimension line should be parallel to wall");
                }
            }
        }

        #endregion

        #region AC-2.3.3: Reference Array Generation

        [Test]
        public void PlanDimensions_RoomWithOpenings_IncludesOpeningReferences()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateWithOpenings();

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert - At least one chain should have more than 2 reference points (start, door edges, end)
            var chainsWithOpenings = chains.Where(c => c.ReferencePoints.Count > 2).ToList();
            Assert.IsTrue(chainsWithOpenings.Any(), "Chains with openings should have > 2 reference points");
        }

        [Test]
        public void PlanDimensions_HorizontalWall_ReferencesOrderedLeftToRight()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular(width: 20.0, height: 15.0);

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Bottom wall (horizontal) - should be ordered by X coordinate
            var horizontalChain = chains.FirstOrDefault(c =>
                Math.Abs(c.WallSegment.OrientationDegrees % 180) < 10);

            // Assert
            Assert.IsNotNull(horizontalChain, "Should find at least one horizontal wall");
            if (horizontalChain != null)
            {
                var refPoints = horizontalChain.ReferencePoints;
                for (int i = 1; i < refPoints.Count; i++)
                {
                    Assert.IsTrue(refPoints[i].X >= refPoints[i - 1].X,
                        $"Horizontal wall reference points should be ordered left-to-right (X increasing). " +
                        $"Point {i - 1}: X={refPoints[i - 1].X}, Point {i}: X={refPoints[i].X}");
                }
            }
        }

        [Test]
        public void PlanDimensions_VerticalWall_ReferencesOrderedBottomToTop()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular(width: 20.0, height: 15.0);

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Right wall (vertical) - should be ordered by Y coordinate
            var verticalChain = chains.FirstOrDefault(c =>
                Math.Abs((c.WallSegment.OrientationDegrees - 90) % 180) < 10);

            // Assert
            Assert.IsNotNull(verticalChain, "Should find at least one vertical wall");
            if (verticalChain != null)
            {
                var refPoints = verticalChain.ReferencePoints;
                for (int i = 1; i < refPoints.Count; i++)
                {
                    Assert.IsTrue(refPoints[i].Y >= refPoints[i - 1].Y,
                        $"Vertical wall reference points should be ordered bottom-to-top (Y increasing). " +
                        $"Point {i - 1}: Y={refPoints[i - 1].Y}, Point {i}: Y={refPoints[i].Y}");
                }
            }
        }

        [Test]
        public void PlanDimensions_CornerReferences_IncludedAtJunctions()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert - Corner points should appear in multiple chains
            var allReferencePoints = chains.SelectMany(c => c.ReferencePoints).ToList();
            var cornerPointCounts = boundary.Corners
                .Select(corner => allReferencePoints.Count(refPt =>
                    Math.Abs(refPt.X - corner.X) < 0.01 &&
                    Math.Abs(refPt.Y - corner.Y) < 0.01 &&
                    Math.Abs(refPt.Z - corner.Z) < 0.01))
                .ToList();

            // Each corner should appear in at least 2 dimension chains (meeting walls)
            Assert.IsTrue(cornerPointCounts.All(count => count >= 2),
                "Corner points should be included in dimension chains of adjacent walls");
        }

        #endregion

        #region AC-2.3.4: Dimension Style Application

        [Test]
        public void PlanDimensions_NullDimensionStyle_UsesFallback()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();
            var parametersWithoutStyle = new DimensionParameters
            {
                OffsetMm = 200,
                DimensionStyle = null!  // No style specified
            };

            // Act
            var chains = _planner.PlanDimensions(boundary, parametersWithoutStyle);

            // Assert
            Assert.IsTrue(chains.All(c => !string.IsNullOrEmpty(c.DimensionStyle)),
                "Should provide fallback dimension style when none specified");
            Assert.IsTrue(chains.All(c => c.DimensionStyle == "Linear - 3/32\" Arial"),
                "Fallback should be 'Linear - 3/32\" Arial'");
        }

        #endregion

        #region Factory Method Tests

        [Test]
        public void DimensionChainInfo_CreateSimple_ReturnsValidChain()
        {
            // Act
            var chain = DimensionChainInfo.CreateSimple(wallLength: 20.0, offsetDistance: 0.656);

            // Assert
            Assert.IsNotNull(chain);
            Assert.AreEqual(2, chain.ReferencePoints.Count, "Simple chain should have 2 reference points");
            Assert.AreEqual(20.0, chain.WallSegment.Length, 0.001);
            Assert.AreEqual(0.656, Math.Sqrt(
                chain.OffsetVector.X * chain.OffsetVector.X +
                chain.OffsetVector.Y * chain.OffsetVector.Y
            ), 0.001, "Offset magnitude should match specified distance");
        }

        [Test]
        public void DimensionChainInfo_CreateWithOpenings_IncludesDoorEdges()
        {
            // Act
            var chain = DimensionChainInfo.CreateWithOpenings(
                wallLength: 20.0,
                doorCenterX: 10.0,
                doorWidth: 3.0);

            // Assert
            Assert.IsNotNull(chain);
            Assert.AreEqual(4, chain.ReferencePoints.Count,
                "Chain with opening should have 4 reference points (start, door edges, end)");
            Assert.AreEqual(1, chain.OpeningIndices.Count, "Should track 1 opening");
        }

        [Test]
        public void DimensionChainInfo_CreateLShaped_ReturnsTwoChains()
        {
            // Act
            var chains = DimensionChainInfo.CreateLShaped(
                horizontalLength: 20.0,
                verticalLength: 15.0);

            // Assert
            Assert.AreEqual(2, chains.Count, "L-shaped should have 2 dimension chains");
            Assert.IsTrue(chains[0].WallSegment.OrientationDegrees == 0 ||
                         chains[0].WallSegment.OrientationDegrees == 180,
                "First chain should be horizontal");
            Assert.IsTrue(chains[1].WallSegment.OrientationDegrees == 90 ||
                         chains[1].WallSegment.OrientationDegrees == 270,
                "Second chain should be vertical");
        }

        #endregion

        #region Error Handling Tests

        [Test]
        public void PlanDimensions_NullBoundary_ThrowsArgumentNullException()
        {
            // Arrange
            RoomBoundaryInfo? nullBoundary = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _planner.PlanDimensions(nullBoundary!, _defaultParameters));
        }

        [Test]
        public void PlanDimensions_NullParameters_ThrowsArgumentNullException()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateRectangular();
            DimensionParameters? nullParameters = null;

            // Act & Assert
            Assert.Throws<ArgumentNullException>(() =>
                _planner.PlanDimensions(boundary, nullParameters!));
        }

        [Test]
        public void PlanDimensions_EmptyWallSegments_ReturnsEmptyList()
        {
            // Arrange
            var boundary = new RoomBoundaryInfo
            {
                Room = RoomInfo.CreateRectangular(20, 15, "Empty Room"),
                WallSegments = new List<WallSegmentInfo>(),  // No walls
                Corners = new List<(double, double, double)>(),
                Openings = new List<OpeningInfo>()
            };

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            Assert.IsEmpty(chains, "No wall segments should result in empty dimension chains");
        }

        #endregion

        #region Integration Tests

        [Test]
        public void EndToEnd_RoomInfo_To_DimensionChains_Integration()
        {
            // Arrange - Full pipeline: RoomInfo → RoomBoundaryAnalyzer → DimensionChainPlanner
            var roomInfo = RoomInfo.CreateRectangular(width: 20.0, length: 15.0, name: "Office 101");
            var boundaryAnalyzer = new RoomBoundaryAnalyzer();
            var boundary = boundaryAnalyzer.AnalyzeBoundary(roomInfo);

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            Assert.AreEqual(4, chains.Count, "Rectangular room should produce 4 dimension chains");
            Assert.IsTrue(chains.All(c => c.ReferencePoints.Count >= 2),
                "Each chain should have at least start and end references");
            Assert.IsTrue(chains.All(c => !string.IsNullOrEmpty(c.DimensionStyle)),
                "Each chain should have a dimension style");
            Assert.IsTrue(chains.All(c =>
                Math.Sqrt(c.OffsetVector.X * c.OffsetVector.X +
                         c.OffsetVector.Y * c.OffsetVector.Y) > 0),
                "Each chain should have non-zero offset vector");
        }

        [Test]
        public void PlanDimensions_CurvedWall_SkippedWithoutError()
        {
            // Arrange - Room with one curved wall
            var boundary = new RoomBoundaryInfo
            {
                Room = RoomInfo.CreateRectangular(20, 15, "Curved Room"),
                WallSegments = new List<WallSegmentInfo>
                {
                    WallSegmentInfo.CreateHorizontal((0, 0, 0), (20, 0, 0), (0, -1, 0)),
                    WallSegmentInfo.CreateCurved((20, 0, 0), (20, 15, 0), radius: 10.0),  // Curved wall
                    WallSegmentInfo.CreateHorizontal((20, 15, 0), (0, 15, 0), (0, 1, 0)),
                    WallSegmentInfo.CreateVertical((0, 15, 0), (0, 0, 0), (-1, 0, 0))
                },
                Corners = new List<(double, double, double)>
                {
                    (0, 0, 0), (20, 0, 0), (20, 15, 0), (0, 15, 0)
                }
            };

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            Assert.AreEqual(3, chains.Count, "Curved wall should be skipped, leaving 3 straight walls");
            Assert.IsTrue(chains.All(c => !c.WallSegment.IsCurved),
                "Result should not contain curved walls");
        }

        [Test]
        public void PlanDimensions_RoomSeparators_FilteredOut()
        {
            // Arrange - Room with one room separator
            var boundary = new RoomBoundaryInfo
            {
                Room = RoomInfo.CreateRectangular(20, 15, "Room with Separator"),
                WallSegments = new List<WallSegmentInfo>
                {
                    WallSegmentInfo.CreateHorizontal((0, 0, 0), (20, 0, 0), (0, -1, 0)),
                    WallSegmentInfo.CreateVertical((20, 0, 0), (20, 15, 0), (1, 0, 0)),
                    WallSegmentInfo.CreateRoomSeparator((20, 15, 0), (0, 15, 0)),  // Room separator
                    WallSegmentInfo.CreateVertical((0, 15, 0), (0, 0, 0), (-1, 0, 0))
                },
                Corners = new List<(double, double, double)>
                {
                    (0, 0, 0), (20, 0, 0), (20, 15, 0), (0, 15, 0)
                }
            };

            // Act
            var chains = _planner.PlanDimensions(boundary, _defaultParameters);

            // Assert
            Assert.AreEqual(3, chains.Count, "Room separator should be filtered, leaving 3 physical walls");
            Assert.IsTrue(chains.All(c => !c.WallSegment.IsRoomSeparator),
                "Result should not contain room separators");
        }

        #endregion
    }
}
