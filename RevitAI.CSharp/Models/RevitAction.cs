using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RevitAI.Models
{
    /// <summary>
    /// Represents a structured action parsed from natural language
    /// </summary>
    public class RevitAction
    {
        [JsonPropertyName("operation")]
        public string Operation { get; set; }

        [JsonPropertyName("target")]
        public ActionTarget Target { get; set; }

        [JsonPropertyName("params")]
        public Dictionary<string, object> Params { get; set; }

        /// <summary>
        /// Clarifying questions if the command is ambiguous
        /// </summary>
        [JsonPropertyName("clarifications")]
        public List<string> Clarifications { get; set; } = new List<string>();

        /// <summary>
        /// Check if the action requires clarification
        /// </summary>
        public bool NeedsClarification => Clarifications != null && Clarifications.Count > 0;
    }

    /// <summary>
    /// Target elements for the action
    /// </summary>
    public class ActionTarget
    {
        /// <summary>
        /// Element category (e.g., "Doors", "Walls", "Rooms")
        /// </summary>
        [JsonPropertyName("category")]
        public string Category { get; set; }

        /// <summary>
        /// Scope of the operation (e.g., "current_view", "level:Level 1", "selection")
        /// </summary>
        [JsonPropertyName("scope")]
        public string Scope { get; set; }

        /// <summary>
        /// Filter to apply (e.g., "all", "untagged_only")
        /// </summary>
        [JsonPropertyName("filter")]
        public string Filter { get; set; }

        // Legacy fields for backward compatibility with Epic 1
        [JsonPropertyName("element_type")]
        public string ElementType { get; set; }

        [JsonPropertyName("filters")]
        public Dictionary<string, object> Filters { get; set; }
    }

    /// <summary>
    /// Revit context information
    /// </summary>
    public class RevitContext
    {
        [JsonPropertyName("levels")]
        public List<string> Levels { get; set; }

        [JsonPropertyName("current_view")]
        public string CurrentView { get; set; }

        [JsonPropertyName("selection")]
        public SelectionInfo Selection { get; set; }

        [JsonPropertyName("project")]
        public ProjectInfo Project { get; set; }

        /// <summary>
        /// Available tag types in the project (for auto_tag operation)
        /// </summary>
        [JsonPropertyName("available_tag_types")]
        public List<TagTypeInfo> AvailableTagTypes { get; set; } = new List<TagTypeInfo>();

        /// <summary>
        /// Element summary by category (counts, untagged counts)
        /// </summary>
        [JsonPropertyName("element_summary")]
        public Dictionary<string, ElementCount> ElementSummary { get; set; } = new Dictionary<string, ElementCount>();
    }

    /// <summary>
    /// Selection information
    /// </summary>
    public class SelectionInfo
    {
        [JsonPropertyName("count")]
        public int Count { get; set; }

        [JsonPropertyName("types")]
        public List<string> Types { get; set; }
    }

    /// <summary>
    /// Project information
    /// </summary>
    public class ProjectInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("number")]
        public string Number { get; set; }
    }

    /// <summary>
    /// Tag type information for auto_tag operations
    /// </summary>
    public class TagTypeInfo
    {
        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("category")]
        public string Category { get; set; }

        [JsonPropertyName("family")]
        public string Family { get; set; }
    }

    /// <summary>
    /// Element count information for a category
    /// </summary>
    public class ElementCount
    {
        [JsonPropertyName("total")]
        public int Total { get; set; }

        [JsonPropertyName("untagged")]
        public int Untagged { get; set; }

        [JsonPropertyName("tagged")]
        public int Tagged => Total - Untagged;
    }
}
