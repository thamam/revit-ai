using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RevitAI.Models.Commands;

namespace RevitAI.Services.NLU
{
    /// <summary>
    /// Parser service for converting natural language dimension commands into structured POCOs.
    /// LAYER 1 SIL Pattern: Uses POCOs only, no Revit API dependencies.
    /// Can be unit tested in milliseconds without Revit installed.
    /// </summary>
    public class DimensionCommandParser
    {
        private readonly IClaudeService _claudeService;

        public DimensionCommandParser(IClaudeService claudeService)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
        }

        /// <summary>
        /// Parses a natural language dimension command (Hebrew or English) into a structured DimensionCommand.
        /// </summary>
        /// <param name="userPrompt">Natural language command (e.g., "תוסיף מידות לכל החדרים בקומה 1")</param>
        /// <param name="context">Revit context information (levels, current view, selection)</param>
        /// <returns>Structured DimensionCommand with parsed intent</returns>
        /// <exception cref="ArgumentException">If prompt is null or empty</exception>
        /// <exception cref="FormatException">If Claude returns invalid JSON</exception>
        public async Task<DimensionCommand> ParseAsync(
            string userPrompt,
            DimensionCommandContext context)
        {
            if (string.IsNullOrWhiteSpace(userPrompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty", nameof(userPrompt));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            try
            {
                // Build system prompt with Revit context
                string systemPrompt = PromptTemplates.GetDimensionCommandSystemPrompt(
                    context.AvailableLevels,
                    context.CurrentView,
                    context.HasSelection
                );

                // Build user message
                string userMessage = PromptTemplates.BuildDimensionCommandUserMessage(userPrompt);

                // Call Claude API
                string claudeResponse = await _claudeService.SendMessageAsync(
                    systemPrompt,
                    userMessage
                );

                // Parse JSON response into DimensionCommand POCO
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var command = JsonSerializer.Deserialize<DimensionCommand>(claudeResponse, options);

                if (command == null)
                {
                    throw new FormatException("Claude returned null command");
                }

                // Validate operation is in allowlist
                ValidateOperation(command.Operation);

                return command;
            }
            catch (JsonException ex)
            {
                throw new FormatException($"Failed to parse Claude response as DimensionCommand: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Validates that the operation is in the safety allowlist.
        /// </summary>
        private void ValidateOperation(string operation)
        {
            var allowedOperations = new[] { "create_dimensions", "create_tags", "read_elements" };

            if (!Array.Exists(allowedOperations, op => op == operation))
            {
                throw new InvalidOperationException(
                    $"Operation '{operation}' is not in the allowlist. " +
                    $"Allowed: {string.Join(", ", allowedOperations)}"
                );
            }
        }
    }

    /// <summary>
    /// POCO containing Revit context information for dimension command parsing.
    /// Layer 1 model - no Revit API dependencies.
    /// </summary>
    public class DimensionCommandContext
    {
        /// <summary>
        /// List of available level names in the project.
        /// </summary>
        public List<string> AvailableLevels { get; set; } = new();

        /// <summary>
        /// Name of the current view.
        /// </summary>
        public string CurrentView { get; set; } = string.Empty;

        /// <summary>
        /// Whether the user has elements currently selected.
        /// </summary>
        public bool HasSelection { get; set; }

        /// <summary>
        /// Creates a simple context for testing.
        /// </summary>
        public static DimensionCommandContext CreateSimple()
        {
            return new DimensionCommandContext
            {
                AvailableLevels = new List<string> { "Level 1", "Level 2", "Level 3" },
                CurrentView = "Floor Plan: Level 1",
                HasSelection = false
            };
        }

        /// <summary>
        /// Creates a context with Hebrew levels for testing.
        /// </summary>
        public static DimensionCommandContext CreateHebrew()
        {
            return new DimensionCommandContext
            {
                AvailableLevels = new List<string> { "קומה 1", "קומה 2", "קומה 3" },
                CurrentView = "תוכנית קומה: קומה 1",
                HasSelection = false
            };
        }

        /// <summary>
        /// Creates a context with selection active.
        /// </summary>
        public static DimensionCommandContext CreateWithSelection()
        {
            return new DimensionCommandContext
            {
                AvailableLevels = new List<string> { "Level 1", "Level 2" },
                CurrentView = "Floor Plan: Level 1",
                HasSelection = true
            };
        }
    }
}
