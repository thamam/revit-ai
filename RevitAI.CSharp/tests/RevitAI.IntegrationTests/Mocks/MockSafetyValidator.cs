using RevitAI.Models;
using RevitAI.Services;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock wrapper around SafetyValidator for integration testing.
    /// Allows controlling validation results for testing error paths.
    /// Uses composition (not inheritance) since SafetyValidator.Validate is not virtual.
    /// </summary>
    public class MockSafetyValidator : SafetyValidator
    {
        private bool? _forceValidationResult;
        private string _forcedMessage;
        private readonly SafetyValidator _realValidator;

        public MockSafetyValidator() : base()
        {
            _realValidator = new SafetyValidator();
        }

        /// <summary>
        /// Force the validator to return a specific result.
        /// </summary>
        /// <param name="isValid">Forced validation result (true = pass, false = fail)</param>
        /// <param name="message">Forced validation message</param>
        public void SetForcedResult(bool isValid, string message = null)
        {
            _forceValidationResult = isValid;
            _forcedMessage = message ?? (isValid ? "Approved" : "Rejected");
        }

        /// <summary>
        /// Clear forced result and use actual validation logic.
        /// </summary>
        public void ClearForcedResult()
        {
            _forceValidationResult = null;
            _forcedMessage = null;
        }

        /// <summary>
        /// Wrapper around Validate that allows forced results.
        /// This shadows the base class method (not overrides, since it's not virtual).
        /// </summary>
        public new ValidationResult Validate(RevitAction action, int elementCount = 0)
        {
            // If forced result is set, return it
            if (_forceValidationResult.HasValue)
            {
                return new ValidationResult
                {
                    IsValid = _forceValidationResult.Value,
                    Message = _forcedMessage
                };
            }

            // Otherwise, use real validator logic
            return _realValidator.Validate(action, elementCount);
        }
    }
}
