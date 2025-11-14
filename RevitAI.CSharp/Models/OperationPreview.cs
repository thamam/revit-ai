using System.Collections.Generic;

namespace RevitAI.Models
{
    /// <summary>
    /// Represents a preview of a Revit operation before execution
    /// Story 1.5: Preview/Confirm UX Pattern
    /// </summary>
    public class OperationPreview
    {
        /// <summary>
        /// Type of operation (create_dimensions, create_tags, read_elements)
        /// </summary>
        public string OperationType { get; set; }

        /// <summary>
        /// User-friendly summary of what will happen
        /// Example: "47 dimension chains will be created in 12 rooms"
        /// </summary>
        public string Summary { get; set; }

        /// <summary>
        /// Number of elements that will be affected
        /// </summary>
        public int AffectedElementCount { get; set; }

        /// <summary>
        /// IDs of affected elements (for highlighting in preview)
        /// </summary>
        public List<string> AffectedElementIds { get; set; }

        /// <summary>
        /// Detailed breakdown of operation
        /// Example: { "rooms": 12, "walls": 48, "dimension_chains": 47 }
        /// </summary>
        public Dictionary<string, object> Details { get; set; }

        /// <summary>
        /// Whether this operation can be previewed visually
        /// (DirectContext3D graphics - Epic 2 feature)
        /// </summary>
        public bool SupportsVisualPreview { get; set; }

        public OperationPreview()
        {
            AffectedElementIds = new List<string>();
            Details = new Dictionary<string, object>();
            SupportsVisualPreview = false; // Will be true in Epic 2
        }

        /// <summary>
        /// Create a simple preview for read-only operations
        /// </summary>
        public static OperationPreview CreateReadOnlyPreview(string operationType, int elementCount)
        {
            return new OperationPreview
            {
                OperationType = operationType,
                Summary = $"Query will return {elementCount} elements",
                AffectedElementCount = elementCount,
                SupportsVisualPreview = false
            };
        }

        /// <summary>
        /// Create a preview for creation operations
        /// </summary>
        public static OperationPreview CreateCreationPreview(
            string operationType,
            int itemsToCreate,
            int affectedElements,
            string itemTypeName)
        {
            return new OperationPreview
            {
                OperationType = operationType,
                Summary = $"{itemsToCreate} {itemTypeName} will be created affecting {affectedElements} elements",
                AffectedElementCount = affectedElements,
                SupportsVisualPreview = true,
                Details = new Dictionary<string, object>
                {
                    { "items_to_create", itemsToCreate },
                    { "affected_elements", affectedElements },
                    { "item_type", itemTypeName }
                }
            };
        }
    }
}
