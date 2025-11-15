using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAI.Services.Interfaces
{
    /// <summary>
    /// Interface for wrapping Revit document operations.
    /// This abstraction provides a seam for dependency injection and testing,
    /// allowing business logic to be tested without a running Revit instance.
    /// </summary>
    public interface IRevitDocumentWrapper
    {
        /// <summary>
        /// Gets the currently active document.
        /// </summary>
        /// <returns>The active Revit document.</returns>
        Document GetActiveDocument();

        /// <summary>
        /// Gets the currently active view.
        /// </summary>
        /// <returns>The active view in the document.</returns>
        View GetActiveView();

        /// <summary>
        /// Gets the UIDocument for UI operations.
        /// </summary>
        /// <returns>The UI document wrapper.</returns>
        UIDocument GetUIDocument();

        /// <summary>
        /// Starts a new transaction with the given name.
        /// </summary>
        /// <param name="name">Name for the transaction (appears in undo history).</param>
        /// <returns>The started transaction (caller must commit or rollback).</returns>
        Transaction StartTransaction(string name);

        /// <summary>
        /// Gets the document title (filename without path).
        /// </summary>
        /// <returns>Document title string.</returns>
        string GetDocumentTitle();

        /// <summary>
        /// Checks if the document is modifiable (not read-only, not in family editor, etc.).
        /// </summary>
        /// <returns>True if document can be modified, false otherwise.</returns>
        bool IsDocumentModifiable();
    }
}
