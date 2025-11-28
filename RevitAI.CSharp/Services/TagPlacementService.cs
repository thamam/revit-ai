using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

namespace RevitAI.Services;

/// <summary>
/// Core service for calculating intelligent tag placements with collision avoidance.
/// This is Layer 1 pure business logic with no Revit API dependencies.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// Responsibilities:
/// - Orchestrates the placement algorithm for a batch of elements
/// - Uses IPlacementStrategy to generate candidate locations
/// - Uses ITagCollisionDetector to validate candidates
/// - Tries up to 10 placements per element before marking as failed
/// - Returns PlacementResult with success/failure statistics
///
/// Algorithm:
/// 1. For each element in batch:
///    a. Generate preferred placement candidate
///    b. Test for collision against existing tags
///    c. If collision: try alternatives (attempts 2-10)
///    d. If success: add to result + update collision set
///    e. If all fail: add failed placement to result
/// 2. Return PlacementResult with summary statistics
///
/// Performance target: 500 elements in < 5 seconds
/// Quality target: 95%+ success rate (no collision)
/// </remarks>
public class TagPlacementService
{
    private readonly ITagCollisionDetector _collisionDetector;
    private readonly IPlacementStrategy _placementStrategy;
    private readonly LoggingService? _logger;

    private const int MaxPlacementAttempts = 10;

    /// <summary>
    /// Creates a tag placement service with injected dependencies.
    /// </summary>
    /// <param name="collisionDetector">Service for detecting tag collisions</param>
    /// <param name="placementStrategy">Strategy for generating candidate placements</param>
    /// <param name="logger">Optional logging service (null for tests)</param>
    public TagPlacementService(
        ITagCollisionDetector collisionDetector,
        IPlacementStrategy placementStrategy,
        LoggingService? logger = null)
    {
        _collisionDetector = collisionDetector ?? throw new ArgumentNullException(nameof(collisionDetector));
        _placementStrategy = placementStrategy ?? throw new ArgumentNullException(nameof(placementStrategy));
        _logger = logger;
    }

    /// <summary>
    /// Calculates tag placements for a batch of elements with collision avoidance.
    /// </summary>
    /// <param name="elements">Elements to tag (represented as MockElement POCOs)</param>
    /// <param name="existingTagBounds">Bounding boxes of tags already in the view</param>
    /// <param name="viewType">The type of view (defaults to FloorPlan for backward compatibility)</param>
    /// <returns>Placement result with success/failure details</returns>
    public PlacementResult CalculatePlacements(
        List<MockElement> elements,
        List<BoundingBox> existingTagBounds,
        ViewType viewType = ViewType.FloorPlan)
    {
        // Validate parameters
        if (elements == null) throw new ArgumentNullException(nameof(elements));
        if (existingTagBounds == null) throw new ArgumentNullException(nameof(existingTagBounds));

        var stopwatch = Stopwatch.StartNew();
        var placements = new List<TagPlacement>();

        // Track all tag bounds (existing + newly calculated) to avoid intra-batch collisions
        var allTagBounds = new List<BoundingBox>(existingTagBounds);

        _logger?.Info($"Starting tag placement calculation for {elements.Count} elements in {viewType} view", "TAG_PLACEMENT");

        foreach (var element in elements)
        {
            var placement = CalculateSinglePlacement(element, allTagBounds, viewType);
            placements.Add(placement);

            // If successful, add to collision set for next iterations
            if (placement.IsSuccess)
            {
                allTagBounds.Add(GetBoundsForPlacement(placement));
            }
        }

        stopwatch.Stop();

        var result = PlacementResult.CreatePartialSuccess(placements, stopwatch.ElapsedMilliseconds);

        _logger?.Info(
            $"Placement calculation complete: {result.SummaryMessage} in {result.CalculationTimeMs}ms",
            "TAG_PLACEMENT");

        if (!result.MeetsQualityTarget)
        {
            _logger?.Warning(
                $"Quality target missed: {result.SuccessRate:P1} success rate (target: 95%)",
                "TAG_PLACEMENT");
        }

        return result;
    }

    /// <summary>
    /// Calculates placement for a single element, trying up to 10 attempts.
    /// </summary>
    private TagPlacement CalculateSinglePlacement(MockElement element, List<BoundingBox> existingTagBounds, ViewType viewType)
    {
        // Attempt 1: Try preferred placement
        var preferredCandidate = _placementStrategy.GetPreferredPlacement(
            element.ElementId,
            element.Center,
            element.Bounds,
            viewType);

        if (!_collisionDetector.HasCollision(preferredCandidate, existingTagBounds))
        {
            // Success on first attempt
            return preferredCandidate.ToSuccessfulPlacement();
        }

        // Attempts 2-10: Try alternatives with radial search
        for (int attempt = 2; attempt <= MaxPlacementAttempts; attempt++)
        {
            var alternativeCandidate = _placementStrategy.GetAlternativePlacement(
                element.ElementId,
                element.Center,
                element.Bounds,
                attempt,
                viewType);

            if (!_collisionDetector.HasCollision(alternativeCandidate, existingTagBounds))
            {
                // Success on alternative placement
                if (attempt > 3)
                {
                    _logger?.Debug(
                        $"Element {element.ElementId} required {attempt} attempts to find collision-free placement",
                        "TAG_PLACEMENT");
                }
                return alternativeCandidate.ToSuccessfulPlacement();
            }
        }

        // All attempts failed
        _logger?.Warning(
            $"Failed to place tag for element {element.ElementId} after {MaxPlacementAttempts} attempts",
            "TAG_PLACEMENT");

        return TagPlacement.CreateFailed(
            element.ElementId,
            $"No collision-free placement found after {MaxPlacementAttempts} attempts");
    }

    /// <summary>
    /// Gets the bounding box for a successful placement (for collision tracking).
    /// </summary>
    private BoundingBox GetBoundsForPlacement(TagPlacement placement)
    {
        // Estimate tag bounds at the placed location
        // Typical tag: 0.3' wide x 0.15' tall
        const double TagWidth = 0.3;
        const double TagHeight = 0.15;

        return BoundingBox.FromCenterAndSize(placement.Location, TagWidth, TagHeight);
    }
}

/// <summary>
/// Mock element representation for Layer 1 testing.
/// In Layer 2, this will be replaced with actual Revit Element references.
/// </summary>
public class MockElement
{
    public int ElementId { get; set; }
    public XYZ Center { get; set; }
    public BoundingBox Bounds { get; set; }
    public string Category { get; set; } = "Door";

    public static MockElement CreateDoor(int id, XYZ center)
    {
        return new MockElement
        {
            ElementId = id,
            Center = center,
            Bounds = BoundingBox.FromCenterAndSize(center, 3.0, 0.25), // 3' wide x 3" thick door
            Category = "Door"
        };
    }

    public static MockElement CreateWall(int id, XYZ center)
    {
        return new MockElement
        {
            ElementId = id,
            Center = center,
            Bounds = BoundingBox.FromCenterAndSize(center, 10.0, 0.5), // 10' wide x 6" thick wall
            Category = "Wall"
        };
    }

    public static List<MockElement> CreateGrid(int count, double spacing = 10.0)
    {
        var elements = new List<MockElement>();
        int gridSize = (int)Math.Ceiling(Math.Sqrt(count));

        for (int i = 0; i < count; i++)
        {
            int row = i / gridSize;
            int col = i % gridSize;

            elements.Add(CreateDoor(
                1000 + i,
                new XYZ(col * spacing, row * spacing, 0)));
        }

        return elements;
    }
}

