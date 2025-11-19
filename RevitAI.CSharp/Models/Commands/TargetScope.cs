using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace RevitAI.Models.Commands
{
    /// <summary>
    /// POCO defining which elements to target for a dimension command.
    /// Layer 1 model - no Revit API dependencies.
    /// </summary>
    public class TargetScope
    {
        /// <summary>
        /// Type of elements to target (rooms, walls, doors, windows).
        /// </summary>
        [JsonPropertyName("element_type")]
        public string ElementType { get; set; } = string.Empty;

        /// <summary>
        /// Scope type (all, selected, level, current_view).
        /// </summary>
        [JsonPropertyName("scope_type")]
        public string ScopeType { get; set; } = "all";

        /// <summary>
        /// Level name filter (required if ScopeType = "level").
        /// Can be in Hebrew or English (e.g., "Level 1", "קומה 1").
        /// </summary>
        [JsonPropertyName("level_name")]
        public string? LevelName { get; set; }

        /// <summary>
        /// Exclusion filters (e.g., ["corridors", "bathrooms"]).
        /// Elements matching these names will be excluded.
        /// </summary>
        [JsonPropertyName("exclusion_filters")]
        public List<string>? ExclusionFilters { get; set; }

        /// <summary>
        /// Creates a "target all" scope for testing.
        /// </summary>
        public static TargetScope CreateAll(string elementType = "rooms")
        {
            return new TargetScope
            {
                ElementType = elementType,
                ScopeType = "all"
            };
        }

        /// <summary>
        /// Creates a level-filtered scope for testing.
        /// </summary>
        public static TargetScope CreateLevel(string levelName, string elementType = "rooms")
        {
            return new TargetScope
            {
                ElementType = elementType,
                ScopeType = "level",
                LevelName = levelName
            };
        }

        /// <summary>
        /// Creates a selected elements scope for testing.
        /// </summary>
        public static TargetScope CreateSelected(string elementType = "rooms")
        {
            return new TargetScope
            {
                ElementType = elementType,
                ScopeType = "selected"
            };
        }

        /// <summary>
        /// Creates a current view scope for testing.
        /// </summary>
        public static TargetScope CreateCurrentView(string elementType = "rooms")
        {
            return new TargetScope
            {
                ElementType = elementType,
                ScopeType = "current_view"
            };
        }

        /// <summary>
        /// Creates a scope with exclusion filters for testing.
        /// </summary>
        public static TargetScope CreateWithExclusions(string elementType, List<string> exclusions)
        {
            return new TargetScope
            {
                ElementType = elementType,
                ScopeType = "all",
                ExclusionFilters = exclusions
            };
        }
    }
}
