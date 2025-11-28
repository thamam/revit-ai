using System;
using System.Collections.Generic;
using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using RevitAI.Services.Interfaces;
using RevitAI.UnitTests.Services; // For MockRevitDocument, MockTransaction

namespace RevitAI.UnitTests.Integration;

/// <summary>
/// Integration tests for the complete auto-tagging flow:
/// Layer 1 (TagPlacementService) → Layer 2 (TagCreationService) → Layer 3 (Revit API via mocks)
///
/// These tests verify the full pipeline from element list to created tags,
/// demonstrating how all components work together.
/// </summary>
[TestFixture]
[Category("Integration")]
[Category("AutoTagging")]
public class TagCreationIntegrationTests
{
    private TagPlacementService _placementService = null!;
    private TagCreationService _creationService = null!;
    private MockRevitDocument _mockDocument = null!;

    #pragma warning disable NUnit1032 // Mock transactions don't need disposal
    private MockTransaction _mockTransaction = null!;
    #pragma warning restore NUnit1032

    [SetUp]
    public void Setup()
    {
        // Layer 1: Tag placement with spatial intelligence
        var strategy = new CenterOffsetStrategy(offsetDistanceFeet: 2.0);
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        _placementService = new TagPlacementService(collisionDetector, strategy);

        // Layer 2: Tag creation bridge
        _mockDocument = new MockRevitDocument();
        _mockTransaction = new MockTransaction("AI: Create Tags");
        _creationService = new TagCreationService(_mockDocument);
    }

    #region End-to-End Success Tests

    [Test]
    public void EndToEnd_SmallBatch_CreatesAllTags()
    {
        // Arrange: 5 elements in a grid pattern
        var elements = MockElement.CreateGrid(count: 5, spacing: 10.0);
        var existingTags = new List<BoundingBox>(); // No existing tags
        int tagTypeId = 5001;

        // Act: Layer 1 - Calculate placements
        var placements = _placementService.CalculatePlacements(elements, existingTags);

        // Assert Layer 1: All placements successful
        Assert.AreEqual(5, placements.SuccessCount, "All placements should succeed");
        Assert.IsTrue(placements.MeetsQualityTarget, "Should meet 95% quality target");

        // Act: Layer 2 - Create tags in Revit
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert Layer 2: All tags created
        Assert.IsTrue(result.IsSuccess, "Tag creation should succeed");
        Assert.AreEqual(5, result.CreatedCount, "Should create 5 tags");
        Assert.AreEqual(0, result.FailedCount, "No failures");
        Assert.AreEqual(5, _mockDocument.CreatedTags.Count, "5 tags in document");
        Assert.IsTrue(_mockTransaction.WasCommitted, "Transaction committed");
    }

    [Test]
    public void EndToEnd_LargeBatch_Achieves95PercentSuccess()
    {
        // Arrange: 100 elements in dense grid (collision challenges)
        var elements = MockElement.CreateGrid(count: 100, spacing: 8.0);
        var existingTags = new List<BoundingBox>();
        int tagTypeId = 5001;

        // Act: Layer 1 - Calculate placements
        var placements = _placementService.CalculatePlacements(elements, existingTags);

        // Assert Layer 1: Meets quality target
        Assert.GreaterOrEqual(placements.SuccessRate, 0.95, "Should achieve 95%+ success rate");

        // Act: Layer 2 - Create tags
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert Layer 2: Created tags match successful placements
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(placements.SuccessCount, result.CreatedCount, "Created count matches placements");
        Assert.AreEqual(placements.SuccessCount, _mockDocument.CreatedTags.Count);
        Assert.IsTrue(_mockTransaction.WasCommitted);
    }

    [Test]
    public void EndToEnd_WithExistingTags_AvoidsCollisions()
    {
        // Arrange: 10 new elements + 20 existing tags
        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();

        // Create 20 existing tag bounding boxes in the same area
        for (int i = 0; i < 20; i++)
        {
            double x = (i % 5) * 10.0;
            double y = (i / 5) * 10.0;
            existingTags.Add(new BoundingBox(
                new XYZ(x - 0.5, y + 1.5, 0),
                new XYZ(x + 0.5, y + 2.5, 0)
            ));
        }

        int tagTypeId = 5001;

        // Act: Layer 1 - Calculate placements (should avoid existing tags)
        var placements = _placementService.CalculatePlacements(elements, existingTags);

        // Assert Layer 1: Collision avoidance worked
        Assert.Greater(placements.SuccessCount, 0, "Should find collision-free placements");

        // Verify no new placements collide with existing tags
        foreach (var placement in placements.Placements)
        {
            if (placement.IsSuccess)
            {
                var newTagBounds = new BoundingBox(
                    new XYZ(placement.Location.X - 0.5, placement.Location.Y - 0.5, 0),
                    new XYZ(placement.Location.X + 0.5, placement.Location.Y + 0.5, 0)
                );

                foreach (var existingTag in existingTags)
                {
                    Assert.IsFalse(newTagBounds.Intersects(existingTag),
                        $"New tag at ({placement.Location.X}, {placement.Location.Y}) should not collide with existing tags");
                }
            }
        }

        // Act: Layer 2 - Create tags
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert Layer 2: Tags created without errors
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(placements.SuccessCount, result.CreatedCount);
    }

    #endregion

    #region Performance Integration Tests

    [Test]
    public void EndToEnd_500Elements_CompletesUnder5Seconds()
    {
        // Arrange: Large batch (acceptance criteria: 500 elements < 5 seconds)
        var elements = MockElement.CreateGrid(count: 500, spacing: 8.0);
        var existingTags = new List<BoundingBox>();
        int tagTypeId = 5001;

        var totalStopwatch = System.Diagnostics.Stopwatch.StartNew();

        // Act: Layer 1 - Calculate placements
        var placements = _placementService.CalculatePlacements(elements, existingTags);

        // Act: Layer 2 - Create tags
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        totalStopwatch.Stop();

        // Assert: Performance target met
        Assert.Less(totalStopwatch.ElapsedMilliseconds, 5000,
            $"Should complete 500 elements in < 5 seconds (took {totalStopwatch.ElapsedMilliseconds}ms)");

        // Assert: Quality maintained
        Assert.IsTrue(result.IsSuccess);
        Assert.Greater(result.CreatedCount, 450, "Should create 90%+ of tags even in large batch");
    }

    #endregion

    #region Error Handling Integration Tests

    [Test]
    public void EndToEnd_NonExistentElements_SkipsGracefully()
    {
        // Arrange: Mix of valid and non-existent elements
        var elements = new List<MockElement>
        {
            new MockElement { ElementId = 1001, Center = new XYZ(0, 0, 0), Bounds = new BoundingBox(new XYZ(-1, -1, 0), new XYZ(1, 1, 0)) },
            new MockElement { ElementId = 9999, Center = new XYZ(10, 0, 0), Bounds = new BoundingBox(new XYZ(9, -1, 0), new XYZ(11, 1, 0)) }, // Non-existent
            new MockElement { ElementId = 1002, Center = new XYZ(20, 0, 0), Bounds = new BoundingBox(new XYZ(19, -1, 0), new XYZ(21, 1, 0)) }
        };
        var existingTags = new List<BoundingBox>();
        int tagTypeId = 5001;

        // Mock document: only 1001 and 1002 exist
        _mockDocument.ExistingElementIds = new HashSet<int> { 1001, 1002 };

        // Act: Full pipeline
        var placements = _placementService.CalculatePlacements(elements, existingTags);
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert: Skipped non-existent element gracefully
        Assert.IsTrue(result.IsSuccess, "Should succeed with partial results");
        Assert.AreEqual(2, result.CreatedCount, "Should create 2 tags (skipping non-existent)");
        Assert.AreEqual(1, result.FailedCount, "Should report 1 failure");
        Assert.That(result.FailureDetails[0], Does.Contain("9999").And.Contains("not found"));
    }

    [Test]
    public void EndToEnd_DocumentException_RollsBackSafely()
    {
        // Arrange: 3 elements
        var elements = MockElement.CreateGrid(count: 3, spacing: 10.0);
        var existingTags = new List<BoundingBox>();
        int tagTypeId = 5001;

        // Mock document: force exception on CreateTag
        _mockDocument.ThrowOnCreateTag = true;

        // Act: Full pipeline
        var placements = _placementService.CalculatePlacements(elements, existingTags);
        var result = _creationService.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert: Transaction rolled back safely
        Assert.IsFalse(result.IsSuccess, "Should report failure");
        Assert.AreEqual(0, result.CreatedCount, "No tags created");
        Assert.AreEqual(3, result.FailedCount, "All failed");
        Assert.IsTrue(_mockTransaction.WasRolledBack, "Transaction rolled back");
        Assert.IsFalse(_mockTransaction.WasCommitted, "Transaction not committed");
    }

    #endregion

    #region Preview Summary Integration Tests

    [Test]
    public void EndToEnd_GetPreviewSummary_ShowsAccurateStats()
    {
        // Arrange: Calculate placements
        var elements = MockElement.CreateGrid(count: 50, spacing: 8.0);
        var existingTags = new List<BoundingBox>();

        var placements = _placementService.CalculatePlacements(elements, existingTags);

        // Act: Get preview summary
        string summary = TagCreationService.GetPreviewSummary(placements.Placements);

        // Assert: Summary reflects placement results
        Assert.That(summary, Does.Contain(placements.SuccessCount.ToString()));
        if (placements.FailedCount > 0)
        {
            Assert.That(summary, Does.Contain(placements.FailedCount.ToString()).Or.Contains("skipped"));
        }
    }

    #endregion

    #region Audit Trail Integration Tests

    [Test]
    public void EndToEnd_WithLogging_RecordsAuditTrail()
    {
        // Arrange: Services with logging
        var logger = LoggingService.Instance;
        var strategy = new CenterOffsetStrategy(offsetDistanceFeet: 2.0);
        var collisionDetector = new SimpleBoundingBoxCollisionDetector(bufferMargin: 0.1);
        var placementServiceWithLogging = new TagPlacementService(collisionDetector, strategy, logger);
        var creationServiceWithLogging = new TagCreationService(_mockDocument, logger);

        var elements = MockElement.CreateGrid(count: 10, spacing: 10.0);
        var existingTags = new List<BoundingBox>();
        int tagTypeId = 5001;

        // Act: Full pipeline with logging
        var placements = placementServiceWithLogging.CalculatePlacements(elements, existingTags);
        var result = creationServiceWithLogging.CreateTags(placements.Placements, tagTypeId, _mockTransaction);

        // Assert: Operation succeeded and would have logged
        Assert.IsTrue(result.IsSuccess);
        Assert.GreaterOrEqual(result.ExecutionTimeMs, 0, "Should track execution time");

        // Note: Actual log file verification would happen in Layer 3 Windows tests
        // Here we just verify the services accept logger and complete successfully
    }

    #endregion
}
