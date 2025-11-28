using System;

namespace RevitAI.Models.Domain;

/// <summary>
/// Represents a proposed tag location that needs collision detection validation.
/// This is a Layer 1 POCO used during the placement algorithm to evaluate candidates.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// The placement algorithm works in two phases:
/// 1. Generate candidates (preferred + alternatives)
/// 2. Test each candidate for collisions → convert to TagPlacement
///
/// This model represents phase 1 output, containing the proposed location
/// and estimated bounding box for collision detection.
/// </remarks>
public class TagPlacementCandidate
{
    /// <summary>
    /// Identifier for the element being tagged.
    /// </summary>
    public int ElementId { get; set; }

    /// <summary>
    /// Proposed location for the tag in 3D space (feet units).
    /// </summary>
    public XYZ ProposedLocation { get; set; }

    /// <summary>
    /// Whether this candidate requires a leader line.
    /// Typically true for alternative placements far from element center.
    /// </summary>
    public bool RequiresLeader { get; set; }

    /// <summary>
    /// Estimated bounding box for the tag at this location.
    /// Used for collision detection before creating the actual tag.
    /// </summary>
    public BoundingBox EstimatedBounds { get; set; }

    /// <summary>
    /// Attempt number (1 = preferred placement, 2-10 = alternatives).
    /// </summary>
    public int AttemptNumber { get; set; }

    /// <summary>
    /// The element's center point (for distance calculations).
    /// </summary>
    public XYZ ElementCenter { get; set; }

    /// <summary>
    /// Optional angle in degrees for radial placements (0°, 45°, 90°, etc.).
    /// Null for preferred center placement.
    /// </summary>
    public double? PlacementAngle { get; set; }

    /// <summary>
    /// Offset distance from element center in feet.
    /// </summary>
    public double OffsetDistance { get; set; }

    /// <summary>
    /// The type of view in which this tag is being placed.
    /// Determines the offset direction (Y+ for floor plans, X+ for elevations, etc.).
    /// </summary>
    public ViewType ViewType { get; set; }

    // Factory Methods

    /// <summary>
    /// Creates a preferred placement candidate (attempt 1, center offset).
    /// </summary>
    /// <param name="viewType">The type of view (defaults to FloorPlan for backward compatibility)</param>
    public static TagPlacementCandidate CreatePreferred(
        int elementId,
        XYZ elementCenter,
        XYZ proposedLocation,
        double offsetDistance,
        ViewType viewType = ViewType.FloorPlan)
    {
        return new TagPlacementCandidate
        {
            ElementId = elementId,
            ProposedLocation = proposedLocation,
            RequiresLeader = false,
            EstimatedBounds = EstimateTagBounds(proposedLocation),
            AttemptNumber = 1,
            ElementCenter = elementCenter,
            PlacementAngle = null,
            OffsetDistance = offsetDistance,
            ViewType = viewType
        };
    }

    /// <summary>
    /// Creates an alternative placement candidate with radial offset.
    /// </summary>
    /// <param name="viewType">The type of view (defaults to FloorPlan for backward compatibility)</param>
    public static TagPlacementCandidate CreateAlternative(
        int elementId,
        XYZ elementCenter,
        double angleDegrees,
        double offsetDistance,
        int attemptNumber,
        ViewType viewType = ViewType.FloorPlan)
    {
        if (attemptNumber < 2 || attemptNumber > 10)
            throw new ArgumentException("Alternative attempts must be 2-10", nameof(attemptNumber));

        var radialOffset = XYZ.FromPolar(angleDegrees, offsetDistance);
        var proposedLocation = elementCenter + radialOffset;

        return new TagPlacementCandidate
        {
            ElementId = elementId,
            ProposedLocation = proposedLocation,
            RequiresLeader = true, // Always use leader for alternatives
            EstimatedBounds = EstimateTagBounds(proposedLocation),
            AttemptNumber = attemptNumber,
            ElementCenter = elementCenter,
            PlacementAngle = angleDegrees,
            OffsetDistance = offsetDistance,
            ViewType = viewType
        };
    }

    /// <summary>
    /// Estimates the bounding box for a tag at the given location.
    /// Uses typical tag dimensions: 0.3' wide x 0.15' tall.
    /// </summary>
    private static BoundingBox EstimateTagBounds(XYZ center)
    {
        const double TagWidth = 0.3;  // feet (approx 3.6 inches)
        const double TagHeight = 0.15; // feet (approx 1.8 inches)

        return new BoundingBox
        {
            Min = new XYZ(
                center.X - TagWidth / 2,
                center.Y - TagHeight / 2,
                center.Z
            ),
            Max = new XYZ(
                center.X + TagWidth / 2,
                center.Y + TagHeight / 2,
                center.Z
            )
        };
    }

    /// <summary>
    /// Converts this candidate to a successful TagPlacement.
    /// Called when collision detection passes.
    /// </summary>
    public TagPlacement ToSuccessfulPlacement()
    {
        return new TagPlacement
        {
            ElementId = ElementId,
            Location = ProposedLocation,
            HasLeader = RequiresLeader,
            IsSuccess = true,
            FailureReason = null,
            AttemptsUsed = AttemptNumber,
            ElementCenter = ElementCenter
        };
    }

    public override string ToString()
    {
        if (PlacementAngle.HasValue)
        {
            return $"Candidate[Element={ElementId}, Attempt={AttemptNumber}, Angle={PlacementAngle:F0}°, Distance={OffsetDistance:F2}']";
        }
        else
        {
            return $"Candidate[Element={ElementId}, Preferred, Offset={OffsetDistance:F2}']";
        }
    }
}

/// <summary>
/// Axis-aligned bounding box for collision detection.
/// Layer 1 POCO replacement for Autodesk.Revit.DB.BoundingBoxXYZ.
/// </summary>
public class BoundingBox
{
    public XYZ Min { get; set; }
    public XYZ Max { get; set; }

    public BoundingBox()
    {
        Min = XYZ.Zero;
        Max = XYZ.Zero;
    }

    public BoundingBox(XYZ min, XYZ max)
    {
        Min = min;
        Max = max;
    }

    /// <summary>
    /// Width of the bounding box (X dimension).
    /// </summary>
    public double Width => Max.X - Min.X;

    /// <summary>
    /// Height of the bounding box (Y dimension).
    /// </summary>
    public double Height => Max.Y - Min.Y;

    /// <summary>
    /// Depth of the bounding box (Z dimension).
    /// </summary>
    public double Depth => Max.Z - Min.Z;

    /// <summary>
    /// Center point of the bounding box.
    /// </summary>
    public XYZ Center => new XYZ(
        (Min.X + Max.X) / 2,
        (Min.Y + Max.Y) / 2,
        (Min.Z + Max.Z) / 2
    );

    /// <summary>
    /// Checks if this bounding box intersects with another.
    /// Uses <= and >= to consider touching edges as intersecting.
    /// </summary>
    public bool Intersects(BoundingBox other)
    {
        return !(Max.X <= other.Min.X || Min.X >= other.Max.X ||
                 Max.Y <= other.Min.Y || Min.Y >= other.Max.Y ||
                 Max.Z <= other.Min.Z || Min.Z >= other.Max.Z);
    }

    /// <summary>
    /// Checks if this bounding box intersects with another, including a buffer margin.
    /// </summary>
    public bool IntersectsWithMargin(BoundingBox other, double margin)
    {
        var expandedThis = new BoundingBox(
            new XYZ(Min.X - margin, Min.Y - margin, Min.Z - margin),
            new XYZ(Max.X + margin, Max.Y + margin, Max.Z + margin)
        );

        return expandedThis.Intersects(other);
    }

    /// <summary>
    /// Creates a bounding box from center point and dimensions.
    /// </summary>
    public static BoundingBox FromCenterAndSize(XYZ center, double width, double height, double depth = 0)
    {
        return new BoundingBox(
            new XYZ(center.X - width / 2, center.Y - height / 2, center.Z - depth / 2),
            new XYZ(center.X + width / 2, center.Y + height / 2, center.Z + depth / 2)
        );
    }

    public override string ToString()
    {
        return $"BBox[{Width:F2}' x {Height:F2}' @ {Center}]";
    }
}
