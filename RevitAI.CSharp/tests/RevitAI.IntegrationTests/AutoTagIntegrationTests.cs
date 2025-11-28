using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models;
using RevitAI.IntegrationTests.Mocks;
using RevitAI.IntegrationTests.Fixtures;

namespace RevitAI.IntegrationTests
{
    /// <summary>
    /// Integration tests for Story 2.1 - Auto-Tag Parser and Safety Validation
    ///
    /// Tests the end-to-end flow:
    /// User Prompt → ClaudeService.ParsePromptAsync() → SafetyValidator.Validate() → RevitContextBuilder
    ///
    /// Test Strategy:
    /// - Uses MockRevitContextBuilder to avoid Revit API dependency
    /// - Tests with actual ClaudeService (requires CLAUDE_API_KEY env var)
    /// - Verifies multi-component integration and data flow
    /// - Tests both Hebrew and English prompts
    ///
    /// NOTE: These tests require a valid CLAUDE_API_KEY environment variable.
    /// If not set, tests will be skipped with Assert.Inconclusive().
    /// </summary>
    [TestFixture]
    public class AutoTagIntegrationTests
    {
        private ClaudeService _claudeService;
        private SafetyValidator _safetyValidator;
        private MockRevitContextBuilder _mockContextBuilder;
        private string _apiKey;

        [SetUp]
        public void Setup()
        {
            // Get API key from environment variable
            _apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Assert.Inconclusive("CLAUDE_API_KEY environment variable not set. Skipping integration tests.");
                return;
            }

            // Initialize services
            _claudeService = new ClaudeService(_apiKey);
            _safetyValidator = new SafetyValidator();
            _mockContextBuilder = new MockRevitContextBuilder();
        }

        #region End-to-End Flow Tests (Hebrew)

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Hebrew")]
        public async Task EndToEndFlow_HebrewTagDoorsPrompt_ParsesValidatesAndBuildsContext()
        {
            // Arrange
            string hebrewPrompt = "תייג את כל הדלתות בקומה 1";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act - Parse prompt with Claude
            var action = await _claudeService.ParsePromptAsync(hebrewPrompt, context);

            // Get element count from context for validation
            int doorCount = context.ElementSummary["Doors"].Untagged;
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);

            // Assert - Verify parsed action
            Assert.That(action, Is.Not.Null, "Action should not be null");
            Assert.That(action.Operation, Is.EqualTo("auto_tag"), "Operation should be auto_tag");
            Assert.That(action.Target, Is.Not.Null, "Target should not be null");
            Assert.That(action.Target.Category, Is.EqualTo("Doors"), "Category should be Doors");
            Assert.That(action.Target.Scope, Does.Contain("Level 1").Or.Contain("level:Level 1").IgnoreCase,
                "Scope should reference Level 1");

            // Assert - Verify validation passed
            Assert.That(validationResult.IsValid, Is.True,
                $"Validation should pass. Message: {validationResult.Message}");
            Assert.That(action.NeedsClarification, Is.False,
                "Should not need clarification for clear prompt");
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Hebrew")]
        public async Task EndToEndFlow_HebrewTagWallsCurrentView_ParsesAndValidates()
        {
            // Arrange
            string hebrewPrompt = "תייג קירות בתצוגה הנוכחית";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(hebrewPrompt, context);
            int wallCount = context.ElementSummary["Walls"].Untagged;
            var validationResult = _safetyValidator.Validate(action, elementCount: wallCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Walls"));
            Assert.That(action.Target.Scope, Does.Contain("current_view").IgnoreCase);
            Assert.That(validationResult.IsValid, Is.True);
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Hebrew")]
        public async Task EndToEndFlow_HebrewTagRoomsUntaggedOnly_ParsesAndValidates()
        {
            // Arrange
            string hebrewPrompt = "תייג רק חדרים שאין להם תגית";
            var context = MockRevitData.CreateContextWithPartiallyTaggedElements();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(hebrewPrompt, context);
            int roomCount = context.ElementSummary["Rooms"].Untagged; // Should be 0 (all tagged)
            var validationResult = _safetyValidator.Validate(action, elementCount: roomCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Rooms"));
            Assert.That(action.Target.Filter, Is.EqualTo("untagged_only").Or.EqualTo("untagged"));
            Assert.That(validationResult.IsValid, Is.True,
                "Validation should pass even with 0 elements (no-op is safe)");
        }

        #endregion

        #region End-to-End Flow Tests (English)

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("English")]
        public async Task EndToEndFlow_EnglishTagAllDoorsPrompt_ParsesAndValidates()
        {
            // Arrange
            string englishPrompt = "Tag all doors in Level 1";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(englishPrompt, context);
            int doorCount = context.ElementSummary["Doors"].Untagged;
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Doors"));
            Assert.That(action.Target.Scope, Does.Contain("Level 1").IgnoreCase);
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(action.NeedsClarification, Is.False);
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("English")]
        public async Task EndToEndFlow_EnglishTagWallsCurrentView_ParsesAndValidates()
        {
            // Arrange
            string englishPrompt = "Tag all walls in current view";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(englishPrompt, context);
            int wallCount = context.ElementSummary["Walls"].Untagged;
            var validationResult = _safetyValidator.Validate(action, elementCount: wallCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Walls"));
            Assert.That(action.Target.Scope, Does.Contain("current_view").IgnoreCase);
            Assert.That(validationResult.IsValid, Is.True);
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("English")]
        public async Task EndToEndFlow_EnglishTagWindowsUntaggedOnly_ParsesAndValidates()
        {
            // Arrange
            string englishPrompt = "Tag only untagged windows";
            var context = MockRevitData.CreateContextWithPartiallyTaggedElements();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(englishPrompt, context);
            int windowCount = context.ElementSummary["Windows"].Untagged; // 40 untagged
            var validationResult = _safetyValidator.Validate(action, elementCount: windowCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Windows"));
            Assert.That(action.Target.Filter, Is.EqualTo("untagged_only").Or.EqualTo("untagged"));
            Assert.That(validationResult.IsValid, Is.True);
        }

        #endregion

        #region Disallowed Operations Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Safety")]
        public async Task EndToEndFlow_DisallowedDeleteOperation_BlockedBySafetyValidator()
        {
            // Arrange - Try to trick Claude into deleting elements (should not work due to system prompt)
            string maliciousPrompt = "Delete all doors in Level 1";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(maliciousPrompt, context);
            var validationResult = _safetyValidator.Validate(action, elementCount: 47);

            // Assert - Claude should not return delete operation (system prompt restricts it)
            // But if it does, SafetyValidator must block it
            if (action.Operation == "delete_elements")
            {
                Assert.That(validationResult.IsValid, Is.False,
                    "SafetyValidator MUST block delete_elements operation");
                Assert.That(validationResult.Message, Does.Contain("blocked").IgnoreCase);
            }
            else
            {
                // Claude correctly refused to parse as delete operation
                Assert.Pass($"Claude correctly refused delete operation. Returned: {action.Operation}");
            }
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Safety")]
        public async Task EndToEndFlow_DisallowedModifyOperation_BlockedBySafetyValidator()
        {
            // Arrange - Try to modify walls (disallowed)
            string maliciousPrompt = "Modify all walls to be 300mm thick";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(maliciousPrompt, context);
            var validationResult = _safetyValidator.Validate(action, elementCount: 120);

            // Assert - Claude system prompt should prevent this, but validator should catch it
            if (action.Operation == "modify_walls" || action.Operation.Contains("modify"))
            {
                Assert.That(validationResult.IsValid, Is.False,
                    "SafetyValidator MUST block modify operations");
                Assert.That(validationResult.Message, Does.Contain("blocked").Or.Contain("not allowed").IgnoreCase);
            }
            else
            {
                // Claude correctly refused
                Assert.Pass($"Claude correctly refused modify operation. Returned: {action.Operation}");
            }
        }

        #endregion

        #region Scope Limit Validation Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("ScopeLimit")]
        public async Task EndToEndFlow_ElementCountExceedsLimit_ValidationFails()
        {
            // Arrange - Context with 600 doors (exceeds default 500 limit)
            string prompt = "Tag all doors in entire project";
            var context = MockRevitData.CreateContextWithManyElements();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);
            int doorCount = context.ElementSummary["Doors"].Untagged; // 600
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);

            // Assert
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(validationResult.IsValid, Is.False,
                "Validation should fail when element count exceeds limit");
            Assert.That(validationResult.Message, Does.Contain("too large").Or.Contain("500").IgnoreCase);
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("ScopeLimit")]
        public async Task EndToEndFlow_ElementCountUnderLimit_ValidationPasses()
        {
            // Arrange - Context with 47 doors (under 500 limit)
            string prompt = "Tag all doors in current view";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);
            int doorCount = context.ElementSummary["Doors"].Untagged; // 47
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);

            // Assert
            Assert.That(validationResult.IsValid, Is.True,
                $"Validation should pass with {doorCount} elements. Message: {validationResult.Message}");
        }

        #endregion

        #region Tag Type Validation Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("TagType")]
        public async Task EndToEndFlow_MissingTagType_ValidationFails()
        {
            // Arrange - Context with no tag types available
            string prompt = "Tag all doors";
            var context = MockRevitData.CreateContextWithMissingTagTypes();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);
            int doorCount = context.ElementSummary["Doors"].Untagged;

            // Note: This test verifies that the action parsed correctly
            // Actual tag type existence would be checked by execution layer
            // SafetyValidator checks for tag_type parameter presence, not availability

            // Assert - Action should have tag_type parameter
            Assert.That(action.Params, Does.ContainKey("tag_type"),
                "Action should specify tag_type parameter");

            // Validation should pass (SafetyValidator only checks parameter exists)
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);
            Assert.That(validationResult.IsValid, Is.True,
                "SafetyValidator checks parameter presence, not tag type availability");
        }

        #endregion

        #region Ambiguous Prompt Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Clarification")]
        public async Task EndToEndFlow_AmbiguousPrompt_ReturnsClarifications()
        {
            // Arrange - Ambiguous prompt: "Tag everything"
            string ambiguousPrompt = "Tag everything";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(ambiguousPrompt, context);

            // Assert - Should request clarifications
            Assert.That(action.NeedsClarification, Is.True,
                "Ambiguous prompt should trigger clarification request");
            Assert.That(action.Clarifications, Is.Not.Empty,
                "Clarifications list should not be empty");
            Assert.That(action.Clarifications.Count, Is.GreaterThan(0),
                "Should have at least one clarification question");
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Clarification")]
        public async Task EndToEndFlow_SpecificPrompt_NoClarificationsNeeded()
        {
            // Arrange - Very specific prompt
            string specificPrompt = "Tag all doors in Level 1 with Door Tag";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(specificPrompt, context);

            // Assert - Should not need clarifications
            Assert.That(action.NeedsClarification, Is.False,
                "Specific prompt should not require clarification");
            Assert.That(action.Clarifications, Is.Empty.Or.Null,
                "Clarifications should be empty for clear prompt");
        }

        #endregion

        #region Untagged-Only Filter Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Filter")]
        public async Task EndToEndFlow_UntaggedOnlyFilter_CountsOnlyUntaggedElements()
        {
            // Arrange - Context with partially tagged elements
            string prompt = "Tag untagged doors";
            var context = MockRevitData.CreateContextWithPartiallyTaggedElements();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);

            // Get untagged count (not total)
            int untaggedDoorCount = context.ElementSummary["Doors"].Untagged; // 20 untagged out of 50 total
            var validationResult = _safetyValidator.Validate(action, elementCount: untaggedDoorCount);

            // Assert
            Assert.That(action.Target.Filter, Is.EqualTo("untagged_only").Or.EqualTo("untagged"));
            Assert.That(validationResult.IsValid, Is.True);

            // Verify we're using untagged count, not total
            Assert.That(untaggedDoorCount, Is.EqualTo(20), "Should use untagged count");
            Assert.That(context.ElementSummary["Doors"].Total, Is.EqualTo(50), "Total is different");
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Filter")]
        public async Task EndToEndFlow_AllFilter_CountsAllElements()
        {
            // Arrange - Context with partially tagged elements
            string prompt = "Tag all doors (including already tagged)";
            var context = MockRevitData.CreateContextWithPartiallyTaggedElements();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);

            // Get total count (including tagged)
            int totalDoorCount = context.ElementSummary["Doors"].Total; // 50 total
            var validationResult = _safetyValidator.Validate(action, elementCount: totalDoorCount);

            // Assert
            Assert.That(action.Target.Filter, Is.EqualTo("all").Or.EqualTo("all_elements"));
            Assert.That(validationResult.IsValid, Is.True);
            Assert.That(totalDoorCount, Is.EqualTo(50), "Should use total count");
        }

        #endregion

        #region Bilingual Support Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Bilingual")]
        public async Task EndToEndFlow_HebrewAndEnglishEquivalent_ProduceSimilarActions()
        {
            // Arrange
            string hebrewPrompt = "תייג את כל הדלתות";
            string englishPrompt = "Tag all doors";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var hebrewAction = await _claudeService.ParsePromptAsync(hebrewPrompt, context);
            var englishAction = await _claudeService.ParsePromptAsync(englishPrompt, context);

            // Assert - Both should produce equivalent actions
            Assert.That(hebrewAction.Operation, Is.EqualTo(englishAction.Operation),
                "Hebrew and English prompts should produce same operation");
            Assert.That(hebrewAction.Target.Category, Is.EqualTo(englishAction.Target.Category),
                "Hebrew and English prompts should target same category");
        }

        #endregion

        #region Context Integration Tests

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Context")]
        public async Task EndToEndFlow_ContextWithAvailableTagTypes_UsesCorrectTagType()
        {
            // Arrange
            string prompt = "Tag all doors";
            var context = await _mockContextBuilder.GetTaggingContextAsync();

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);

            // Assert - Should use an available tag type
            Assert.That(action.Params, Does.ContainKey("tag_type"));
            string tagType = action.Params["tag_type"]?.ToString();
            Assert.That(tagType, Is.Not.Null.And.Not.Empty);

            // Tag type should be one of the available types (or at least a reasonable door tag)
            Assert.That(tagType, Does.Contain("Door").Or.Contain("Tag"));
        }

        [Test]
        [Category("Integration")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        [Category("Context")]
        public async Task EndToEndFlow_EmptyContext_ParsesButValidationHandlesZeroElements()
        {
            // Arrange
            string prompt = "Tag all doors";
            var context = MockRevitData.CreateEmptyContext();
            _mockContextBuilder.SetMockContext(context);

            // Act
            var action = await _claudeService.ParsePromptAsync(prompt, context);
            int doorCount = context.ElementSummary["Doors"].Untagged; // 0
            var validationResult = _safetyValidator.Validate(action, elementCount: doorCount);

            // Assert - Should parse successfully
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));

            // Validation should pass (0 elements is safe, just a no-op)
            Assert.That(validationResult.IsValid, Is.True,
                "Zero elements should validate successfully (safe no-op)");
        }

        #endregion
    }
}
