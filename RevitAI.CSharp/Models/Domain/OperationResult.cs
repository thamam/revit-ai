using System;
using System.Collections.Generic;

namespace RevitAI.Models.Domain;

/// <summary>
/// Result of a tag creation operation, including success/failure counts and messages.
/// Layer 1 POCO for communicating results from TagCreationService.
/// </summary>
public class OperationResult
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public int CreatedCount { get; set; }
    public int FailedCount { get; set; }
    public List<string> FailureDetails { get; set; } = new();
    public long ExecutionTimeMs { get; set; }

    public static OperationResult Success(string message, int createdCount, int failedCount = 0)
    {
        return new OperationResult
        {
            IsSuccess = true,
            Message = message,
            CreatedCount = createdCount,
            FailedCount = failedCount
        };
    }

    public static OperationResult Failure(string message)
    {
        return new OperationResult
        {
            IsSuccess = false,
            Message = message,
            CreatedCount = 0,
            FailedCount = 0
        };
    }

    public override string ToString()
    {
        return IsSuccess
            ? $"Success: {Message} ({CreatedCount} created, {FailedCount} failed)"
            : $"Failure: {Message}";
    }
}
