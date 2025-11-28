using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

namespace RevitAI.Services;

/// <summary>
/// Layer 2 service that creates Revit tags from Layer 1 placement calculations.
/// Bridges pure logic (TagPlacementService) to Revit API via IRevitDocument abstraction.
/// </summary>
public class TagCreationService
{
    private readonly IRevitDocument _document;
    private readonly LoggingService? _logger;

    /// <summary>
    /// Creates a new TagCreationService with the specified Revit document wrapper.
    /// </summary>
    /// <param name="document">Revit document abstraction for creating tags</param>
    /// <param name="logger">Optional logging service for audit trail</param>
    public TagCreationService(IRevitDocument document, LoggingService? logger = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _logger = logger;
    }

    /// <summary>
    /// Creates tags in Revit from calculated placements using atomic transactions.
    /// Handles partial success (some tags succeed, some fail) and provides detailed audit trail.
    /// </summary>
    /// <param name="placements">List of calculated tag placements from TagPlacementService</param>
    /// <param name="tagTypeId">Revit tag type ID to use</param>
    /// <param name="transaction">Transaction for atomic commit/rollback</param>
    /// <returns>Operation result with success/failure counts and execution time</returns>
    public OperationResult CreateTags(
        List<TagPlacement> placements,
        int tagTypeId,
        ITransaction transaction)
    {
        // Validation
        if (placements == null) throw new ArgumentNullException(nameof(placements));
        if (transaction == null) throw new ArgumentNullException(nameof(transaction));
        if (tagTypeId <= 0) throw new ArgumentException("Tag type ID must be positive", nameof(tagTypeId));

        _logger?.Info($"Starting tag creation: {placements.Count} placements", "TAG_CREATION");

        var stopwatch = Stopwatch.StartNew();
        var createdTags = new List<int>();
        var failureDetails = new List<string>();
        int successCount = 0;
        int failedCount = 0;

        try
        {
            // Start transaction
            transaction.Start();
            _logger?.Debug($"Transaction started: {transaction.Name}", "TAG_CREATION");

            // Get active view for tag placement
            int viewId = _document.GetActiveViewId();
            _logger?.Debug($"Active view ID: {viewId}", "TAG_CREATION");

            // Process each placement
            foreach (var placement in placements)
            {
                // Skip placements that already failed in Layer 1 (collision avoidance)
                if (!placement.IsSuccess)
                {
                    failedCount++;
                    string reason = $"Element {placement.ElementId}: {placement.FailureReason}";
                    failureDetails.Add(reason);
                    _logger?.Warning(reason, "TAG_CREATION");
                    continue;
                }

                // Validate element exists
                if (!_document.ElementExists(placement.ElementId))
                {
                    failedCount++;
                    string reason = $"Element {placement.ElementId}: Element not found in document";
                    failureDetails.Add(reason);
                    _logger?.Warning(reason, "TAG_CREATION");
                    continue;
                }

                // Create tag in Revit
                try
                {
                    int tagId = _document.CreateTag(
                        tagTypeId: tagTypeId,
                        viewId: viewId,
                        elementId: placement.ElementId,
                        addLeader: placement.HasLeader,
                        location: placement.Location
                    );

                    createdTags.Add(tagId);
                    successCount++;
                    _logger?.Debug($"Created tag {tagId} for element {placement.ElementId}", "TAG_CREATION");
                }
                catch (Exception ex)
                {
                    failedCount++;
                    string reason = $"Element {placement.ElementId}: {ex.Message}";
                    failureDetails.Add(reason);
                    _logger?.Error($"Failed to create tag for element {placement.ElementId}", "TAG_CREATION", ex);
                }
            }

            // Commit transaction if any tags were created
            if (successCount > 0)
            {
                transaction.Commit();
                _logger?.Info($"Transaction committed: {successCount} tags created", "TAG_CREATION");
            }
            else
            {
                transaction.RollBack();
                _logger?.Warning("Transaction rolled back: no tags created", "TAG_CREATION");
            }

            stopwatch.Stop();

            // Build result
            if (successCount == placements.Count)
            {
                _logger?.Info($"Tag creation completed: {successCount}/{placements.Count} succeeded in {stopwatch.ElapsedMilliseconds}ms", "TAG_CREATION");
                var result = OperationResult.Success(
                    $"Successfully created {successCount} tags",
                    successCount,
                    failedCount: 0
                );
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
            else if (successCount > 0)
            {
                _logger?.Warning($"Tag creation partial: {successCount}/{placements.Count} succeeded, {failedCount} failed", "TAG_CREATION");
                var result = OperationResult.Success(
                    $"Created {successCount} tags, {failedCount} failed",
                    successCount,
                    failedCount
                );
                result.FailureDetails = failureDetails;
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
            else
            {
                _logger?.Error($"Tag creation failed: 0/{placements.Count} succeeded", "TAG_CREATION");
                var result = OperationResult.Failure($"Failed to create any tags: {string.Join("; ", failureDetails.Take(3))}");
                result.FailedCount = failedCount;
                result.FailureDetails = failureDetails;
                result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
                return result;
            }
        }
        catch (Exception ex)
        {
            // Transaction error - rollback and report failure
            try
            {
                if (transaction.IsActive)
                {
                    transaction.RollBack();
                    _logger?.Warning("Transaction rolled back due to error", "TAG_CREATION");
                }
            }
            catch (Exception rollbackEx)
            {
                _logger?.Error("Failed to rollback transaction", "TAG_CREATION", rollbackEx);
            }

            stopwatch.Stop();
            _logger?.Error($"Tag creation failed with exception after {stopwatch.ElapsedMilliseconds}ms", "TAG_CREATION", ex);

            var result = OperationResult.Failure($"Tag creation failed: {ex.Message}");
            result.ExecutionTimeMs = stopwatch.ElapsedMilliseconds;
            return result;
        }
    }

    /// <summary>
    /// Validates that the specified tag type exists in the document.
    /// </summary>
    /// <param name="tagTypeId">Tag type ID to validate</param>
    /// <returns>True if tag type exists, false otherwise</returns>
    public bool ValidateTagType(int tagTypeId)
    {
        if (tagTypeId <= 0) return false;

        try
        {
            return _document.ElementExists(tagTypeId);
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Failed to validate tag type {tagTypeId}: {ex.Message}", "TAG_CREATION");
            return false;
        }
    }

    /// <summary>
    /// Gets summary statistics for tag creation operation.
    /// Helper method for preview dialog display.
    /// </summary>
    /// <param name="placements">List of calculated placements</param>
    /// <returns>Summary string for user display</returns>
    public static string GetPreviewSummary(List<TagPlacement> placements)
    {
        if (placements == null || placements.Count == 0)
        {
            return "No tags to create";
        }

        int successfulPlacements = placements.Count(p => p.IsSuccess);
        int failedPlacements = placements.Count - successfulPlacements;

        if (failedPlacements == 0)
        {
            return $"Ready to create {successfulPlacements} tags (100% collision-free)";
        }
        else
        {
            double successRate = (double)successfulPlacements / placements.Count;
            return $"Ready to create {successfulPlacements} tags ({successRate:P1} collision-free), {failedPlacements} skipped";
        }
    }
}
