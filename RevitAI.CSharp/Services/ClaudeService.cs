using System;
using System.Threading.Tasks;
using System.Text.Json;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Claude API Service
    /// Handles communication with Claude AI for natural language understanding
    /// </summary>
    public class ClaudeService
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
            var messages = new[]
            {
                new Message
                {
                    Role = "user",
                    Content = userMessage
                }
            };

            var request = new MessageRequest
            {
                Model = _model,
                MaxTokens = _maxTokens,
                System = systemPrompt,
                Messages = messages
            };

            var response = await _client.Messages.CreateAsync(request);

            // Extract JSON from response
            string responseText = response.Content[0].Text;
            string jsonText = ExtractJsonFromResponse(responseText);

            // Parse into RevitAction
            var action = JsonSerializer.Deserialize<RevitAction>(jsonText);

            return action ?? throw new InvalidOperationException("Failed to parse action from Claude response");
        }

        /// <summary>
        /// Test API connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                var messages = new[]
                {
                    new Message
                    {
                        Role = "user",
                        Content = "Hello"
                    }
                };

                var request = new MessageRequest
                {
                    Model = _model,
                    MaxTokens = 10,
                    Messages = messages
                };

                var response = await _client.Messages.CreateAsync(request);
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
