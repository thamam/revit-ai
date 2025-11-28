using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using System;
using System.Collections.Generic;

namespace RevitAI.UnitTests.Services;

/// <summary>
/// Unit tests for SimpleBoundingBoxCollisionDetector (Story 2.2: Tag Placement Engine).
/// Tests the bounding box intersection algorithm with buffer margins.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Layer1")]
public class CollisionDetectorTests
{
    private SimpleBoundingBoxCollisionDetector _detector = null!;

    [SetUp]
    public void Setup()
    {
        _detector = new SimpleBoundingBoxCollisionDetector();
    }

    // ========== Constructor & Initialization ==========

    [Test]
    public void Constructor_DefaultBufferMargin_IsPoint1Feet()
    {
        // Act
        var detector = new SimpleBoundingBoxCollisionDetector();

        // Assert: Default buffer margin is 0.1' (verified through behavior)
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTag = BoundingBox.FromCenterAndSize(new XYZ(0.35, 0, 0), width: 0.3, height: 0.15);

        // Tags are 0.35' apart (center to center), tag width 0.3' = 0.15' radius
        // Distance between edges: 0.35 - 0.15 - 0.15 = 0.05' < 0.1' buffer
        // Should collide
        Assert.IsTrue(detector.HasCollision(candidate, new List<BoundingBox> { existingTag }),
            "Default buffer margin should be 0.1'");
    }

    [Test]
    public void Constructor_CustomBufferMargin_UsesSpecifiedValue()
    {
        // Arrange
        var detectorLargeMargin = new SimpleBoundingBoxCollisionDetector(bufferMargin: 1.0);
        var detectorNoMargin = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.0);

        var candidate = CreateCandidateAt(new XYZ(0, 0, 0)); // Bounds: X [-0.15, 0.15]
        var existingTag = BoundingBox.FromCenterAndSize(new XYZ(1.0, 0, 0), width: 0.3, height: 0.15); // Bounds: X [0.85, 1.15]

        // Edge-to-edge distance: 1.0 - 0.15 - 0.15 = 0.7'
        // Large margin (1.0') > 0.7' → should collide
        // No margin (0.0') < 0.7' → should NOT collide

        // Assert: Large margin should detect collision, no margin should not
        Assert.IsTrue(detectorLargeMargin.HasCollision(candidate, new List<BoundingBox> { existingTag }),
            "Large buffer margin (1.0') should detect collision at 0.7' edge distance");

        Assert.IsFalse(detectorNoMargin.HasCollision(candidate, new List<BoundingBox> { existingTag }),
            "No buffer margin should not detect collision for well-separated tags (0.7' apart)");
    }

    [Test]
    public void Constructor_NegativeBufferMargin_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            new SimpleBoundingBoxCollisionDetector(bufferMargin: -0.1));
    }

    // ========== AC-2.2.2: Collision Detection ==========

    [Test]
    public void HasCollision_NoExistingTags_ReturnsFalse()
    {
        // Arrange
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>();

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsFalse(hasCollision, "No existing tags means no collision");
    }

    [Test]
    public void HasCollision_TagsFarApart_ReturnsFalse()
    {
        // Arrange
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(10, 0, 0), width: 0.3, height: 0.15)
        };

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsFalse(hasCollision, "Tags 10' apart should not collide");
    }

    [Test]
    public void HasCollision_TagsOverlapping_ReturnsTrue()
    {
        // Arrange: Candidate at origin, existing tag also at origin (direct overlap)
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0, 0, 0), width: 0.3, height: 0.15)
        };

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsTrue(hasCollision, "Overlapping tags should collide");
    }

    [Test]
    public void HasCollision_TagsTouching_ReturnsTrue()
    {
        // Arrange: Tags touching but not overlapping (within buffer margin)
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0)); // Tag bounds: X [-0.15, 0.15]
        var existingTags = new List<BoundingBox>
        {
            // Tag at X=0.3, bounds: X [0.15, 0.45] - touching edge at X=0.15
            BoundingBox.FromCenterAndSize(new XYZ(0.3, 0, 0), width: 0.3, height: 0.15)
        };

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsTrue(hasCollision, "Tags touching should collide (within buffer margin)");
    }

    [Test]
    public void HasCollision_MultipleExistingTags_DetectsAnyCollision()
    {
        // Arrange: 5 existing tags, candidate collides with the 3rd one
        var candidate = CreateCandidateAt(new XYZ(5, 0, 0));
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0, 0, 0), width: 0.3, height: 0.15),
            BoundingBox.FromCenterAndSize(new XYZ(2, 0, 0), width: 0.3, height: 0.15),
            BoundingBox.FromCenterAndSize(new XYZ(5, 0, 0), width: 0.3, height: 0.15), // Collision here
            BoundingBox.FromCenterAndSize(new XYZ(8, 0, 0), width: 0.3, height: 0.15),
            BoundingBox.FromCenterAndSize(new XYZ(10, 0, 0), width: 0.3, height: 0.15)
        };

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsTrue(hasCollision, "Should detect collision with at least one existing tag");
    }

    [Test]
    public void HasCollision_3DSpace_ConsidersZCoordinate()
    {
        // Arrange: Same XY position but different Z (e.g., different levels)
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0, 0, 10), width: 0.3, height: 0.15) // 10' above
        };

        // Act
        var hasCollision = _detector.HasCollision(candidate, existingTags);

        // Assert
        Assert.IsFalse(hasCollision, "Tags at different Z levels should not collide (different floors)");
    }

    // ========== Buffer Margin Behavior ==========

    [Test]
    public void HasCollisionWithMargin_LargerMargin_DetectsMoreCollisions()
    {
        // Arrange: Tags 0.5' apart (center to center)
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0.5, 0, 0), width: 0.3, height: 0.15)
        };

        // Act: Test with different margins
        var collisionSmallMargin = _detector.HasCollisionWithMargin(candidate, existingTags, bufferMargin: 0.05);
        var collisionLargeMargin = _detector.HasCollisionWithMargin(candidate, existingTags, bufferMargin: 0.3);

        // Assert
        Assert.IsFalse(collisionSmallMargin, "Small margin (0.05') should not detect collision");
        Assert.IsTrue(collisionLargeMargin, "Large margin (0.3') should detect collision");
    }

    [Test]
    public void HasCollisionWithMargin_ZeroMargin_OnlyDetectsActualOverlap()
    {
        // Arrange: Tags touching but not overlapping
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0)); // Bounds: X [-0.15, 0.15]
        var existingTags = new List<BoundingBox>
        {
            BoundingBox.FromCenterAndSize(new XYZ(0.3, 0, 0), width: 0.3, height: 0.15) // Bounds: X [0.15, 0.45]
        };

        // Act
        var hasCollision = _detector.HasCollisionWithMargin(candidate, existingTags, bufferMargin: 0.0);

        // Assert
        Assert.IsFalse(hasCollision, "Zero margin should not detect touching tags");
    }

    // ========== Performance Considerations ==========

    [Test]
    [Category("Performance")]
    public void HasCollision_100ExistingTags_CompletesQuickly()
    {
        // Arrange: 100 existing tags in a grid
        var candidate = CreateCandidateAt(new XYZ(0, 0, 0));
        var existingTags = new List<BoundingBox>();

        for (int i = 0; i < 100; i++)
        {
            existingTags.Add(BoundingBox.FromCenterAndSize(
                new XYZ(i * 5, 0, 0), // 5' spacing
                width: 0.3,
                height: 0.15));
        }

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var hasCollision = _detector.HasCollision(candidate, existingTags);
        stopwatch.Stop();

        // Assert: Should complete in < 1ms (O(n) linear scan)
        Assert.Less(stopwatch.ElapsedMilliseconds, 1,
            $"Collision detection against 100 tags took {stopwatch.ElapsedMilliseconds}ms (should be < 1ms)");

        TestContext.WriteLine($"Collision detection: {existingTags.Count} tags in {stopwatch.ElapsedTicks} ticks");
    }

    // ========== Helper Methods ==========

    /// <summary>
    /// Creates a candidate at the specified location with standard tag dimensions.
    /// </summary>
    private TagPlacementCandidate CreateCandidateAt(XYZ location)
    {
        return TagPlacementCandidate.CreatePreferred(
            elementId: 1001,
            elementCenter: location,
            proposedLocation: location,
            offsetDistance: 0.5);
    }
}
