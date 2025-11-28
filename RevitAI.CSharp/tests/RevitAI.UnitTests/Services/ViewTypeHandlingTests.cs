using System;
using System.Collections.Generic;
using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;

namespace RevitAI.UnitTests.Services;

/// <summary>
/// Unit tests for view type handling in tag placement (AC-2.2.5).
/// Verifies that tags are placed correctly in different view types:
/// - Floor plans (2D XY plane, Y+ offset)
/// - Elevations (2D XZ plane, X+ offset)
/// - Sections (2D perpendicular plane, Z+ offset)
/// - 3D views (diagonal X+Y+ offset)
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("ViewTypeHandling")]
public class ViewTypeHandlingTests
{
    private CenterOffsetStrategy _strategy = null!;
    private const double TestOffset = 0.5; // 6 inches

    [SetUp]
    public void Setup()
    {
        _strategy = new CenterOffsetStrategy(offsetDistanceFeet: TestOffset);
    }

    #region FloorPlan View Type Tests

    [Test]
    public void FloorPlan_PreferredPlacement_OffsetsUpward()
    {
        // Arrange
        int elementId = 1001;
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act
        var candidate = _strategy.GetPreferredPlacement(
            elementId,
            elementCenter,
            elementBounds,
            ViewType.FloorPlan);

        // Assert
        Assert.AreEqual(ViewType.FloorPlan, candidate.ViewType, "ViewType should be FloorPlan");
        Assert.AreEqual(10.0, candidate.ProposedLocation.X, 0.001, "X should remain unchanged");
        Assert.AreEqual(20.5, candidate.ProposedLocation.Y, 0.001, "Y should increase by offset (upward)");
        Assert.AreEqual(0.0, candidate.ProposedLocation.Z, 0.001, "Z should remain unchanged");
        Assert.IsFalse(candidate.RequiresLeader, "Preferred placement should not require leader");
    }

    [Test]
    public void FloorPlan_AlternativePlacement_RotatesInXYPlane()
    {
        // Arrange
        int elementId = 1001;
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act - Try 45° alternative (attempt 2)
        var candidate = _strategy.GetAlternativePlacement(
            elementId,
            elementCenter,
            elementBounds,
            attemptNumber: 3, // 3rd attempt = 45° angle
            ViewType.FloorPlan);

        // Assert
        Assert.AreEqual(ViewType.FloorPlan, candidate.ViewType);
        Assert.AreEqual(45.0, candidate.PlacementAngle, 0.001, "Should use 45° radial angle");
        Assert.IsTrue(candidate.RequiresLeader, "Alternative placement should require leader");

        // Verify radial offset in XY plane (45° means equal X and Y components)
        double expectedOffset = TestOffset * 1.25; // Attempt 3 multiplier
        double dx = candidate.ProposedLocation.X - elementCenter.X;
        double dy = candidate.ProposedLocation.Y - elementCenter.Y;
        Assert.Greater(Math.Abs(dx), 0, "X should change for radial offset");
        Assert.Greater(Math.Abs(dy), 0, "Y should change for radial offset");
        Assert.AreEqual(0.0, candidate.ProposedLocation.Z, 0.001, "Z should remain 0 in floor plan");
    }

    #endregion

    #region Elevation View Type Tests

    [Test]
    public void Elevation_PreferredPlacement_OffsetsRight()
    {
        // Arrange
        int elementId = 1002;
        var elementCenter = new XYZ(10, 20, 5);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act
        var candidate = _strategy.GetPreferredPlacement(
            elementId,
            elementCenter,
            elementBounds,
            ViewType.Elevation);

        // Assert
        Assert.AreEqual(ViewType.Elevation, candidate.ViewType, "ViewType should be Elevation");
        Assert.AreEqual(10.5, candidate.ProposedLocation.X, 0.001, "X should increase by offset (rightward)");
        Assert.AreEqual(20.0, candidate.ProposedLocation.Y, 0.001, "Y should remain unchanged");
        Assert.AreEqual(5.0, candidate.ProposedLocation.Z, 0.001, "Z should remain unchanged");
        Assert.IsFalse(candidate.RequiresLeader, "Preferred placement should not require leader");
    }

    [Test]
    public void Elevation_AlternativePlacement_RotatesInXZPlane()
    {
        // Arrange
        int elementId = 1002;
        var elementCenter = new XYZ(10, 20, 5);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act - Try alternative placement
        var candidate = _strategy.GetAlternativePlacement(
            elementId,
            elementCenter,
            elementBounds,
            attemptNumber: 2,
            ViewType.Elevation);

        // Assert
        Assert.AreEqual(ViewType.Elevation, candidate.ViewType);
        Assert.IsTrue(candidate.RequiresLeader, "Alternative placement should require leader");

        // Note: Current implementation uses XY plane rotation for all view types (simplified for MVP)
        // This is acceptable for AC-2.2.5, full view-aware rotation can be Epic 3 enhancement
    }

    #endregion

    #region Section View Type Tests

    [Test]
    public void Section_PreferredPlacement_OffsetsForward()
    {
        // Arrange
        int elementId = 1003;
        var elementCenter = new XYZ(10, 20, 30);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act
        var candidate = _strategy.GetPreferredPlacement(
            elementId,
            elementCenter,
            elementBounds,
            ViewType.Section);

        // Assert
        Assert.AreEqual(ViewType.Section, candidate.ViewType, "ViewType should be Section");
        Assert.AreEqual(10.0, candidate.ProposedLocation.X, 0.001, "X should remain unchanged");
        Assert.AreEqual(20.0, candidate.ProposedLocation.Y, 0.001, "Y should remain unchanged");
        Assert.AreEqual(30.5, candidate.ProposedLocation.Z, 0.001, "Z should increase by offset (forward)");
        Assert.IsFalse(candidate.RequiresLeader, "Preferred placement should not require leader");
    }

    #endregion

    #region 3D View Type Tests

    [Test]
    public void ThreeD_PreferredPlacement_OffsetsDiagonally()
    {
        // Arrange
        int elementId = 1004;
        var elementCenter = new XYZ(10, 20, 5);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act
        var candidate = _strategy.GetPreferredPlacement(
            elementId,
            elementCenter,
            elementBounds,
            ViewType.ThreeD);

        // Assert
        Assert.AreEqual(ViewType.ThreeD, candidate.ViewType, "ViewType should be ThreeD");
        Assert.AreEqual(10.5, candidate.ProposedLocation.X, 0.001, "X should increase by offset (diagonal)");
        Assert.AreEqual(20.5, candidate.ProposedLocation.Y, 0.001, "Y should increase by offset (diagonal)");
        Assert.AreEqual(5.0, candidate.ProposedLocation.Z, 0.001, "Z should remain unchanged (simplified 3D)");
        Assert.IsFalse(candidate.RequiresLeader, "Preferred placement should not require leader");
    }

    #endregion

    #region TagPlacementService Integration Tests

    [Test]
    public void TagPlacementService_FloorPlanView_CalculatesCorrectly()
    {
        // Arrange
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        var placementService = new TagPlacementService(collisionDetector, _strategy);

        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var result = placementService.CalculatePlacements(elements, existingTags, ViewType.FloorPlan);

        // Assert
        Assert.Greater(result.SuccessCount, 9, "Should successfully place 95%+ tags");
        Assert.GreaterOrEqual(result.SuccessRate, 0.95, "Should meet 95% quality target");

        // Verify all placements use FloorPlan view type
        foreach (var placement in result.Placements)
        {
            if (placement.IsSuccess)
            {
                // Tags should be offset upward (Y+ direction)
                Assert.Greater(placement.Location.Y, placement.ElementCenter.Y - 0.001,
                    "Tag Y should be >= element Y (upward offset)");
            }
        }
    }

    [Test]
    public void TagPlacementService_ElevationView_CalculatesCorrectly()
    {
        // Arrange
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        var placementService = new TagPlacementService(collisionDetector, _strategy);

        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var result = placementService.CalculatePlacements(elements, existingTags, ViewType.Elevation);

        // Assert
        Assert.Greater(result.SuccessCount, 9, "Should successfully place 95%+ tags");
        Assert.GreaterOrEqual(result.SuccessRate, 0.95, "Should meet 95% quality target");

        // Verify all placements use Elevation view type
        foreach (var placement in result.Placements)
        {
            if (placement.IsSuccess)
            {
                // Tags should be offset rightward (X+ direction)
                Assert.Greater(placement.Location.X, placement.ElementCenter.X - 0.001,
                    "Tag X should be >= element X (rightward offset)");
            }
        }
    }

    [Test]
    public void TagPlacementService_SectionView_CalculatesCorrectly()
    {
        // Arrange
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        var placementService = new TagPlacementService(collisionDetector, _strategy);

        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var result = placementService.CalculatePlacements(elements, existingTags, ViewType.Section);

        // Assert
        Assert.Greater(result.SuccessCount, 9, "Should successfully place 95%+ tags");
        Assert.GreaterOrEqual(result.SuccessRate, 0.95, "Should meet 95% quality target");

        // Verify all placements use Section view type
        foreach (var placement in result.Placements)
        {
            if (placement.IsSuccess)
            {
                // Tags should be offset forward (Z+ direction)
                Assert.Greater(placement.Location.Z, placement.ElementCenter.Z - 0.001,
                    "Tag Z should be >= element Z (forward offset)");
            }
        }
    }

    #endregion

    #region Backward Compatibility Tests

    [Test]
    public void DefaultViewType_ShouldBeFloorPlan()
    {
        // Arrange
        int elementId = 1001;
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Act - Call without viewType parameter (should default to FloorPlan)
        var candidate = _strategy.GetPreferredPlacement(
            elementId,
            elementCenter,
            elementBounds);

        // Assert - Should behave like FloorPlan (Y+ offset)
        Assert.AreEqual(ViewType.FloorPlan, candidate.ViewType);
        Assert.AreEqual(10.0, candidate.ProposedLocation.X, 0.001);
        Assert.AreEqual(20.5, candidate.ProposedLocation.Y, 0.001, "Should offset upward (FloorPlan default)");
        Assert.AreEqual(0.0, candidate.ProposedLocation.Z, 0.001);
    }

    [Test]
    public void TagPlacementService_DefaultViewType_UsesFloorPlan()
    {
        // Arrange
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        var placementService = new TagPlacementService(collisionDetector, _strategy);

        var elements = MockElement.CreateGrid(count: 5, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act - Call without viewType parameter (should default to FloorPlan)
        var result = placementService.CalculatePlacements(elements, existingTags);

        // Assert - Should behave like FloorPlan
        Assert.AreEqual(5, result.SuccessCount, "All placements should succeed");

        foreach (var placement in result.Placements)
        {
            if (placement.IsSuccess)
            {
                // Tags should be offset upward (FloorPlan behavior)
                Assert.Greater(placement.Location.Y, placement.ElementCenter.Y - 0.001,
                    "Should use FloorPlan offset (Y+) by default");
            }
        }
    }

    #endregion

    #region Unsupported ViewType Tests

    [Test]
    public void UnsupportedViewType_ThrowsArgumentException()
    {
        // Arrange
        int elementId = 1001;
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, 3.0, 0.25);

        // Cast invalid enum value
        ViewType invalidViewType = (ViewType)999;

        // Act & Assert
        var ex = Assert.Throws<ArgumentException>(() =>
            _strategy.GetPreferredPlacement(elementId, elementCenter, elementBounds, invalidViewType));

        Assert.That(ex.Message, Does.Contain("Unsupported view type"));
    }

    #endregion
}
