using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using System;

namespace RevitAI.UnitTests.Services;

/// <summary>
/// Unit tests for CenterOffsetStrategy (Story 2.2: Tag Placement Engine).
/// Tests the radial placement algorithm with increasing distances.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Layer1")]
public class CenterOffsetStrategyTests
{
    private CenterOffsetStrategy _strategy = null!;
    private const double DefaultOffset = 0.5; // feet

    [SetUp]
    public void Setup()
    {
        _strategy = new CenterOffsetStrategy(offsetDistanceFeet: DefaultOffset);
    }

    // ========== Constructor & Initialization ==========

    [Test]
    public void Constructor_NegativeOffset_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CenterOffsetStrategy(offsetDistanceFeet: -0.5));
    }

    [Test]
    public void Constructor_ZeroOffset_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new CenterOffsetStrategy(offsetDistanceFeet: 0));
    }

    // ========== AC-2.2.1: Preferred Placement Calculation ==========

    [Test]
    public void GetPreferredPlacement_ReturnsUpwardOffset()
    {
        // Arrange
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act
        var candidate = _strategy.GetPreferredPlacement(
            elementId: 1001,
            elementCenter: elementCenter,
            elementBounds: elementBounds);

        // Assert
        Assert.AreEqual(1001, candidate.ElementId);
        Assert.AreEqual(1, candidate.AttemptNumber);
        Assert.IsFalse(candidate.RequiresLeader, "Preferred placement should not require leader");

        // Verify location: center + upward offset (Y+)
        Assert.AreEqual(elementCenter.X, candidate.ProposedLocation.X, 0.001);
        Assert.AreEqual(elementCenter.Y + DefaultOffset, candidate.ProposedLocation.Y, 0.001);
        Assert.AreEqual(elementCenter.Z, candidate.ProposedLocation.Z, 0.001);
    }

    [Test]
    public void GetPreferredPlacement_CustomOffset_UsesCorrectDistance()
    {
        // Arrange
        var customStrategy = new CenterOffsetStrategy(offsetDistanceFeet: 1.0);
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act
        var candidate = customStrategy.GetPreferredPlacement(
            elementId: 1001,
            elementCenter: elementCenter,
            elementBounds: elementBounds);

        // Assert
        Assert.AreEqual(1.0, candidate.ProposedLocation.Y, 0.001, "Should use custom offset distance");
    }

    // ========== AC-2.2.3: Alternative Placement with Radial Search ==========

    [Test]
    public void GetAlternativePlacement_Attempt2_Uses0DegreesAngle()
    {
        // Arrange
        var elementCenter = new XYZ(10, 20, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act
        var candidate = _strategy.GetAlternativePlacement(
            elementId: 1001,
            elementCenter: elementCenter,
            elementBounds: elementBounds,
            attemptNumber: 2);

        // Assert
        Assert.AreEqual(2, candidate.AttemptNumber);
        Assert.IsTrue(candidate.RequiresLeader, "Alternative placements should use leader");
        Assert.AreEqual(0, candidate.PlacementAngle, "Attempt 2 should use 0° (East direction)");

        // Verify location: 0° means X+ direction
        double expectedDistance = DefaultOffset * 1.125; // Distance multiplier for attempt 2
        Assert.Greater(candidate.ProposedLocation.X, elementCenter.X, "Should be to the right (X+)");
        Assert.AreEqual(elementCenter.Y, candidate.ProposedLocation.Y, 0.001, "Y should remain at center for 0°");
    }

    [Test]
    public void GetAlternativePlacement_Attempt3_Uses45DegreesAngle()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act
        var candidate = _strategy.GetAlternativePlacement(
            elementId: 1001,
            elementCenter: elementCenter,
            elementBounds: elementBounds,
            attemptNumber: 3);

        // Assert
        Assert.AreEqual(45, candidate.PlacementAngle, "Attempt 3 should use 45° (NE direction)");

        // Verify location: 45° means equal X and Y positive offset
        Assert.Greater(candidate.ProposedLocation.X, 0, "Should have positive X offset");
        Assert.Greater(candidate.ProposedLocation.Y, 0, "Should have positive Y offset");

        // At 45°, X and Y offsets should be approximately equal
        Assert.AreEqual(
            candidate.ProposedLocation.X,
            candidate.ProposedLocation.Y,
            0.001,
            "45° angle should have equal X and Y components");
    }

    [Test]
    public void GetAlternativePlacement_Cycles8Angles()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);
        var expectedAngles = new[] { 0, 45, 90, 135, 180, 225, 270, 315 };

        // Act & Assert: Attempts 2-9 should cycle through 8 angles
        for (int attempt = 2; attempt <= 9; attempt++)
        {
            var candidate = _strategy.GetAlternativePlacement(
                elementId: 1001,
                elementCenter: elementCenter,
                elementBounds: elementBounds,
                attemptNumber: attempt);

            var expectedAngle = expectedAngles[(attempt - 2) % 8];
            Assert.AreEqual(expectedAngle, candidate.PlacementAngle,
                $"Attempt {attempt} should use angle {expectedAngle}°");
        }
    }

    [Test]
    public void GetAlternativePlacement_DistanceIncreasesWithAttempts()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act: Get multiple attempts
        var candidate2 = _strategy.GetAlternativePlacement(1001, elementCenter, elementBounds, attemptNumber: 2);
        var candidate5 = _strategy.GetAlternativePlacement(1001, elementCenter, elementBounds, attemptNumber: 5);
        var candidate10 = _strategy.GetAlternativePlacement(1001, elementCenter, elementBounds, attemptNumber: 10);

        // Assert: Distance from center should increase with attempt number
        var distance2 = candidate2.ProposedLocation.DistanceTo(elementCenter);
        var distance5 = candidate5.ProposedLocation.DistanceTo(elementCenter);
        var distance10 = candidate10.ProposedLocation.DistanceTo(elementCenter);

        Assert.Less(distance2, distance5, "Attempt 5 should be farther than attempt 2");
        Assert.Less(distance5, distance10, "Attempt 10 should be farther than attempt 5");

        TestContext.WriteLine($"Distance progression:");
        TestContext.WriteLine($"  Attempt 2:  {distance2:F3}' from center");
        TestContext.WriteLine($"  Attempt 5:  {distance5:F3}' from center");
        TestContext.WriteLine($"  Attempt 10: {distance10:F3}' from center");
    }

    [Test]
    public void GetAlternativePlacement_Attempt10_UsesMaximumDistance()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act
        var candidate = _strategy.GetAlternativePlacement(
            elementId: 1001,
            elementCenter: elementCenter,
            elementBounds: elementBounds,
            attemptNumber: 10);

        // Assert
        // Attempt 10: multiplier = 1.0 + (10 - 1) * 0.125 = 2.125
        double expectedDistance = DefaultOffset * 2.125;
        double actualDistance = candidate.ProposedLocation.DistanceTo(elementCenter);

        Assert.AreEqual(expectedDistance, actualDistance, 0.01,
            "Attempt 10 should use maximum distance multiplier (2.125x)");
    }

    // ========== Edge Cases & Error Handling ==========

    [Test]
    public void GetAlternativePlacement_AttemptTooLow_ThrowsException()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _strategy.GetAlternativePlacement(1001, elementCenter, elementBounds, attemptNumber: 1),
            "Attempt 1 is reserved for preferred placement");
    }

    [Test]
    public void GetAlternativePlacement_AttemptTooHigh_ThrowsException()
    {
        // Arrange
        var elementCenter = new XYZ(0, 0, 0);
        var elementBounds = BoundingBox.FromCenterAndSize(elementCenter, width: 3, height: 0.5);

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _strategy.GetAlternativePlacement(1001, elementCenter, elementBounds, attemptNumber: 11),
            "Max attempt number is 10");
    }
}
