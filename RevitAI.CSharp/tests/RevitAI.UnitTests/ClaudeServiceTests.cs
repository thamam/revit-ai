using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Living Specification Tests for ClaudeService
    /// These tests serve as executable documentation of how natural language
    /// commands should be parsed into RevitAction objects.
    /// </summary>
    [TestFixture]
    public class ClaudeServiceTests
    {
        private ClaudeService _service;

        [SetUp]
        public void Setup()
        {
            // Note: These tests require CLAUDE_API_KEY environment variable
            // In a production setup, we'd mock the HTTP client
            string apiKey = System.Environment.GetEnvironmentVariable("CLAUDE_API_KEY") ?? "test-key";
            _service = new ClaudeService(apiKey);
        }

        /// <summary>
        /// Living Specification: Hebrew dimension command parsing
        /// PRD Example: "תוסיף מידות לכל החדרים" → create_dimensions operation
        /// </summary>
        [Test]
        [Category("LivingSpec")]
        [Category("Hebrew")]
        public async Task ParsePromptAsync_HebrewDimensionCommand_ReturnsCreateDimensionsOperation()
        {
            // Arrange
            string hebrewPrompt = "תוסיף מידות לכל החדרים";
            var context = new Dictionary<string, object>();

            // Act
            var action = await _service.ParsePromptAsync(hebrewPrompt, context);

            // Assert
            Assert.That(action, Is.Not.Null, "Action should not be null");
            Assert.That(action.Operation, Is.EqualTo("create_dimensions"),
                $"Hebrew dimension command should parse to 'create_dimensions', got '{action.Operation}'");
        }

        /// <summary>
        /// Living Specification: English dimension command parsing
        /// PRD Example: "Add dimensions to all rooms" → create_dimensions operation
        /// </summary>
        [Test]
        [Category("LivingSpec")]
        [Category("English")]
        public async Task ParsePromptAsync_EnglishDimensionCommand_ReturnsCreateDimensionsOperation()
        {
            // Arrange
            string englishPrompt = "Add dimensions to all rooms";
            var context = new Dictionary<string, object>();

            // Act
            var action = await _service.ParsePromptAsync(englishPrompt, context);

            // Assert
            Assert.That(action, Is.Not.Null, "Action should not be null");
            Assert.That(action.Operation, Is.EqualTo("create_dimensions"),
                $"English dimension command should parse to 'create_dimensions', got '{action.Operation}'");
        }

        /// <summary>
        /// Living Specification: Tag command parsing
        /// PRD Example: "Tag all walls on Level 1" → create_tags operation
        /// </summary>
        [Test]
        [Category("LivingSpec")]
        [Category("English")]
        public async Task ParsePromptAsync_TagCommand_ReturnsCreateTagsOperation()
        {
            // Arrange
            string tagPrompt = "Tag all walls on Level 1";
            var context = new Dictionary<string, object>();

            // Act
            var action = await _service.ParsePromptAsync(tagPrompt, context);

            // Assert
            Assert.That(action, Is.Not.Null, "Action should not be null");
            Assert.That(action.Operation, Is.EqualTo("create_tags"),
                $"Tag command should parse to 'create_tags', got '{action.Operation}'");
        }

        /// <summary>
        /// Living Specification: Read elements command
        /// </summary>
        [Test]
        [Category("LivingSpec")]
        public async Task ParsePromptAsync_ReadElementsCommand_ReturnsReadElementsOperation()
        {
            // Arrange
            string readPrompt = "List all rooms in the project";
            var context = new Dictionary<string, object>();

            // Act
            var action = await _service.ParsePromptAsync(readPrompt, context);

            // Assert
            Assert.That(action, Is.Not.Null, "Action should not be null");
            Assert.That(action.Operation, Is.EqualTo("read_elements"),
                $"Read command should parse to 'read_elements', got '{action.Operation}'");
        }

        /// <summary>
        /// Test API connection without making actual Claude API call
        /// </summary>
        [Test]
        [Category("Unit")]
        public void Constructor_WithValidApiKey_DoesNotThrow()
        {
            // Arrange & Act & Assert
            Assert.DoesNotThrow(() => new ClaudeService("valid-api-key"),
                "ClaudeService constructor should not throw with valid API key");
        }
    }
}
