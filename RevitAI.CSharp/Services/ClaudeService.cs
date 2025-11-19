using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Constants;
using Anthropic.SDK.Messaging;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Claude API Service
    /// Handles communication with Claude AI for natural language understanding
    /// </summary>
    public class ClaudeService : IClaudeService
    {
        private readonly AnthropicClient _client;
        private readonly string _model;
        private readonly int _maxTokens;

        public ClaudeService(string apiKey, string model = "claude-sonnet-4-20250514", int maxTokens = 4096)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("API key cannot be null or empty", nameof(apiKey));
            }

            _client = new AnthropicClient(apiKey);
            _model = model;
            _maxTokens = maxTokens;
        }

        /// <summary>
        /// Parse natural language prompt into structured action
        /// </summary>
        public async Task<RevitAction> ParsePromptAsync(string prompt, RevitContext context)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            }

            // Build system prompt
            string systemPrompt = GetSystemPrompt();

            // Build context message
            string contextJson = JsonSerializer.Serialize(context, new JsonSerializerOptions
            {
                WriteIndented = true
            });

            string userMessage = $@"User Command: {prompt}

Current Revit Context:
{contextJson}

Parse this command and return a JSON action following the schema defined in the system prompt.";

            // Call Claude API
            var messages = new List<Message>
            {
                new Message
                {
                    Role = RoleType.User,
                    Content = userMessage
                }
            };

            var parameters = new MessageParameters
            {
                Model = _model,
                MaxTokens = _maxTokens,
                SystemMessage = systemPrompt,
                Messages = messages,
                Stream = false
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);

            // Extract JSON from response
            string responseText = response.Content[0].Text ?? string.Empty;
            string jsonText = ExtractJsonFromResponse(responseText);

            // Parse into RevitAction
            var action = JsonSerializer.Deserialize<RevitAction>(jsonText);

            return action ?? throw new InvalidOperationException("Failed to parse action from Claude response");
        }

        /// <summary>
        /// Send a message to Claude with a system prompt and user message.
        /// Returns the raw text response.
        /// </summary>
        /// <param name="systemPrompt">System prompt defining Claude's behavior</param>
        /// <param name="userMessage">User's message/query</param>
        /// <param name="maxRetries">Number of retries for transient failures (default: 3)</param>
        /// <returns>Claude's response text</returns>
        public async Task<string> SendMessageAsync(
            string systemPrompt,
            string userMessage,
            int maxRetries = 3)
        {
            if (string.IsNullOrWhiteSpace(systemPrompt))
            {
                throw new ArgumentException("System prompt cannot be null or empty", nameof(systemPrompt));
            }

            if (string.IsNullOrWhiteSpace(userMessage))
            {
                throw new ArgumentException("User message cannot be null or empty", nameof(userMessage));
            }

            int attemptCount = 0;
            Exception? lastException = null;

            while (attemptCount < maxRetries)
            {
                try
                {
                    attemptCount++;

                    var messages = new List<Message>
                    {
                        new Message
                        {
                            Role = RoleType.User,
                            Content = userMessage
                        }
                    };

                    var parameters = new MessageParameters
                    {
                        Model = _model,
                        MaxTokens = _maxTokens,
                        SystemMessage = systemPrompt,
                        Messages = messages,
                        Stream = false
                    };

                    var response = await _client.Messages.GetClaudeMessageAsync(parameters);

                    if (response == null || response.Content == null || response.Content.Count == 0)
                    {
                        throw new InvalidOperationException("Claude returned empty response");
                    }

                    // Extract text and clean up any code fences
                    string responseText = response.Content[0].Text ?? string.Empty;
                    return ExtractJsonFromResponse(responseText);
                }
                catch (Exception ex) when (attemptCount < maxRetries && IsTransientError(ex))
                {
                    // Save exception and retry
                    lastException = ex;

                    // Exponential backoff: wait 1s, 2s, 4s, etc.
                    int delayMs = (int)Math.Pow(2, attemptCount - 1) * 1000;
                    await Task.Delay(delayMs);

                    continue;
                }
                catch (Exception ex)
                {
                    // Non-retryable error or max retries exceeded
                    throw new InvalidOperationException(
                        $"Claude API call failed after {attemptCount} attempts: {ex.Message}",
                        ex
                    );
                }
            }

            // Max retries exceeded
            throw new InvalidOperationException(
                $"Claude API call failed after {maxRetries} retry attempts",
                lastException
            );
        }

        /// <summary>
        /// Determines if an exception is a transient error that should be retried.
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            // Network errors, timeouts, rate limits are transient
            var message = ex.Message.ToLower();
            return message.Contains("timeout") ||
                   message.Contains("network") ||
                   message.Contains("rate limit") ||
                   message.Contains("429") ||
                   message.Contains("503") ||
                   message.Contains("504");
        }

        /// <summary>
        /// Test API connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var messages = new List<Message>
                {
                    new Message
                    {
                        Role = RoleType.User,
                        Content = "Hello"
                    }
                };

                var parameters = new MessageParameters
                {
                    Model = _model,
                    MaxTokens = 10,
                    Messages = messages,
                    Stream = false
                };

                var response = await _client.Messages.GetClaudeMessageAsync(parameters);
                return response != null && response.Content != null && response.Content.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get system prompt that defines Claude's behavior
        /// </summary>
        private string GetSystemPrompt()
        {
            return @"You are an AI assistant for Revit, helping architects automate tasks through natural language.

Your job is to parse the user's command and return a structured JSON action that describes what they want to do.

**Supported Operations:**
- create_dimensions: Add dimension chains to rooms or elements
- create_tags: Add tags to doors, windows, rooms, or other elements
- read_elements: Query element properties

**Output Format:**
Return ONLY a JSON object with this structure:
{
  ""operation"": ""create_dimensions"" | ""create_tags"" | ""read_elements"",
  ""target"": {
    ""element_type"": ""rooms"" | ""doors"" | ""windows"" | ""walls"" | ""all"",
    ""filters"": {
      ""level"": ""Level name"" (optional),
      ""category"": ""Category name"" (optional),
      ""selected"": true (if user said 'selected elements')
    }
  },
  ""params"": {
    // Operation-specific parameters
    // For create_dimensions: { ""dimension_type"": ""interior"" | ""exterior"", ""offset_mm"": 200 }
    // For create_tags: { ""tag_type"": ""door_tag"" | ""room_tag"" | ""window_tag"" }
    // For read_elements: { ""properties"": [""Number"", ""Name"", ""Area""] }
  }
}

**Important:**
- Only use supported operations
- Parse Hebrew and English commands
- Return valid JSON only
- Do not include explanations, only JSON";
        }

        /// <summary>
        /// Extract JSON from markdown code fence if present
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            string text = response.Trim();

            // Check for code fence
            if (text.StartsWith("```"))
            {
                // Remove opening fence
                int startIndex = text.IndexOf('\n') + 1;
                text = text.Substring(startIndex);

                // Remove closing fence
                int endIndex = text.LastIndexOf("```");
                if (endIndex > 0)
                {
                    text = text.Substring(0, endIndex);
                }
            }

            return text.Trim();
        }
    }
}
