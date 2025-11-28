using System;

namespace RevitAI.Models.Domain;

/// <summary>
/// Represents the result of calculating a tag placement for a single element.
/// This is a Layer 1 POCO with no Revit API dependencies, enabling fast unit testing.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
/// This model stores the outcome of the placement algorithm, including:
/// - The target element identifier
/// - The calculated 3D location (XYZ coordinates)
/// - Whether a leader line should be used
/// - Success/failure status with optional failure reason
/// </remarks>
public class TagPlacement
{
    /// <summary>
    /// Unique identifier for the element being tagged.
    /// In Layer 2, this will map to Revit's ElementId.IntegerValue.
    /// </summary>
    public int ElementId { get; set; }

    /// <summary>
    /// The calculated location for the tag in 3D space.
    /// Coordinates are in Revit's internal units (feet).
    /// </summary>
    public XYZ Location { get; set; }

    /// <summary>
    /// Indicates whether a leader line should connect the tag to the element.
    /// Automatically enabled when tag is placed far from element center.
    /// </summary>
    public bool HasLeader { get; set; }

    /// <summary>
    /// Indicates whether the placement was successful (collision-free location found).
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Human-readable explanation if placement failed.
    /// Example: "No collision-free placement found after 10 attempts"
    /// </summary>
    public string? FailureReason { get; set; }

    /// <summary>
    /// Number of placement attempts before success or failure.
    /// Range: 1 (preferred placement succeeded) to 10 (all attempts failed).
    /// </summary>
    public int AttemptsUsed { get; set; }

    /// <summary>
    /// Optional reference to the element's bounding box center for logging/debugging.
    /// </summary>
    public XYZ? ElementCenter { get; set; }

    // Factory Methods for Test Data

    /// <summary>
    /// Creates a successful tag placement at the preferred location.
    /// </summary>
    public static TagPlacement CreateSuccess(int elementId, XYZ location, bool hasLeader = false)
    {
        return new TagPlacement
        {
            ElementId = elementId,
            Location = location,
            HasLeader = hasLeader,
            IsSuccess = true,
            FailureReason = null,
            AttemptsUsed = 1
        };
    }

    /// <summary>
    /// Creates a successful tag placement after multiple attempts.
    /// </summary>
    public static TagPlacement CreateSuccessAfterRetry(
        int elementId,
        XYZ location,
        int attemptsUsed,
        bool hasLeader = true)
    {
        if (attemptsUsed < 1 || attemptsUsed > 10)
            throw new ArgumentException("Attempts must be between 1 and 10", nameof(attemptsUsed));

        return new TagPlacement
        {
            ElementId = elementId,
            Location = location,
            HasLeader = hasLeader,
            IsSuccess = true,
            FailureReason = null,
            AttemptsUsed = attemptsUsed
        };
    }

    /// <summary>
    /// Creates a failed tag placement with a specific reason.
    /// </summary>
    public static TagPlacement CreateFailed(int elementId, string failureReason)
    {
        if (string.IsNullOrWhiteSpace(failureReason))
            throw new ArgumentException("Failure reason cannot be empty", nameof(failureReason));

        return new TagPlacement
        {
            ElementId = elementId,
            Location = XYZ.Zero, // Invalid location for failed placement
            HasLeader = false,
            IsSuccess = false,
            FailureReason = failureReason,
            AttemptsUsed = 10 // Max attempts exhausted
        };
    }

    /// <summary>
    /// Creates a tag placement for testing with all properties specified.
    /// </summary>
    public static TagPlacement CreateForTest(
        int elementId,
        XYZ location,
        bool hasLeader,
        bool isSuccess,
        string? failureReason = null,
        int attemptsUsed = 1,
        XYZ? elementCenter = null)
    {
        return new TagPlacement
        {
            ElementId = elementId,
            Location = location,
            HasLeader = hasLeader,
            IsSuccess = isSuccess,
            FailureReason = failureReason,
            AttemptsUsed = attemptsUsed,
            ElementCenter = elementCenter
        };
    }

    public override string ToString()
    {
        if (IsSuccess)
        {
            return $"TagPlacement[Element={ElementId}, Success, Location={Location}, Leader={HasLeader}, Attempts={AttemptsUsed}]";
        }
        else
        {
            return $"TagPlacement[Element={ElementId}, Failed, Reason={FailureReason}]";
        }
    }
}

/// <summary>
/// Simple 3D point representation using Revit's coordinate system.
/// Layer 1 POCO replacement for Autodesk.Revit.DB.XYZ.
/// Coordinates are in feet (Revit's internal unit).
/// </summary>
public class XYZ
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }

    public XYZ(double x, double y, double z)
    {
        X = x;
        Y = y;
        Z = z;
    }

    public static XYZ Zero => new XYZ(0, 0, 0);

    /// <summary>
    /// Calculates Euclidean distance to another point.
    /// </summary>
    public double DistanceTo(XYZ other)
    {
        double dx = X - other.X;
        double dy = Y - other.Y;
        double dz = Z - other.Z;
        return Math.Sqrt(dx * dx + dy * dy + dz * dz);
    }

    /// <summary>
    /// Adds two XYZ vectors (used for offset calculations).
    /// </summary>
    public static XYZ operator +(XYZ a, XYZ b)
    {
        return new XYZ(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
    }

    /// <summary>
    /// Subtracts two XYZ vectors.
    /// </summary>
    public static XYZ operator -(XYZ a, XYZ b)
    {
        return new XYZ(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
    }

    /// <summary>
    /// Multiplies XYZ by a scalar (used for offset scaling).
    /// </summary>
    public static XYZ operator *(XYZ a, double scale)
    {
        return new XYZ(a.X * scale, a.Y * scale, a.Z * scale);
    }

    /// <summary>
    /// Creates XYZ from polar coordinates (angle in degrees, radius in feet).
    /// Used for radial placement algorithm.
    /// </summary>
    public static XYZ FromPolar(double angleDegrees, double radius)
    {
        double angleRad = angleDegrees * Math.PI / 180.0;
        return new XYZ(
            radius * Math.Cos(angleRad),
            radius * Math.Sin(angleRad),
            0
        );
    }

    public override string ToString()
    {
        return $"({X:F3}, {Y:F3}, {Z:F3})";
    }

    public override bool Equals(object? obj)
    {
        if (obj is XYZ other)
        {
            const double tolerance = 0.0001; // 0.01mm tolerance
            return Math.Abs(X - other.X) < tolerance &&
                   Math.Abs(Y - other.Y) < tolerance &&
                   Math.Abs(Z - other.Z) < tolerance;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y, Z);
    }
}
