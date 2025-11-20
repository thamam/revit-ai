# RevitAI Testing Framework

**Version:** 1.0
**Date:** 2025-11-20
**Research Basis:** Task 3 (Revit API Testing Strategies Research)

---

## Executive Summary

This document defines RevitAI's comprehensive testing strategy, informed by academic research and industry best practices for Revit API testing. The framework addresses the unique challenge of the "Inverted Testing Pyramid" in BIM development and implements a multi-layer approach combining unit tests, integration tests, MCP mocking, and benchmark-driven quality assurance.

**Key Principles:**
1. **SIL Architecture** (Separation of Interface Layers): 80% pure logic, 15% Revit API wrapper, 5% E2E glue
2. **Academic Benchmarking**: 80-query test suite based on BIMCoder and Synergistic BIM Aligners research
3. **Human-in-Loop Validation**: Audit trail and preview mechanisms for safety-critical operations
4. **CI/CD Compatible**: Fast layer 1 tests run on every commit, slower layers run nightly/weekly

---

## 1. Testing Pyramid Adaptation for Revit

### 1.1 The Problem: Inverted Pyramid

Traditional software testing follows the "Testing Pyramid":
```
      /\
     /E2E\      10% - Slow, expensive, brittle
    /──────\
   / Integ. \   20% - Medium speed, some dependencies
  /──────────\
 /   Unit     \ 70% - Fast, cheap, maintainable
/______________\
```

**Revit Reality** (before SIL architecture):
```
 ______________
\   Unit     /  10% - Hard to isolate Revit API dependencies
 \──────────/
  \ Integ. /    20% - Requires Revit running, slow
   \──────/
    \ E2E/      70% - Manual clicking, 10min per test
     \──/
      \/
```

**Root Cause** (from Task 3 research):
> "The Revit API is essentially single-threaded (STA). External requests must marshal execution back to the main Revit thread. A Wall object cannot exist without a Document, which cannot exist without an Application instance, which requires the entire Revit executable loaded into memory."

### 1.2 Our Solution: SIL Architecture

**Separation of Interface Layers (SIL):**

```
Layer 1: Pure Business Logic (80% of code)
├── No Revit API dependencies
├── Fast unit tests (milliseconds)
├── Algorithms, validation, parsing
└── Mock implementations for testing

Layer 2: Revit API Wrapper (15% of code)
├── Thin integration layer
├── Interfaces (IRoomAnalyzer, IDimensionFactory)
├── Real Revit API calls wrapped in abstractions
└── Integration tests with mocked Revit or Test Framework

Layer 3: End-to-End Glue (5% of code)
├── Full workflow validation
├── User-facing feature testing
└── Run weekly or before releases
```

**Restored Pyramid:**
```
      /\
     /E2E\      5% - Acceptance tests (weekly)
    /──────\
   / Layer2 \  15% - Integration (nightly CI)
  /──────────\
 /  Layer 1   \ 80% - Unit tests (every commit)
/______________\
```

---

## 2. Test Structure

### 2.1 Directory Layout

```
RevitAI.CSharp/
├── tests/
│   ├── RevitAI.Tests/                   # Main test project
│   │   ├── Unit/                        # Layer 1 - Pure logic tests
│   │   │   ├── ClaudeServiceTests.cs
│   │   │   ├── SafetyValidatorTests.cs
│   │   │   ├── TagPlacementServiceTests.cs
│   │   │   ├── CollisionDetectorTests.cs
│   │   │   └── HybridCommandRouterTests.cs
│   │   ├── Integration/                 # Layer 2 - Mocked Revit API
│   │   │   ├── TagCreationServiceTests.cs
│   │   │   ├── DimensionServiceTests.cs
│   │   │   └── MCPServerIntegrationTests.cs
│   │   ├── Acceptance/                  # Layer 3 - E2E workflows
│   │   │   ├── AutoTagWorkflowTests.cs
│   │   │   ├── DimensionWorkflowTests.cs
│   │   │   └── MCPClientTests.cs
│   │   ├── Benchmarks/                  # Academic benchmark suite
│   │   │   ├── queries.json             # 80+ test queries
│   │   │   ├── expected_operations.json
│   │   │   ├── BenchmarkRunner.cs
│   │   │   └── results/
│   │   │       ├── accuracy_report.md
│   │   │       └── benchmark_<date>.json
│   │   ├── Fixtures/                    # Test data and mocks
│   │   │   ├── MockRevitAPI.cs
│   │   │   ├── TestDocuments/
│   │   │   │   ├── simple_room.rvt
│   │   │   │   ├── complex_geometry.rvt
│   │   │   │   └── multi_level.rvt
│   │   │   └── ExpectedOutputs/
│   │   │       └── auto_tag_24_doors.json
│   │   └── RevitAI.Tests.csproj
│   └── RevitAI.UITests/                 # Selenium-based UI tests (future)
│       └── RevitAI.UITests.csproj
├── Services/                            # Source code
└── RevitAI.csproj
```

### 2.2 Test Naming Convention

```csharp
[TestClass]
public class ClaudeServiceTests {
    // Pattern: MethodName_Scenario_ExpectedBehavior

    [TestMethod]
    public async Task ParsePromptAsync_HebrewTagPrompt_ReturnsAutoTagAction() {
        // Arrange
        var service = new ClaudeService("test_api_key");
        var prompt = "תייג את כל הדלתות בקומה 1";

        // Act
        var action = await service.ParsePromptAsync(prompt, context);

        // Assert
        Assert.AreEqual("auto_tag", action.Operation);
        Assert.AreEqual("Doors", action.Targets["category"]);
    }

    [TestMethod]
    public void ValidateOperation_ExceedsMaxElements_ReturnsRejected() {
        // Arrange
        var validator = new SafetyValidator();
        var action = new RevitAction {
            Operation = "auto_tag",
            Targets = { ["element_count"] = 600 } // Exceeds 500 limit
        };

        // Act
        var result = validator.ValidateOperation(action);

        // Assert
        Assert.IsFalse(result.IsApproved);
        Assert.IsTrue(result.ErrorMessage.Contains("Too many elements"));
    }
}
```

---

## 3. Layer 1: Unit Tests (Pure Logic)

### 3.1 Characteristics
- **No Revit API dependencies** - tests run in <100ms
- **High coverage target** - 80%+ code coverage
- **Run on every commit** - fast feedback loop
- **Framework:** NUnit 3.x or MSTest

### 3.2 What to Test

**Claude Prompt Parsing:**
```csharp
[TestClass]
public class ClaudeServiceTests {
    [TestMethod]
    public async Task ParsePromptAsync_EnglishTagPrompt_ReturnsCorrectScope() {
        // Test: "Tag all doors in Level 1" → scope: "Level 1"
    }

    [TestMethod]
    public async Task ParsePromptAsync_HebrewDimensionPrompt_ReturnsCorrectOperation() {
        // Test: "תוסיף מידות פנימיות" → operation: "create_dimensions"
    }

    [TestMethod]
    public async Task ParsePromptAsync_AmbiguousPrompt_ReturnsClarificationQuestions() {
        // Test: "Tag doors" → clarifications: ["Which tag type?"]
    }
}
```

**Safety Validation:**
```csharp
[TestClass]
public class SafetyValidatorTests {
    [TestMethod]
    public void ValidateOperation_AllowedOperation_ReturnsApproved() {
        // Test: "auto_tag" → approved
    }

    [TestMethod]
    public void ValidateOperation_BlockedOperation_ReturnsRejected() {
        // Test: "delete_elements" → rejected
    }

    [TestMethod]
    public void ValidateOperation_ExceedsScope_ReturnsRejected() {
        // Test: 600 elements > 500 limit → rejected
    }

    [TestMethod]
    public void ValidateOperation_InvalidTagType_ReturnsRejected() {
        // Test: tag_type "NonExistent" → rejected
    }
}
```

**Tag Placement Logic:**
```csharp
[TestClass]
public class TagPlacementServiceTests {
    [TestMethod]
    public void CalculatePlacements_NoCollisions_AllSucceed() {
        // Test: 10 doors, no overlapping tags → 10 success
    }

    [TestMethod]
    public void CalculatePlacements_WithCollisions_TriesAlternatives() {
        // Test: Crowded area → placement engine tries 10 positions
    }

    [TestMethod]
    public void GetAlternativePlacement_MultipleAttempts_IncreasesDistance() {
        // Test: Attempt 1 → 0.5ft, Attempt 8 → 1.5ft
    }
}
```

**Collision Detection:**
```csharp
[TestClass]
public class SimpleBoundingBoxCollisionDetectorTests {
    [TestMethod]
    public void HasCollision_NoOverlap_ReturnsFalse() {
        // Test: Tag at (0,0) vs tag at (5,5) → no collision
    }

    [TestMethod]
    public void HasCollision_PartialOverlap_ReturnsTrue() {
        // Test: Tag at (0,0) vs tag at (0.2,0.1) → collision
    }

    [TestMethod]
    public void EstimateTagBounds_StandardTag_ReturnsCorrectSize() {
        // Test: "Door 101" → ~0.3ft x 0.15ft bounds
    }
}
```

### 3.3 Test Data Builders

Use builder pattern for complex test data:

```csharp
public class RevitActionBuilder {
    private string _operation = "auto_tag";
    private Dictionary<string, object> _targets = new();
    private Dictionary<string, object> _parameters = new();

    public RevitActionBuilder WithOperation(string operation) {
        _operation = operation;
        return this;
    }

    public RevitActionBuilder ForDoors() {
        _targets["category"] = "Doors";
        return this;
    }

    public RevitActionBuilder InCurrentView() {
        _targets["scope"] = "current_view";
        return this;
    }

    public RevitActionBuilder WithTagType(string tagType) {
        _parameters["tag_type"] = tagType;
        return this;
    }

    public RevitActionBuilder WithElementCount(int count) {
        _targets["element_count"] = count;
        return this;
    }

    public RevitAction Build() {
        return new RevitAction {
            Operation = _operation,
            Targets = _targets,
            Parameters = _parameters
        };
    }
}

// Usage in tests:
var action = new RevitActionBuilder()
    .ForDoors()
    .InCurrentView()
    .WithTagType("Door Tag")
    .WithElementCount(24)
    .Build();
```

---

## 4. Layer 2: Integration Tests (Mocked Revit API)

### 4.1 Characteristics
- **Mocked Revit API** - no full Revit required
- **Medium speed** - 1-5 seconds per test
- **Run nightly** or on PR merges
- **Framework:** NUnit + Moq or custom mocks

### 4.2 Mock Strategy

**Interface Abstraction:**
```csharp
// Interfaces for Revit API wrappers
public interface IRoomAnalyzer {
    IEnumerable<Room> GetAllRooms(Document doc);
    IEnumerable<Wall> GetBoundingWalls(Room room);
}

public interface ITagFactory {
    IndependentTag CreateTag(Document doc, TagType tagType, Reference reference,
                              bool leader, XYZ location);
    IEnumerable<IndependentTag> GetExistingTags(Document doc, View view);
}
```

**Mock Implementations:**
```csharp
// Fixtures/MockRevitAPI.cs
public class MockDocument {
    public List<MockElement> Elements { get; set; } = new();
    public List<MockTag> CreatedTags { get; set; } = new();
    public bool TransactionCommitted { get; set; } = false;

    public static MockDocument CreateWithElements(int doors = 0, int walls = 0, int rooms = 0) {
        var doc = new MockDocument();

        // Create mock doors
        for (int i = 0; i < doors; i++) {
            doc.Elements.Add(new MockElement {
                Id = new ElementId(1000 + i),
                Category = "Doors",
                Location = new XYZ(i * 10, 0, 0),
                Name = $"Door {i + 1}"
            });
        }

        // Create mock walls
        for (int i = 0; i < walls; i++) {
            doc.Elements.Add(new MockElement {
                Id = new ElementId(2000 + i),
                Category = "Walls",
                Location = new XYZ(0, i * 10, 0),
                Name = $"Wall {i + 1}"
            });
        }

        return doc;
    }
}

public class MockTagFactory : ITagFactory {
    private MockDocument _doc;

    public MockTagFactory(MockDocument doc) {
        _doc = doc;
    }

    public IndependentTag CreateTag(Document doc, TagType tagType, Reference reference,
                                     bool leader, XYZ location) {
        var mockTag = new MockTag {
            Id = new ElementId(_doc.CreatedTags.Count + 5000),
            TaggedElementId = reference.ElementId,
            Location = location,
            HasLeader = leader
        };

        _doc.CreatedTags.Add(mockTag);
        return mockTag as IndependentTag; // Cast to actual type for interface compliance
    }

    public IEnumerable<IndependentTag> GetExistingTags(Document doc, View view) {
        return _doc.CreatedTags.Cast<IndependentTag>();
    }
}
```

### 4.3 Integration Test Examples

**Tag Creation Workflow:**
```csharp
[TestClass]
public class TagCreationServiceIntegrationTests {
    private MockDocument _mockDoc;
    private MockTagFactory _mockTagFactory;
    private TagCreationService _service;

    [TestInitialize]
    public void Setup() {
        _mockDoc = MockDocument.CreateWithElements(doors: 24);
        _mockTagFactory = new MockTagFactory(_mockDoc);
        _service = new TagCreationService(_mockTagFactory, mockLogger);
    }

    [TestMethod]
    public async Task CreateTagsAsync_24Doors_AllTagsCreated() {
        // Arrange
        var placements = CreateMockPlacements(_mockDoc.Elements, allSuccess: true);

        // Act
        var result = await _service.CreateTagsAsync(_mockDoc, placements, mockTagType);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(24, _mockDoc.CreatedTags.Count);
        Assert.AreEqual(0, result.FailedCount);
    }

    [TestMethod]
    public async Task CreateTagsAsync_TransactionFails_RollsBack() {
        // Arrange
        _mockDoc.SimulateTransactionFailure = true;
        var placements = CreateMockPlacements(_mockDoc.Elements, allSuccess: true);

        // Act
        var result = await _service.CreateTagsAsync(_mockDoc, placements, mockTagType);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.AreEqual(0, _mockDoc.CreatedTags.Count); // Rolled back
        Assert.IsFalse(_mockDoc.TransactionCommitted);
    }

    [TestMethod]
    public async Task CreateTagsAsync_PartialFailures_CommitsSuccesses() {
        // Arrange
        var placements = CreateMockPlacements(_mockDoc.Elements, successCount: 20, failCount: 4);

        // Act
        var result = await _service.CreateTagsAsync(_mockDoc, placements, mockTagType);

        // Assert
        Assert.IsTrue(result.IsSuccess); // Partial success still true
        Assert.AreEqual(20, result.CreatedCount);
        Assert.AreEqual(4, result.FailedCount);
        Assert.IsTrue(_mockDoc.TransactionCommitted);
    }
}
```

**MCP Server Integration:**
```csharp
[TestClass]
public class MCPServerIntegrationTests {
    private MCPServer _server;
    private MockDocument _mockDoc;

    [TestInitialize]
    public void Setup() {
        _mockDoc = MockDocument.CreateWithElements(doors: 10);
        _server = new MCPServer(mockClaude, mockEvent, mockValidator, mockTagService);
        _server.Start(port: 48885); // Test port
    }

    [TestCleanup]
    public void Cleanup() {
        _server.Stop();
    }

    [TestMethod]
    public async Task MCPServer_ToolsList_ReturnsAllTools() {
        // Arrange
        var client = new HttpClient();
        var request = new {
            jsonrpc = "2.0",
            id = 1,
            method = "tools/list",
            @params = new { }
        };

        // Act
        var response = await client.PostAsync(
            "http://localhost:48885/mcp",
            new StringContent(JsonConvert.SerializeObject(request))
        );

        // Assert
        var body = await response.Content.ReadAsStringAsync();
        var mcpResponse = JsonConvert.DeserializeObject<MCPResponse>(body);

        Assert.IsNotNull(mcpResponse.Result);
        var tools = mcpResponse.Result["tools"] as JArray;
        Assert.IsTrue(tools.Count >= 4); // At least 4 tools registered
    }

    [TestMethod]
    public async Task MCPServer_AutoTagCall_CreatesTagsViaService() {
        // Arrange
        var request = new {
            jsonrpc = "2.0",
            id = 2,
            method = "tools/call",
            @params = new {
                name = "auto_tag_elements",
                arguments = new {
                    category = "Doors",
                    scope = "current_view",
                    tag_type = "Door Tag",
                    leader = false
                }
            }
        };

        // Act
        var response = await PostMCPRequest(_server, request);

        // Assert
        Assert.IsNull(response.Error);
        Assert.IsNotNull(response.Result);
        Assert.IsTrue(mockTagService.CreateTagsCalled); // Verify service invocation
    }
}
```

---

## 5. Layer 3: End-to-End Acceptance Tests

### 5.1 Characteristics
- **Full Revit required** - tests run in actual Revit instance
- **Slow** - 30 seconds to 5 minutes per test
- **Run weekly** or before major releases
- **Framework:** Revit Test Framework (RTF) or manual QA

### 5.2 Acceptance Test Structure

**Test Definition:**
```csharp
// tests/Acceptance/AutoTagWorkflowTests.cs
[TestFixture]
public class AutoTagWorkflowTests {
    private UIApplication _uiApp;
    private Document _doc;

    [SetUp]
    public void Setup() {
        // RTF provides UIApplication automatically
        _uiApp = RevitTestFramework.UIApplication;

        // Open test fixture
        var testFilePath = @"C:\RevitAI\tests\Fixtures\TestDocuments\simple_room.rvt";
        _doc = _uiApp.Application.OpenDocumentFile(testFilePath);
    }

    [TearDown]
    public void Teardown() {
        if (_doc != null && !_doc.IsModified) {
            _doc.Close(false); // Close without saving
        }
    }

    [Test]
    public void AutoTag_24DoorsInLevel1_AllTagsCreated() {
        // Arrange: Verify document has 24 untagged doors
        var doors = new FilteredElementCollector(_doc)
            .OfCategory(BuiltInCategory.OST_Doors)
            .WhereElementIsNotElementType()
            .ToElements();

        Assert.AreEqual(24, doors.Count, "Test fixture should have 24 doors");

        // Act: Invoke RevitAI copilot command
        var copilotCommand = new CopilotCommand();
        copilotCommand.Execute(_uiApp, "Tag all doors in Level 1");

        // User interaction simulation: Wait for preview dialog, click Confirm
        System.Threading.Thread.Sleep(3000); // Wait for Claude API response
        // TODO: Automate dialog interaction via UI Automation

        // Assert: Verify 24 tags created
        var tags = new FilteredElementCollector(_doc)
            .OfClass(typeof(IndependentTag))
            .Cast<IndependentTag>()
            .Where(t => t.TaggedElementId.IntegerValue >= doors.First().Id.IntegerValue)
            .ToList();

        Assert.AreEqual(24, tags.Count, "Should have created 24 tags");

        // Verify tags are properly positioned (no overlaps)
        var bounds = tags.Select(t => t.TagHeadPosition).ToList();
        for (int i = 0; i < bounds.Count; i++) {
            for (int j = i + 1; j < bounds.Count; j++) {
                var distance = bounds[i].DistanceTo(bounds[j]);
                Assert.IsTrue(distance > 0.3, $"Tags {i} and {j} are too close ({distance}ft)");
            }
        }
    }

    [Test]
    public void AutoTag_UndoAfterConfirm_RemovesAllTags() {
        // Act: Create tags
        var copilotCommand = new CopilotCommand();
        copilotCommand.Execute(_uiApp, "Tag all doors");
        System.Threading.Thread.Sleep(3000); // Wait for completion

        var tagsBeforeUndo = new FilteredElementCollector(_doc)
            .OfClass(typeof(IndependentTag))
            .ToElements()
            .Count;

        Assert.IsTrue(tagsBeforeUndo > 0, "Tags should be created");

        // Act: Undo
        _uiApp.Application.PostCommand(RevitCommandId.LookupPostableCommandId(PostableCommand.Undo));
        System.Threading.Thread.Sleep(1000);

        // Assert: Tags removed
        var tagsAfterUndo = new FilteredElementCollector(_doc)
            .OfClass(typeof(IndependentTag))
            .ToElements()
            .Count;

        Assert.AreEqual(0, tagsAfterUndo, "Undo should remove all tags");
    }
}
```

### 5.3 Running E2E Tests

**Using Revit Test Framework (RTF):**
```bash
# Install RTF
# Download from https://github.com/DynamoDS/RevitTestFramework

# Run tests
"C:\Program Files\RTF\RevitTestFrameworkConsole.exe" ^
  --dir "C:\RevitAI\tests\Acceptance" ^
  --assembly "RevitAI.Tests.dll" ^
  --results "C:\RevitAI\tests\results" ^
  --revit "2026" ^
  --continuous
```

**Test Results:**
```
==============================
Revit Test Framework Results
==============================
Assembly: RevitAI.Tests.dll
Revit: 2026
Tests Run: 5
Passed: 4
Failed: 1
Time: 8 minutes 32 seconds

PASSED: AutoTag_24DoorsInLevel1_AllTagsCreated (2m 15s)
PASSED: AutoTag_UndoAfterConfirm_RemovesAllTags (1m 48s)
PASSED: Dimension_SimpleRoom_CreatesChains (2m 52s)
PASSED: MCP_ExternalClient_InvokesAutoTag (1m 12s)
FAILED: AutoTag_CurvedWalls_HandlesGracefully (25s)
  Error: NullReferenceException at TagPlacementService.cs:145
```

---

## 6. Benchmark Suite (Academic Validation)

### 6.1 Rationale

Research findings (Task 2: PyRevit+LLM Integration Analysis) cite academic papers:
- **BIMCoder** (2025): Created dataset of 1,680 natural language + structured query pairs
- **Synergistic BIM Aligners** (2024): Evaluated on 80 queries of varying complexity
- **Natural Language BIM Information Retrieval** (2025): Achieved 80% accuracy across architectural, structural, MEP domains

**Goal:** Create open benchmark for RevitAI to:
1. Measure accuracy objectively (vs. subjective "it works")
2. Track improvements over time (version to version)
3. Publish results for competitive differentiation
4. Identify weak areas (e.g., curved walls, angled geometry)

### 6.2 Query Dataset Structure

**File:** `tests/Benchmarks/queries.json`

```json
{
  "metadata": {
    "version": "1.0",
    "date": "2025-11-20",
    "total_queries": 85,
    "categories": {
      "auto_tagging": 30,
      "dimensioning": 25,
      "querying": 20,
      "parameter_updates": 10
    },
    "languages": {
      "hebrew": 45,
      "english": 40
    }
  },
  "queries": [
    {
      "id": "AT-001",
      "category": "auto_tagging",
      "language": "english",
      "prompt": "Tag all doors in Level 1",
      "context": {
        "active_view": "Level 1 Floor Plan",
        "available_levels": ["Level 1", "Level 2", "Roof"],
        "element_summary": {
          "Doors": 24,
          "Walls": 156,
          "Rooms": 18
        }
      },
      "expected_operation": "auto_tag",
      "expected_targets": {
        "category": "Doors",
        "scope": "Level 1"
      },
      "expected_parameters": {
        "tag_type": "Door Tag"
      },
      "difficulty": "easy"
    },
    {
      "id": "AT-002",
      "category": "auto_tagging",
      "language": "hebrew",
      "prompt": "תייג רק את הדלתות שעדיין לא מתוייגות",
      "context": {
        "active_view": "Level 1 Floor Plan",
        "element_summary": {
          "Doors": 24,
          "Doors_Untagged": 12
        }
      },
      "expected_operation": "auto_tag",
      "expected_targets": {
        "category": "Doors",
        "filter": "untagged_only"
      },
      "difficulty": "medium",
      "notes": "Tests conditional filtering logic"
    },
    {
      "id": "AT-015",
      "category": "auto_tagging",
      "language": "english",
      "prompt": "Add room tags with leaders for all rooms on Level 2, but only if they don't already have tags",
      "context": {
        "active_view": "Level 2 Floor Plan",
        "element_summary": {
          "Rooms": 18,
          "Rooms_Tagged": 6,
          "Rooms_Untagged": 12
        }
      },
      "expected_operation": "auto_tag",
      "expected_targets": {
        "category": "Rooms",
        "scope": "Level 2",
        "filter": "untagged_only"
      },
      "expected_parameters": {
        "tag_type": "Room Tag",
        "leader": true
      },
      "difficulty": "hard",
      "notes": "Tests complex multi-condition prompt"
    },
    {
      "id": "DIM-001",
      "category": "dimensioning",
      "language": "english",
      "prompt": "Add internal dimensions to all rooms",
      "context": {
        "active_view": "Level 1 Floor Plan",
        "element_summary": {
          "Rooms": 12
        }
      },
      "expected_operation": "create_dimensions",
      "expected_targets": {
        "scope": "all_rooms"
      },
      "expected_parameters": {
        "dimension_type": "internal",
        "offset_mm": 200
      },
      "difficulty": "medium"
    }
    // ... 81 more queries
  ]
}
```

### 6.3 Benchmark Runner

```csharp
// tests/Benchmarks/BenchmarkRunner.cs
public class BenchmarkRunner {
    private readonly ClaudeService _claudeService;
    private readonly BenchmarkDataset _dataset;
    private readonly List<BenchmarkResult> _results = new();

    public async Task<BenchmarkReport> RunBenchmark() {
        Console.WriteLine($"Running benchmark with {_dataset.Queries.Count} queries...");

        foreach (var query in _dataset.Queries) {
            Console.Write($"[{query.Id}] {query.Prompt.Substring(0, Math.Min(50, query.Prompt.Length))}... ");

            try {
                // Act: Parse prompt
                var action = await _claudeService.ParsePromptAsync(query.Prompt, query.Context);

                // Evaluate: Compare to expected
                var result = EvaluateResult(query, action);
                _results.Add(result);

                Console.WriteLine(result.IsCorrect ? "✓ PASS" : "✗ FAIL");

                if (!result.IsCorrect) {
                    Console.WriteLine($"  Expected: {query.ExpectedOperation}");
                    Console.WriteLine($"  Got: {action.Operation}");
                }
            } catch (Exception ex) {
                _results.Add(BenchmarkResult.Error(query.Id, ex.Message));
                Console.WriteLine($"✗ ERROR: {ex.Message}");
            }
        }

        return GenerateReport();
    }

    private BenchmarkResult EvaluateResult(BenchmarkQuery query, RevitAction action) {
        var result = new BenchmarkResult {
            QueryId = query.Id,
            Category = query.Category,
            Difficulty = query.Difficulty,
            Language = query.Language
        };

        // Check operation
        if (action.Operation != query.ExpectedOperation) {
            result.IsCorrect = false;
            result.Error = $"Operation mismatch: expected {query.ExpectedOperation}, got {action.Operation}";
            return result;
        }

        // Check targets
        foreach (var (key, expectedValue) in query.ExpectedTargets) {
            if (!action.Targets.TryGetValue(key, out var actualValue) ||
                actualValue.ToString() != expectedValue.ToString()) {
                result.IsCorrect = false;
                result.Error = $"Target '{key}' mismatch: expected {expectedValue}, got {actualValue}";
                return result;
            }
        }

        // Check parameters
        foreach (var (key, expectedValue) in query.ExpectedParameters) {
            if (!action.Parameters.TryGetValue(key, out var actualValue) ||
                actualValue.ToString() != expectedValue.ToString()) {
                result.IsCorrect = false;
                result.Error = $"Parameter '{key}' mismatch";
                return result;
            }
        }

        result.IsCorrect = true;
        return result;
    }

    private BenchmarkReport GenerateReport() {
        var report = new BenchmarkReport {
            TotalQueries = _results.Count,
            CorrectCount = _results.Count(r => r.IsCorrect),
            ErrorCount = _results.Count(r => r.Error != null),
            AccuracyRate = (double)_results.Count(r => r.IsCorrect) / _results.Count
        };

        // Breakdown by category
        report.ByCategory = _results
            .GroupBy(r => r.Category)
            .ToDictionary(
                g => g.Key,
                g => new CategoryStats {
                    Total = g.Count(),
                    Correct = g.Count(r => r.IsCorrect),
                    AccuracyRate = (double)g.Count(r => r.IsCorrect) / g.Count()
                }
            );

        // Breakdown by difficulty
        report.ByDifficulty = _results
            .GroupBy(r => r.Difficulty)
            .ToDictionary(
                g => g.Key,
                g => new DifficultyStats {
                    Total = g.Count(),
                    Correct = g.Count(r => r.IsCorrect),
                    AccuracyRate = (double)g.Count(r => r.IsCorrect) / g.Count()
                }
            );

        // Breakdown by language
        report.ByLanguage = _results
            .GroupBy(r => r.Language)
            .ToDictionary(
                g => g.Key,
                g => new LanguageStats {
                    Total = g.Count(),
                    Correct = g.Count(r => r.IsCorrect),
                    AccuracyRate = (double)g.Count(r => r.IsCorrect) / g.Count()
                }
            );

        return report;
    }
}

// Run benchmark from command line
public class Program {
    public static async Task Main(string[] args) {
        var dataset = BenchmarkDataset.Load("tests/Benchmarks/queries.json");
        var claudeService = new ClaudeService(Environment.GetEnvironmentVariable("CLAUDE_API_KEY"));

        var runner = new BenchmarkRunner(claudeService, dataset);
        var report = await runner.RunBenchmark();

        // Save results
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var resultsPath = $"tests/Benchmarks/results/benchmark_{timestamp}.json";
        File.WriteAllText(resultsPath, JsonConvert.SerializeObject(report, Formatting.Indented));

        // Generate markdown report
        var markdownPath = $"tests/Benchmarks/results/accuracy_report.md";
        File.WriteAllText(markdownPath, report.ToMarkdown());

        Console.WriteLine($"\n==============================");
        Console.WriteLine($"Benchmark Complete");
        Console.WriteLine($"==============================");
        Console.WriteLine($"Total Queries: {report.TotalQueries}");
        Console.WriteLine($"Correct: {report.CorrectCount}");
        Console.WriteLine($"Errors: {report.ErrorCount}");
        Console.WriteLine($"Accuracy: {report.AccuracyRate:P2}");
        Console.WriteLine($"\nResults saved to: {resultsPath}");
    }
}
```

### 6.4 Benchmark Report Format

**File:** `tests/Benchmarks/results/accuracy_report.md`

```markdown
# RevitAI Benchmark Report

**Date:** 2025-11-20 14:35:22
**Version:** RevitAI v0.2.0 (Epic 2 Complete)
**Model:** Claude Sonnet 4.5
**Dataset:** 85 queries (v1.0)

---

## Overall Results

| Metric | Value |
|--------|-------|
| Total Queries | 85 |
| Correct | 68 |
| Errors | 3 |
| Accuracy | **80.00%** |

---

## Breakdown by Category

| Category | Total | Correct | Accuracy |
|----------|-------|---------|----------|
| Auto-Tagging | 30 | 27 | 90.00% ✓ |
| Dimensioning | 25 | 18 | 72.00% ⚠ |
| Querying | 20 | 19 | 95.00% ✓ |
| Parameter Updates | 10 | 4 | 40.00% ✗ |

---

## Breakdown by Difficulty

| Difficulty | Total | Correct | Accuracy |
|------------|-------|---------|----------|
| Easy | 35 | 34 | 97.14% ✓ |
| Medium | 35 | 27 | 77.14% ⚠ |
| Hard | 15 | 7 | 46.67% ✗ |

---

## Breakdown by Language

| Language | Total | Correct | Accuracy |
|----------|-------|---------|----------|
| English | 40 | 33 | 82.50% ✓ |
| Hebrew | 45 | 35 | 77.78% ⚠ |

---

## Failed Queries Analysis

### High Priority (Easy queries that failed)

1. **AT-012** (Easy, English): "Tag all windows"
   - Expected: `auto_tag`, category: Windows
   - Got: `auto_tag`, category: Doors
   - **Root Cause:** Prompt ambiguity - "windows" interpreted as "openings"

### Medium Priority (Medium queries that failed)

2. **DIM-018** (Medium, Hebrew): "תוסיף מידות רק לחדרים מעל 20 מ\"ר"
   - Expected: `create_dimensions`, filter: area > 20sqm
   - Got: `create_dimensions`, filter: none
   - **Root Cause:** Conditional logic not parsed correctly

3. **PARAM-005** (Medium, English): "Set fire rating to 2 hours for all corridor walls"
   - Expected: `update_parameter`, category: Walls, filter: room_type==corridor
   - Got: ERROR - Operation not allowed
   - **Root Cause:** Safety validator blocks parameter updates (not implemented yet)

---

## Comparison to Academic Benchmarks

| Benchmark | Accuracy | Notes |
|-----------|----------|-------|
| **BIMCoder (2025)** | 80% | Structured query translation |
| **Synergistic BIM Aligners (2024)** | ~75% | Complex queries across 80 samples |
| **RevitAI v0.2.0** | **80%** | Comparable to state-of-art research |

---

## Recommendations

1. **Improve Hebrew conditional parsing** (7 failures)
   - Add more Hebrew training examples to system prompt
   - Consider fine-tuning or RAG with Hebrew Revit terminology

2. **Implement parameter update operations** (10 failures)
   - Currently blocked by safety validator
   - Add to Epic 3 scope

3. **Handle ambiguous terms** (3 failures)
   - "windows" vs. "Window" category
   - Add clarification questions for ambiguous prompts

4. **Edge case handling** (5 failures)
   - Curved walls, angled geometry
   - Implement in Epic 2 Story 2.5 (deferred)

---

_Generated by RevitAI Benchmark Runner v1.0_
```

---

## 7. CI/CD Integration

### 7.1 GitHub Actions Workflow

**File:** `.github/workflows/test.yml`

```yaml
name: RevitAI Tests

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main ]

jobs:
  unit-tests:
    name: Unit Tests (Layer 1)
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore RevitAI.CSharp/RevitAI.csproj

      - name: Build
        run: dotnet build RevitAI.CSharp/RevitAI.csproj --configuration Release --no-restore

      - name: Run Unit Tests
        run: dotnet test RevitAI.CSharp/tests/RevitAI.Tests/RevitAI.Tests.csproj --filter "TestCategory=Unit" --logger "trx;LogFileName=test-results.trx"

      - name: Publish Test Results
        uses: EnricoMi/publish-unit-test-result-action@v2
        if: always()
        with:
          files: '**/test-results.trx'

  integration-tests:
    name: Integration Tests (Layer 2)
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Restore dependencies
        run: dotnet restore

      - name: Run Integration Tests (Mocked Revit)
        run: dotnet test --filter "TestCategory=Integration" --logger "trx;LogFileName=integration-results.trx"

      - name: Publish Results
        uses: EnricoMi/publish-unit-test-result-action@v2
        if: always()
        with:
          files: '**/integration-results.trx'

  benchmark:
    name: Accuracy Benchmark
    runs-on: ubuntu-latest
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v3

      - name: Setup .NET
        uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'

      - name: Run Benchmark
        env:
          CLAUDE_API_KEY: ${{ secrets.CLAUDE_API_KEY }}
        run: dotnet run --project RevitAI.CSharp/tests/Benchmarks/BenchmarkRunner.csproj

      - name: Upload Benchmark Results
        uses: actions/upload-artifact@v3
        with:
          name: benchmark-results
          path: tests/Benchmarks/results/

      - name: Comment PR with Accuracy
        uses: actions/github-script@v6
        if: github.event_name == 'pull_request'
        with:
          script: |
            const fs = require('fs');
            const report = JSON.parse(fs.readFileSync('tests/Benchmarks/results/accuracy_report.json'));
            github.rest.issues.createComment({
              issue_number: context.issue.number,
              owner: context.repo.owner,
              repo: context.repo.repo,
              body: `## Benchmark Results\n\n**Accuracy:** ${(report.AccuracyRate * 100).toFixed(2)}%\n**Queries:** ${report.TotalQueries}\n**Correct:** ${report.CorrectCount}`
            });

  # E2E tests run on self-hosted Windows runner with Revit installed
  e2e-tests:
    name: E2E Tests (Layer 3)
    runs-on: self-hosted  # Requires Windows machine with Revit 2026
    if: github.event_name == 'push' && github.ref == 'refs/heads/main'
    steps:
      - uses: actions/checkout@v3

      - name: Build RevitAI
        run: dotnet build RevitAI.CSharp/RevitAI.csproj --configuration Release

      - name: Deploy to Revit Addins
        run: |
          Copy-Item -Path "RevitAI.CSharp/bin/Release/*" -Destination "$env:APPDATA/Autodesk/Revit/Addins/2026/RevitAI/" -Recurse -Force

      - name: Run E2E Tests with RTF
        run: |
          & "C:\Program Files\RTF\RevitTestFrameworkConsole.exe" `
            --dir "tests/Acceptance" `
            --assembly "RevitAI.Tests.dll" `
            --results "tests/results" `
            --revit "2026" `
            --continuous

      - name: Upload E2E Results
        uses: actions/upload-artifact@v3
        if: always()
        with:
          name: e2e-results
          path: tests/results/
```

### 7.2 Test Execution Timeline

| Trigger | Tests Run | Duration | Environment |
|---------|-----------|----------|-------------|
| **Every Commit** | Unit (Layer 1) | 2-5 minutes | GitHub Actions (Ubuntu) |
| **Every PR** | Unit + Integration (Layer 1-2) | 10-15 minutes | GitHub Actions (Ubuntu) |
| **Daily (Nightly)** | Unit + Integration + Benchmark | 30-45 minutes | GitHub Actions + Claude API |
| **Weekly** | Full Suite (Layer 1-2-3) | 2-3 hours | Self-hosted Windows + Revit |
| **Before Release** | Full Suite + Manual QA | 4-8 hours | Self-hosted + QA Team |

---

## 8. Test Coverage Targets

### 8.1 Coverage Goals

| Layer | Coverage Target | Rationale |
|-------|----------------|-----------|
| Layer 1 (Pure Logic) | **80%+** | Fast tests, no excuses for low coverage |
| Layer 2 (API Wrapper) | **60%+** | Interface contracts, happy/sad paths |
| Layer 3 (E2E Glue) | **Manual QA** | Too slow for automated coverage |

### 8.2 Coverage Tools

```bash
# Install coverage tool
dotnet tool install --global dotnet-coverage

# Run tests with coverage
dotnet coverage collect "dotnet test" --output coverage.xml

# Generate HTML report
dotnet coverage merge coverage.xml --output coverage-report --output-format html

# View report
open coverage-report/index.html
```

### 8.3 Coverage Enforcement

**GitHub Actions Check:**
```yaml
- name: Check Coverage
  run: |
    dotnet test --collect:"XPlat Code Coverage"
    coverage=$(grep -oP 'Line coverage: \K[0-9.]+' coverage.xml)
    if (( $(echo "$coverage < 80.0" | bc -l) )); then
      echo "Coverage $coverage% is below 80% threshold"
      exit 1
    fi
```

---

## 9. Quality Checklist

Before merging to main branch:

### 9.1 Code Quality
- [ ] All unit tests pass (Layer 1)
- [ ] All integration tests pass (Layer 2)
- [ ] Code coverage >= 80% (Layer 1)
- [ ] No compiler warnings
- [ ] Code follows C# conventions (PascalCase, XML comments)
- [ ] No hardcoded credentials or secrets

### 9.2 Functional Quality
- [ ] Benchmark accuracy >= 75% overall
- [ ] Auto-tagging accuracy >= 85%
- [ ] No P0 bugs (crashes, data corruption)
- [ ] Preview/confirm workflow tested manually
- [ ] Undo (Ctrl+Z) works correctly
- [ ] Audit log captures operation details

### 9.3 Safety Quality
- [ ] Safety validator enforces scope limits
- [ ] Blocked operations return clear error messages
- [ ] Transactions roll back on errors
- [ ] No orphaned elements created on failure
- [ ] Maximum element count enforced (500 default)

### 9.4 Performance Quality
- [ ] Unit tests complete in < 5 minutes
- [ ] Integration tests complete in < 30 minutes
- [ ] Tag 500 elements in < 30 seconds (integration test)
- [ ] Claude API calls timeout after 10 seconds
- [ ] Preview graphics render in < 2 seconds

### 9.5 Documentation Quality
- [ ] Architecture.md updated with new ADRs
- [ ] CLAUDE.md updated with usage examples
- [ ] README includes installation instructions
- [ ] Inline code comments for complex logic
- [ ] Benchmark report published to docs/

---

## 10. Future Enhancements

### 10.1 Advanced Testing Techniques

**Visual Regression Testing:**
- Capture screenshots of preview dialogs
- Compare against baseline images
- Detect unintended UI changes

**Performance Profiling:**
- Track execution time for each operation
- Identify performance regressions
- Profile memory usage for large models (500+ rooms)

**Chaos Engineering:**
- Simulate network failures (Claude API timeout)
- Simulate Revit API errors (transaction failures)
- Test graceful degradation

### 10.2 Test Data Generation

**Synthetic Revit Models:**
- Use Revit API to generate test fixtures programmatically
- Create models with specific characteristics (curved walls, angled geometry)
- Avoid reliance on brittle .rvt files

**Property-Based Testing:**
- Use FsCheck or similar library
- Generate random prompts, verify safety validator catches unsafe operations
- Fuzz test MCP JSON-RPC endpoint

### 10.3 Continuous Benchmarking

**Track Accuracy Over Time:**
```
Version  | Date       | Accuracy | Notes
---------|------------|----------|---------------------------
v0.1.0   | 2025-11-15 | 72%      | Epic 1 baseline
v0.2.0   | 2025-11-20 | 80%      | Epic 2: Auto-tagging added
v0.3.0   | 2025-12-01 | 83%      | RAG hybrid improved parsing
v0.4.0   | 2025-12-15 | 86%      | Local LLM fine-tuning
```

**Publish Leaderboard:**
- Public dashboard showing RevitAI accuracy vs. competitors
- Breakdown by category, difficulty, language
- Link to open-source benchmark dataset

---

## Research Attribution

This testing framework incorporates findings from:

**Task 3: Revit API Testing Strategies Research** (2025-11-20)
- SIL Architecture (Layer 1/2/3 separation)
- Testing Pyramid inversion problem
- Studio Tema risk mitigation case study
- Academic benchmarking standards (80-query datasets)

**Task 2: PyRevit+LLM Integration Analysis** (2025-11-20)
- BIMCoder: 80% accuracy benchmark
- Synergistic BIM Aligners: 80-query evaluation
- Gap analysis: No open benchmarking framework exists

**Quote from Task 3 Research:**
> "The 'Base'—Unit Testing—is vanishingly small or non-existent in many projects... Developers are forced to rely heavily on the 'Apex'—E2E testing. This is the 'Inverted Pyramid' or 'Ice Cream Cone' anti-pattern."

---

_Generated: 2025-11-20_
_Status: Framework Defined (Ready for Implementation)_
_Next Steps: Implement Layer 1 unit tests for Epic 2 stories_
