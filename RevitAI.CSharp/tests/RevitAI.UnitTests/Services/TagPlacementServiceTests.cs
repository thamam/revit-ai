using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace RevitAI.UnitTests.Services;

/// <summary>
/// Unit tests for TagPlacementService (Story 2.2: Tag Placement Engine).
/// Tests the core placement algorithm with collision avoidance.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Layer1")]
public class TagPlacementServiceTests
{
    private TagPlacementService _service = null!;
    private SimpleBoundingBoxCollisionDetector _collisionDetector = null!;
    private CenterOffsetStrategy _strategy = null!;

    [SetUp]
    public void Setup()
    {
        _collisionDetector = new SimpleBoundingBoxCollisionDetector();
        _strategy = new CenterOffsetStrategy(offsetDistanceFeet: 0.5);
        _service = new TagPlacementService(_collisionDetector, _strategy, logger: null);
    }

    // ========== AC-2.2.1: Basic Tag Placement Calculation ==========

    [Test]
    public void CalculatePlacements_NoElements_ReturnsEmptyResult()
    {
        // Arrange
        var elements = new List<MockElement>();
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(0, result.TotalCount);
        Assert.AreEqual(0, result.SuccessCount);
        Assert.AreEqual(0, result.FailedCount);
        Assert.AreEqual(0, result.SuccessRate);
    }

    [Test]
    public void CalculatePlacements_SingleElement_ReturnsSuccessfulPlacement()
    {
        // Arrange
        var elements = new List<MockElement>
        {
            MockElement.CreateDoor(1001, new XYZ(0, 0, 0))
        };
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(1, result.SuccessCount);
        Assert.AreEqual(0, result.FailedCount);
        Assert.AreEqual(1.0, result.SuccessRate);

        var placement = result.Placements[0];
        Assert.IsTrue(placement.IsSuccess);
        Assert.AreEqual(1001, placement.ElementId);
        Assert.IsFalse(placement.HasLeader); // Preferred placement, no leader
        Assert.AreEqual(1, placement.AttemptsUsed); // First attempt succeeded
    }

    [Test]
    public void CalculatePlacements_TenElements_AllSucceed()
    {
        // Arrange: 10 elements in a line with 10' spacing (no collisions)
        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(10, result.TotalCount);
        Assert.AreEqual(10, result.SuccessCount);
        Assert.AreEqual(0, result.FailedCount);
        Assert.AreEqual(1.0, result.SuccessRate);
    }

    // ========== AC-2.2.2: Collision Detection & Avoidance ==========

    [Test]
    public void CalculatePlacements_WithExistingTags_AvoidsCollisions()
    {
        // Arrange: Element at origin, existing tag blocks preferred placement
        var elements = new List<MockElement>
        {
            MockElement.CreateDoor(1001, new XYZ(0, 0, 0))
        };

        // Block preferred placement (above element center at Y+0.5)
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0, 0.5, 0), width: 0.3, height: 0.15)
        };

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(1, result.SuccessCount);
        var placement = result.Placements[0];
        Assert.IsTrue(placement.IsSuccess);
        Assert.Greater(placement.AttemptsUsed, 1, "Should require alternative placement");
        Assert.IsTrue(placement.HasLeader, "Alternative placements use leader lines");
    }

    [Test]
    public void CalculatePlacements_IntraBatchCollisionAvoidance_Works()
    {
        // Arrange: 3 elements at the same location (extreme collision scenario)
        var elements = new List<MockElement>
        {
            MockElement.CreateDoor(1001, new XYZ(0, 0, 0)),
            MockElement.CreateDoor(1002, new XYZ(0, 0, 0)),
            MockElement.CreateDoor(1003, new XYZ(0, 0, 0))
        };
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert: All should succeed by using different alternative placements
        Assert.AreEqual(3, result.TotalCount);
        Assert.AreEqual(3, result.SuccessCount);

        // Verify each has different location
        var locations = result.Placements.Select(p => p.Location).ToList();
        Assert.AreEqual(3, locations.Distinct().Count(), "All placements should have unique locations");
    }

    // ========== AC-2.2.3: Alternative Placement Strategy ==========

    [Test]
    public void CalculatePlacements_MaxAttemptsExhausted_MarksFailed()
    {
        // Arrange: Element surrounded by existing tags (block all 10 placements)
        var elements = new List<MockElement>
        {
            MockElement.CreateDoor(1001, new XYZ(0, 0, 0))
        };

        // Block all possible placements (dense grid around origin)
        var existingTags = new List<BoundingBox>();
        for (double x = -2; x <= 2; x += 0.5)
        {
            for (double y = -2; y <= 2; y += 0.5)
            {
                existingTags.Add(BoundingBox.FromCenterAndSize(
                    new XYZ(x, y, 0), width: 0.4, height: 0.2));
            }
        }

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(1, result.TotalCount);
        Assert.AreEqual(0, result.SuccessCount);
        Assert.AreEqual(1, result.FailedCount);

        var placement = result.Placements[0];
        Assert.IsFalse(placement.IsSuccess);
        Assert.IsNotNull(placement.FailureReason);
        Assert.That(placement.FailureReason, Does.Contain("10 attempts"));
        Assert.AreEqual(10, placement.AttemptsUsed);
    }

    // ========== AC-2.2.4: Success Rate Target (95%+) ==========

    [Test]
    public void CalculatePlacements_100Elements_Achieves95PercentSuccessRate()
    {
        // Arrange: 100 elements in a 10x10 grid (realistic architectural scenario)
        var elements = MockElement.CreateGrid(count: 100, spacing: 8.0); // 8' spacing (tight but realistic)
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert: Must meet 95% success rate acceptance criteria
        Assert.GreaterOrEqual(result.SuccessRate, 0.95,
            $"Success rate {result.SuccessRate:P1} below 95% target. " +
            $"Succeeded: {result.SuccessCount}, Failed: {result.FailedCount}");

        Assert.IsTrue(result.MeetsQualityTarget, "Should meet quality target");

        // Additional validation
        Assert.GreaterOrEqual(result.SuccessCount, 95, "At least 95 of 100 should succeed");
        Assert.LessOrEqual(result.FailedCount, 5, "At most 5 of 100 should fail");
    }

    [Test]
    public void CalculatePlacements_PartialSuccess_ReturnsCorrectStatistics()
    {
        // Arrange: 10 elements, manually ensure some will fail
        var elements = MockElement.CreateGrid(count: 10, spacing: 5.0);

        // Block some placements to force failures
        var existingTags = new List<BoundingBox>();
        for (int i = 0; i < 3; i++)
        {
            var element = elements[i];
            // Surround element with tags to block all placements
            for (double angle = 0; angle < 360; angle += 30)
            {
                var offset = XYZ.FromPolar(angle, 0.8);
                existingTags.Add(BoundingBox.FromCenterAndSize(
                    element.Center + offset, width: 0.5, height: 0.3));
            }
        }

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.AreEqual(10, result.TotalCount);
        Assert.LessOrEqual(result.SuccessCount, 10);
        Assert.GreaterOrEqual(result.FailedCount, 3);
        Assert.Less(result.SuccessRate, 1.0);

        // Verify statistics are consistent
        Assert.AreEqual(result.TotalCount, result.SuccessCount + result.FailedCount);

        // Verify failure messages are populated
        Assert.IsNotEmpty(result.FailureMessages);
    }

    // ========== AC-2.2.6: Performance Target (500 elements < 5 seconds) ==========

    [Test]
    [Category("Performance")]
    public void CalculatePlacements_500Elements_CompletesUnder5Seconds()
    {
        // Arrange: 500 elements (acceptance criteria target)
        var elements = MockElement.CreateGrid(count: 500, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var stopwatch = Stopwatch.StartNew();
        var result = _service.CalculatePlacements(elements, existingTags);
        stopwatch.Stop();

        // Assert: Performance target
        Assert.Less(stopwatch.ElapsedMilliseconds, 5000,
            $"Performance target missed: {stopwatch.ElapsedMilliseconds}ms > 5000ms");

        // Verify correctness wasn't sacrificed for speed
        Assert.AreEqual(500, result.TotalCount);
        Assert.GreaterOrEqual(result.SuccessRate, 0.90, "Should maintain reasonable success rate");

        TestContext.WriteLine($"Performance: {result.TotalCount} placements in {stopwatch.ElapsedMilliseconds}ms");
        TestContext.WriteLine($"Success rate: {result.SuccessRate:P1}");
    }

    // ========== Edge Cases & Error Handling ==========

    [Test]
    public void CalculatePlacements_NullElements_ThrowsException()
    {
        // Arrange
        List<MockElement> elements = null!;
        var existingTags = new List<BoundingBox>();

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() =>
            _service.CalculatePlacements(elements, existingTags));
    }

    [Test]
    public void CalculatePlacements_NullExistingTags_ThrowsException()
    {
        // Arrange
        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        List<BoundingBox> existingTags = null!;

        // Act & Assert
        Assert.Throws<System.ArgumentNullException>(() =>
            _service.CalculatePlacements(elements, existingTags));
    }

    [Test]
    public void CalculatePlacements_ReturnsCalculationTime()
    {
        // Arrange
        var elements = MockElement.CreateGrid(count: 50, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Act
        var result = _service.CalculatePlacements(elements, existingTags);

        // Assert
        Assert.GreaterOrEqual(result.CalculationTimeMs, 0, "Should record calculation time (may be 0ms for fast ops)");
    }

    [Test]
    public void PlacementResult_SummaryMessage_IsHumanReadable()
    {
        // Arrange
        var result = PlacementResult.CreateForTest(successCount: 95, failedCount: 5);

        // Act
        var message = result.SummaryMessage;

        // Assert
        Assert.IsNotEmpty(message);
        Assert.That(message, Does.Contain("95"));
        Assert.That(message, Does.Contain("100"));
        TestContext.WriteLine($"Summary message: {message}");
    }
}
