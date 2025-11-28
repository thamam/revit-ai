using System;
using System.Collections.Generic;
using System.Linq;
using RevitAI.Models;

namespace RevitAI.Services
{
    /// <summary>
    /// Safety Validator
    /// Enforces operation allowlist and validates scope limits
    /// </summary>
    public class SafetyValidator
    {
        private readonly HashSet<string> _allowedOperations;
        private readonly HashSet<string> _blockedOperations;
        private readonly int _maxElementsPerOperation;
        private readonly int _maxDimensions;
        private readonly int _maxTags;

        public SafetyValidator(
            int maxElementsPerOperation = 500,
            int maxDimensions = 1000,
            int maxTags = 1000)
        {
            _maxElementsPerOperation = maxElementsPerOperation;
            _maxDimensions = maxDimensions;
            _maxTags = maxTags;

            // Define allowed operations
            _allowedOperations = new HashSet<string>
            {
                "auto_tag",          // Epic 2: Intelligent auto-tagging with NLU
                "create_dimensions", // Epic 2 Phase 2: Dimension automation
                "create_tags",       // Epic 1: Legacy tag operation
                "read_elements"      // Epic 1: Read-only queries
            };

            // Define explicitly blocked operations
            _blockedOperations = new HashSet<string>
            {
                "delete_elements",
                "modify_walls",
                "modify_floors",
                "modify_roofs",
                "modify_levels",
                "delete_views",
                "modify_families",
                "export_model"
            };
        }

        /// <summary>
        /// Validate action against safety rules
        /// </summary>
        public ValidationResult Validate(RevitAction action, int elementCount = 0)
        {
            if (action == null)
            {
                return ValidationResult.Failure("Action cannot be null");
            }

            if (string.IsNullOrWhiteSpace(action.Operation))
            {
                return ValidationResult.Failure("Action missing operation field");
            }

            // Check blocklist first
            if (_blockedOperations.Contains(action.Operation))
            {
                return ValidationResult.Failure(
                    $"Operation '{action.Operation}' is explicitly blocked for safety. " +
                    "RevitAI can only perform read and annotation operations.");
            }

            // Check allowlist
            if (!_allowedOperations.Contains(action.Operation))
            {
                return ValidationResult.Failure(
                    $"Operation '{action.Operation}' is not allowed. " +
                    $"Supported operations: {string.Join(", ", _allowedOperations)}");
            }

            // Validate scope
            if (elementCount > _maxElementsPerOperation)
            {
                return ValidationResult.Failure(
                    $"Operation scope too large: {elementCount} elements " +
                    $"(maximum: {_maxElementsPerOperation}). " +
                    "Please narrow the scope or work in batches.");
            }

            // Operation-specific validation
            switch (action.Operation)
            {
                case "auto_tag":
                    return ValidateAutoTag(action, elementCount);
                case "create_dimensions":
                    return ValidateCreateDimensions(action, elementCount);
                case "create_tags":
                    return ValidateCreateTags(action, elementCount);
                case "read_elements":
                    return ValidateReadElements(action, elementCount);
                default:
                    return ValidationResult.Success();
            }
        }

        /// <summary>
        /// Validate auto_tag operation (Epic 2 Story 2.1)
        /// </summary>
        private ValidationResult ValidateAutoTag(RevitAction action, int elementCount)
        {
            // Check scope limits
            if (elementCount > _maxTags)
            {
                return ValidationResult.Failure(
                    $"Too many elements to tag: {elementCount} " +
                    $"(maximum: {_maxTags}). " +
                    "Please narrow the scope to a specific level or view.");
            }

            // Validate target is not null
            if (action.Target == null)
            {
                return ValidationResult.Failure("auto_tag operation requires a target");
            }

            // Validate category is specified (not ambiguous)
            if (string.IsNullOrEmpty(action.Target.Category) || action.Target.Category == "?")
            {
                return ValidationResult.Failure(
                    "Element category not specified. " +
                    "Please specify which elements to tag (Doors, Walls, Rooms, etc.)");
            }

            // Validate tag type is specified in params
            if (action.Params == null || !action.Params.ContainsKey("tag_type"))
            {
                return ValidationResult.Failure("auto_tag operation requires tag_type parameter");
            }

            string tagType = action.Params["tag_type"]?.ToString();
            if (string.IsNullOrEmpty(tagType) || tagType == "?")
            {
                return ValidationResult.Failure(
                    "Tag type not specified. " +
                    "Please specify which tag type to use.");
            }

            // Additional safety: Only allow annotation operations (no geometry modification)
            // This is enforced by the allowlist, but double-check here
            if (action.Operation != "auto_tag")
            {
                return ValidationResult.Failure(
                    "Only auto_tag operation is permitted for tagging. " +
                    "Geometry modification operations are blocked.");
            }

            return ValidationResult.Success();
        }

        private ValidationResult ValidateCreateDimensions(RevitAction action, int elementCount)
        {
            int estimatedDimensions = elementCount * 4; // Rough estimate

            if (estimatedDimensions > _maxDimensions)
            {
                return ValidationResult.Failure(
                    $"Too many dimensions: estimated {estimatedDimensions} " +
                    $"(maximum: {_maxDimensions}). " +
                    "Please select fewer rooms or elements.");
            }

            return ValidationResult.Success();
        }

        private ValidationResult ValidateCreateTags(RevitAction action, int elementCount)
        {
            if (elementCount > _maxTags)
            {
                return ValidationResult.Failure(
                    $"Too many tags: {elementCount} " +
                    $"(maximum: {_maxTags}). " +
                    "Please select fewer elements.");
            }

            return ValidationResult.Success();
        }

        private ValidationResult ValidateReadElements(RevitAction action, int elementCount)
        {
            if (elementCount > _maxElementsPerOperation)
            {
                return ValidationResult.Failure(
                    $"Too many elements to read: {elementCount} " +
                    $"(maximum: {_maxElementsPerOperation}).");
            }

            return ValidationResult.Success();
        }
    }

    /// <summary>
    /// Validation result
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }

        public static ValidationResult Success() => new ValidationResult { IsValid = true };
        public static ValidationResult Failure(string message) =>
            new ValidationResult { IsValid = false, Message = message };
    }
}
