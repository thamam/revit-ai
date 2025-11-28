using System;
using RevitAI.Services.Interfaces;

#if WINDOWS
using Autodesk.Revit.DB;
#endif

namespace RevitAI.Services;

/// <summary>
/// Production implementation of ITransaction that wraps the Revit Transaction API.
/// Provides thread-safe atomic operations with commit/rollback support.
/// Only compiles on Windows with Revit API references.
/// </summary>
#if WINDOWS
public class TransactionWrapper : ITransaction
{
    private readonly Transaction _transaction;
    private readonly LoggingService? _logger;
    private bool _disposed;

    /// <summary>
    /// Gets the name of the transaction (appears in Revit's undo history).
    /// </summary>
    public string Name => _transaction.GetName();

    /// <summary>
    /// Gets whether the transaction has been started and not yet committed or rolled back.
    /// </summary>
    public bool IsActive => _transaction.GetStatus() == TransactionStatus.Started;

    /// <summary>
    /// Creates a new TransactionWrapper around a Revit Transaction.
    /// </summary>
    /// <param name="document">The Revit Document for the transaction</param>
    /// <param name="transactionName">The name for the transaction (shown in undo history)</param>
    /// <param name="logger">Optional logging service</param>
    public TransactionWrapper(Document document, string transactionName, LoggingService? logger = null)
    {
        if (document == null) throw new ArgumentNullException(nameof(document));
        if (string.IsNullOrWhiteSpace(transactionName)) throw new ArgumentException("Transaction name required", nameof(transactionName));

        _transaction = new Transaction(document, transactionName);
        _logger = logger;
        _disposed = false;
    }

    /// <summary>
    /// Internal constructor for wrapping an existing Transaction.
    /// Used for advanced scenarios where Transaction is created externally.
    /// </summary>
    /// <param name="transaction">The Revit Transaction to wrap</param>
    /// <param name="logger">Optional logging service</param>
    internal TransactionWrapper(Transaction transaction, LoggingService? logger = null)
    {
        _transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
        _logger = logger;
        _disposed = false;
    }

    /// <summary>
    /// Starts the transaction, allowing modifications to the Revit document.
    /// Must be called before any document changes.
    /// </summary>
    public void Start()
    {
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionWrapper));
            }

            TransactionStatus status = _transaction.Start();

            if (status != TransactionStatus.Started)
            {
                throw new InvalidOperationException($"Failed to start transaction: status = {status}");
            }

            _logger?.Debug($"Transaction started: {Name}", "REVIT_TRANSACTION");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to start transaction: {Name}", "REVIT_TRANSACTION", ex);
            throw new InvalidOperationException($"Revit transaction start failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Commits the transaction, making all changes permanent.
    /// Should only be called after successful operations.
    /// </summary>
    public void Commit()
    {
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionWrapper));
            }

            if (!IsActive)
            {
                throw new InvalidOperationException($"Cannot commit inactive transaction: {Name}");
            }

            TransactionStatus status = _transaction.Commit();

            if (status != TransactionStatus.Committed)
            {
                throw new InvalidOperationException($"Failed to commit transaction: status = {status}");
            }

            _logger?.Debug($"Transaction committed: {Name}", "REVIT_TRANSACTION");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to commit transaction: {Name}", "REVIT_TRANSACTION", ex);
            throw new InvalidOperationException($"Revit transaction commit failed: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Rolls back the transaction, discarding all changes.
    /// Should be called when errors occur or user cancels.
    /// </summary>
    public void RollBack()
    {
        try
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(TransactionWrapper));
            }

            if (!IsActive)
            {
                _logger?.Debug($"Transaction already inactive, skipping rollback: {Name}", "REVIT_TRANSACTION");
                return;
            }

            TransactionStatus status = _transaction.RollBack();

            if (status != TransactionStatus.RolledBack)
            {
                _logger?.Warning($"Unexpected rollback status: {status} for transaction: {Name}", "REVIT_TRANSACTION");
            }

            _logger?.Debug($"Transaction rolled back: {Name}", "REVIT_TRANSACTION");
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to rollback transaction: {Name}", "REVIT_TRANSACTION", ex);
            // Don't throw - rollback should be safe even in error scenarios
        }
    }

    /// <summary>
    /// Disposes the transaction, automatically rolling back if still active.
    /// Implements IDisposable for use in using statements.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            // Auto-rollback if transaction is still active
            if (IsActive)
            {
                _logger?.Warning($"Transaction disposed while active, auto-rolling back: {Name}", "REVIT_TRANSACTION");
                RollBack();
            }

            _transaction.Dispose();
            _disposed = true;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Error disposing transaction: {Name}", "REVIT_TRANSACTION", ex);
            // Don't throw in Dispose
        }
    }

    /// <summary>
    /// Gets the underlying Revit Transaction for advanced operations.
    /// Use sparingly - prefer using interface methods for better testability.
    /// </summary>
    public Transaction GetTransaction()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(TransactionWrapper));
        }

        return _transaction;
    }
}
#else
/// <summary>
/// Placeholder class for non-Windows platforms.
/// The real implementation requires Revit API references which are Windows-only.
/// </summary>
public class TransactionWrapper : IDisposable
{
    public TransactionWrapper(object document, string transactionName, object? logger = null)
    {
        throw new PlatformNotSupportedException("TransactionWrapper requires Windows and Revit API");
    }

    public void Dispose()
    {
        // No-op on non-Windows platforms
    }
}
#endif
