using System.Text.Json.Serialization;

namespace RevitAI.Models.Commands
{
    /// <summary>
    /// POCO representing a parsed dimension command from natural language.
    /// This is a Layer 1 model - NO Revit API dependencies, enabling millisecond unit tests.
    /// </summary>
    public class DimensionCommand
    {
        /// <summary>
        /// Operation type (create_dimensions, create_tags, read_elements).
        /// </summary>
        [JsonPropertyName("operation")]
        public string Operation { get; set; } = string.Empty;

        /// <summary>
        /// Target scope for the operation (which elements to operate on).
        /// </summary>
        [JsonPropertyName("target")]
        public TargetScope Target { get; set; } = new();

        /// <summary>
        /// Parameters for dimension creation.
        /// </summary>
        [JsonPropertyName("parameters")]
        public DimensionParameters Parameters { get; set; } = new();

        /// <summary>
        /// Whether the command requires user clarification.
        /// </summary>
        [JsonPropertyName("requires_clarification")]
        public bool RequiresClarification { get; set; }

        /// <summary>
        /// Clarification question to ask the user (if RequiresClarification is true).
        /// </summary>
        [JsonPropertyName("clarification_question")]
        public string? ClarificationQuestion { get; set; }

        /// <summary>
        /// Creates a simple command for testing purposes.
        /// </summary>
        public static DimensionCommand CreateSimple(
            string operation = "create_dimensions",
            string elementType = "rooms",
            string scopeType = "all")
        {
            return new DimensionCommand
            {
                Operation = operation,
                Target = new TargetScope
                {
                    ElementType = elementType,
                    ScopeType = scopeType
                },
                Parameters = new DimensionParameters
                {
                    DimensionStyle = "default",
                    OffsetMm = 200,
                    Placement = "both"
                },
                RequiresClarification = false
            };
        }

        /// <summary>
        /// Creates a command that requires clarification.
        /// </summary>
        public static DimensionCommand CreateAmbiguous(string clarificationQuestion)
        {
            var command = CreateSimple();
            command.RequiresClarification = true;
            command.ClarificationQuestion = clarificationQuestion;
            return command;
        }
    }
}
