namespace RevitAI.Models.Domain;

/// <summary>
/// Represents the type of view in which tags are being placed.
/// Different view types require different tag offset calculations.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence (AC-2.2.5).
///
/// View Types and Offset Directions:
/// - FloorPlan: Tags offset upward (Y+ direction) in 2D XY plane
/// - Elevation: Tags offset rightward (X+ direction) in 2D XZ plane
/// - Section: Tags offset forward (Z+ direction) in 2D plane perpendicular to section line
/// - ThreeD: Tags offset diagonally (X+, Y+ direction) for visibility in 3D space
///
/// Usage:
/// The view type determines which axis/direction is used for the primary tag offset
/// from the element center. This ensures tags remain visible and properly oriented
/// in each view context.
/// </remarks>
public enum ViewType
{
    /// <summary>
    /// Floor plan view (2D XY plane).
    /// Tags are offset upward (Y+ direction).
    /// Most common view type for architectural drawings.
    /// </summary>
    FloorPlan = 0,

    /// <summary>
    /// Elevation view (2D XZ plane).
    /// Tags are offset rightward (X+ direction).
    /// Used for exterior/interior wall elevations.
    /// </summary>
    Elevation = 1,

    /// <summary>
    /// Section view (2D plane perpendicular to section line).
    /// Tags are offset forward (Z+ direction, simplified for MVP).
    /// Used for building sections and wall details.
    /// </summary>
    Section = 2,

    /// <summary>
    /// 3D isometric view.
    /// Tags are offset diagonally (X+, Y+ direction).
    /// Less common for tagging, included for completeness.
    /// </summary>
    ThreeD = 3
}
