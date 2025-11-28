using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RevitAI.Models;
using RevitAI.Services;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock implementation of IClaudeService for integration testing.
    /// Allows configuring predefined responses without actual API calls.
    /// </summary>
    public class MockClaudeService : IClaudeService
    {
        private RevitAction _response;
        private Exception _exceptionToThrow;
        private bool _shouldTestConnection = true;

        /// <summary>
        /// Configure the mock to return a specific RevitAction.
        /// </summary>
        public void SetResponse(RevitAction action)
        {
            _response = action ?? throw new ArgumentNullException(nameof(action));
            _exceptionToThrow = null; // Clear any exception
        }

        /// <summary>
        /// Configure the mock to throw an exception.
        /// </summary>
        public void SetException(Exception exception)
        {
            _exceptionToThrow = exception ?? throw new ArgumentNullException(nameof(exception));
        }

        /// <summary>
        /// Configure the mock connection test result.
        /// </summary>
        public void SetTestConnectionResult(bool result)
        {
            _shouldTestConnection = result;
        }

        public Task<RevitAction> ParsePromptAsync(string prompt, RevitContext context)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            if (_response == null)
            {
                throw new InvalidOperationException(
                    "MockClaudeService not configured. Call SetResponse() or SetException() first.");
            }

            return Task.FromResult(_response);
        }

        public Task<string> SendMessageAsync(string systemPrompt, string userMessage, int maxRetries = 3)
        {
            if (_exceptionToThrow != null)
            {
                throw _exceptionToThrow;
            }

            return Task.FromResult("Mock response");
        }

        public Task<bool> TestConnectionAsync()
        {
            return Task.FromResult(_shouldTestConnection);
        }
    }
}
