using System.Collections.Generic;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

namespace RevitAI.Services;

/// <summary>
/// Fast collision detection using axis-aligned bounding box intersection.
/// Suitable for most tag placement scenarios (95%+ accuracy).
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// This implementation uses simple 3D bounding box intersection with a buffer margin
/// to detect collisions between tag placements. It's optimized for speed over precision:
///
/// Algorithm:
/// 1. For each existing tag bounds
/// 2. Expand candidate bounds by buffer margin
/// 3. Check if expanded bounds intersect with existing bounds
/// 4. Return true on first collision (early exit)
///
/// Performance: O(n) per check where n = number of existing tags
///
/// Buffer margin (default 0.1' ≈ 30mm):
/// - Prevents tags from being technically non-overlapping but visually too close
/// - Creates professional spacing between tags
/// - Configurable for different projects/standards
///
/// Limitations:
/// - Does not consider text rotation or precise character geometry
/// - Assumes rectangular tag bounds (good enough for most cases)
/// - For complex scenarios, future: PreciseGeometryCollisionDetector
/// </remarks>
public class SimpleBoundingBoxCollisionDetector : ITagCollisionDetector
{
    /// <summary>
    /// Default buffer margin in feet (0.1' ≈ 30mm ≈ 1.2 inches).
    /// This creates visual separation between tags.
    /// </summary>
    public const double DefaultBufferMargin = 0.1;

    private readonly double _bufferMargin;

    /// <summary>
    /// Creates a collision detector with the default buffer margin (0.1').
    /// </summary>
    public SimpleBoundingBoxCollisionDetector()
        : this(DefaultBufferMargin)
    {
    }

    /// <summary>
    /// Creates a collision detector with a custom buffer margin.
    /// </summary>
    /// <param name="bufferMargin">Buffer distance in feet (must be non-negative)</param>
    public SimpleBoundingBoxCollisionDetector(double bufferMargin)
    {
        if (bufferMargin < 0)
            throw new System.ArgumentException("Buffer margin cannot be negative", nameof(bufferMargin));

        _bufferMargin = bufferMargin;
    }

    /// <summary>
    /// Checks if candidate collides with any existing tags using the default buffer margin.
    /// </summary>
    public bool HasCollision(
        TagPlacementCandidate candidate,
        List<BoundingBox> existingTagBounds)
    {
        return HasCollisionWithMargin(candidate, existingTagBounds, _bufferMargin);
    }

    /// <summary>
    /// Checks if candidate collides with any existing tags using a custom buffer margin.
    /// </summary>
    public bool HasCollisionWithMargin(
        TagPlacementCandidate candidate,
        List<BoundingBox> existingTagBounds,
        double bufferMargin)
    {
        var candidateBounds = candidate.EstimatedBounds;

        // Early exit if no existing tags
        if (existingTagBounds.Count == 0)
            return false;

        // Check collision against each existing tag
        foreach (var existingBounds in existingTagBounds)
        {
            if (candidateBounds.IntersectsWithMargin(existingBounds, bufferMargin))
            {
                return true; // Collision detected, early exit
            }
        }

        return false; // No collision
    }

    /// <summary>
    /// Gets all colliding tag bounds for debugging/logging.
    /// Not used in main algorithm but useful for troubleshooting.
    /// </summary>
    public List<BoundingBox> GetCollidingBounds(
        TagPlacementCandidate candidate,
        List<BoundingBox> existingTagBounds)
    {
        var collisions = new List<BoundingBox>();
        var candidateBounds = candidate.EstimatedBounds;

        foreach (var existingBounds in existingTagBounds)
        {
            if (candidateBounds.IntersectsWithMargin(existingBounds, _bufferMargin))
            {
                collisions.Add(existingBounds);
            }
        }

        return collisions;
    }
}
