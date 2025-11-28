using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models;
using System.Collections.Generic;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Unit tests for SafetyValidator.ValidateAutoTag() - Story 2.1
    /// Tests the auto-tagging validation logic (Layer 1 - Pure business logic)
    ///
    /// Test Coverage:
    /// - Scope limit validation (max 500 elements, max 1000 tags)
    /// - Category validation (must be specified, not "?")
    /// - Tag type validation (must be specified in params)
    /// - Target validation (must not be null)
    /// - Edge cases (null params, missing keys, empty strings)
    /// </summary>
    [TestFixture]
    public class AutoTagValidationTests
    {
        private SafetyValidator _validator;

        [SetUp]
        public void Setup()
        {
            // Use default limits: maxElements=500, maxDimensions=1000, maxTags=1000
            _validator = new SafetyValidator();
        }

        #region Happy Path Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ValidDoorTagging_ReturnsSuccess()
        {
            // Arrange - Tag 50 doors with "Door Tag"
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "untagged_only"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 50);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Valid auto_tag operation with Doors category should succeed");
            Assert.That(result.Message, Is.Null.Or.Empty);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ValidWallTagging_ReturnsSuccess()
        {
            // Arrange - Tag 200 walls with "Wall Tag"
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Walls",
                    Scope = "level:Level 1",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Wall Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 200);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Valid auto_tag operation with Walls category should succeed");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ValidRoomTagging_ReturnsSuccess()
        {
            // Arrange - Tag 30 rooms with "Room Tag"
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Rooms",
                    Scope = "current_view",
                    Filter = "untagged_only"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Room Tag",
                    ["leader"] = false
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 30);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Valid auto_tag operation with Rooms category should succeed");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_AtMaxTagLimit_ReturnsSuccess()
        {
            // Arrange - Tag exactly 1000 elements (at the limit)
            // Need custom validator with higher maxElementsPerOperation since general check is 500
            var customValidator = new SafetyValidator(
                maxElementsPerOperation: 1500,
                maxTags: 1000
            );

            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Windows",
                    Scope = "entire_project",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Window Tag"
                }
            };

            // Act
            var result = customValidator.Validate(action, elementCount: 1000);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Element count at max tag limit (1000) should be valid");
        }

        #endregion

        #region Scope Limit Validation Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ElementCountExceedsMaxElements_ReturnsFailure()
        {
            // Arrange - 600 elements exceeds default max of 500
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "entire_project",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 600);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Element count exceeding max elements (500) should fail in general validation");
            Assert.That(result.Message, Does.Contain("too large").IgnoreCase);
            Assert.That(result.Message, Does.Contain("500"));
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ElementCountExceedsMaxTags_ReturnsFailure()
        {
            // Arrange - 1500 elements exceeds max tags of 1000
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Walls",
                    Scope = "entire_project",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Wall Tag"
                }
            };

            // Act - First validation catches max elements (500), so create validator with higher limit
            var customValidator = new SafetyValidator(maxElementsPerOperation: 2000, maxTags: 1000);
            var result = customValidator.Validate(action, elementCount: 1500);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Element count exceeding max tags (1000) should fail");
            Assert.That(result.Message, Does.Contain("Too many elements to tag").IgnoreCase);
            Assert.That(result.Message, Does.Contain("1500"));
            Assert.That(result.Message, Does.Contain("1000"));
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_UnderMaxElements_ReturnsSuccess()
        {
            // Arrange - 400 elements is under max of 500
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "level:Level 1",
                    Filter = "untagged_only"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 400);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Element count under max should succeed");
        }

        #endregion

        #region Category Validation Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_CategoryIsQuestionMark_ReturnsFailure()
        {
            // Arrange - Category is "?" indicating ambiguity
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "?",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 50);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Category '?' should be rejected as ambiguous");
            Assert.That(result.Message, Does.Contain("category").IgnoreCase);
            Assert.That(result.Message, Does.Contain("not specified").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_CategoryIsEmpty_ReturnsFailure()
        {
            // Arrange - Category is empty string
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Wall Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 20);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Empty category should be rejected");
            Assert.That(result.Message, Does.Contain("category").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_CategoryIsNull_ReturnsFailure()
        {
            // Arrange - Category is null
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = null,
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Room Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 10);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Null category should be rejected");
            Assert.That(result.Message, Does.Contain("category").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ValidCategoryDoors_ReturnsSuccess()
        {
            // Arrange - Category is "Doors"
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 25);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Valid category 'Doors' should succeed");
        }

        #endregion

        #region Tag Type Validation Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_TagTypeIsQuestionMark_ReturnsFailure()
        {
            // Arrange - tag_type is "?" indicating ambiguity
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "?"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 30);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Tag type '?' should be rejected as ambiguous");
            Assert.That(result.Message, Does.Contain("tag type").IgnoreCase);
            Assert.That(result.Message, Does.Contain("not specified").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_TagTypeIsEmpty_ReturnsFailure()
        {
            // Arrange - tag_type is empty string
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Walls",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = ""
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 15);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Empty tag_type should be rejected");
            Assert.That(result.Message, Does.Contain("tag type").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_TagTypeMissing_ReturnsFailure()
        {
            // Arrange - tag_type key is missing from params
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Rooms",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["leader"] = false // Other param but no tag_type
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 20);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Missing tag_type parameter should be rejected");
            Assert.That(result.Message, Does.Contain("tag_type").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ParamsIsNull_ReturnsFailure()
        {
            // Arrange - Params dictionary is null
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = null
            };

            // Act
            var result = _validator.Validate(action, elementCount: 10);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Null params should be rejected");
            Assert.That(result.Message, Does.Contain("tag_type").IgnoreCase);
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ValidTagTypeDoorTag_ReturnsSuccess()
        {
            // Arrange - tag_type is "Door Tag"
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 40);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Valid tag_type 'Door Tag' should succeed");
        }

        #endregion

        #region Target Validation Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_TargetIsNull_ReturnsFailure()
        {
            // Arrange - Target is null
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = null,
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 10);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Null target should be rejected");
            Assert.That(result.Message, Does.Contain("target").IgnoreCase);
        }

        #endregion

        #region Operation Type Validation Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_OperationIsAutoTag_ReturnsSuccess()
        {
            // Arrange - Operation is "auto_tag" (correct)
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Walls",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Wall Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 50);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "auto_tag operation should be allowed");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void Validate_AutoTagOperationInAllowlist_ReturnsTrue()
        {
            // Arrange - Verify auto_tag is in the allowlist
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 10);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "auto_tag should be in the operation allowlist");
        }

        #endregion

        #region Edge Case Tests

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_ZeroElements_ReturnsSuccess()
        {
            // Arrange - No elements match the filter
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "current_view",
                    Filter = "untagged_only"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 0);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Zero elements should be valid (no-op is safe)");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_MultipleParamsIncludingTagType_ReturnsSuccess()
        {
            // Arrange - Multiple parameters including tag_type
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Rooms",
                    Scope = "current_view",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Room Tag",
                    ["leader"] = true,
                    ["horizontal"] = false,
                    ["offset"] = 100
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 25);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Additional valid parameters should not affect validation");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_CustomMaxTags_RespectsCustomLimit()
        {
            // Arrange - Create validator with custom max tags (100)
            var customValidator = new SafetyValidator(
                maxElementsPerOperation: 2000,
                maxTags: 100
            );

            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Doors",
                    Scope = "entire_project",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Door Tag"
                }
            };

            // Act
            var result = customValidator.Validate(action, elementCount: 150);

            // Assert
            Assert.That(result.IsValid, Is.False,
                "Custom max tags limit (100) should be enforced");
            Assert.That(result.Message, Does.Contain("150"));
            Assert.That(result.Message, Does.Contain("100"));
        }

        #endregion

        #region Multiple Category Tests (for future expansion)

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_WindowCategory_ReturnsSuccess()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Windows",
                    Scope = "current_view",
                    Filter = "untagged_only"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Window Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 35);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Windows category should be valid");
        }

        [Test]
        [Category("Unit")]
        [Category("AutoTag")]
        [Category("Story2.1")]
        public void ValidateAutoTag_MechanicalEquipmentCategory_ReturnsSuccess()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget
                {
                    Category = "Mechanical Equipment",
                    Scope = "level:Level 1",
                    Filter = "all"
                },
                Params = new Dictionary<string, object>
                {
                    ["tag_type"] = "Mechanical Equipment Tag"
                }
            };

            // Act
            var result = _validator.Validate(action, elementCount: 20);

            // Assert
            Assert.That(result.IsValid, Is.True,
                "Mechanical Equipment category should be valid");
        }

        #endregion
    }
}
