using RevitAI.Models.Domain;

namespace RevitAI.Services.Interfaces;

/// <summary>
/// Abstraction for Revit Document API to enable Layer 1 testing without Revit.
/// In Layer 2, this maps to Autodesk.Revit.DB.Document.
/// </summary>
public interface IRevitDocument
{
    /// <summary>
    /// Creates a new independent tag at the specified location.
    /// </summary>
    /// <param name="tagTypeId">ID of the tag type to use</param>
    /// <param name="viewId">ID of the view where tag will be placed</param>
    /// <param name="elementId">ID of the element to tag</param>
    /// <param name="addLeader">Whether to add a leader line</param>
    /// <param name="location">XYZ location for the tag</param>
    /// <returns>ID of the created tag</returns>
    int CreateTag(int tagTypeId, int viewId, int elementId, bool addLeader, XYZ location);

    /// <summary>
    /// Gets the current active view ID.
    /// </summary>
    int GetActiveViewId();

    /// <summary>
    /// Checks if an element exists in the document.
    /// </summary>
    bool ElementExists(int elementId);
}
