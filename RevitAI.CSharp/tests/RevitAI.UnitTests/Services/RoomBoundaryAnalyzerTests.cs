using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using System.Collections.Generic;
using System.Linq;

namespace RevitAI.UnitTests.Services
{
    /// <summary>
    /// Unit tests for RoomBoundaryAnalyzer.
    /// Layer 1 SIL pattern: Tests run in MILLISECONDS with POCOs only.
    /// NO Revit API dependencies, NO external services.
    /// </summary>
    [TestFixture]
    public class RoomBoundaryAnalyzerTests
    {
        private RoomBoundaryAnalyzer _analyzer;

        [SetUp]
        public void Setup()
        {
            _analyzer = new RoomBoundaryAnalyzer();
        }

        #region AC-2.2.1: Wall Segment Extraction

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void AnalyzeBoundary_RectangularRoom_Extracts4WallSegments()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(width: 20, length: 15);

            // Act
            var result = _analyzer.AnalyzeBoundary(room);

            // Assert
            Assert.AreEqual(4, result.WallSegments.Count, "Rectangular room should have 4 walls");
            Assert.IsTrue(result.WallSegments.All(w => w.Length > 0), "All walls should have positive length");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void AnalyzeBoundary_RectangularRoom_CalculatesCorrectWallNormals()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(width: 20, length: 15);

            // Act
            var result = _analyzer.AnalyzeBoundary(room);

            // Assert - Normals should point outward from room
            // Bottom wall (South) - normal points down (0, -1, 0)
            Assert.AreEqual((0, -1, 0), result.WallSegments[0].Normal, "Bottom wall normal incorrect");
            // Right wall (East) - normal points right (1, 0, 0)
            Assert.AreEqual((1, 0, 0), result.WallSegments[1].Normal, "Right wall normal incorrect");
            // Top wall (North) - normal points up (0, 1, 0)
            Assert.AreEqual((0, 1, 0), result.WallSegments[2].Normal, "Top wall normal incorrect");
            // Left wall (West) - normal points left (-1, 0, 0)
            Assert.AreEqual((-1, 0, 0), result.WallSegments[3].Normal, "Left wall normal incorrect");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void AnalyzeBoundary_RectangularRoom_CalculatesCorrectPerimeter()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(width: 20, length: 15);

            // Act
            var result = _analyzer.AnalyzeBoundary(room);

            // Assert
            double expectedPerimeter = 2 * (20 + 15); // 70 feet
            Assert.AreEqual(expectedPerimeter, result.Perimeter, 0.01, "Perimeter calculation incorrect");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void CalculateWallNormal_HorizontalWall_ReturnsPerpendicularVector()
        {
            // Arrange - Horizontal wall from (0,0) to (10,0)
            var start = (0.0, 0.0, 0.0);
            var end = (10.0, 0.0, 0.0);

            // Act
            var normal = _analyzer.CalculateWallNormal(start, end);

            // Assert - Normal should be (0, 1, 0) or (0, -1, 0) perpendicular to X-axis
            Assert.AreEqual(0, normal.X, 0.001, "Normal X should be 0 for horizontal wall");
            Assert.That(System.Math.Abs(normal.Y), Is.EqualTo(1).Within(0.001), "Normal Y should be ±1");
            Assert.AreEqual(0, normal.Z, 0.001, "Normal Z should be 0");
        }

        #endregion

        #region AC-2.2.2: Geometric Feature Detection

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void IdentifyCorners_RectangularRoom_Finds4Corners()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(width: 20, length: 15);
            var boundary = _analyzer.AnalyzeBoundary(room);

            // Act
            var corners = _analyzer.IdentifyCorners(boundary.WallSegments);

            // Assert
            Assert.AreEqual(4, corners.Count, "Rectangular room should have 4 corners");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void AnalyzeBoundary_RoomWithOpenings_DetectsOpenings()
        {
            // Arrange - Use factory method that creates room with door and window
            var boundary = RoomBoundaryInfo.CreateWithOpenings();

            // Assert
            Assert.AreEqual(2, boundary.Openings.Count, "Room should have 2 openings");
            Assert.IsTrue(boundary.Openings.Any(o => o.Type == OpeningType.Door), "Should have door opening");
            Assert.IsTrue(boundary.Openings.Any(o => o.Type == OpeningType.Window), "Should have window opening");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void IdentifyCorners_LShapedRoom_Finds6Corners()
        {
            // Arrange
            var boundary = RoomBoundaryInfo.CreateLShaped();

            // Act
            var corners = _analyzer.IdentifyCorners(boundary.WallSegments);

            // Assert
            Assert.AreEqual(6, corners.Count, "L-shaped room should have 6 corners");
        }

        #endregion

        #region AC-2.2.3: Curved Wall Handling

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void IsCurvedWall_CurvedSegment_ReturnsTrue()
        {
            // Arrange
            var curvedWall = WallSegmentInfo.CreateCurved(
                startPoint: (0, 0, 0),
                endPoint: (10, 10, 0),
                radius: 10.0
            );

            // Act
            bool isCurved = _analyzer.IsCurvedWall(curvedWall);

            // Assert
            Assert.IsTrue(isCurved, "Curved wall should be detected");
            Assert.IsTrue(curvedWall.IsCurved, "IsCurved flag should be true");
            Assert.IsNotNull(curvedWall.CurveRadius, "Curve radius should be set");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void IsCurvedWall_StraightSegment_ReturnsFalse()
        {
            // Arrange
            var straightWall = WallSegmentInfo.CreateHorizontal(
                startPoint: (0, 0, 0),
                endPoint: (10, 0, 0),
                normal: (0, -1, 0)
            );

            // Act
            bool isCurved = _analyzer.IsCurvedWall(straightWall);

            // Assert
            Assert.IsFalse(isCurved, "Straight wall should not be curved");
            Assert.IsFalse(straightWall.IsCurved, "IsCurved flag should be false");
        }

        #endregion

        #region AC-2.2.4: Angled Wall Processing

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void CalculateWallAngle_45DegreeWall_ReturnsCorrectAngle()
        {
            // Arrange - Wall going northeast at 45 degrees
            var start = (0.0, 0.0, 0.0);
            var end = (10.0, 10.0, 0.0);

            // Act
            double angle = _analyzer.CalculateWallAngle(start, end);

            // Assert
            Assert.AreEqual(45.0, angle, 0.1, "45-degree wall angle incorrect");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void CalculateWallAngle_HorizontalWall_Returns0Degrees()
        {
            // Arrange - Horizontal wall going east
            var start = (0.0, 0.0, 0.0);
            var end = (10.0, 0.0, 0.0);

            // Act
            double angle = _analyzer.CalculateWallAngle(start, end);

            // Assert
            Assert.AreEqual(0.0, angle, 0.1, "Horizontal east wall should be 0 degrees");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void CalculateWallAngle_VerticalWall_Returns90Degrees()
        {
            // Arrange - Vertical wall going north
            var start = (0.0, 0.0, 0.0);
            var end = (0.0, 10.0, 0.0);

            // Act
            double angle = _analyzer.CalculateWallAngle(start, end);

            // Assert
            Assert.AreEqual(90.0, angle, 0.1, "Vertical north wall should be 90 degrees");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void CreateAngled_NonOrthogonalWall_DoesNotThrow()
        {
            // Arrange & Act - Should not throw exception
            var angledWall = WallSegmentInfo.CreateAngled(
                startPoint: (0, 0, 0),
                endPoint: (10, 5, 0),
                angleInDegrees: 26.57  // arctan(5/10)
            );

            // Assert
            Assert.IsNotNull(angledWall, "Angled wall should be created successfully");
            Assert.IsFalse(angledWall.IsCurved, "Angled wall is not curved");
            Assert.Greater(angledWall.Length, 0, "Angled wall should have positive length");
        }

        #endregion

        #region AC-2.2.5: Room Separator Filtering

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void FilterRoomSeparators_MixedSegments_FiltersCorrectly()
        {
            // Arrange
            var segments = new List<WallSegmentInfo>
            {
                WallSegmentInfo.CreateHorizontal((0,0,0), (10,0,0), (0,-1,0)),  // Physical wall
                WallSegmentInfo.CreateRoomSeparator((10,0,0), (10,10,0)),        // Separator
                WallSegmentInfo.CreateHorizontal((10,10,0), (0,10,0), (0,1,0))  // Physical wall
            };

            // Act
            var (physicalWalls, filteredCount) = _analyzer.FilterRoomSeparators(segments);

            // Assert
            Assert.AreEqual(2, physicalWalls.Count, "Should have 2 physical walls");
            Assert.AreEqual(1, filteredCount, "Should filter 1 room separator");
            Assert.IsTrue(physicalWalls.All(w => !w.IsRoomSeparator), "Filtered list should not contain separators");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void FilterRoomSeparators_AllPhysicalWalls_NoFiltering()
        {
            // Arrange
            var segments = new List<WallSegmentInfo>
            {
                WallSegmentInfo.CreateHorizontal((0,0,0), (10,0,0), (0,-1,0)),
                WallSegmentInfo.CreateVertical((10,0,0), (10,10,0), (1,0,0)),
                WallSegmentInfo.CreateHorizontal((10,10,0), (0,10,0), (0,1,0)),
                WallSegmentInfo.CreateVertical((0,10,0), (0,0,0), (-1,0,0))
            };

            // Act
            var (physicalWalls, filteredCount) = _analyzer.FilterRoomSeparators(segments);

            // Assert
            Assert.AreEqual(4, physicalWalls.Count, "All 4 walls should remain");
            Assert.AreEqual(0, filteredCount, "No filtering should occur");
        }

        #endregion

        #region POCO Factory Method Tests

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void RoomBoundaryInfo_CreateRectangular_ValidFactory()
        {
            // Act
            var boundary = RoomBoundaryInfo.CreateRectangular(width: 20, height: 15);

            // Assert
            Assert.IsNotNull(boundary, "Factory should create boundary");
            Assert.AreEqual(4, boundary.WallSegments.Count, "Rectangular room has 4 walls");
            Assert.AreEqual(4, boundary.Corners.Count, "Rectangular room has 4 corners");
            Assert.AreEqual(70, boundary.Perimeter, "Perimeter should be 2*(20+15)");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void RoomBoundaryInfo_CreateLShaped_ValidFactory()
        {
            // Act
            var boundary = RoomBoundaryInfo.CreateLShaped();

            // Assert
            Assert.IsNotNull(boundary, "Factory should create L-shaped boundary");
            Assert.AreEqual(6, boundary.WallSegments.Count, "L-shaped room has 6 walls");
            Assert.AreEqual(6, boundary.Corners.Count, "L-shaped room has 6 corners");
            Assert.Greater(boundary.Perimeter, 0, "Perimeter should be positive");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void WallSegmentInfo_CreateWithOpening_ValidFactory()
        {
            // Act
            var boundary = RoomBoundaryInfo.CreateWithOpenings();

            // Assert
            Assert.IsNotNull(boundary, "Factory should create boundary with openings");
            Assert.AreEqual(2, boundary.Openings.Count, "Should have door and window");
            var door = boundary.Openings.FirstOrDefault(o => o.Type == OpeningType.Door);
            Assert.IsNotNull(door, "Door opening should exist");
            Assert.AreEqual(3.0, door.Width, "Door width should be 3 feet");
        }

        #endregion

        #region Edge Cases and Error Handling

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void AnalyzeBoundary_NullRoom_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<System.ArgumentNullException>(() =>
                _analyzer.AnalyzeBoundary(null)
            );
        }

        #endregion
    }
}
