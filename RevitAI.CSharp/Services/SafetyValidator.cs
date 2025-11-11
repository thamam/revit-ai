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
                "create_dimensions",
                "create_tags",
                "read_elements"
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
