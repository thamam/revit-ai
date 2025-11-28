using NUnit.Framework;
using RevitAI.Models;
using System.Collections.Generic;
using System.Text.Json;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Unit tests for RevitAction model - Story 2.1
    /// Tests the new Clarifications field and NeedsClarification property
    /// Tests JSON deserialization with new fields (category, scope, filter)
    ///
    /// Test Coverage:
    /// - Clarifications array populated → NeedsClarification = true
    /// - Clarifications empty → NeedsClarification = false
    /// - JSON deserialization with new Epic 2 fields
    /// - Backward compatibility with Epic 1 fields
    /// </summary>
    [TestFixture]
    public class RevitActionModelTests
    {
        #region NeedsClarification Property Tests

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void NeedsClarification_ClarificationsEmpty_ReturnsFalse()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget { Category = "Doors" },
                Params = new Dictionary<string, object>(),
                Clarifications = new List<string>() // Empty list
            };

            // Act
            bool needsClarification = action.NeedsClarification;

            // Assert
            Assert.That(needsClarification, Is.False,
                "Empty Clarifications list should result in NeedsClarification = false");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void NeedsClarification_ClarificationsNull_ReturnsFalse()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget { Category = "Doors" },
                Params = new Dictionary<string, object>(),
                Clarifications = null
            };

            // Act
            bool needsClarification = action.NeedsClarification;

            // Assert
            Assert.That(needsClarification, Is.False,
                "Null Clarifications should result in NeedsClarification = false");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void NeedsClarification_OneClarification_ReturnsTrue()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget { Category = "?" },
                Params = new Dictionary<string, object>(),
                Clarifications = new List<string>
                {
                    "Which element category? (Doors, Walls, Rooms, Windows)"
                }
            };

            // Act
            bool needsClarification = action.NeedsClarification;

            // Assert
            Assert.That(needsClarification, Is.True,
                "One clarification should result in NeedsClarification = true");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void NeedsClarification_MultipleClarifications_ReturnsTrue()
        {
            // Arrange
            var action = new RevitAction
            {
                Operation = "auto_tag",
                Target = new ActionTarget { Category = "?" },
                Params = new Dictionary<string, object> { ["tag_type"] = "?" },
                Clarifications = new List<string>
                {
                    "Which element category? (Doors, Walls, Rooms, Windows)",
                    "Which tag type? (Door Tag, Wall Tag, Room Tag)"
                }
            };

            // Act
            bool needsClarification = action.NeedsClarification;

            // Assert
            Assert.That(needsClarification, Is.True,
                "Multiple clarifications should result in NeedsClarification = true");
            Assert.That(action.Clarifications.Count, Is.EqualTo(2));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void NeedsClarification_DefaultConstructor_ReturnsFalse()
        {
            // Arrange - Default initialization
            var action = new RevitAction();

            // Act
            bool needsClarification = action.NeedsClarification;

            // Assert
            Assert.That(needsClarification, Is.False,
                "Default constructor should initialize Clarifications as empty, resulting in false");
            Assert.That(action.Clarifications, Is.Not.Null);
            Assert.That(action.Clarifications.Count, Is.EqualTo(0));
        }

        #endregion

        #region ActionTarget New Fields Tests

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ActionTarget_CategoryProperty_CanBeSet()
        {
            // Arrange & Act
            var target = new ActionTarget
            {
                Category = "Doors"
            };

            // Assert
            Assert.That(target.Category, Is.EqualTo("Doors"));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ActionTarget_ScopeProperty_CanBeSet()
        {
            // Arrange & Act
            var target = new ActionTarget
            {
                Category = "Walls",
                Scope = "level:Level 1"
            };

            // Assert
            Assert.That(target.Scope, Is.EqualTo("level:Level 1"));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ActionTarget_FilterProperty_CanBeSet()
        {
            // Arrange & Act
            var target = new ActionTarget
            {
                Category = "Rooms",
                Scope = "current_view",
                Filter = "untagged_only"
            };

            // Assert
            Assert.That(target.Filter, Is.EqualTo("untagged_only"));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ActionTarget_AllNewProperties_CanBeSetTogether()
        {
            // Arrange & Act
            var target = new ActionTarget
            {
                Category = "Windows",
                Scope = "selection",
                Filter = "all"
            };

            // Assert
            Assert.That(target.Category, Is.EqualTo("Windows"));
            Assert.That(target.Scope, Is.EqualTo("selection"));
            Assert.That(target.Filter, Is.EqualTo("all"));
        }

        #endregion

        #region JSON Deserialization Tests

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonDeserialization_AutoTagWithNewFields_Success()
        {
            // Arrange - JSON from Claude API with new Epic 2 fields
            string json = @"{
                ""operation"": ""auto_tag"",
                ""target"": {
                    ""category"": ""Doors"",
                    ""scope"": ""current_view"",
                    ""filter"": ""untagged_only""
                },
                ""params"": {
                    ""tag_type"": ""Door Tag"",
                    ""leader"": false
                },
                ""clarifications"": []
            }";

            // Act
            var action = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Operation, Is.EqualTo("auto_tag"));
            Assert.That(action.Target.Category, Is.EqualTo("Doors"));
            Assert.That(action.Target.Scope, Is.EqualTo("current_view"));
            Assert.That(action.Target.Filter, Is.EqualTo("untagged_only"));
            Assert.That(action.Params["tag_type"].ToString(), Is.EqualTo("Door Tag"));
            Assert.That(action.NeedsClarification, Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonDeserialization_WithClarifications_Success()
        {
            // Arrange - JSON with clarifications (ambiguous prompt)
            string json = @"{
                ""operation"": ""auto_tag"",
                ""target"": {
                    ""category"": ""?"",
                    ""scope"": ""current_view"",
                    ""filter"": ""all""
                },
                ""params"": {
                    ""tag_type"": ""?""
                },
                ""clarifications"": [
                    ""Which element category? (Doors, Walls, Rooms, Windows)"",
                    ""Which tag type?""
                ]
            }";

            // Act
            var action = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert
            Assert.That(action, Is.Not.Null);
            Assert.That(action.NeedsClarification, Is.True);
            Assert.That(action.Clarifications.Count, Is.EqualTo(2));
            Assert.That(action.Clarifications[0], Does.Contain("element category"));
            Assert.That(action.Clarifications[1], Does.Contain("tag type"));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonDeserialization_BackwardCompatibility_Epic1Fields()
        {
            // Arrange - JSON using old Epic 1 field names
            string json = @"{
                ""operation"": ""create_tags"",
                ""target"": {
                    ""element_type"": ""walls"",
                    ""filters"": {
                        ""level"": ""Level 1""
                    }
                },
                ""params"": {
                    ""tag_type"": ""Wall Tag""
                }
            }";

            // Act
            var action = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Operation, Is.EqualTo("create_tags"));
            Assert.That(action.Target.ElementType, Is.EqualTo("walls"));
            Assert.That(action.Target.Filters, Is.Not.Null);
            Assert.That(action.Target.Filters.ContainsKey("level"), Is.True);
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonDeserialization_MixedOldAndNewFields_Success()
        {
            // Arrange - JSON with both old and new fields (transition period)
            string json = @"{
                ""operation"": ""auto_tag"",
                ""target"": {
                    ""category"": ""Walls"",
                    ""scope"": ""level:Level 1"",
                    ""filter"": ""all"",
                    ""element_type"": ""walls"",
                    ""filters"": {
                        ""level"": ""Level 1""
                    }
                },
                ""params"": {
                    ""tag_type"": ""Wall Tag""
                }
            }";

            // Act
            var action = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert - Both old and new fields should be accessible
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Target.Category, Is.EqualTo("Walls"));
            Assert.That(action.Target.Scope, Is.EqualTo("level:Level 1"));
            Assert.That(action.Target.ElementType, Is.EqualTo("walls"));
            Assert.That(action.Target.Filters, Is.Not.Null);
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonSerialization_AutoTagWithClarifications_Success()
        {
            // Arrange
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
                },
                Clarifications = new List<string>()
            };

            // Act
            string json = JsonSerializer.Serialize(action);
            var deserialized = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert
            Assert.That(deserialized, Is.Not.Null);
            Assert.That(deserialized.Operation, Is.EqualTo(action.Operation));
            Assert.That(deserialized.Target.Category, Is.EqualTo(action.Target.Category));
            Assert.That(deserialized.Target.Scope, Is.EqualTo(action.Target.Scope));
            Assert.That(deserialized.Target.Filter, Is.EqualTo(action.Target.Filter));
            Assert.That(deserialized.NeedsClarification, Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void JsonDeserialization_MissingClarifications_UsesDefault()
        {
            // Arrange - JSON without clarifications field
            string json = @"{
                ""operation"": ""auto_tag"",
                ""target"": {
                    ""category"": ""Rooms"",
                    ""scope"": ""current_view"",
                    ""filter"": ""all""
                },
                ""params"": {
                    ""tag_type"": ""Room Tag""
                }
            }";

            // Act
            var action = JsonSerializer.Deserialize<RevitAction>(json);

            // Assert - Should use default empty list
            Assert.That(action, Is.Not.Null);
            Assert.That(action.Clarifications, Is.Not.Null);
            Assert.That(action.Clarifications.Count, Is.EqualTo(0));
            Assert.That(action.NeedsClarification, Is.False);
        }

        #endregion

        #region RevitContext TagTypeInfo Tests

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void TagTypeInfo_Properties_CanBeSet()
        {
            // Arrange & Act
            var tagType = new TagTypeInfo
            {
                Name = "Door Tag",
                Category = "Door Tags",
                Family = "M_Door Tag"
            };

            // Assert
            Assert.That(tagType.Name, Is.EqualTo("Door Tag"));
            Assert.That(tagType.Category, Is.EqualTo("Door Tags"));
            Assert.That(tagType.Family, Is.EqualTo("M_Door Tag"));
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void RevitContext_AvailableTagTypes_CanBePopulated()
        {
            // Arrange & Act
            var context = new RevitContext
            {
                AvailableTagTypes = new List<TagTypeInfo>
                {
                    new TagTypeInfo { Name = "Door Tag", Category = "Door Tags", Family = "M_Door Tag" },
                    new TagTypeInfo { Name = "Wall Tag", Category = "Wall Tags", Family = "M_Wall Tag" }
                }
            };

            // Assert
            Assert.That(context.AvailableTagTypes, Is.Not.Null);
            Assert.That(context.AvailableTagTypes.Count, Is.EqualTo(2));
            Assert.That(context.AvailableTagTypes[0].Name, Is.EqualTo("Door Tag"));
            Assert.That(context.AvailableTagTypes[1].Name, Is.EqualTo("Wall Tag"));
        }

        #endregion

        #region ElementCount Tests

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ElementCount_TaggedCalculation_ReturnsCorrectValue()
        {
            // Arrange
            var count = new ElementCount
            {
                Total = 100,
                Untagged = 35
            };

            // Act
            int tagged = count.Tagged;

            // Assert
            Assert.That(tagged, Is.EqualTo(65),
                "Tagged should be calculated as Total - Untagged");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ElementCount_AllUntagged_ReturnsZeroTagged()
        {
            // Arrange
            var count = new ElementCount
            {
                Total = 50,
                Untagged = 50
            };

            // Act
            int tagged = count.Tagged;

            // Assert
            Assert.That(tagged, Is.EqualTo(0),
                "When all elements are untagged, Tagged should be 0");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void ElementCount_AllTagged_ReturnsZeroUntagged()
        {
            // Arrange
            var count = new ElementCount
            {
                Total = 75,
                Untagged = 0
            };

            // Act
            int tagged = count.Tagged;

            // Assert
            Assert.That(tagged, Is.EqualTo(75),
                "When all elements are tagged, Tagged should equal Total");
        }

        [Test]
        [Category("Unit")]
        [Category("Model")]
        [Category("Story2.1")]
        public void RevitContext_ElementSummary_CanBePopulated()
        {
            // Arrange & Act
            var context = new RevitContext
            {
                ElementSummary = new Dictionary<string, ElementCount>
                {
                    ["Doors"] = new ElementCount { Total = 100, Untagged = 25 },
                    ["Walls"] = new ElementCount { Total = 200, Untagged = 50 },
                    ["Rooms"] = new ElementCount { Total = 50, Untagged = 0 }
                }
            };

            // Assert
            Assert.That(context.ElementSummary, Is.Not.Null);
            Assert.That(context.ElementSummary.Count, Is.EqualTo(3));
            Assert.That(context.ElementSummary["Doors"].Total, Is.EqualTo(100));
            Assert.That(context.ElementSummary["Doors"].Tagged, Is.EqualTo(75));
            Assert.That(context.ElementSummary["Rooms"].Untagged, Is.EqualTo(0));
        }

        #endregion
    }
}
