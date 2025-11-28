using System;

namespace RevitAI.Services.Interfaces;

/// <summary>
/// Abstraction for Revit Transaction API to enable atomic operations.
/// In Layer 2, this maps to Autodesk.Revit.DB.Transaction.
/// </summary>
public interface ITransaction : IDisposable
{
    /// <summary>
    /// Starts the transaction.
    /// </summary>
    void Start();

    /// <summary>
    /// Commits all changes made within the transaction.
    /// </summary>
    void Commit();

    /// <summary>
    /// Rolls back all changes made within the transaction.
    /// </summary>
    void RollBack();

    /// <summary>
    /// Gets the transaction name (for logging/debugging).
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Indicates if the transaction is currently active.
    /// </summary>
    bool IsActive { get; }
}
