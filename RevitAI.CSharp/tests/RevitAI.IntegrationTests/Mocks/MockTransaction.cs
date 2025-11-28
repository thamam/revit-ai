using System;
using RevitAI.Services.Interfaces;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock implementation of ITransaction for integration testing.
    /// Tracks transaction lifecycle (Start, Commit, RollBack) and simulates failures.
    /// </summary>
    public class MockTransaction : ITransaction
    {
        private bool _isStarted;
        private bool _isCommitted;
        private bool _isRolledBack;
        private bool _isDisposed;
        private Exception _exceptionToThrowOnCommit;

        public string Name { get; }
        public bool IsActive => _isStarted && !_isCommitted && !_isRolledBack;

        /// <summary>
        /// Indicates if Start() was called.
        /// </summary>
        public bool WasStarted => _isStarted;

        /// <summary>
        /// Indicates if Commit() was called successfully.
        /// </summary>
        public bool Committed => _isCommitted;

        /// <summary>
        /// Indicates if RollBack() was called.
        /// </summary>
        public bool RolledBack => _isRolledBack;

        /// <summary>
        /// Indicates if Dispose() was called.
        /// </summary>
        public bool Disposed => _isDisposed;

        public MockTransaction(string name)
        {
            Name = name ?? "Mock Transaction";
        }

        /// <summary>
        /// Configure the mock to throw an exception on Commit().
        /// </summary>
        public void SetExceptionOnCommit(Exception exception)
        {
            _exceptionToThrowOnCommit = exception;
        }

        public void Start()
        {
            if (_isStarted)
            {
                throw new InvalidOperationException("Transaction already started");
            }

            _isStarted = true;
        }

        public void Commit()
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Transaction not started");
            }

            if (_isCommitted || _isRolledBack)
            {
                throw new InvalidOperationException("Transaction already completed");
            }

            // Simulate exception if configured
            if (_exceptionToThrowOnCommit != null)
            {
                throw _exceptionToThrowOnCommit;
            }

            _isCommitted = true;
        }

        public void RollBack()
        {
            if (!_isStarted)
            {
                throw new InvalidOperationException("Transaction not started");
            }

            if (_isCommitted || _isRolledBack)
            {
                throw new InvalidOperationException("Transaction already completed");
            }

            _isRolledBack = true;
        }

        public void Dispose()
        {
            _isDisposed = true;
        }

        /// <summary>
        /// Reset the mock to initial state (for test reuse).
        /// </summary>
        public void Reset()
        {
            _isStarted = false;
            _isCommitted = false;
            _isRolledBack = false;
            _isDisposed = false;
            _exceptionToThrowOnCommit = null;
        }
    }
}
