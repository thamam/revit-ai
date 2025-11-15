# RevitAI Testing Guide

## Hybrid Testing Architecture (SIL - Software-in-the-Loop)

This project uses a **3-layer testing architecture** that enables fast feedback loops during development while maintaining confidence in Revit integration.

### Why This Architecture?

**The Problem:** Traditional Revit add-in development has a 5-10 minute feedback loop:
```
Write code → Build (30s) → Close Revit → Restart Revit (60s) → Manual test → Read logs → Repeat
```

**The Solution:** Software-in-the-Loop (SIL) testing enables:
- **Layer 1:** Millisecond tests (no Revit) - test 100x per hour
- **Layer 2:** Automated integration (with Revit) - CI/nightly
- **Layer 3:** End-to-end acceptance - semi-automated

---

## Layer 1: Pure Business Logic Tests

### Characteristics
- **Speed:** < 1 second for full suite
- **Dependencies:** NONE (no Revit, no network, no files)
- **Purpose:** Test algorithms, validation, planning logic
- **Location:** `tests/RevitAI.UnitTests/`

### Key Pattern: POCO Domain Objects

The core innovation is separating business logic from Revit API using Plain C# Object (POCO) wrappers:

```csharp
// ❌ BAD: Direct Revit dependency (untestable without Revit)
public class RoomDimensioner
{
    public void DimensionRoom(Room room) // Revit type!
    {
        var walls = room.GetBoundarySegments()
            .SelectMany(seg => seg)
            .Select(s => doc.GetElement(s.ElementId) as Wall);
        // ... logic tightly coupled to Revit
    }
}

// ✅ GOOD: POCO-based (testable instantly)
public class DimensionPlanningService
{
    public DimensionPlanResult PlanRoomDimensions(
        IEnumerable<RoomInfo> rooms,      // POCO!
        IEnumerable<WallInfo> walls)       // POCO!
    {
        // Pure logic with no Revit dependency
        foreach (var room in rooms)
        {
            if (!room.IsPlaced) continue;
            foreach (var wallId in room.BoundingWallIds)
            {
                // Planning logic testable with fake data
            }
        }
    }
}
```

### Domain Objects

Located in `Models/Domain/`:

| Class | Purpose | Example Usage |
|-------|---------|---------------|
| `RoomInfo` | Room geometry data | `RoomInfo.CreateRectangular(10, 15)` |
| `WallInfo` | Wall segment data | `WallInfo.CreateWithDoorOpening(12, 3)` |
| `DimensionInfo` | Planned dimension | `DimensionInfo.CreateChain(wallIds, total)` |

### Example Test

```csharp
[Test]
[Category("Layer1")]
public void PlanRoomDimensions_RectangularRoom_Creates4Dimensions()
{
    // Arrange - Create fake data instantly
    var room = RoomInfo.CreateRectangular(10, 15, "Office");
    var walls = CreateRectangularWalls(room);

    // Act - Test pure logic
    var result = _service.PlanRoomDimensions(new[] { room }, walls);

    // Assert - Verify planning logic
    Assert.That(result.IsFeasible, Is.True);
    Assert.That(result.PlannedDimensions.Count, Is.EqualTo(4));
}
```

### Running Layer 1 Tests

```bash
# Run all unit tests
dotnet test tests/RevitAI.UnitTests/RevitAI.UnitTests.csproj

# Run only Layer 1 tests
dotnet test --filter "Category=Layer1"

# Run specific test class
dotnet test --filter "FullyQualifiedName~DimensionPlanningServiceTests"

# Verbose output
dotnet test -v detailed
```

**Expected runtime:** < 1 second for all tests

---

## Layer 2: Revit API Integration Tests

### Characteristics
- **Speed:** Seconds to minutes (Revit startup)
- **Dependencies:** Revit installed, test fixtures
- **Purpose:** Verify Revit API wrapper correctness
- **Location:** `tests/RevitAI.IntegrationTests/` (to be created)

### Pattern: Adapter Layer

The adapter pattern bridges POCOs and Revit API:

```csharp
// Adapter: Converts Revit → POCO
public class RevitToPocoAdapter
{
    public RoomInfo ConvertRoom(Room revitRoom)
    {
        return new RoomInfo
        {
            ElementId = revitRoom.Id.IntegerValue,
            Name = revitRoom.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString(),
            Area = revitRoom.Area,
            BoundingWallIds = GetBoundingWallIds(revitRoom)
        };
    }
}

// Usage in Layer 2 test
[Test]
public void RevitToPocoAdapter_ConvertsRoomCorrectly()
{
    // Uses real Revit API with test fixture
    var doc = OpenTestFixture("test_rooms.rvt");
    var revitRoom = GetRoomFromFixture(doc, "Office");

    var adapter = new RevitToPocoAdapter();
    var roomInfo = adapter.ConvertRoom(revitRoom);

    Assert.That(roomInfo.Area, Is.EqualTo(150).Within(0.1));
}
```

### Test Fixtures

Located in `tests/fixtures/`:

| File | Contents | Purpose |
|------|----------|---------|
| `test_rooms.rvt` | 3 rooms with known dimensions | Layer 2 integration tests |
| `fixture_readme.md` | Exact measurements | Validation reference |

**Fixture Geometry:**
1. Rectangular room: 10' × 15' (4 walls)
2. L-shaped room: 150 sq ft (6 walls)
3. Room with door: 12' × 15' (1 door opening)

### Revit Test Framework Setup

```xml
<!-- In RevitAI.IntegrationTests.csproj -->
<PackageReference Include="RevitTestFramework" Version="..." />
<PackageReference Include="RevitTestRunner" Version="..." />
```

```csharp
[TestFixture]
public class RevitIntegrationTests
{
    private Document _doc;

    [OneTimeSetUp]
    public void OpenFixture()
    {
        _doc = RevitTestHelper.OpenDocument("test_rooms.rvt");
    }

    [Test]
    public void GetAllRooms_ReturnsExpectedCount()
    {
        var analyzer = new RoomAnalyzerImpl(_doc);
        var rooms = analyzer.GetAllRooms(_doc);

        Assert.That(rooms.Count(), Is.EqualTo(3));
    }
}
```

---

## Layer 3: End-to-End Acceptance Tests

### Characteristics
- **Speed:** Minutes (includes UI interaction)
- **Dependencies:** Full Revit environment
- **Purpose:** Validate user workflows
- **Location:** `tests/RevitAI.AcceptanceTests/` (planned)

### Living Specification Pattern

PRD examples become executable tests:

```csharp
// From PRD: "תוסיף מידות לכל החדרים" should add dimensions to all rooms
[Test]
[Category("LivingSpec")]
[Category("Hebrew")]
public async Task HebrewDimensionCommand_AddsDimensionsToAllRooms()
{
    // Given: Hebrew natural language command
    string command = "תוסיף מידות לכל החדרים";

    // When: Parsed by Claude service
    var action = await _claudeService.ParsePromptAsync(command, context);

    // Then: Maps to correct operation
    Assert.That(action.Operation, Is.EqualTo("create_dimensions"));
}
```

---

## Test Categories

Use NUnit categories to organize tests:

```csharp
[Test]
[Category("Unit")]      // Fast, isolated
[Category("Layer1")]    // No Revit dependency
[Category("Safety")]    // Security/validation
public void Test_SafetyValidation() { ... }

[Test]
[Category("Integration")]  // Requires Revit
[Category("Layer2")]       // API wrapper tests
public void Test_RevitIntegration() { ... }

[Test]
[Category("LivingSpec")]   // PRD requirements
[Category("Hebrew")]       // Language-specific
public void Test_HebrewCommand() { ... }
```

### Running by Category

```bash
# Only fast unit tests
dotnet test --filter "Category=Unit"

# Skip integration tests
dotnet test --filter "Category!=Integration"

# Living specs only
dotnet test --filter "Category=LivingSpec"
```

---

## Best Practices

### 1. Test Naming Convention

```
MethodName_Scenario_ExpectedResult
```

Examples:
- `PlanRoomDimensions_RectangularRoom_Creates4Dimensions`
- `Validate_DeleteOperation_ReturnsInvalid`
- `ParsePromptAsync_HebrewCommand_ReturnsCreateDimensions`

### 2. Arrange-Act-Assert Pattern

```csharp
[Test]
public void TestName()
{
    // Arrange - Set up test data
    var service = new DimensionPlanningService();
    var room = RoomInfo.CreateRectangular(10, 15);

    // Act - Execute the method under test
    var result = service.ValidateRoomsForDimensioning(new[] { room });

    // Assert - Verify the outcome
    Assert.That(result.IsFeasible, Is.True);
}
```

### 3. Test Independence

Each test must:
- Set up its own state in `[SetUp]`
- Clean up in `[TearDown]` if needed
- Not depend on other test execution order
- Not share mutable state

### 4. Mock vs Fake vs Stub

| Type | When to Use | Example |
|------|-------------|---------|
| **Fake** | Complete working implementation with shortcuts | `RoomInfo.CreateRectangular()` |
| **Mock** | Verify interactions/calls | `Mock<IRoomAnalyzer>.Setup()` |
| **Stub** | Return canned answers | `MockFactory.CreateEmptyRoomAnalyzer()` |

---

## Coverage Goals

### Epic 1 Foundation
- [x] SafetyValidator: 9 tests
- [x] ClaudeService: 4 living spec tests
- [x] DimensionPlanningService: 14 tests (Layer 1)

**Total: 27 tests**

### Epic 2 Dimension Automation (Target)
- DimensionPlanningService: 20+ tests
- RevitToPocoAdapter: 10+ tests
- End-to-end workflows: 5+ tests

**Target: 50+ tests**

---

## Continuous Integration

### CI Pipeline (GitHub Actions - Planned)

```yaml
name: RevitAI Tests

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: windows-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-dotnet@v3
        with:
          dotnet-version: '8.0.x'
      - run: dotnet test tests/RevitAI.UnitTests --filter "Category=Unit"

  # Integration tests run nightly (require Revit)
```

---

## Troubleshooting

### Tests won't compile
```bash
# Missing NuGet packages
dotnet restore tests/RevitAI.UnitTests/RevitAI.UnitTests.csproj

# Check project reference
# Ensure <ProjectReference Include="../../RevitAI.csproj" /> exists
```

### Tests are slow
- Check for accidental external dependencies (network, file I/O)
- Use `[Category("Unit")]` to isolate pure logic tests
- Profile with `dotnet test --blame-hang-timeout 10s`

### Mock setup failing
```csharp
// Verify Moq is installed
// <PackageReference Include="Moq" Version="4.20.70" />

// Use It.IsAny<T>() for flexible matching
mock.Setup(r => r.GetAllRooms(It.IsAny<Document>()))
    .Returns(new List<Room>());
```

---

## Architecture Decision Record

**ADR-009: Hybrid Testing Architecture** (See `docs/architecture.md`)

Key decisions:
1. POCO domain objects decouple business logic from Revit API
2. Three-layer testing enables different feedback speeds
3. Living specifications keep tests aligned with PRD
4. Adapter pattern bridges testable logic with Revit API

---

## Future Enhancements

1. **Property-based testing** - FsCheck for edge case discovery
2. **Mutation testing** - Stryker for test quality validation
3. **Visual regression** - UI screenshot comparison
4. **Performance benchmarks** - BenchmarkDotNet for operation timing

---

*Last Updated: 2025-11-15*
*Story: 0-0-sil-foundation*
*Author: RevitAI Development Team*
