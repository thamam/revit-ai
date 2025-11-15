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
- [x] Define `IRoomAnalyzer` interface
- [x] Define `IDimensionFactory` interface
- [x] Define `IRevitDocumentWrapper` interface
- [x] Create mock implementations (MockFactory + POCO domain models)
- [ ] Refactor `RevitEventHandler` to use DI
- [x] Create 5 unit tests demonstrating pattern (27 total tests: 9 SafetyValidator + 4 ClaudeService + 14 DimensionPlanningService)

### Day 3: Test Infrastructure
- [ ] Set up NUnit with Revit Test Framework
- [ ] Create test .rvt fixture with known geometry
- [ ] Write first Layer 2 integration test
- [ ] Configure test runner

### Day 4-5: Living Specs + Documentation
- [x] Implement Claude prompt→action specification tests (4 living spec tests)
- [x] Document ADR-009 (already completed in Epic 1 retro)
- [x] Update TESTING_GUIDE.md (comprehensive guide created)
- [x] Update README with Story 0 status (already done)
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

## Dev Agent Record

### Debug Log
<!-- Implementation notes, decisions, and debugging info -->

**2025-11-15 - Task: Define IRoomAnalyzer interface**
Plan:
1. Create Services/Interfaces/ directory structure
2. Define IRoomAnalyzer with methods for room analysis
3. Use Revit API types (Room, Wall, Document) in signatures
4. Keep interface focused - single responsibility (room analysis only)
5. Design for testability - return IEnumerable for easy mocking

Edge cases considered:
- Rooms with no bounding walls (open spaces)
- Rooms spanning multiple levels
- Curved vs linear wall segments

**2025-11-15 - Progress Update (Session 1)**
Completed from Linux:
- ✅ All 3 interface definitions (IRoomAnalyzer, IDimensionFactory, IRevitDocumentWrapper)
- ✅ Unit test project structure with NUnit + Moq
- ✅ 9 SafetyValidator unit tests
- ✅ 4 ClaudeService living specification tests
- ✅ Sprint status tracking file

**2025-11-15 - Major Progress Update (Session 2)**
Completed - True Layer 1 SIL Architecture:
- ✅ POCO domain models created (RoomInfo, WallInfo, DimensionInfo)
- ✅ DimensionPlanningService - Pure business logic with NO Revit dependencies
- ✅ 14 new unit tests for DimensionPlanningService (total 27 tests now)
- ✅ MockFactory helper for Moq-based interface mocking
- ✅ Comprehensive TESTING_GUIDE.md documentation (600+ lines)
- ✅ Fixed test project reference path (was pointing to wrong location)

**Key Architecture Insight:**
The initial interfaces (IRoomAnalyzer, IDimensionFactory) use Revit API types directly, which limits testability. The solution is a **dual-layer approach**:
- Layer 1: POCO domain objects (RoomInfo, WallInfo) + pure logic services (DimensionPlanningService)
- Layer 2: Adapter pattern converts Revit ↔ POCOs (to be implemented)

This enables true millisecond testing: 14 tests for DimensionPlanningService use only POCOs, demonstrating room dimension planning, validation, chain creation, and error handling WITHOUT any Revit dependency.

**BLOCKED - Requires Windows/Revit:**
- AC-0.1: Cannot verify tests run < 1 second (no dotnet on Linux)
- AC-0.2: Layer 2 integration tests require Revit Test Framework + Windows
- AC-0.3: Test .rvt fixtures require Revit to create
- Cannot build/compile to verify interface correctness

**Next Steps (On Windows):**
1. Pull latest changes: `git pull`
2. Build main project: `dotnet build RevitAI.csproj`
3. Build test project: `dotnet build RevitAI.CSharp/tests/RevitAI.UnitTests/`
4. Run unit tests: `dotnet test RevitAI.CSharp/tests/RevitAI.UnitTests/`
5. Verify tests pass in < 1 second (expect 27 tests)
6. Fix any compilation issues (may need to adjust Revit API references in MockFactory)
7. Continue with Layer 2 setup (Revit Test Framework)

### Completion Notes
<!-- Summary of what was implemented -->
**Substantial Implementation - Core SIL Architecture Established**

**Layer 1 Infrastructure (COMPLETE):**
- 3 interface abstractions for Revit API operations
- 3 POCO domain models (RoomInfo, WallInfo, DimensionInfo) enabling true unit testing
- 1 pure business logic service (DimensionPlanningService) with zero Revit dependencies
- Unit test project with NUnit 3.14.0, Moq 4.20.70
- MockFactory for interface mocking
- 27 total tests:
  - 9 SafetyValidator tests (operation allowlist, scope validation)
  - 4 ClaudeService living spec tests (Hebrew/English NLU)
  - 14 DimensionPlanningService tests (Layer 1 pure logic)
- Comprehensive TESTING_GUIDE.md (600+ lines) explaining hybrid architecture
- Sprint status tracking enabled

**Key Achievement:**
Created the foundational pattern for SIL testing: POCOs + pure logic services = testable business logic without Revit. The DimensionPlanningService demonstrates exactly how to structure code for fast feedback loops.

**Still Blocked on Windows/Revit environment for:**
- Compilation verification (need dotnet CLI)
- Test execution timing validation (verify < 1 second)
- Layer 2 Revit Test Framework setup
- .rvt fixture creation

---

## File List
<!-- Files created, modified, or deleted -->

### Session 1 (Initial Setup)
- **Created:** `RevitAI.CSharp/Services/Interfaces/IRoomAnalyzer.cs`
- **Created:** `RevitAI.CSharp/Services/Interfaces/IDimensionFactory.cs`
- **Created:** `RevitAI.CSharp/Services/Interfaces/IRevitDocumentWrapper.cs`
- **Created:** `RevitAI.CSharp/tests/RevitAI.UnitTests/RevitAI.UnitTests.csproj`
- **Created:** `RevitAI.CSharp/tests/RevitAI.UnitTests/ClaudeServiceTests.cs`
- **Created:** `RevitAI.CSharp/tests/RevitAI.UnitTests/SafetyValidatorTests.cs`
- **Created:** `docs/sprint-status.yaml`
- **Renamed:** `docs/stories/story-0-sil-foundation.md` → `docs/stories/0-0-sil-foundation.md`

### Session 2 (Layer 1 Architecture)
- **Created:** `RevitAI.CSharp/Models/Domain/RoomInfo.cs` - POCO for room data
- **Created:** `RevitAI.CSharp/Models/Domain/WallInfo.cs` - POCO for wall/segment data
- **Created:** `RevitAI.CSharp/Models/Domain/DimensionInfo.cs` - POCO for dimension plans
- **Created:** `RevitAI.CSharp/Services/DimensionPlanningService.cs` - Pure business logic (NO Revit deps)
- **Created:** `RevitAI.CSharp/tests/RevitAI.UnitTests/DimensionPlanningServiceTests.cs` - 14 Layer 1 tests
- **Created:** `RevitAI.CSharp/tests/RevitAI.UnitTests/Mocks/MockFactory.cs` - Moq helper factory
- **Created:** `RevitAI.CSharp/docs/TESTING_GUIDE.md` - Comprehensive testing documentation
- **Modified:** `RevitAI.CSharp/tests/RevitAI.UnitTests/RevitAI.UnitTests.csproj` - Fixed project reference path

---

## Change Log
<!-- Chronological record of major changes -->
- 2025-11-15: Story created from Epic 1 Retrospective
- 2025-11-15: Session 2 - Implemented true Layer 1 SIL architecture with POCOs and pure logic services

---

**Created:** 2025-11-15
**Source:** Epic 1 Retrospective - Party Mode Discussion
**Rationale:** Transition from open-loop to Software-in-the-Loop development
