using NUnit.Framework;
using Moq;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using RevitAI.Services;
using RevitAI.Services.NLU;
using RevitAI.Models.Commands;

namespace RevitAI.UnitTests.NLU
{
    /// <summary>
    /// Unit tests for DimensionCommandParser.
    /// Layer 1 SIL pattern: Tests run in MILLISECONDS with mocked Claude API.
    /// NO Revit API dependencies, NO external services.
    /// </summary>
    [TestFixture]
    public class DimensionCommandParserTests
    {
        private Mock<IClaudeService> _mockClaudeService;
        private DimensionCommandParser _parser;

        [SetUp]
        public void Setup()
        {
            // Mock IClaudeService to avoid actual API calls
            _mockClaudeService = new Mock<IClaudeService>();
            _parser = new DimensionCommandParser(_mockClaudeService.Object);
        }

        #region AC-2.1.1: Hebrew/English Command Parsing

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_HebrewPrompt_AllRoomsLevel1_ReturnsCorrectCommand()
        {
            // Arrange
            string hebrewPrompt = "תוסיף מידות לכל החדרים בקומה 1";
            var context = DimensionCommandContext.CreateHebrew();

            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = new TargetScope
                {
                    ElementType = "rooms",
                    ScopeType = "level",
                    LevelName = "קומה 1"
                },
                Parameters = new DimensionParameters
                {
                    DimensionStyle = "default",
                    OffsetMm = 200,
                    Placement = "both"
                },
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync(hebrewPrompt, context);

            // Assert
            Assert.AreEqual("create_dimensions", result.Operation);
            Assert.AreEqual("rooms", result.Target.ElementType);
            Assert.AreEqual("level", result.Target.ScopeType);
            Assert.AreEqual("קומה 1", result.Target.LevelName);
            Assert.AreEqual(200, result.Parameters.OffsetMm);
            Assert.IsFalse(result.RequiresClarification);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_EnglishPrompt_AllRoomsLevel1_ReturnsCorrectCommand()
        {
            // Arrange
            string englishPrompt = "Add dimensions to all rooms on Level 1";
            var context = DimensionCommandContext.CreateSimple();

            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = new TargetScope
                {
                    ElementType = "rooms",
                    ScopeType = "level",
                    LevelName = "Level 1"
                },
                Parameters = new DimensionParameters
                {
                    DimensionStyle = "default",
                    OffsetMm = 200,
                    Placement = "both"
                },
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync(englishPrompt, context);

            // Assert
            Assert.AreEqual("create_dimensions", result.Operation);
            Assert.AreEqual("rooms", result.Target.ElementType);
            Assert.AreEqual("level", result.Target.ScopeType);
            Assert.AreEqual("Level 1", result.Target.LevelName);
            Assert.AreEqual(200, result.Parameters.OffsetMm);
            Assert.IsFalse(result.RequiresClarification);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_HebrewAndEnglish_ProduceIdenticalStructure()
        {
            // Arrange - Hebrew prompt
            string hebrewPrompt = "תוסיף מידות לכל החדרים";
            var hebrewContext = DimensionCommandContext.CreateHebrew();

            var hebrewCommand = DimensionCommand.CreateSimple();
            string hebrewResponse = JsonSerializer.Serialize(hebrewCommand);

            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.Is<string>(msg => msg.Contains("תוסיף")), It.IsAny<int>()))
                .ReturnsAsync(hebrewResponse);

            // Arrange - English prompt
            string englishPrompt = "Add dimensions to all rooms";
            var englishContext = DimensionCommandContext.CreateSimple();

            var englishCommand = DimensionCommand.CreateSimple();
            string englishResponse = JsonSerializer.Serialize(englishCommand);

            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.Is<string>(msg => msg.Contains("Add dimensions")), It.IsAny<int>()))
                .ReturnsAsync(englishResponse);

            // Act
            var hebrewResult = await _parser.ParseAsync(hebrewPrompt, hebrewContext);
            var englishResult = await _parser.ParseAsync(englishPrompt, englishContext);

            // Assert - Both have identical structure
            Assert.AreEqual(hebrewResult.Operation, englishResult.Operation);
            Assert.AreEqual(hebrewResult.Target.ElementType, englishResult.Target.ElementType);
            Assert.AreEqual(hebrewResult.Target.ScopeType, englishResult.Target.ScopeType);
            Assert.AreEqual(hebrewResult.Parameters.OffsetMm, englishResult.Parameters.OffsetMm);
        }

        #endregion

        #region AC-2.1.2: Structured Action Schema

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_ValidPrompt_ReturnsCorrectSchema()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = new TargetScope
                {
                    ElementType = "rooms",
                    ScopeType = "all"
                },
                Parameters = new DimensionParameters
                {
                    DimensionStyle = "default",
                    OffsetMm = 200,
                    Placement = "both"
                },
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to all rooms", context);

            // Assert - Verify schema structure
            Assert.IsNotNull(result.Operation);
            Assert.IsNotNull(result.Target);
            Assert.IsNotNull(result.Target.ElementType);
            Assert.IsNotNull(result.Target.ScopeType);
            Assert.IsNotNull(result.Parameters);
            Assert.IsNotNull(result.Parameters.DimensionStyle);
            Assert.Greater(result.Parameters.OffsetMm, 0);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_CreateTags_OperationTypeIdentified()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_tags",
                Target = TargetScope.CreateAll("doors"),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add tags to all doors", context);

            // Assert
            Assert.AreEqual("create_tags", result.Operation);
        }

        #endregion

        #region AC-2.1.3: Scope Recognition

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_AllRooms_TargetsAllRooms()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = TargetScope.CreateAll("rooms"),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to all rooms", context);

            // Assert
            Assert.AreEqual("rooms", result.Target.ElementType);
            Assert.AreEqual("all", result.Target.ScopeType);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_SelectedRooms_TargetsSelected()
        {
            // Arrange
            var context = DimensionCommandContext.CreateWithSelection();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = TargetScope.CreateSelected("rooms"),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to selected rooms", context);

            // Assert
            Assert.AreEqual("rooms", result.Target.ElementType);
            Assert.AreEqual("selected", result.Target.ScopeType);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_RoomsOnLevel_FiltersByLevelName()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = TargetScope.CreateLevel("Level 1", "rooms"),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to rooms on Level 1", context);

            // Assert
            Assert.AreEqual("level", result.Target.ScopeType);
            Assert.AreEqual("Level 1", result.Target.LevelName);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_CurrentView_LimitsToVisibleElements()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = TargetScope.CreateCurrentView("rooms"),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to rooms in current view", context);

            // Assert
            Assert.AreEqual("current_view", result.Target.ScopeType);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_RoomsExceptCorridors_SupportsExclusionFilters()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = new DimensionCommand
            {
                Operation = "create_dimensions",
                Target = TargetScope.CreateWithExclusions("rooms", new List<string> { "corridors" }),
                Parameters = DimensionParameters.CreateDefault(),
                RequiresClarification = false
            };

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to all rooms except corridors", context);

            // Assert
            Assert.IsNotNull(result.Target.ExclusionFilters);
            Assert.Contains("corridors", result.Target.ExclusionFilters);
        }

        #endregion

        #region AC-2.1.4: Ambiguity Resolution

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_AmbiguousPrompt_TriggersClarificationQuestion()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = DimensionCommand.CreateAmbiguous(
                "Which rooms would you like to dimension? Options: all rooms, selected rooms, rooms on a specific level, or rooms in current view."
            );

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to rooms", context);

            // Assert
            Assert.IsTrue(result.RequiresClarification);
            Assert.IsNotNull(result.ClarificationQuestion);
            Assert.IsTrue(result.ClarificationQuestion.Contains("Which rooms"));
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public async Task ParseAsync_MissingLevelName_PromptsClarification()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var expectedCommand = DimensionCommand.CreateAmbiguous(
                "Which level would you like to dimension? Available levels: Level 1, Level 2, Level 3."
            );

            string mockResponse = JsonSerializer.Serialize(expectedCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act
            var result = await _parser.ParseAsync("Add dimensions to rooms on some level", context);

            // Assert
            Assert.IsTrue(result.RequiresClarification);
            Assert.IsTrue(result.ClarificationQuestion.Contains("level"));
        }

        #endregion

        #region AC-2.1.5: Error Handling

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ParseAsync_NullPrompt_ThrowsArgumentException()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _parser.ParseAsync(null, context)
            );
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ParseAsync_EmptyPrompt_ThrowsArgumentException()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();

            // Act & Assert
            Assert.ThrowsAsync<ArgumentException>(async () =>
                await _parser.ParseAsync("", context)
            );
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ParseAsync_MalformedJSON_ThrowsFormatException()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            string invalidJson = "{ invalid json }";

            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(invalidJson);

            // Act & Assert
            Assert.ThrowsAsync<FormatException>(async () =>
                await _parser.ParseAsync("Add dimensions", context)
            );
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void ParseAsync_InvalidOperation_ThrowsInvalidOperationException()
        {
            // Arrange
            var context = DimensionCommandContext.CreateSimple();
            var invalidCommand = new DimensionCommand
            {
                Operation = "delete_elements",  // NOT in allowlist
                Target = TargetScope.CreateAll(),
                Parameters = DimensionParameters.CreateDefault()
            };

            string mockResponse = JsonSerializer.Serialize(invalidCommand);
            _mockClaudeService
                .Setup(s => s.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
                .ReturnsAsync(mockResponse);

            // Act & Assert
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await _parser.ParseAsync("Delete all rooms", context)
            );
        }

        #endregion

        #region POCO Factory Methods Tests

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void DimensionCommand_CreateSimple_ReturnsValidCommand()
        {
            // Act
            var command = DimensionCommand.CreateSimple();

            // Assert
            Assert.AreEqual("create_dimensions", command.Operation);
            Assert.AreEqual("rooms", command.Target.ElementType);
            Assert.AreEqual("all", command.Target.ScopeType);
            Assert.AreEqual(200, command.Parameters.OffsetMm);
            Assert.IsFalse(command.RequiresClarification);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void TargetScope_CreateLevel_SetsLevelFilter()
        {
            // Act
            var scope = TargetScope.CreateLevel("Level 2");

            // Assert
            Assert.AreEqual("level", scope.ScopeType);
            Assert.AreEqual("Level 2", scope.LevelName);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void DimensionParameters_CreateDefault_UsesFirmStandards()
        {
            // Act
            var parameters = DimensionParameters.CreateDefault();

            // Assert
            Assert.AreEqual("default", parameters.DimensionStyle);
            Assert.AreEqual(200, parameters.OffsetMm);
            Assert.AreEqual("both", parameters.Placement);
        }

        [Test]
        [Category("Unit")]
        [Category("Layer1")]
        public void DimensionCommandContext_CreateHebrew_ContainsHebrewLevels()
        {
            // Act
            var context = DimensionCommandContext.CreateHebrew();

            // Assert
            Assert.Contains("קומה 1", context.AvailableLevels);
            Assert.IsTrue(context.CurrentView.Contains("קומה"));
        }

        #endregion
    }
}
