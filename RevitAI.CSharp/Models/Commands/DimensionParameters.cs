using System.Text.Json.Serialization;

namespace RevitAI.Models.Commands
{
    /// <summary>
    /// POCO representing dimension creation parameters.
    /// Layer 1 model - no Revit API dependencies.
    /// </summary>
    public class DimensionParameters
    {
        /// <summary>
        /// Dimension style name (e.g., "default", "Architectural").
        /// </summary>
        [JsonPropertyName("dimension_style")]
        public string DimensionStyle { get; set; } = "default";

        /// <summary>
        /// Offset from wall centerline in millimeters.
        /// Firm standard default: 200mm.
        /// </summary>
        [JsonPropertyName("offset_mm")]
        public double OffsetMm { get; set; } = 200;

        /// <summary>
        /// Dimension placement type (horizontal, vertical, both).
        /// </summary>
        [JsonPropertyName("placement")]
        public string Placement { get; set; } = "both";

        /// <summary>
        /// Creates default parameters for testing.
        /// </summary>
        public static DimensionParameters CreateDefault()
        {
            return new DimensionParameters
            {
                DimensionStyle = "default",
                OffsetMm = 200,
                Placement = "both"
            };
        }

        /// <summary>
        /// Creates custom parameters for testing.
        /// </summary>
        public static DimensionParameters CreateCustom(
            string style,
            double offsetMm,
            string placement)
        {
            return new DimensionParameters
            {
                DimensionStyle = style,
                OffsetMm = offsetMm,
                Placement = placement
            };
        }
    }
}
