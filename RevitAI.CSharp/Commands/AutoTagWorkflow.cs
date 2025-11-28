using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// Revit API
using Autodesk.Revit.DB;

// RevitAI namespaces
using RevitAI.Models;
using RevitAI.Models.Domain;
using RevitAI.Services;
using RevitAI.Services.Interfaces;
using RevitAI.UI;

namespace RevitAI.Commands
{
    /// <summary>
    /// Orchestrates the complete auto-tagging workflow from natural language prompt to tag creation.
    /// Integrates ClaudeService, SafetyValidator, TagPlacementService, TagCreationService, and TagPreviewDialog.
    /// </summary>
    /// <remarks>
    /// Part of Story 2.3: Preview UI Integration (Task 3).
    ///
    /// This workflow implements the 7-step auto-tagging process:
    /// 1. Parse natural language prompt with ClaudeService
    /// 2. Validate operation with SafetyValidator
    /// 3. Build context with RevitContextBuilder
    /// 4. Get elements from Revit document
    /// 5. Calculate placements with TagPlacementService
    /// 6. Show preview with TagPreviewDialog
    /// 7. If confirmed, create tags with TagCreationService
    ///
    /// Error Handling:
    /// - ApiException: Claude API failures (network, authentication)
    /// - ValidationException: Safety validation failures (blocked operations)
    /// - RevitApiException: Revit API operation failures
    /// - Generic exceptions: Unexpected errors
    ///
    /// All steps are logged for audit trail and debugging.
    /// </remarks>
    public class AutoTagWorkflow
    {
        private readonly IClaudeService _claudeService;
        private readonly SafetyValidator _validator;
        private readonly IRevitContextBuilder _contextBuilder;
        private readonly TagPlacementService _placementService;
        private readonly TagCreationService _creationService;
        private readonly LoggingService _logger;

        /// <summary>
        /// Creates a new AutoTagWorkflow with injected dependencies.
        /// </summary>
        /// <param name="claudeService">Service for parsing natural language prompts</param>
        /// <param name="validator">Service for validating operations against safety rules</param>
        /// <param name="contextBuilder">Service for building Revit context (tag types, element counts)</param>
        /// <param name="placementService">Service for calculating collision-free tag placements</param>
        /// <param name="creationService">Service for creating tags in Revit</param>
        /// <param name="logger">Logging service for audit trail</param>
        public AutoTagWorkflow(
            IClaudeService claudeService,
            SafetyValidator validator,
            IRevitContextBuilder contextBuilder,
            TagPlacementService placementService,
            TagCreationService creationService,
            LoggingService logger)
        {
            _claudeService = claudeService ?? throw new ArgumentNullException(nameof(claudeService));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _contextBuilder = contextBuilder ?? throw new ArgumentNullException(nameof(contextBuilder));
            _placementService = placementService ?? throw new ArgumentNullException(nameof(placementService));
            _creationService = creationService ?? throw new ArgumentNullException(nameof(creationService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Executes the complete auto-tagging workflow.
        /// </summary>
        /// <param name="userPrompt">Natural language prompt (Hebrew or English)</param>
        /// <param name="document">Revit document wrapper</param>
        /// <param name="transaction">Transaction for atomic tag creation</param>
        /// <returns>Operation result with success/failure details</returns>
        public async Task<OperationResult> ExecuteAsync(
            string userPrompt,
            IRevitDocument document,
            ITransaction transaction)
        {
            // Validate parameters
            if (string.IsNullOrWhiteSpace(userPrompt))
                throw new ArgumentException("User prompt cannot be null or empty", nameof(userPrompt));
            if (document == null)
                throw new ArgumentNullException(nameof(document));
            if (transaction == null)
                throw new ArgumentNullException(nameof(transaction));

            _logger.Info($"Auto-tag workflow started: \"{userPrompt}\"", "AUTO_TAG");

            try
            {
                // ============================================================
                // Step 1: Parse Prompt with ClaudeService
                // ============================================================
                _logger.Info("Step 1: Parsing natural language prompt with Claude API", "AUTO_TAG");

                // Build context for Claude API
                var context = await _contextBuilder.GetTaggingContextAsync();
                _logger.Debug($"Built Revit context: {context.ElementSummary.Count} categories available", "AUTO_TAG");

                // Parse prompt with Claude
                var action = await _claudeService.ParsePromptAsync(userPrompt, context);
                _logger.Debug($"Parsed action: {action.Operation} on {action.Target?.Category ?? "unknown"}", "AUTO_TAG");

                // Check for operation type
                if (action.Operation != "auto_tag")
                {
                    _logger.Warning($"Unexpected operation type: {action.Operation} (expected auto_tag)", "AUTO_TAG");
                    return OperationResult.Failure($"Expected auto_tag operation, got {action.Operation}");
                }

                // Check for clarifications needed
                if (action.NeedsClarification)
                {
                    string clarifications = string.Join("; ", action.Clarifications);
                    _logger.Info($"Clarifications needed: {clarifications}", "AUTO_TAG");
                    return OperationResult.Failure($"Please clarify: {clarifications}");
                }

                // ============================================================
                // Step 2: Validate with SafetyValidator
                // ============================================================
                _logger.Info("Step 2: Validating operation against safety rules", "AUTO_TAG");

                // Validate without element count first (we'll validate again with actual count later)
                var initialValidation = _validator.Validate(action, elementCount: 0);
                if (!initialValidation.IsValid)
                {
                    _logger.Warning($"Validation failed: {initialValidation.Message}", "AUTO_TAG");
                    return OperationResult.Failure(initialValidation.Message);
                }

                _logger.Info("Safety validation passed", "AUTO_TAG");

                // ============================================================
                // Step 3-4: Get Elements from Document
                // ============================================================
                _logger.Info("Step 3-4: Querying elements from Revit document", "AUTO_TAG");

                // Extract target information from action
                string category = action.Target?.Category ?? "Unknown";
                string scope = action.Target?.Scope ?? "current_view";
                string filter = action.Target?.Filter ?? "all";

                // Get elements from document (placeholder for now - will be implemented in integration task)
                var elements = GetElementsFromAction(action, document);
                _logger.Info($"Found {elements.Count} {category} elements (scope: {scope}, filter: {filter})", "AUTO_TAG");

                if (elements.Count == 0)
                {
                    _logger.Warning($"No {category} elements found in specified scope", "AUTO_TAG");
                    return OperationResult.Failure($"No {category} elements found in specified scope");
                }

                // Validate with actual element count
                var finalValidation = _validator.Validate(action, elementCount: elements.Count);
                if (!finalValidation.IsValid)
                {
                    _logger.Warning($"Validation failed with element count: {finalValidation.Message}", "AUTO_TAG");
                    return OperationResult.Failure(finalValidation.Message);
                }

                // ============================================================
                // Step 5: Calculate Placements with TagPlacementService
                // ============================================================
                _logger.Info("Step 5: Calculating collision-free tag placements", "AUTO_TAG");

                // Get tag type from action parameters
                string tagTypeName = action.Params?.ContainsKey("tag_type") == true
                    ? action.Params["tag_type"]?.ToString()
                    : "Default Tag";

                int tagTypeId = GetTagTypeId(tagTypeName, category, document);
                _logger.Debug($"Tag type ID: {tagTypeId} ({tagTypeName})", "AUTO_TAG");

                // Get view type (default to FloorPlan for now)
                ViewType viewType = ViewType.FloorPlan;
                _logger.Debug($"View type: {viewType}", "AUTO_TAG");

                // Get existing tag bounds to avoid collisions
                // TODO: Query existing tags from document in integration task
                var existingTagBounds = new List<BoundingBox>();
                _logger.Debug($"Existing tags in view: {existingTagBounds.Count}", "AUTO_TAG");

                // Calculate placements
                var placementResult = _placementService.CalculatePlacements(
                    elements,
                    existingTagBounds,
                    viewType);

                _logger.Info(
                    $"Placement calculation complete: {placementResult.SummaryMessage} in {placementResult.CalculationTimeMs}ms",
                    "AUTO_TAG");

                if (placementResult.FailedCount > 0)
                {
                    _logger.Warning(
                        $"{placementResult.FailedCount} placements failed (success rate: {placementResult.SuccessRate:P1})",
                        "AUTO_TAG");
                }

                // ============================================================
                // Step 6: Show Preview with TagPreviewDialog
                // ============================================================
                _logger.Info("Step 6: Showing preview dialog to user", "AUTO_TAG");

                // Show preview dialog
                var dialog = new TagPreviewDialog(placementResult.Placements, category);
                bool confirmed = dialog.ShowDialog() == true;

                if (!confirmed)
                {
                    _logger.Info("User cancelled operation", "AUTO_TAG");
                    return OperationResult.Failure("Operation cancelled by user");
                }

                _logger.Info("User confirmed operation", "AUTO_TAG");

                // ============================================================
                // Step 7: Create Tags with TagCreationService
                // ============================================================
                _logger.Info("Step 7: Creating tags in Revit document", "AUTO_TAG");

                // Create tags with TagCreationService
                var result = _creationService.CreateTags(
                    placementResult.Placements,
                    tagTypeId,
                    transaction);

                _logger.Info($"Tag creation completed: {result}", "AUTO_TAG");
                return result;
            }
            catch (ApiException ex)
            {
                _logger.Error("Claude API error", "AUTO_TAG", ex);
                return OperationResult.Failure("Could not connect to AI service. Check internet connection.");
            }
            catch (ValidationException ex)
            {
                _logger.Warning($"Validation failed: {ex.Message}", "AUTO_TAG");
                return OperationResult.Failure($"Operation not allowed: {ex.Message}");
            }
            catch (RevitApiException ex)
            {
                _logger.Error("Revit API error", "AUTO_TAG", ex);
                return OperationResult.Failure("Could not modify Revit model. See logs for details.");
            }
            catch (Exception ex)
            {
                _logger.Error("Unexpected error", "AUTO_TAG", ex);
                return OperationResult.Failure($"Unexpected error: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets elements from Revit document based on action target.
        /// </summary>
        /// <remarks>
        /// Queries Revit document using FilteredElementCollector with:
        /// - Category filter (Doors, Walls, Windows, etc.)
        /// - Scope filter (current_view, level:X, selection)
        /// - Additional filter (all, untagged_only)
        /// Returns elements as MockElement POCOs for compatibility with TagPlacementService.
        /// </remarks>
        private List<MockElement> GetElementsFromAction(RevitAction action, IRevitDocument document)
        {
            _logger.Debug($"Querying elements: category={action.Target?.Category}, scope={action.Target?.Scope}, filter={action.Target?.Filter}", "AUTO_TAG");

            // Parse category from action.Target.Category
            string categoryName = action.Target?.Category ?? "Doors";
            BuiltInCategory category = ParseCategory(categoryName);

            // Query elements using FilteredElementCollector
            List<Element> revitElements = document.GetElementsByCategory(category);

            // Apply scope filter
            revitElements = ApplyScopeFilter(revitElements, action.Target?.Scope, document);

            // Apply additional filter (e.g., untagged_only)
            if (action.Target?.Filter == "untagged_only")
            {
                revitElements = FilterUntaggedElements(revitElements, document);
            }

            // Convert Revit Elements to MockElement POCOs
            var mockElements = new List<MockElement>();
            foreach (var element in revitElements)
            {
                var boundingBox = element.get_BoundingBox(null);
                if (boundingBox != null)
                {
                    XYZ center = (boundingBox.Min + boundingBox.Max) / 2.0;
                    mockElements.Add(new MockElement
                    {
                        Id = element.Id.IntegerValue,
                        Category = categoryName,
                        Center = center,
                        BoundingBox = new BoundingBoxXYZ
                        {
                            Min = boundingBox.Min,
                            Max = boundingBox.Max
                        }
                    });
                }
            }

            _logger.Info($"Found {mockElements.Count} {categoryName} elements", "AUTO_TAG");

            return mockElements;
        }

        /// <summary>
        /// Parses category name to BuiltInCategory enum.
        /// </summary>
        private BuiltInCategory ParseCategory(string categoryName)
        {
            return categoryName.ToLower() switch
            {
                "doors" => BuiltInCategory.OST_Doors,
                "walls" => BuiltInCategory.OST_Walls,
                "windows" => BuiltInCategory.OST_Windows,
                "rooms" => BuiltInCategory.OST_Rooms,
                "floors" => BuiltInCategory.OST_Floors,
                "ceilings" => BuiltInCategory.OST_Ceilings,
                "furniture" => BuiltInCategory.OST_Furniture,
                "columns" => BuiltInCategory.OST_Columns,
                "beams" => BuiltInCategory.OST_StructuralFraming,
                _ => BuiltInCategory.OST_GenericModel
            };
        }

        /// <summary>
        /// Applies scope filter (current_view, level:X, selection).
        /// </summary>
        private List<Element> ApplyScopeFilter(List<Element> elements, string scope, IRevitDocument document)
        {
            if (string.IsNullOrWhiteSpace(scope) || scope == "all")
            {
                return elements;
            }

            if (scope == "current_view")
            {
                // Filter elements visible in current view
                int activeViewId = document.GetActiveViewId();
                return elements.Where(e => e.ViewSpecific || IsVisibleInView(e, activeViewId, document)).ToList();
            }

            if (scope.StartsWith("level:"))
            {
                string levelName = scope.Substring(6); // Extract level name after "level:"
                // Filter elements on specified level
                return elements.Where(e => GetElementLevel(e) == levelName).ToList();
            }

            if (scope == "selection")
            {
                // Filter only selected elements
                var selectedIds = document.GetSelectedElementIds();
                return elements.Where(e => selectedIds.Contains(e.Id.IntegerValue)).ToList();
            }

            _logger.Warning($"Unknown scope filter: {scope}, returning all elements", "AUTO_TAG");
            return elements;
        }

        /// <summary>
        /// Filters out elements that already have tags.
        /// </summary>
        private List<Element> FilterUntaggedElements(List<Element> elements, IRevitDocument document)
        {
            // Get all existing tags in the document
            var allTags = document.GetElementsByCategory(BuiltInCategory.OST_Tags);
            var taggedElementIds = new HashSet<int>();

            foreach (var tag in allTags)
            {
                if (tag is IndependentTag independentTag)
                {
                    var taggedId = independentTag.TaggedLocalElementId;
                    if (taggedId != ElementId.InvalidElementId)
                    {
                        taggedElementIds.Add(taggedId.IntegerValue);
                    }
                }
            }

            // Filter out tagged elements
            return elements.Where(e => !taggedElementIds.Contains(e.Id.IntegerValue)).ToList();
        }

        /// <summary>
        /// Checks if element is visible in specified view.
        /// </summary>
        private bool IsVisibleInView(Element element, int viewId, IRevitDocument document)
        {
            // Simplified: Check if element's level matches view's level
            // More sophisticated implementation would use Revit's visibility API
            return true; // Placeholder - actual implementation would check view filters
        }

        /// <summary>
        /// Gets level name for element.
        /// </summary>
        private string GetElementLevel(Element element)
        {
            var levelParam = element.get_Parameter(BuiltInParameter.SCHEDULE_LEVEL_PARAM);
            if (levelParam != null && levelParam.HasValue)
            {
                return levelParam.AsValueString();
            }

            var levelIdParam = element.get_Parameter(BuiltInParameter.LEVEL_PARAM);
            if (levelIdParam != null && levelIdParam.HasValue)
            {
                var levelId = levelIdParam.AsElementId();
                var level = element.Document.GetElement(levelId) as Level;
                return level?.Name ?? "Unknown";
            }

            return "Unknown";
        }

        /// <summary>
        /// Gets tag type ID from document by name.
        /// </summary>
        /// <remarks>
        /// Queries Revit document for FamilySymbol with matching name:
        /// - Searches all tag families (BuiltInCategory.OST_Tags)
        /// - Validates tag type is appropriate for target category
        /// - Returns actual Revit ElementId.IntegerValue
        /// - Falls back to default tag type if named type not found
        /// </remarks>
        private int GetTagTypeId(string tagTypeName, string targetCategory, IRevitDocument document)
        {
            _logger.Debug($"Looking up tag type: name='{tagTypeName}', category='{targetCategory}'", "AUTO_TAG");

            // Query all tag family symbols
            var tagTypes = document.GetElementsByCategory(BuiltInCategory.OST_Tags);

            // Try to find exact match by name
            foreach (var tagType in tagTypes)
            {
                if (tagType is FamilySymbol symbol && symbol.Name.Equals(tagTypeName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Info($"Found tag type '{tagTypeName}' with ID {symbol.Id.IntegerValue}", "AUTO_TAG");
                    return symbol.Id.IntegerValue;
                }
            }

            // If exact match not found, try to find default tag type for category
            string defaultTagName = GetDefaultTagNameForCategory(targetCategory);
            foreach (var tagType in tagTypes)
            {
                if (tagType is FamilySymbol symbol && symbol.Name.Contains(defaultTagName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.Warning($"Tag type '{tagTypeName}' not found, using default '{symbol.Name}' (ID: {symbol.Id.IntegerValue})", "AUTO_TAG");
                    return symbol.Id.IntegerValue;
                }
            }

            // If no suitable tag type found, use first available tag
            var firstTag = tagTypes.FirstOrDefault() as FamilySymbol;
            if (firstTag != null)
            {
                _logger.Warning($"No suitable tag type found for '{tagTypeName}', using first available tag '{firstTag.Name}' (ID: {firstTag.Id.IntegerValue})", "AUTO_TAG");
                return firstTag.Id.IntegerValue;
            }

            // No tags in document at all - throw exception
            throw new RevitApiException($"No tag types found in document. Please load tag families before auto-tagging.");
        }

        /// <summary>
        /// Gets default tag type name for category (e.g., "Door Tag" for "Doors").
        /// </summary>
        private string GetDefaultTagNameForCategory(string category)
        {
            return category.ToLower() switch
            {
                "doors" => "Door Tag",
                "walls" => "Wall Tag",
                "windows" => "Window Tag",
                "rooms" => "Room Tag",
                "floors" => "Floor Tag",
                "ceilings" => "Ceiling Tag",
                "furniture" => "Furniture Tag",
                "columns" => "Column Tag",
                "beams" => "Structural Framing Tag",
                _ => "Generic Tag"
            };
        }
    }

    // ============================================================
    // Custom Exception Classes (Placeholders for Future Implementation)
    // ============================================================

    /// <summary>
    /// Thrown when Claude API calls fail.
    /// </summary>
    /// <remarks>
    /// TODO: Move to RevitAI.Exceptions namespace in future refactoring.
    /// For now, defined here as placeholder to enable compilation.
    /// </remarks>
    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
        public ApiException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when safety validation fails.
    /// </summary>
    /// <remarks>
    /// TODO: Move to RevitAI.Exceptions namespace in future refactoring.
    /// For now, defined here as placeholder to enable compilation.
    /// </remarks>
    public class ValidationException : Exception
    {
        public ValidationException(string message) : base(message) { }
        public ValidationException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Thrown when Revit API operations fail.
    /// </summary>
    /// <remarks>
    /// TODO: Move to RevitAI.Exceptions namespace in future refactoring.
    /// For now, defined here as placeholder to enable compilation.
    /// </remarks>
    public class RevitApiException : Exception
    {
        public RevitApiException(string message) : base(message) { }
        public RevitApiException(string message, Exception innerException) : base(message, innerException) { }
    }
}
