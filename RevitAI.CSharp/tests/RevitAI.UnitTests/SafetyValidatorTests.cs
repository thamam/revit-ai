using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models;
using System.Collections.Generic;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Unit tests for SafetyValidator
    /// These tests run in milliseconds without Revit (Layer 1 testing)
    /// Demonstrates the pattern of extracting testable business logic.
    /// </summary>
    [TestFixture]
    public class SafetyValidatorTests
    {
        private SafetyValidator _validator;

        [SetUp]
        public void Setup()
        {
            _validator = new SafetyValidator();
        }

        #region Operation Allowlist Tests

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_CreateDimensionsOperation_ReturnsValid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "create_dimensions",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action, elementCount: 10);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "create_dimensions should be allowed");
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_CreateTagsOperation_ReturnsValid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "create_tags",
                Target = new ActionTarget { ElementType = "walls" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action, elementCount: 50);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "create_tags should be allowed");
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_ReadElementsOperation_ReturnsValid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "read_elements",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action, elementCount: 100);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "read_elements should be allowed");
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_DeleteOperation_ReturnsInvalid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "delete_elements",
                Target = new ActionTarget { ElementType = "walls" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "delete_elements should be BLOCKED");
            Assert.That(result.Message, Does.Contain("blocked").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_ModifyWallsOperation_ReturnsInvalid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "modify_walls",
                Target = new ActionTarget { ElementType = "walls" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "modify_walls should be BLOCKED");
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_UnknownOperation_ReturnsInvalid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "unknown_dangerous_operation",
                Target = new ActionTarget { ElementType = "all" },
                Params = new Dictionary<string, object>()
            };

            // Act
            var result = _validator.Validate(action);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Unknown operations should be BLOCKED by default");
        }

        #endregion

        #region Scope Validation Tests

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_ScopeUnderLimit_ReturnsValid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "create_dimensions",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new Dictionary<string, object>()
            };
            int elementCount = 100; // Under default 500 limit

            // Act
            var result = _validator.Validate(action, elementCount);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Element count under limit should be valid");
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_ScopeOverLimit_ReturnsInvalid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "create_dimensions",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new Dictionary<string, object>()
            };
            int elementCount = 1000; // Over default 500 limit

            // Act
            var result = _validator.Validate(action, elementCount);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Element count over limit should be invalid");
            Assert.That(result.Message, Does.Contain("too large").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("Safety")]
        public void Validate_ScopeAtLimit_ReturnsValid()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "read_elements",
                Target = new ActionTarget { ElementType = "rooms" },
                Params = new Dictionary<string, object>()
            };
            int elementCount = 500; // At default limit

            // Act
            var result = _validator.Validate(action, elementCount);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Element count at limit should be valid");
        }

        #endregion
    }
}
