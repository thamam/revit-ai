# RevitAI Integration Tests

Integration tests for RevitAI C# add-in, verifying end-to-end flow across multiple components without requiring actual Revit API.

## Quick Start

### Prerequisites

Set your Claude API key as environment variable:

```bash
export CLAUDE_API_KEY="sk-ant-..."
```

### Run All Tests

```bash
dotnet test RevitAI.CSharp/tests/RevitAI.IntegrationTests/RevitAI.IntegrationTests.csproj
```

### Run Specific Test Categories

```bash
# Hebrew language tests only
dotnet test --filter "Category=Hebrew"

# Safety validation tests only
dotnet test --filter "Category=Safety"

# Story 2.1 tests only
dotnet test --filter "Category=Story2.1"
```

## Project Structure

```
RevitAI.IntegrationTests/
├── AutoTagIntegrationTests.cs        # Main integration test suite (18 tests)
├── Mocks/
│   └── MockRevitContextBuilder.cs    # Mock implementation of IRevitContextBuilder
├── Fixtures/
│   └── MockRevitData.cs              # Test data fixtures (7 mock contexts)
├── Story-2.1-Integration-Test-Summary.md  # Detailed test documentation
├── README.md                         # This file
└── RevitAI.IntegrationTests.csproj  # Project file
```

## Test Categories

| Category | Description | Test Count |
|----------|-------------|------------|
| Integration | All integration tests | 18 |
| AutoTag | Auto-tagging feature | 18 |
| Story2.1 | Story 2.1 specific tests | 18 |
| Hebrew | Hebrew language support | 3 |
| English | English language support | 3 |
| Safety | Safety validation | 2 |
| ScopeLimit | Element count limits | 2 |
| TagType | Tag type validation | 1 |
| Clarification | Ambiguous prompt handling | 2 |
| Filter | Untagged-only filter | 2 |
| Bilingual | Hebrew/English equivalence | 1 |
| Context | Context integration | 2 |

## What These Tests Verify

Integration tests verify the complete flow from user prompt to validated action:

```
User Prompt (Hebrew/English)
    ↓
ClaudeService.ParsePromptAsync()  ← Calls actual Claude API
    ↓
SafetyValidator.Validate()        ← Validates parsed action
    ↓
MockRevitContextBuilder           ← Provides mock Revit data
```

## Key Test Scenarios

### 1. Hebrew Prompt Parsing
```csharp
Prompt: "תייג את כל הדלתות בקומה 1"
Expected: Operation=auto_tag, Category=Doors, Scope=Level 1
```

### 2. English Prompt Parsing
```csharp
Prompt: "Tag all doors in Level 1"
Expected: Operation=auto_tag, Category=Doors, Scope=Level 1
```

### 3. Safety Validation
```csharp
Prompt: "Delete all doors" (malicious)
Expected: SafetyValidator blocks operation
```

### 4. Scope Limit Enforcement
```csharp
Context: 600 doors (exceeds 500 limit)
Expected: Validation fails with "too large" message
```

### 5. Ambiguous Prompt Handling
```csharp
Prompt: "Tag everything"
Expected: Returns clarification questions
```

## Mock Strategy

### Why Mock RevitContextBuilder?

`RevitContextBuilder` requires actual Revit API objects (`UIApplication`, `Document`, `FilteredElementCollector`) that cannot be instantiated without Revit running. This makes cross-platform testing impossible.

**Solution**: `MockRevitContextBuilder` implements `IRevitContextBuilder` and returns pre-configured test data.

**Benefits**:
- Tests run on any platform (Linux, macOS, Windows)
- Tests run in milliseconds (no Revit startup overhead)
- Deterministic test data (no dependency on actual Revit projects)
- Easy to test edge cases (empty projects, large projects, missing tag types)

### Mock Data Fixtures

`MockRevitData.cs` provides 7 pre-configured contexts:

1. **CreateTypicalContext()** - Standard residential project
   - 47 doors, 120 walls, 12 rooms, 35 windows
   - All elements untagged

2. **CreateContextWithManyElements()** - Large project for scope testing
   - 600 doors, 800 walls, 1200 windows
   - Tests scope limit validation (500 element max)

3. **CreateContextWithSelection()** - Active element selection
   - Configurable selection count and type

4. **CreateContextWithMissingTagTypes()** - Missing tag types
   - Only Door Tag available (tests validation)

5. **CreateContextWithPartiallyTaggedElements()** - Mixed tagged/untagged
   - 50 doors (30 tagged, 20 untagged)
   - Tests untagged-only filter

6. **CreateHebrewContext()** - Hebrew language project
   - Hebrew level names (קומה 1, קומה 2)

7. **CreateEmptyContext()** - Empty project
   - 0 elements (edge case testing)

## Test Execution Times

- **Without API Key**: <1 second (all tests inconclusive)
- **With API Key**: 2-5 minutes (18 API calls to Claude)
  - Each API call: 3-10 seconds
  - Sequential execution (no parallelization due to rate limits)

## Test Naming Convention

Format: `EndToEndFlow_[Scenario]_[ExpectedBehavior]`

Examples:
- `EndToEndFlow_HebrewTagDoorsPrompt_ParsesValidatesAndBuildsContext`
- `EndToEndFlow_ElementCountExceedsLimit_ValidationFails`
- `EndToEndFlow_AmbiguousPrompt_ReturnsClarifications`

## Adding New Tests

### 1. Add Test Data Fixture (if needed)

```csharp
// In Fixtures/MockRevitData.cs
public static RevitContext CreateCustomContext()
{
    return new RevitContext
    {
        // ... custom test data
    };
}
```

### 2. Add Test Method

```csharp
// In AutoTagIntegrationTests.cs
[Test]
[Category("Integration")]
[Category("AutoTag")]
[Category("Story2.1")]
public async Task EndToEndFlow_CustomScenario_ExpectedBehavior()
{
    // Arrange
    string prompt = "Your test prompt";
    var context = MockRevitData.CreateCustomContext();
    _mockContextBuilder.SetMockContext(context);

    // Act
    var action = await _claudeService.ParsePromptAsync(prompt, context);
    var validationResult = _safetyValidator.Validate(action, elementCount: 100);

    // Assert
    Assert.That(action.Operation, Is.EqualTo("auto_tag"));
    Assert.That(validationResult.IsValid, Is.True);
}
```

### 3. Update Documentation

Update `Story-2.1-Integration-Test-Summary.md` with new test details.

## Troubleshooting

### Tests Skipped with "Inconclusive"

**Problem**: Tests show as "Inconclusive" instead of Pass/Fail

**Cause**: `CLAUDE_API_KEY` environment variable not set

**Solution**:
```bash
export CLAUDE_API_KEY="sk-ant-..."
dotnet test
```

### API Rate Limit Errors

**Problem**: Tests fail with "Rate limit exceeded" errors

**Cause**: Too many API calls in short time period

**Solution**:
- Wait a few minutes before re-running
- Run smaller test subsets using categories
- Consider API key tier upgrade for higher rate limits

### Nullable Reference Warnings

**Problem**: Build shows 22 nullable reference warnings

**Cause**: Shared model classes (`RevitAction.cs`, etc.) don't initialize nullable properties

**Solution**: These are expected warnings from shared code. Tests still run correctly.

### Assertion Flexibility

**Problem**: Test fails with "Level 1" vs "level:Level 1" mismatch

**Cause**: Claude may format responses differently

**Solution**: Use flexible assertions:
```csharp
Assert.That(action.Target.Scope,
    Does.Contain("Level 1").Or.Contain("level:Level 1").IgnoreCase);
```

## CI/CD Integration

### GitHub Actions Example

```yaml
name: Integration Tests

on: [pull_request]

jobs:
  test:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v2
      - name: Setup .NET
        uses: actions/setup-dotnet@v1
        with:
          dotnet-version: 8.0.x
      - name: Run Integration Tests
        env:
          CLAUDE_API_KEY: ${{ secrets.CLAUDE_API_KEY }}
        run: |
          dotnet test RevitAI.CSharp/tests/RevitAI.IntegrationTests/RevitAI.IntegrationTests.csproj
```

**Important**: Store `CLAUDE_API_KEY` as GitHub secret, not in code.

## Acceptance Criteria Coverage

| AC | Description | Test Count | Status |
|----|-------------|------------|--------|
| AC1 | Claude parses Hebrew/English prompts | 6 tests | ✅ COVERED |
| AC2 | Ambiguous prompts return clarifications | 2 tests | ✅ COVERED |
| AC3 | SafetyValidator blocks disallowed ops | 4 tests | ✅ COVERED |
| AC4 | Bilingual support (Hebrew/English) | 7 tests | ✅ COVERED |

**Overall**: 100% of Story 2.1 acceptance criteria

## Next Steps

### Layer 2 Integration Tests (Future)

Create tests with actual Revit API:
- Require Revit installation on test machine
- Test with real Revit documents
- Verify actual tag creation
- Test across Revit versions (2024, 2025, 2026)

### E2E Tests (Future)

Full end-to-end tests in Revit:
- User clicks button in Revit ribbon
- Dialog opens, user enters prompt
- Tags are created in actual project
- Verify via Revit UI

## Documentation

- **Detailed Test Summary**: `Story-2.1-Integration-Test-Summary.md`
- **Architecture Decisions**: `/home/thh3/personal/revit-ai/docs/architecture.md`
- **Testing Framework**: `/home/thh3/personal/revit-ai/docs/testing-framework.md`

## Support

For issues or questions:
1. Check `Story-2.1-Integration-Test-Summary.md` for detailed documentation
2. Review existing test patterns in `AutoTagIntegrationTests.cs`
3. Consult `MockRevitData.cs` for available test fixtures

---

**Framework**: NUnit 3.14.0
**Language**: C# .NET 8.0
**Test Count**: 18 integration tests
**Last Updated**: 2025-11-21
