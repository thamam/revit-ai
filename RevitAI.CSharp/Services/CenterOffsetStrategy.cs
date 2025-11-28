using System;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

namespace RevitAI.Services;

/// <summary>
/// Default tag placement strategy that places tags above (or beside) element centers.
/// Uses radial search with increasing distance for collision avoidance.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// Algorithm:
/// - Preferred placement: Element center + upward offset (configurable distance)
/// - Alternative placements: Radial search at 8 compass directions (0°, 45°, 90°, ...)
/// - Distance increases with attempt number: 1.0x, 1.125x, 1.25x, ..., 2.0x
/// - Leader lines enabled for alternatives (tag placed away from element)
///
/// Example for offset = 0.5' (6 inches):
/// - Attempt 1: (center.X, center.Y + 0.5', center.Z) - preferred
/// - Attempt 2: (center.X + 0.56', center.Y, center.Z) - 0° angle, 1.125x distance
/// - Attempt 3: (center.X, center.Y + 0.63', center.Z) - 45° angle, 1.25x distance
/// - ...
/// - Attempt 10: (center.X, center.Y + 1.0', center.Z) - final attempt, 2.0x distance
/// </remarks>
public class CenterOffsetStrategy : IPlacementStrategy
{
    private readonly double _baseOffsetDistance;

    // Radial search angles in degrees (8 compass directions)
    private static readonly double[] RadialAngles = { 0, 45, 90, 135, 180, 225, 270, 315 };

    /// <summary>
    /// Creates a placement strategy with the specified offset distance.
    /// </summary>
    /// <param name="offsetDistanceFeet">Base offset from element center in feet (default 0.5' = 6 inches)</param>
    public CenterOffsetStrategy(double offsetDistanceFeet = 0.5)
    {
        if (offsetDistanceFeet <= 0)
            throw new ArgumentException("Offset distance must be positive", nameof(offsetDistanceFeet));

        _baseOffsetDistance = offsetDistanceFeet;
    }

    /// <summary>
    /// Calculates preferred placement: element center + view-specific offset.
    /// </summary>
    public TagPlacementCandidate GetPreferredPlacement(
        int elementId,
        XYZ elementCenter,
        BoundingBox elementBounds,
        ViewType viewType = ViewType.FloorPlan)
    {
        // Calculate view-specific offset direction
        var offset = CalculateOffsetForViewType(viewType, _baseOffsetDistance);
        var proposedLocation = elementCenter + offset;

        return TagPlacementCandidate.CreatePreferred(
            elementId,
            elementCenter,
            proposedLocation,
            _baseOffsetDistance,
            viewType
        );
    }

    /// <summary>
    /// Generates alternative placement using radial search with increasing distance.
    /// </summary>
    public TagPlacementCandidate GetAlternativePlacement(
        int elementId,
        XYZ elementCenter,
        BoundingBox elementBounds,
        int attemptNumber,
        ViewType viewType = ViewType.FloorPlan)
    {
        if (attemptNumber < 2 || attemptNumber > 10)
            throw new ArgumentException("Attempt number must be 2-10 for alternatives", nameof(attemptNumber));

        // Calculate angle: cycle through 8 directions
        int angleIndex = (attemptNumber - 2) % RadialAngles.Length;
        double angle = RadialAngles[angleIndex];

        // Calculate distance multiplier: increases with attempts
        // Attempt 2-9: 1.125x, 1.25x, 1.375x, 1.5x, 1.625x, 1.75x, 1.875x, 2.0x
        // Attempt 10: Final attempt with maximum distance (2.0x)
        double distanceMultiplier = 1.0 + (attemptNumber - 1) * 0.125;
        double adjustedDistance = _baseOffsetDistance * distanceMultiplier;

        return TagPlacementCandidate.CreateAlternative(
            elementId,
            elementCenter,
            angle,
            adjustedDistance,
            attemptNumber,
            viewType
        );
    }

    /// <summary>
    /// Calculates the offset vector for a given view type and distance.
    /// Different view types use different primary offset directions.
    /// </summary>
    /// <param name="viewType">The type of view</param>
    /// <param name="distance">The offset distance in feet</param>
    /// <returns>A 3D offset vector appropriate for the view type</returns>
    private XYZ CalculateOffsetForViewType(ViewType viewType, double distance)
    {
        return viewType switch
        {
            ViewType.FloorPlan => new XYZ(0, distance, 0),        // Upward (Y+)
            ViewType.Elevation => new XYZ(distance, 0, 0),        // Right (X+)
            ViewType.Section => new XYZ(0, 0, distance),          // Forward (Z+) - simplified
            ViewType.ThreeD => new XYZ(distance, distance, 0),    // Diagonal (X+, Y+)
            _ => throw new ArgumentException($"Unsupported view type: {viewType}", nameof(viewType))
        };
    }
}
