using System;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

#if WINDOWS
using Autodesk.Revit.DB;
#endif

namespace RevitAI.Services;

/// <summary>
/// Production implementation of IRevitDocument that wraps the actual Revit Document API.
/// This class bridges Layer 2 business logic to the real Revit API (Layer 3).
/// Only compiles on Windows with Revit API references.
/// </summary>
#if WINDOWS
public class RevitDocumentWrapper : IRevitDocument
{
    private readonly Document _document;
    private readonly LoggingService? _logger;

    /// <summary>
    /// Creates a new RevitDocumentWrapper around a Revit Document.
    /// </summary>
    /// <param name="document">The Revit Document to wrap</param>
    /// <param name="logger">Optional logging service</param>
    public RevitDocumentWrapper(Document document, LoggingService? logger = null)
    {
        _document = document ?? throw new ArgumentNullException(nameof(document));
        _logger = logger;
    }

    /// <summary>
    /// Creates an independent tag in Revit at the specified location.
    /// </summary>
    /// <param name="tagTypeId">The ElementId of the tag type to use</param>
    /// <param name="viewId">The ElementId of the view where the tag will be created</param>
    /// <param name="elementId">The ElementId of the element to tag</param>
    /// <param name="addLeader">Whether to add a leader line to the tag</param>
    /// <param name="location">The XYZ location for the tag</param>
    /// <returns>The ElementId of the created tag</returns>
    public int CreateTag(int tagTypeId, int viewId, int elementId, bool addLeader, XYZ location)
    {
        try
        {
            // Convert Layer 1 POCO types to Revit API types
            var revitLocation = new Autodesk.Revit.DB.XYZ(location.X, location.Y, location.Z);
            var revitTagTypeId = new ElementId(tagTypeId);
            var revitViewId = new ElementId(viewId);
            var revitElementId = new ElementId(elementId);

            // Get the view and element
            View view = _document.GetElement(revitViewId) as View
                ?? throw new InvalidOperationException($"View {viewId} not found");

            Element element = _document.GetElement(revitElementId)
                ?? throw new InvalidOperationException($"Element {elementId} not found");

            // Create the tag using Revit API
            // Note: IndependentTag.Create is the modern API (Revit 2018+)
            IndependentTag tag = IndependentTag.Create(
                _document,
                view.Id,
                new Reference(element),
                addLeader,
                TagMode.TM_ADDBY_CATEGORY,
                TagOrientation.Horizontal,
                revitLocation
            );

            if (tag == null)
            {
                throw new InvalidOperationException("Failed to create tag - Revit API returned null");
            }

            // Set the tag type if specified
            if (tagTypeId > 0 && tag.TagTypeId.IntegerValue != tagTypeId)
            {
                tag.ChangeTypeId(revitTagTypeId);
            }

            _logger?.Debug($"Created tag {tag.Id.IntegerValue} for element {elementId} at ({location.X:F2}, {location.Y:F2})", "REVIT_API");

            return tag.Id.IntegerValue;
        }
        catch (Exception ex)
        {
            _logger?.Error($"Failed to create tag for element {elementId}", "REVIT_API", ex);
            throw new InvalidOperationException($"Revit API error creating tag for element {elementId}: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Gets the ElementId of the currently active view.
    /// </summary>
    /// <returns>The ElementId of the active view</returns>
    public int GetActiveViewId()
    {
        try
        {
            if (_document.ActiveView == null)
            {
                throw new InvalidOperationException("No active view in document");
            }

            int viewId = _document.ActiveView.Id.IntegerValue;
            _logger?.Debug($"Active view ID: {viewId} ({_document.ActiveView.Name})", "REVIT_API");

            return viewId;
        }
        catch (Exception ex)
        {
            _logger?.Error("Failed to get active view ID", "REVIT_API", ex);
            throw new InvalidOperationException($"Revit API error getting active view: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Checks whether an element exists in the document.
    /// </summary>
    /// <param name="elementId">The ElementId to check</param>
    /// <returns>True if the element exists, false otherwise</returns>
    public bool ElementExists(int elementId)
    {
        try
        {
            if (elementId <= 0)
            {
                return false;
            }

            var revitElementId = new ElementId(elementId);
            Element element = _document.GetElement(revitElementId);

            bool exists = element != null && element.IsValidObject;

            if (!exists)
            {
                _logger?.Debug($"Element {elementId} does not exist or is invalid", "REVIT_API");
            }

            return exists;
        }
        catch (Exception ex)
        {
            _logger?.Warning($"Error checking element {elementId} existence", "REVIT_API", ex);
            return false;
        }
    }

    /// <summary>
    /// Gets the underlying Revit Document for advanced operations.
    /// Use sparingly - prefer using interface methods for better testability.
    /// </summary>
    public Document GetDocument()
    {
        return _document;
    }
}
#else
/// <summary>
/// Placeholder class for non-Windows platforms.
/// The real implementation requires Revit API references which are Windows-only.
/// </summary>
public class RevitDocumentWrapper
{
    public RevitDocumentWrapper(object document, object? logger = null)
    {
        throw new PlatformNotSupportedException("RevitDocumentWrapper requires Windows and Revit API");
    }
}
#endif
