using RevitAI.Models.Domain;

namespace RevitAI.Services.Interfaces;

/// <summary>
/// Strategy interface for calculating tag placement locations.
/// Enables multiple placement algorithms without modifying core service logic.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// This interface follows the Strategy Pattern, allowing different placement algorithms:
/// - CenterOffsetStrategy: Default (tag above element center)
/// - GridBasedStrategy: Snap to grid points (future)
/// - SmartClusterStrategy: Avoid dense areas (future)
///
/// The strategy is responsible for:
/// 1. Calculating preferred placement (attempt 1)
/// 2. Generating alternative placements (attempts 2-10) when collision detected
/// 3. Deciding when to use leader lines
/// </remarks>
public interface IPlacementStrategy
{
    /// <summary>
    /// Calculates the preferred (first-choice) placement for a tag.
    /// This is attempt #1, typically placing the tag near the element center.
    /// </summary>
    /// <param name="elementId">Identifier for the element being tagged</param>
    /// <param name="elementCenter">The element's bounding box center in 3D space</param>
    /// <param name="elementBounds">The element's bounding box for context</param>
    /// <param name="viewType">The type of view (defaults to FloorPlan for backward compatibility)</param>
    /// <returns>A placement candidate for collision testing</returns>
    TagPlacementCandidate GetPreferredPlacement(
        int elementId,
        XYZ elementCenter,
        BoundingBox elementBounds,
        ViewType viewType = ViewType.FloorPlan);

    /// <summary>
    /// Generates an alternative placement when the preferred location has a collision.
    /// This method is called iteratively (attempts 2-10) until collision-free or max attempts reached.
    /// </summary>
    /// <param name="elementId">Identifier for the element being tagged</param>
    /// <param name="elementCenter">The element's bounding box center in 3D space</param>
    /// <param name="elementBounds">The element's bounding box for context</param>
    /// <param name="attemptNumber">Current attempt number (2-10)</param>
    /// <param name="viewType">The type of view (defaults to FloorPlan for backward compatibility)</param>
    /// <returns>A placement candidate at a different location/angle</returns>
    /// <remarks>
    /// Typical implementation uses radial search:
    /// - Attempt 2-9: Try 8 compass directions (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°)
    /// - Attempt 10: Final attempt with increased distance
    /// </remarks>
    TagPlacementCandidate GetAlternativePlacement(
        int elementId,
        XYZ elementCenter,
        BoundingBox elementBounds,
        int attemptNumber,
        ViewType viewType = ViewType.FloorPlan);
}
