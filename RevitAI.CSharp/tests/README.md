# RevitAI Unit Tests

This directory contains comprehensive unit tests for the RevitAI project following the **SIL (Separation of Interface Layers)** architecture.

## Test Organization

```
tests/
├── RevitAI.UnitTests/           # Layer 1 - Pure business logic tests (NO Revit dependencies)
│   ├── AutoTagValidationTests.cs       (Story 2.1 - 24 tests)
│   ├── RevitActionModelTests.cs        (Story 2.1 - 21 tests)
│   ├── SafetyValidatorTests.cs         (Epic 1 - 12 tests)
│   ├── DimensionPlanningServiceTests.cs
│   └── ...
└── RevitAI.UITests/            # Layer 3 - UI/Integration tests (requires Revit)
```

## Running Tests

### Run All Unit Tests
```bash
cd /home/thh3/personal/revit-ai/RevitAI.CSharp/tests/RevitAI.UnitTests
dotnet test
```

### Run Specific Story Tests
```bash
# Story 2.1 tests only
dotnet test --filter "Category=Story2.1"

# Auto-tagging tests only
dotnet test --filter "Category=AutoTag"

# Safety validation tests only
dotnet test --filter "Category=Safety"
```

### Run with Detailed Output
```bash
dotnet test --logger "console;verbosity=detailed"
```

### Run with Code Coverage
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## Test Execution Results (as of 2025-11-21)

**Total Tests:** 127
**Passing:** 127 (100%)
**Execution Time:** <600ms

### Test Breakdown by Story
- **Story 2.1 (Auto-Tagging):** 45 tests (100% pass)
- **Epic 1 (Foundation):** 12 tests (100% pass)
- **Epic 2 (Dimension Planning):** 70 tests (100% pass)

## Test Categories

Tests are organized by category for easy filtering:

| Category | Description | Test Count |
|----------|-------------|------------|
| `Unit` | Pure unit tests (Layer 1) | 127 |
| `Story2.1` | Story 2.1 auto-tagging tests | 45 |
| `AutoTag` | Auto-tagging functionality | 24 |
| `Model` | Data model tests | 21 |
| `Safety` | Safety validation tests | 12 |

## SIL Testing Architecture

The test suite follows the **SIL (Separation of Interface Layers)** pattern:

### Layer 1: Pure Business Logic (Unit Tests)
- **Location:** `RevitAI.UnitTests/`
- **Dependencies:** NONE (pure C# logic)
- **Execution:** Milliseconds
- **CI/CD:** ✅ Runs on Linux/Windows/Mac
- **Coverage:** 80% of codebase

**Examples:**
- `SafetyValidator.ValidateAutoTag()` - Pure validation logic
- `RevitAction.NeedsClarification` - Property logic
- `DimensionPlanningService` - Geometric calculations

### Layer 2: Integration Tests (With Mocks)
- **Location:** `RevitAI.UnitTests/` (with Moq)
- **Dependencies:** Mocked Revit API
- **Execution:** Seconds
- **CI/CD:** ✅ Runs on CI with mocks
- **Coverage:** 15% of codebase

**Examples:**
- `ClaudeService.ParsePromptAsync()` with mocked HTTP
- `RevitContextBuilder` with mocked Revit Document

### Layer 3: End-to-End Tests
- **Location:** `RevitAI.UITests/`
- **Dependencies:** Full Revit 2026 installation
- **Execution:** Minutes
- **CI/CD:** ⚠️ Requires Windows + Revit license
- **Coverage:** 5% of codebase (glue code)

**Examples:**
- Full auto-tagging workflow in real Revit project
- UI interaction testing

## Test Quality Standards

All tests in this suite follow these standards:

### 1. Arrange-Act-Assert (AAA) Pattern
```csharp
[Test]
public void ValidateAutoTag_ValidDoorTagging_ReturnsSuccess()
{
    // Arrange - Set up test data
    var action = new RevitAction { ... };

    // Act - Execute the method under test
    var result = _validator.Validate(action, elementCount: 50);

    // Assert - Verify the outcome
    Assert.That(result.IsValid, Is.True,
        "Valid auto_tag operation should succeed");
}
```

### 2. Descriptive Naming
Test names follow: `MethodName_Scenario_ExpectedBehavior`

Examples:
- `ValidateAutoTag_ElementCountExceedsMaxTags_ReturnsFailure`
- `NeedsClarification_ClarificationsEmpty_ReturnsFalse`
- `JsonDeserialization_AutoTagWithNewFields_Success`

### 3. Categorization
All tests have appropriate categories:
```csharp
[Test]
[Category("Unit")]
[Category("Story2.1")]
[Category("AutoTag")]
public void ValidateAutoTag_...() { }
```

### 4. Independence
- Each test has its own setup (via `[SetUp]` or inline)
- No shared mutable state between tests
- Tests can run in any order
- No test depends on another test's results

### 5. Comprehensive Coverage
Tests cover:
- ✅ Happy paths (valid inputs)
- ✅ Error paths (invalid inputs)
- ✅ Edge cases (null, empty, boundary values)
- ✅ Ambiguity handling ("?" values)

## Adding New Tests

### Step 1: Identify Test Layer
- **Layer 1 (Unit)?** → Add to `RevitAI.UnitTests/`
- **Layer 2 (Integration)?** → Add to `RevitAI.UnitTests/` with mocks
- **Layer 3 (E2E)?** → Add to `RevitAI.UITests/`

### Step 2: Create Test File
```csharp
using NUnit.Framework;
using RevitAI.Services;
using RevitAI.Models;

namespace RevitAI.UnitTests
{
    [TestFixture]
    public class MyNewFeatureTests
    {
        private MyService _service;

        [SetUp]
        public void Setup()
        {
            _service = new MyService();
        }

        [Test]
        [Category("Unit")]
        [Category("StoryX.Y")]
        public void MethodName_Scenario_ExpectedBehavior()
        {
            // Arrange
            var input = ...;

            // Act
            var result = _service.MethodName(input);

            // Assert
            Assert.That(result, Is.EqualTo(expected),
                "Descriptive failure message");
        }
    }
}
```

### Step 3: Run Tests
```bash
dotnet test --filter "Category=StoryX.Y"
```

### Step 4: Verify Coverage
Aim for >90% code coverage for new methods.

## Debugging Tests

### Run Single Test
```bash
dotnet test --filter "FullyQualifiedName~ValidateAutoTag_ValidDoorTagging_ReturnsSuccess"
```

### Debug in VS Code
1. Open test file in VS Code
2. Click "Run Test" or "Debug Test" above test method
3. Set breakpoints as needed

### View Test Output
```bash
dotnet test -v detailed
```

## Common Test Patterns

### Testing Validation Logic
```csharp
// Test valid input
var result = _validator.Validate(validAction, elementCount: 100);
Assert.That(result.IsValid, Is.True);

// Test invalid input
var result = _validator.Validate(invalidAction, elementCount: 1000);
Assert.That(result.IsValid, Is.False);
Assert.That(result.Message, Does.Contain("expected error text"));
```

### Testing Model Properties
```csharp
var model = new RevitAction { Clarifications = new List<string> { "question" } };
Assert.That(model.NeedsClarification, Is.True);
```

### Testing JSON Deserialization
```csharp
string json = @"{ ""operation"": ""auto_tag"", ... }";
var action = JsonSerializer.Deserialize<RevitAction>(json);
Assert.That(action.Operation, Is.EqualTo("auto_tag"));
```

### Testing Edge Cases
```csharp
// Null input
Assert.Throws<ArgumentNullException>(() => _service.Process(null));

// Empty input
var result = _service.Process("");
Assert.That(result.IsValid, Is.False);

// Boundary values
var result = _service.Process(elementCount: 500); // At limit
Assert.That(result.IsValid, Is.True);
```

## Continuous Integration

Tests are designed to run in CI/CD pipelines:

```yaml
# .github/workflows/test.yml
- name: Run Unit Tests
  run: |
    cd RevitAI.CSharp/tests/RevitAI.UnitTests
    dotnet test --logger "trx" --collect:"XPlat Code Coverage"
```

**CI/CD Status:**
- ✅ Layer 1 tests: Run on every commit (Linux/Windows/Mac)
- ⚠️ Layer 2 tests: Run nightly (with mocks)
- ⚠️ Layer 3 tests: Run weekly (requires Revit license)

## Test Documentation

For detailed test documentation, see:
- **Story 2.1 Test Summary:** `Story-2.1-Test-Summary.md`
- **Testing Framework:** `/docs/testing-framework.md`
- **Architecture Decisions:** `/docs/architecture.md` (ADR-008: SIL Testing)

## Troubleshooting

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet build
```

### Missing Dependencies
```bash
# Restore NuGet packages
dotnet restore
```

### Test Failures
1. Check test output for error messages
2. Verify test data is correct
3. Run test in debug mode with breakpoints
4. Check for state pollution between tests

## Contact

For questions about testing strategy or test failures:
- Check `/docs/testing-framework.md`
- Review existing test patterns in this directory
- Consult CLAUDE.md for project conventions

---

**Test Suite Health: ✅ 100% Pass Rate (127/127 tests)**
**Last Updated:** 2025-11-21
