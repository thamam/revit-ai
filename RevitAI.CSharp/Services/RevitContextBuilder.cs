using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RevitAI.Models;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

namespace RevitAI.Services
{
    /// <summary>
    /// Builds Revit context for Claude API
    /// Queries tag types, element counts, and document information
    /// </summary>
    public class RevitContextBuilder : IRevitContextBuilder
    {
        private readonly UIApplication _uiApp;
        private readonly Document _document;

        public RevitContextBuilder(UIApplication uiApp)
        {
            _uiApp = uiApp ?? throw new ArgumentNullException(nameof(uiApp));
            _document = uiApp.ActiveUIDocument?.Document ?? throw new InvalidOperationException("No active document");
        }

        /// <summary>
        /// Get tagging context from current Revit document
        /// Queries available tag types, element counts, and untagged counts
        /// IMPORTANT: This method must be called from Revit's main thread (via ExternalEvent)
        /// </summary>
        public async Task<RevitContext> GetTaggingContextAsync()
        {
            // Note: This is a synchronous operation wrapped in Task for interface consistency
            // Revit API queries must run on main thread, so this should be called via ExternalEvent
            return await Task.FromResult(GetTaggingContext());
        }

        /// <summary>
        /// Synchronous version for use within ExternalEvent handlers
        /// </summary>
        private RevitContext GetTaggingContext()
        {
            var context = GetBasicContext();

            // Query available tag types
            context.AvailableTagTypes = GetAvailableTagTypes();

            // Query element counts by category
            context.ElementSummary = GetElementSummary();

            return context;
        }

        /// <summary>
        /// Get basic Revit context (levels, current view, selection)
        /// </summary>
        public async Task<RevitContext> GetBasicContextAsync()
        {
            return await Task.FromResult(GetBasicContext());
        }

        /// <summary>
        /// Synchronous version for use within ExternalEvent handlers
        /// </summary>
        private RevitContext GetBasicContext()
        {
            var context = new RevitContext
            {
                Levels = GetLevels(),
                CurrentView = _document.ActiveView?.Name ?? "Unknown",
                Selection = GetSelectionInfo(),
                Project = GetProjectInfo()
            };

            return context;
        }

        /// <summary>
        /// Get all levels in the project
        /// </summary>
        private List<string> GetLevels()
        {
            var levels = new FilteredElementCollector(_document)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => l.Name)
                .ToList();

            return levels;
        }

        /// <summary>
        /// Get current selection information
        /// </summary>
        private SelectionInfo GetSelectionInfo()
        {
            var selection = _uiApp.ActiveUIDocument.Selection;
            var selectedIds = selection.GetElementIds();

            var types = new List<string>();
            if (selectedIds.Count > 0)
            {
                // Get unique categories of selected elements
                types = selectedIds
                    .Select(id => _document.GetElement(id))
                    .Where(e => e != null && e.Category != null)
                    .Select(e => e.Category.Name)
                    .Distinct()
                    .ToList();
            }

            return new SelectionInfo
            {
                Count = selectedIds.Count,
                Types = types
            };
        }

        /// <summary>
        /// Get project information
        /// </summary>
        private ProjectInfo GetProjectInfo()
        {
            return new ProjectInfo
            {
                Name = _document.Title ?? "Untitled",
                Number = _document.ProjectInformation?.Number ?? "N/A"
            };
        }

        /// <summary>
        /// Get available tag types in the project
        /// Queries all IndependentTag types (Door Tag, Wall Tag, Room Tag, etc.)
        /// </summary>
        private List<TagTypeInfo> GetAvailableTagTypes()
        {
            var tagTypes = new List<TagTypeInfo>();

            try
            {
                // Query all tag types in the project
                var collector = new FilteredElementCollector(_document)
                    .OfClass(typeof(FamilySymbol))
                    .Cast<FamilySymbol>()
                    .Where(fs => fs.Family.FamilyCategory != null &&
                                 fs.Family.FamilyCategory.Name.Contains("Tag"));

                foreach (var tagType in collector)
                {
                    tagTypes.Add(new TagTypeInfo
                    {
                        Name = tagType.Name,
                        Category = tagType.Family.FamilyCategory?.Name ?? "Unknown",
                        Family = tagType.Family.Name
                    });
                }
            }
            catch (Exception ex)
            {
                // Log error but don't fail - return empty list
                System.Diagnostics.Debug.WriteLine($"Error querying tag types: {ex.Message}");
            }

            return tagTypes;
        }

        /// <summary>
        /// Get element counts by category (total, tagged, untagged)
        /// </summary>
        private Dictionary<string, ElementCount> GetElementSummary()
        {
            var summary = new Dictionary<string, ElementCount>();

            // Common categories to query
            var categories = new[]
            {
                BuiltInCategory.OST_Doors,
                BuiltInCategory.OST_Windows,
                BuiltInCategory.OST_Walls,
                BuiltInCategory.OST_Rooms,
                BuiltInCategory.OST_MechanicalEquipment,
                BuiltInCategory.OST_ElectricalEquipment
            };

            foreach (var category in categories)
            {
                try
                {
                    var categoryName = LabelUtils.GetLabelFor(category);
                    var count = GetElementCountForCategory(category);
                    summary[categoryName] = count;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error querying category {category}: {ex.Message}");
                }
            }

            return summary;
        }

        /// <summary>
        /// Get element count for a specific category
        /// Counts total elements and untagged elements
        /// </summary>
        private ElementCount GetElementCountForCategory(BuiltInCategory category)
        {
            // Get all elements in category
            var elements = new FilteredElementCollector(_document)
                .OfCategory(category)
                .WhereElementIsNotElementType()
                .ToList();

            int total = elements.Count;

            // Count untagged elements (elements without IndependentTag referencing them)
            int untagged = CountUntaggedElements(elements);

            return new ElementCount
            {
                Total = total,
                Untagged = untagged
            };
        }

        /// <summary>
        /// Count untagged elements by checking for IndependentTag references
        /// </summary>
        private int CountUntaggedElements(List<Element> elements)
        {
            // Get all tags in the current view
            var tags = new FilteredElementCollector(_document, _document.ActiveView.Id)
                .OfClass(typeof(IndependentTag))
                .Cast<IndependentTag>()
                .ToList();

            // Get IDs of tagged elements
            var taggedElementIds = new HashSet<ElementId>();
            foreach (var tag in tags)
            {
                if (tag.TaggedLocalElementId != null && tag.TaggedLocalElementId != ElementId.InvalidElementId)
                {
                    taggedElementIds.Add(tag.TaggedLocalElementId);
                }
            }

            // Count elements not in the tagged set
            int untaggedCount = elements.Count(e => !taggedElementIds.Contains(e.Id));

            return untaggedCount;
        }
    }
}
