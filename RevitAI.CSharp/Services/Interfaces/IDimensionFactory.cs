using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAI.Services.Interfaces
{
    /// <summary>
    /// Interface for creating dimensions in a Revit document.
    /// This abstraction enables unit testing of dimension placement logic
    /// without requiring a running Revit instance (Layer 1 testing in SIL architecture).
    /// </summary>
    public interface IDimensionFactory
    {
        /// <summary>
        /// Creates a linear dimension between two references.
        /// </summary>
        /// <param name="view">The view where the dimension will be placed.</param>
        /// <param name="line">The dimension line location.</param>
        /// <param name="references">Array of references to dimension between.</param>
        /// <returns>The created dimension, or null if creation failed.</returns>
        Dimension CreateLinearDimension(View view, Line line, ReferenceArray references);

        /// <summary>
        /// Creates a continuous dimension chain from multiple references.
        /// </summary>
        /// <param name="view">The view where the dimension chain will be placed.</param>
        /// <param name="references">Ordered collection of references for the dimension chain.</param>
        /// <param name="dimensionLineOffset">Offset distance from elements to dimension line.</param>
        /// <returns>The created dimension representing the continuous chain, or null if failed.</returns>
        Dimension CreateContinuousDimension(View view, IEnumerable<Reference> references, double dimensionLineOffset);

        /// <summary>
        /// Creates dimensions for all room boundaries in a view.
        /// </summary>
        /// <param name="view">The view where dimensions will be placed.</param>
        /// <param name="rooms">Collection of rooms to dimension.</param>
        /// <param name="offset">Offset from walls for dimension line placement (in feet).</param>
        /// <returns>Collection of created dimensions.</returns>
        IEnumerable<Dimension> CreateRoomDimensions(View view, IEnumerable<Room> rooms, double offset);

        /// <summary>
        /// Gets the default dimension type for the document.
        /// </summary>
        /// <param name="doc">The Revit document.</param>
        /// <returns>Default dimension type, or null if not found.</returns>
        DimensionType GetDefaultDimensionType(Document doc);
    }
}
