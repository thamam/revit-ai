using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models.Domain;
using System.Collections.Generic;
using System.Linq;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Unit tests for DimensionPlanningService.
    /// These tests run in MILLISECONDS - true Layer 1 SIL testing.
    /// NO Revit API dependencies, NO external services.
    /// </summary>
    [TestFixture]
    public class DimensionPlanningServiceTests
    {
        private DimensionPlanningService _service;

        [SetUp]
        public void Setup()
        {
            _service = new DimensionPlanningService
            {
                DefaultOffsetFeet = 2.0,
                MaxDimensionsPerOperation = 1000
            };
        }

        #region Room Dimension Planning Tests

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_RectangularRoom_Creates4Dimensions()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(10, 15, "Office");
            var walls = new List<WallInfo>
            {
                WallInfo.CreateStraight(15, 0, 0, "horizontal"),
                WallInfo.CreateStraight(10, 15, 0, "vertical"),
                WallInfo.CreateStraight(15, 0, 10, "horizontal"),
                WallInfo.CreateStraight(10, 0, 0, "vertical")
            };
            // Assign wall IDs to match room
            for (int i = 0; i < 4; i++)
            {
                walls[i].ElementId = i + 1;
            }
            room.BoundingWallIds = new List<long> { 1, 2, 3, 4 };

            // Act
            var result = _service.PlanRoomDimensions(new[] { room }, walls);

            // Assert
            Assert.That(result.IsFeasible, Is.True);
            Assert.That(result.PlannedDimensions.Count, Is.EqualTo(4),
                "Rectangular room should have 4 dimension segments");
            Assert.That(result.Warnings, Is.Empty);
            Assert.That(result.Errors, Is.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_LShapedRoom_Creates6Dimensions()
        {
            // Arrange
            var room = RoomInfo.CreateLShaped();
            var walls = new List<WallInfo>();
            for (int i = 1; i <= 6; i++)
            {
                var wall = WallInfo.CreateStraight(10, i * 2, 0, i % 2 == 0 ? "horizontal" : "vertical");
                wall.ElementId = i;
                walls.Add(wall);
            }
            room.BoundingWallIds = walls.Select(w => w.ElementId).ToList();

            // Act
            var result = _service.PlanRoomDimensions(new[] { room }, walls);

            // Assert
            Assert.That(result.IsFeasible, Is.True);
            Assert.That(result.PlannedDimensions.Count, Is.EqualTo(6),
                "L-shaped room should have 6 dimension segments");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_WallWithDoor_CreatesMultipleSegments()
        {
            // Arrange
            var room = RoomInfo.CreateWithDoorOpening();
            var wallWithDoor = WallInfo.CreateWithDoorOpening(12, 3);
            wallWithDoor.ElementId = 1;
            room.BoundingWallIds = new List<long> { 1 };

            // Act
            var result = _service.PlanRoomDimensions(new[] { room }, new[] { wallWithDoor });

            // Assert
            Assert.That(result.IsFeasible, Is.True);
            Assert.That(result.PlannedDimensions.Count, Is.EqualTo(2),
                "Wall with door should have 2 dimensionable segments (before and after door)");
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_CurvedWall_GeneratesWarning()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(10, 10);
            var curvedWall = WallInfo.CreateCurved();
            curvedWall.ElementId = 1;
            room.BoundingWallIds = new List<long> { 1 };

            // Act
            var result = _service.PlanRoomDimensions(new[] { room }, new[] { curvedWall });

            // Assert
            Assert.That(result.IsFeasible, Is.True); // Still feasible, just skipped
            Assert.That(result.PlannedDimensions.Count, Is.EqualTo(0),
                "Curved wall cannot be dimensioned");
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("curved"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_UnplacedRoom_GeneratesWarning()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(10, 10);
            room.IsPlaced = false;

            // Act
            var result = _service.PlanRoomDimensions(new[] { room }, new List<WallInfo>());

            // Assert
            Assert.That(result.IsFeasible, Is.True);
            Assert.That(result.PlannedDimensions.Count, Is.EqualTo(0));
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("not placed"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanRoomDimensions_ExceedsLimit_ReturnsFeasibleFalse()
        {
            // Arrange
            _service.MaxDimensionsPerOperation = 10;
            var rooms = new List<RoomInfo>();
            var walls = new List<WallInfo>();

            // Create 5 rooms with 4 walls each = 20 dimensions (exceeds 10)
            for (int r = 0; r < 5; r++)
            {
                var room = RoomInfo.CreateRectangular(10, 10, $"Room {r}");
                var roomWallIds = new List<long>();

                for (int w = 0; w < 4; w++)
                {
                    var wall = WallInfo.CreateStraight(10, r * 20 + w * 2, 0);
                    wall.ElementId = r * 10 + w;
                    walls.Add(wall);
                    roomWallIds.Add(wall.ElementId);
                }

                room.BoundingWallIds = roomWallIds;
                rooms.Add(room);
            }

            // Act
            var result = _service.PlanRoomDimensions(rooms, walls);

            // Assert
            Assert.That(result.IsFeasible, Is.False, "Should exceed dimension limit");
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("exceeding limit"));
            Assert.That(result.TotalDimensionCount, Is.EqualTo(20));
        }

        #endregion

        #region Dimension Chain Tests

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanDimensionChain_MultipleWalls_CreatesContinuousChain()
        {
            // Arrange
            var walls = new List<WallInfo>
            {
                WallInfo.CreateStraight(10, 0, 0, "horizontal"),
                WallInfo.CreateStraight(15, 10, 0, "horizontal"),
                WallInfo.CreateStraight(8, 25, 0, "horizontal")
            };
            walls[0].ElementId = 1;
            walls[1].ElementId = 2;
            walls[2].ElementId = 3;

            // Act
            var result = _service.PlanDimensionChain(walls);

            // Assert
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Value, Is.EqualTo(33), "Chain should sum wall lengths: 10+15+8=33");
            Assert.That(result.ReferencedElementIds.Length, Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanDimensionChain_SingleWall_ReturnsInvalid()
        {
            // Arrange
            var wall = WallInfo.CreateStraight(10, 0, 0);

            // Act
            var result = _service.PlanDimensionChain(new[] { wall });

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("at least 2 walls"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void PlanDimensionChain_WithCurvedWall_ReturnsInvalid()
        {
            // Arrange
            var walls = new List<WallInfo>
            {
                WallInfo.CreateStraight(10, 0, 0),
                WallInfo.CreateCurved(),
                WallInfo.CreateStraight(8, 20, 0)
            };

            // Act
            var result = _service.PlanDimensionChain(walls);

            // Assert
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("curved"));
        }

        #endregion

        #region Validation Tests

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ValidateRoomsForDimensioning_ValidRooms_ReturnsFeasible()
        {
            // Arrange
            var rooms = new List<RoomInfo>
            {
                RoomInfo.CreateRectangular(10, 15),
                RoomInfo.CreateLShaped()
            };

            // Act
            var result = _service.ValidateRoomsForDimensioning(rooms);

            // Assert
            Assert.That(result.IsFeasible, Is.True);
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Warnings, Is.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ValidateRoomsForDimensioning_EmptyList_ReturnsNotFeasible()
        {
            // Arrange
            var rooms = new List<RoomInfo>();

            // Act
            var result = _service.ValidateRoomsForDimensioning(rooms);

            // Assert
            Assert.That(result.IsFeasible, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("No rooms"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ValidateRoomsForDimensioning_RoomWithNoWalls_ReturnsError()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(10, 10);
            room.BoundingWallIds.Clear(); // No walls

            // Act
            var result = _service.ValidateRoomsForDimensioning(new[] { room });

            // Assert
            Assert.That(result.IsFeasible, Is.False);
            Assert.That(result.Errors, Has.Count.EqualTo(1));
            Assert.That(result.Errors[0], Does.Contain("no bounding walls"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ValidateRoomsForDimensioning_ZeroArea_GeneratesWarning()
        {
            // Arrange
            var room = RoomInfo.CreateRectangular(0, 10);

            // Act
            var result = _service.ValidateRoomsForDimensioning(new[] { room });

            // Assert
            Assert.That(result.IsFeasible, Is.True); // Warning, not error
            Assert.That(result.Warnings, Has.Count.EqualTo(1));
            Assert.That(result.Warnings[0], Does.Contain("zero or negative area"));
        }

        #endregion
    }
}
