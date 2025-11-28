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
            return @"You are RevitAI, an AI assistant for Autodesk Revit automation.

Your job is to parse the user's natural language command (in Hebrew or English) and return a structured JSON action.

**AVAILABLE OPERATIONS:**
- auto_tag: Add tags to elements (walls, doors, windows, rooms, etc.)
- read_elements: Query element properties
- create_dimensions: Add dimension chains (future release)

**SUPPORTED LANGUAGES:**
- Hebrew (עברית): Fully supported
- English: Fully supported

**OUTPUT FORMAT:**
Return ONLY a JSON object with this structure:

For auto_tag operations:
{
  ""operation"": ""auto_tag"",
  ""target"": {
    ""category"": ""Doors"" | ""Walls"" | ""Windows"" | ""Rooms"" | ""Equipment"",
    ""scope"": ""current_view"" | ""level:<name>"" | ""selection"",
    ""filter"": ""all"" | ""untagged_only""
  },
  ""params"": {
    ""tag_type"": ""Door Tag"" | ""Wall Tag"" | ""Room Tag"" | etc.,
    ""placement"": ""center"" | ""left"" | ""right"" | ""top"" | ""bottom"",
    ""leader"": true | false
  },
  ""clarifications"": [
    // Optional: Ask questions if prompt is ambiguous
    // Example: ""Which tag type? [Door Tag | Door Number | Custom]""
  ]
}

For read_elements operations:
{
  ""operation"": ""read_elements"",
  ""target"": {
    ""category"": ""Doors"" | ""Walls"" | etc.,
    ""scope"": ""current_view"" | ""level:<name>"" | ""selection""
  },
  ""params"": {
    ""properties"": [""Number"", ""Name"", ""Area"", etc.]
  },
  ""clarifications"": []
}

**PARSING RULES:**
1. If the user command is ambiguous, add clarifying questions to the ""clarifications"" array
2. Use the Revit context provided (available tag types, element counts) to validate requests
3. Default to ""untagged_only"" filter if not specified
4. Default to ""current_view"" scope if not specified
5. If tag type is ambiguous, ask for clarification
6. Only use supported operations (auto_tag, read_elements)

**EXAMPLES:**

Hebrew: ""תייג את כל הדלתות בקומה 1""
→ {""operation"":""auto_tag"",""target"":{""category"":""Doors"",""scope"":""level:Level 1"",""filter"":""untagged_only""},""params"":{""tag_type"":""Door Tag"",""placement"":""center"",""leader"":false},""clarifications"":[]}

English: ""Tag all walls in current view""
→ {""operation"":""auto_tag"",""target"":{""category"":""Walls"",""scope"":""current_view"",""filter"":""untagged_only""},""params"":{""tag_type"":""Wall Tag"",""placement"":""center"",""leader"":false},""clarifications"":[]}

Ambiguous: ""Tag everything""
→ {""operation"":""auto_tag"",""target"":{""category"":""?"",""scope"":""current_view"",""filter"":""all""},""params"":{""tag_type"":""?"",""placement"":""center"",""leader"":false},""clarifications"":[""Which element types to tag? [Doors | Walls | Rooms | All]"",""Which tag types to use?""]}

**IMPORTANT:**
- Return ONLY valid JSON, no explanations
- If you cannot parse the command, return an error in clarifications
- Respect the Revit context limits (max 500 elements)";
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
