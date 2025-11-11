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
    }

    /// <summary>
    /// Target elements for the action
    /// </summary>
    public class ActionTarget
    {
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
}
