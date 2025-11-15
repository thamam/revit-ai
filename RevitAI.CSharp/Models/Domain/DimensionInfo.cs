namespace RevitAI.Models.Domain
{
    /// <summary>
    /// Plain C# representation of a dimension for Layer 1 testing.
    /// Used to verify dimension placement logic without Revit.
    /// </summary>
    public class DimensionInfo
    {
        /// <summary>
        /// Unique identifier (maps to Revit Element.Id after creation).
        /// </summary>
        public long? ElementId { get; set; }

        /// <summary>
        /// The measured value in feet.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Type of dimension: "Linear", "Aligned", "Angular", etc.
        /// </summary>
        public string DimensionType { get; set; } = "Linear";

        /// <summary>
        /// Location of dimension line (X, Y, Z in feet).
        /// </summary>
        public (double X, double Y, double Z) DimensionLineLocation { get; set; }

        /// <summary>
        /// Direction of dimension line.
        /// </summary>
        public string Direction { get; set; } = "Horizontal";

        /// <summary>
        /// Offset from referenced elements (in feet).
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// IDs of elements being dimensioned.
        /// </summary>
        public long[] ReferencedElementIds { get; set; } = System.Array.Empty<long>();

        /// <summary>
        /// Whether dimension was successfully placed.
        /// </summary>
        public bool IsValid { get; set; } = true;

        /// <summary>
        /// Error message if placement failed.
        /// </summary>
        public string ErrorMessage { get; set; } = string.Empty;

        /// <summary>
        /// Creates a dimension info for a room wall measurement.
        /// </summary>
        /// <param name="wallId">Wall element ID.</param>
        /// <param name="measurementFeet">Measured distance in feet.</param>
        /// <param name="offsetFromWall">Offset of dimension line from wall.</param>
        /// <returns>DimensionInfo for testing.</returns>
        public static DimensionInfo CreateForWall(long wallId, double measurementFeet, double offsetFromWall = 2.0)
        {
            return new DimensionInfo
            {
                Value = measurementFeet,
                DimensionType = "Linear",
                Direction = "Horizontal",
                Offset = offsetFromWall,
                ReferencedElementIds = new[] { wallId },
                IsValid = true
            };
        }

        /// <summary>
        /// Creates a dimension chain info for multiple wall segments.
        /// </summary>
        /// <param name="wallIds">IDs of walls in the chain.</param>
        /// <param name="totalMeasurement">Total dimension value.</param>
        /// <returns>DimensionInfo representing a chain.</returns>
        public static DimensionInfo CreateChain(long[] wallIds, double totalMeasurement)
        {
            return new DimensionInfo
            {
                Value = totalMeasurement,
                DimensionType = "Linear",
                Direction = "Horizontal",
                Offset = 2.0,
                ReferencedElementIds = wallIds,
                IsValid = true
            };
        }
    }
}
