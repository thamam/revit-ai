# Story 0: SIL Foundation - Hybrid Testing Architecture

**Epic:** Pre-Epic 2 Infrastructure
**Status:** Ready for Development
**Priority:** Critical (Blocks Epic 2)
**Estimated Effort:** 3-5 days

## User Story

As a developer, I need a fast feedback loop with Software-in-the-Loop (SIL) testing so that I can develop Epic 2 features with confidence and 10x faster iteration cycles.

## Background

**Problem:** Current development cycle is open-loop with 5-10 minute feedback per iteration:
- Write code → Build (30s) → Close Revit → Restart Revit (60s) → Manual test → Read logs → Repeat

**Solution:** Implement hybrid testing architecture with three layers:
- **Layer 1:** Pure business logic (no Revit API) - millisecond tests
- **Layer 2:** Revit API wrapper (thin integration) - CI automated
- **Layer 3:** End-to-end acceptance tests - semi-automated

**ROI:** Investment pays dividends across ALL future epics, not just Epic 2. Estimated 10-100x returns over project lifetime.

## Acceptance Criteria

### AC-0.1: Layer 1 Pure Logic Tests
- [ ] Business logic services extracted into testable classes
- [ ] Interfaces defined for Revit API operations (`IRoomAnalyzer`, `IDimensionFactory`)
- [ ] Unit tests run in < 1 second without Revit installed
- [ ] Mock implementations available for all interfaces
- [ ] At least 5 unit tests demonstrating pattern

### AC-0.2: Layer 2 Integration Tests
- [ ] Revit Test Framework or xRevit integrated with NUnit
- [ ] At least ONE automated integration test that:
  - Launches Revit (or uses running instance)
  - Executes real Revit API operation
  - Asserts expected outcome
- [ ] Test can be triggered from command line
- [ ] Results reported programmatically (pass/fail)

### AC-0.3: Test Fixtures
- [ ] Test `.rvt` file committed to `RevitAI.CSharp/tests/fixtures/`
- [ ] Contains known geometry:
  - 1 rectangular room (4 walls, known dimensions)
  - 1 L-shaped room (6 walls)
  - 1 room with door opening
- [ ] Dimensions documented in fixture README
- [ ] Fixture versioned and reproducible

### AC-0.4: Living Specification Tests
- [ ] At least 3 Claude prompt → action specification tests:
  ```csharp
  [TestCase("תוסיף מידות לכל החדרים", "create_dimensions")]
  [TestCase("Add dimensions to all rooms", "create_dimensions")]
  [TestCase("Tag all walls on Level 1", "create_tags")]
  ```
- [ ] Tests serve as executable documentation
- [ ] PRD examples become test cases

### AC-0.5: Architecture Documentation
- [ ] ADR-009: Hybrid Testing Architecture documented
- [ ] Layer 1/2/3 separation explained with diagrams
- [ ] Interface contracts documented
- [ ] Test strategy documented in TESTING_GUIDE.md

## Technical Design

### Interface Definitions (Layer 1)

```csharp
// Services/Interfaces/IRoomAnalyzer.cs
public interface IRoomAnalyzer
{
    IEnumerable<Room> GetAllRooms(Document doc);
    IEnumerable<Wall> GetBoundingWalls(Room room);
    IEnumerable<WallSegment> GetDimensionableSegments(Wall wall);
}

// Services/Interfaces/IDimensionFactory.cs
public interface IDimensionFactory
{
    Dimension CreateLinearDimension(View view, Line line, ReferenceArray refs);
    DimensionChain CreateContinuousDimension(View view, IEnumerable<Reference> refs);
}

// Services/Interfaces/IRevitDocumentWrapper.cs
public interface IRevitDocumentWrapper
{
    Document GetActiveDocument();
    View GetActiveView();
    Transaction StartTransaction(string name);
}
```

### Mock Implementation Example

```csharp
// Tests/Mocks/MockRoomAnalyzer.cs
public class MockRoomAnalyzer : IRoomAnalyzer
{
    private readonly List<MockRoom> _rooms;

    public MockRoomAnalyzer(List<MockRoom> rooms) => _rooms = rooms;

    public IEnumerable<Room> GetAllRooms(Document doc) => _rooms;

    public IEnumerable<Wall> GetBoundingWalls(Room room)
    {
        // Return predefined walls based on room geometry
        return ((MockRoom)room).BoundingWalls;
    }
}
```

### Service Extraction Pattern

```csharp
// Before: Direct Revit API calls
public class RevitEventHandler
{
    public RevitResponse ProcessRequest(UIApplication app, RevitAction action)
    {
        var doc = app.ActiveUIDocument.Document;
        var rooms = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .ToElements(); // Direct API call - untestable
    }
}

// After: Injected dependency
public class RevitEventHandler
{
    private readonly IRoomAnalyzer _roomAnalyzer;

    public RevitEventHandler(IRoomAnalyzer roomAnalyzer)
    {
        _roomAnalyzer = roomAnalyzer;
    }

    public RevitResponse ProcessRequest(UIApplication app, RevitAction action)
    {
        var doc = app.ActiveUIDocument.Document;
        var rooms = _roomAnalyzer.GetAllRooms(doc); // Testable
    }
}
```

## Tasks

### Day 1-2: Architecture Setup
- [ ] Define `IRoomAnalyzer` interface
- [ ] Define `IDimensionFactory` interface
- [ ] Define `IRevitDocumentWrapper` interface
- [ ] Create mock implementations
- [ ] Refactor `RevitEventHandler` to use DI
- [ ] Create 5 unit tests demonstrating pattern

### Day 3: Test Infrastructure
- [ ] Set up NUnit with Revit Test Framework
- [ ] Create test .rvt fixture with known geometry
- [ ] Write first Layer 2 integration test
- [ ] Configure test runner

### Day 4-5: Living Specs + Documentation
- [ ] Implement Claude prompt→action specification tests
- [ ] Document ADR-009
- [ ] Update TESTING_GUIDE.md
- [ ] Update README with Story 0 status
- [ ] Commit and tag as "sil-foundation-complete"

## Definition of Done

- [ ] All acceptance criteria met
- [ ] Unit tests pass in < 1 second
- [ ] At least one integration test passes with real Revit
- [ ] Test fixture committed with documentation
- [ ] Living spec tests demonstrate NLU validation
- [ ] ADR-009 documented
- [ ] Code reviewed (self-review acceptable for PoC)
- [ ] No increase in technical debt

## Dependencies

- NuGet: `RevitTestFramework` or `xRevit.TestRunner`
- Revit 2026 installed on dev machine
- Sample project file for fixtures

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Revit Test Framework setup complexity | High | Fallback to manual test harness with journal scripts |
| Interface abstraction overhead | Medium | Keep wrappers thin, only abstract what we test |
| Test fixture maintenance | Low | Document dimensions, version control the .rvt file |

## Notes

- This story directly enables Epic 2 success
- Interfaces defined here ARE the contracts for Story 2.2
- ROI compounds: every future story benefits from SIL
- The hybrid architecture is also good for code quality (Dependency Inversion)

---

**Created:** 2025-11-15
**Source:** Epic 1 Retrospective - Party Mode Discussion
**Rationale:** Transition from open-loop to Software-in-the-Loop development
