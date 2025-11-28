using System.Threading.Tasks;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Interface for building Revit context for Claude API
    /// </summary>
    public interface IRevitContextBuilder
    {
        /// <summary>
        /// Get tagging context from current Revit document
        /// Queries available tag types, element counts, and untagged counts
        /// </summary>
        /// <returns>RevitContext with tagging information</returns>
        Task<RevitContext> GetTaggingContextAsync();

        /// <summary>
        /// Get basic Revit context (levels, current view, selection)
        /// </summary>
        /// <returns>RevitContext with basic information</returns>
        Task<RevitContext> GetBasicContextAsync();
    }
}
