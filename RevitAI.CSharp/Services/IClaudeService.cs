using System.Threading.Tasks;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Interface for Claude API Service.
    /// Enables mocking for Layer 1 unit tests.
    /// </summary>
    public interface IClaudeService
    {
        /// <summary>
        /// Parse natural language prompt into structured action.
        /// </summary>
        Task<RevitAction> ParsePromptAsync(string prompt, RevitContext context);

        /// <summary>
        /// Send a message to Claude with a system prompt and user message.
        /// Returns the raw text response.
        /// </summary>
        Task<string> SendMessageAsync(string systemPrompt, string userMessage, int maxRetries = 3);

        /// <summary>
        /// Test API connection.
        /// </summary>
        Task<bool> TestConnectionAsync();
    }
}
