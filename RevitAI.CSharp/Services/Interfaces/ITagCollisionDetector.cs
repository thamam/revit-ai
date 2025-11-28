using System.Collections.Generic;
using RevitAI.Models.Domain;

namespace RevitAI.Services.Interfaces;

/// <summary>
/// Interface for detecting collisions between tag placements.
/// Enables different collision detection algorithms (bounding box, precise geometry, etc.).
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// This interface abstracts collision detection logic, allowing:
/// - SimpleBoundingBoxCollisionDetector: Fast axis-aligned box intersection (default)
/// - PreciseGeometryCollisionDetector: Detailed text bounds (future, slower)
/// - SpatialIndexCollisionDetector: Optimized for large datasets with spatial partitioning (future)
///
/// Collision detection is the performance bottleneck (O(n²) worst case), so this
/// abstraction enables optimization without changing the placement algorithm.
/// </remarks>
public interface ITagCollisionDetector
{
    /// <summary>
    /// Checks if a proposed tag placement collides with any existing tags.
    /// </summary>
    /// <param name="candidate">The proposed tag placement to test</param>
    /// <param name="existingTagBounds">Bounding boxes of tags already placed</param>
    /// <returns>True if collision detected, false if placement is safe</returns>
    /// <remarks>
    /// Implementations should include a buffer margin (e.g., 0.1') to prevent
    /// tags from being visually too close even if not technically intersecting.
    /// </remarks>
    bool HasCollision(TagPlacementCandidate candidate, List<BoundingBox> existingTagBounds);

    /// <summary>
    /// Checks if a proposed tag placement collides with any existing tags,
    /// using a custom buffer margin.
    /// </summary>
    /// <param name="candidate">The proposed tag placement to test</param>
    /// <param name="existingTagBounds">Bounding boxes of tags already placed</param>
    /// <param name="bufferMargin">Custom buffer distance in feet (overrides default)</param>
    /// <returns>True if collision detected, false if placement is safe</returns>
    bool HasCollisionWithMargin(
        TagPlacementCandidate candidate,
        List<BoundingBox> existingTagBounds,
        double bufferMargin);
}
