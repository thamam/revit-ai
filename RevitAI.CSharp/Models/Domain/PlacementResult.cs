using System;
using System.Collections.Generic;
using System.Linq;

namespace RevitAI.Models.Domain;

/// <summary>
/// Represents the summary result of a batch tag placement operation.
/// This is a Layer 1 POCO used to communicate results from TagPlacementService.
/// </summary>
/// <remarks>
/// Part of Story 2.2: Tag Placement Engine with Spatial Intelligence.
///
/// After processing a batch of elements, this result contains:
/// - All individual tag placements (success and failed)
/// - Summary statistics (success rate, total count)
/// - Performance metrics (calculation time)
/// - Actionable error messages for failed placements
///
/// This enables the UI (Story 2.3) to display detailed preview information
/// and the audit log to record operation outcomes.
/// </remarks>
public class PlacementResult
{
    /// <summary>
    /// All tag placements calculated (both successful and failed).
    /// </summary>
    public List<TagPlacement> Placements { get; set; } = new();

    /// <summary>
    /// Number of successfully placed tags (collision-free).
    /// </summary>
    public int SuccessCount => Placements.Count(p => p.IsSuccess);

    /// <summary>
    /// Number of failed placements (collision couldn't be avoided).
    /// </summary>
    public int FailedCount => Placements.Count(p => !p.IsSuccess);

    /// <summary>
    /// Total number of elements processed.
    /// </summary>
    public int TotalCount => Placements.Count;

    /// <summary>
    /// Success rate as a percentage (0.0 to 1.0).
    /// Example: 0.95 = 95% success rate.
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (double)SuccessCount / TotalCount : 0;

    /// <summary>
    /// Time taken to calculate all placements (milliseconds).
    /// </summary>
    public long CalculationTimeMs { get; set; }

    /// <summary>
    /// Average number of attempts needed per successful placement.
    /// Lower is better (1.0 = all preferred placements succeeded).
    /// </summary>
    public double AverageAttemptsPerSuccess
    {
        get
        {
            var successfulPlacements = Placements.Where(p => p.IsSuccess).ToList();
            return successfulPlacements.Any()
                ? successfulPlacements.Average(p => p.AttemptsUsed)
                : 0;
        }
    }

    /// <summary>
    /// Indicates if the operation met the acceptance criteria (95%+ success).
    /// </summary>
    public bool MeetsQualityTarget => SuccessRate >= 0.95;

    /// <summary>
    /// Human-readable summary message.
    /// Example: "Successfully placed 95 of 100 tags (95.0% success rate)"
    /// </summary>
    public string SummaryMessage
    {
        get
        {
            if (TotalCount == 0)
                return "No elements to tag";

            if (FailedCount == 0)
                return $"Successfully placed all {SuccessCount} tags";

            return $"Placed {SuccessCount} of {TotalCount} tags ({SuccessRate:P1} success rate, {FailedCount} failed)";
        }
    }

    /// <summary>
    /// Detailed messages for failed placements (for logging/debugging).
    /// </summary>
    public List<string> FailureMessages
    {
        get
        {
            return Placements
                .Where(p => !p.IsSuccess)
                .Select(p => $"Element {p.ElementId}: {p.FailureReason}")
                .ToList();
        }
    }

    // Factory Methods

    /// <summary>
    /// Creates an empty result (no elements to process).
    /// </summary>
    public static PlacementResult CreateEmpty()
    {
        return new PlacementResult
        {
            Placements = new List<TagPlacement>(),
            CalculationTimeMs = 0
        };
    }

    /// <summary>
    /// Creates a result with all successful placements.
    /// </summary>
    public static PlacementResult CreateAllSuccess(List<TagPlacement> placements, long calculationTimeMs)
    {
        if (placements.Any(p => !p.IsSuccess))
            throw new ArgumentException("All placements must be successful", nameof(placements));

        return new PlacementResult
        {
            Placements = placements,
            CalculationTimeMs = calculationTimeMs
        };
    }

    /// <summary>
    /// Creates a result with mixed success/failure.
    /// </summary>
    public static PlacementResult CreatePartialSuccess(
        List<TagPlacement> placements,
        long calculationTimeMs)
    {
        return new PlacementResult
        {
            Placements = placements,
            CalculationTimeMs = calculationTimeMs
        };
    }

    /// <summary>
    /// Creates a test result with specified success/failure counts.
    /// </summary>
    public static PlacementResult CreateForTest(int successCount, int failedCount)
    {
        var placements = new List<TagPlacement>();

        // Add successful placements
        for (int i = 0; i < successCount; i++)
        {
            placements.Add(TagPlacement.CreateSuccess(
                elementId: 1000 + i,
                location: new XYZ(i * 10, 0, 0)
            ));
        }

        // Add failed placements
        for (int i = 0; i < failedCount; i++)
        {
            placements.Add(TagPlacement.CreateFailed(
                elementId: 2000 + i,
                failureReason: "No collision-free placement found after 10 attempts"
            ));
        }

        return new PlacementResult
        {
            Placements = placements,
            CalculationTimeMs = 100 // Mock value
        };
    }

    /// <summary>
    /// Adds a placement to the result.
    /// </summary>
    public void AddPlacement(TagPlacement placement)
    {
        Placements.Add(placement);
    }

    /// <summary>
    /// Gets statistics for logging/reporting.
    /// </summary>
    public PlacementStatistics GetStatistics()
    {
        return new PlacementStatistics
        {
            TotalElements = TotalCount,
            SuccessfulPlacements = SuccessCount,
            FailedPlacements = FailedCount,
            SuccessRate = SuccessRate,
            AverageAttempts = AverageAttemptsPerSuccess,
            CalculationTimeMs = CalculationTimeMs,
            MeetsQualityTarget = MeetsQualityTarget
        };
    }

    public override string ToString()
    {
        return SummaryMessage + $" (calculated in {CalculationTimeMs}ms)";
    }
}

/// <summary>
/// Statistics summary for placement results.
/// Used for logging and reporting.
/// </summary>
public class PlacementStatistics
{
    public int TotalElements { get; set; }
    public int SuccessfulPlacements { get; set; }
    public int FailedPlacements { get; set; }
    public double SuccessRate { get; set; }
    public double AverageAttempts { get; set; }
    public long CalculationTimeMs { get; set; }
    public bool MeetsQualityTarget { get; set; }

    public override string ToString()
    {
        return $"Placement Stats: {SuccessfulPlacements}/{TotalElements} success " +
               $"({SuccessRate:P1}), Avg attempts: {AverageAttempts:F1}, " +
               $"Time: {CalculationTimeMs}ms, Quality target: {(MeetsQualityTarget ? "MET" : "MISSED")}";
    }
}
