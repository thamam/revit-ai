# Story 2.6: Layer 2 Revit API Integration (Stories 2.1-2.3)

**Epic:** Epic 2: Intelligent Dimension Automation
**Status:** drafted
**Priority:** Critical (Blocks end-to-end testing)
**Estimated Effort:** 2-3 days
**Prerequisites:** Stories 2.1, 2.2, 2.3 (Layer 1 complete)

## Story

As a developer,
I want to integrate the Layer 1 dimension planning logic with the Revit API,
So that the system can extract real room data, create actual dimensions, and enable end-to-end testing in Revit.

## Context

**Current State:** Stories 2.1, 2.2, and 2.3 have complete Layer 1 implementations:
- ✅ 73 unit tests passing in 152ms (cross-platform on Linux)
- ✅ All business logic, geometry calculations, and algorithms complete
- ✅ POCOs designed (DimensionCommand, RoomBoundaryInfo, DimensionChainInfo)
- ❌ **No Revit API integration** - cannot test with real data

**Problem:** The algorithmic "brain" is complete, but we need the "hands" to:
1. Query real rooms and boundaries from Revit documents
2. Detect actual doors/windows in walls
3. Create physical dimension objects in the model
4. Enable end-to-end validation with real architectural geometry

**This Story Consolidates:**
- Story 2.1, Task 3: Integrate with ClaudeService (Revit context)
- Story 2.2, Task 3: Implement Revit Integration (Room.GetBoundarySegments)
- Story 2.2, Task 4: Implement Geometric Feature Detection (opening detection)
- Story 2.3, Task 3: Implement Revit Dimension Creator
- Story 2.3, Task 4: Add Dimension Style Configuration

## Acceptance Criteria

### AC-2.6.1: Revit Room Extraction
- [ ] Given a DimensionCommand with target scope (e.g., "all rooms on Level 1")
- [ ] When the system queries the Revit document
- [ ] Then matching Room elements are extracted using FilteredElementCollector
- [ ] And rooms are converted to RoomInfo POCOs for Layer 1 processing
- [ ] And selection-based targeting uses current Revit selection
- [ ] And level filtering uses Level.Name comparison

### AC-2.6.2: Boundary Segment Extraction
- [ ] Given a list of Revit Room elements
- [ ] When boundaries are analyzed using Room.GetBoundarySegments()
- [ ] Then BoundarySegment curves are converted to WallSegmentInfo POCOs
- [ ] And wall normals are calculated from curve orientation
- [ ] And room separators are filtered from physical walls
- [ ] And multi-segment boundaries (L-shaped rooms) are handled correctly

### AC-2.6.3: Opening Detection (Doors/Windows)
- [ ] Given wall segments from room boundaries
- [ ] When the system queries for openings
- [ ] Then FamilyInstance elements of category Doors are detected
- [ ] And FamilyInstance elements of category Windows are detected
- [ ] And opening positions/widths are converted to OpeningInfo POCOs
- [ ] And openings are associated with their wall segments by spatial query

### AC-2.6.4: Dimension Creation via Revit API
- [ ] Given a list of DimensionChainInfo POCOs from Layer 1 planner
- [ ] When dimensions are created in Revit
- [ ] Then Document.Create.NewDimension() is called for each chain
- [ ] And ReferenceArray is built from wall reference points
- [ ] And dimension line is created at calculated offset position
- [ ] And dimension style is applied from configuration or document default
- [ ] And all dimension creation is wrapped in a single Transaction

### AC-2.6.5: Transaction Safety & Error Handling
- [ ] All Revit API operations wrapped in Transaction with descriptive name
- [ ] Transaction commits atomically (all dimensions or none)
- [ ] Partial failures handled gracefully (e.g., 9 of 12 rooms succeed)
- [ ] Failed dimension creation rolls back entire operation
- [ ] User can undo entire operation with Ctrl+Z after commit
- [ ] Clear error messages for common failures (invalid references, locked elements)

### AC-2.6.6: Configuration & Dimension Styles
- [ ] Default dimension type retrieved from active document
- [ ] Fallback to first available LinearDimensionType if default not found
- [ ] Configuration allows override via settings (future: firm_defaults.yaml)
- [ ] Dimension type validation before use (exists in document)
- [ ] Offset distance configurable (default: 200mm = 0.656ft)

### AC-2.6.7: End-to-End Integration Test
- [ ] Complete workflow executable in Revit: Parse → Extract → Analyze → Plan → Create
- [ ] Test with real rectangular room (Conference Room 101)
- [ ] Test with L-shaped room containing doors/windows
- [ ] Verify dimensions appear correctly in Revit model
- [ ] Verify dimensions are undo-able with Ctrl+Z
- [ ] Performance acceptable (< 5 seconds for 12-room test)

## Tasks / Subtasks

### **Task 1: Create RevitRoomExtractor Service** (AC: 2.6.1, 2.6.2)
- [ ] Create `Services/Revit/RevitRoomExtractor.cs`
- [ ] Implement `ExtractRooms(DimensionCommand command, Document doc) → List<Room>` method
- [ ] Use `FilteredElementCollector` with `OfCategory(BuiltInCategory.OST_Rooms)`
- [ ] Filter by level: `WherePasses(new ElementLevelFilter(levelId))`
- [ ] Filter by selection: check `command.TargetScope.UseSelection` and query `UIDocument.Selection`
- [ ] Convert Revit `Room` to `RoomInfo` POCO (ID, Name, Level, BoundingBox)
- [ ] Handle edge cases: no rooms found, rooms on multiple levels

### **Task 2: Create RevitBoundaryExtractor Service** (AC: 2.6.2, 2.6.3)
- [ ] Create `Services/Revit/RevitBoundaryExtractor.cs`
- [ ] Implement `ExtractBoundary(Room room, Document doc) → RoomBoundaryInfo` method
- [ ] Use `room.GetBoundarySegments(new SpatialElementBoundaryOptions())` to get curves
- [ ] Convert `BoundarySegment` to `WallSegmentInfo` POCO
- [ ] Calculate wall normals from curve direction (perpendicular vector)
- [ ] Filter room separators: check `segment.ElementId` for Room Separation Line category
- [ ] Identify corners: find curve endpoints that connect to other curves
- [ ] Handle multi-segment boundaries (nested loops for complex rooms)

### **Task 3: Create RevitOpeningDetector Service** (AC: 2.6.3)
- [ ] Create `Services/Revit/RevitOpeningDetector.cs`
- [ ] Implement `DetectOpenings(Room room, List<WallSegmentInfo> walls, Document doc) → List<OpeningInfo>` method
- [ ] Query doors: `FilteredElementCollector` with `OfCategory(BuiltInCategory.OST_Doors)`
- [ ] Query windows: `FilteredElementCollector` with `OfCategory(BuiltInCategory.OST_Windows)`
- [ ] Filter to openings in the specific room using `BoundingBoxIntersectsFilter`
- [ ] Extract opening position: `FamilyInstance.Location` as `LocationPoint`
- [ ] Extract opening width: get `FamilyInstance` parameter "Width"
- [ ] Associate opening with wall segment: spatial query (closest wall within tolerance)
- [ ] Convert to `OpeningInfo` POCO with WallSegmentIndex

### **Task 4: Create RevitDimensionCreator Service** (AC: 2.6.4, 2.6.5, 2.6.6)
- [ ] Create `Services/Revit/RevitDimensionCreator.cs`
- [ ] Implement `CreateDimensions(List<DimensionChainInfo> chains, Document doc) → Result<int>` method
- [ ] Start Revit Transaction: `new Transaction(doc, "AI: Create Dimension Chains")`
- [ ] For each DimensionChainInfo:
  - [ ] Build `ReferenceArray` from reference points (query wall references at points)
  - [ ] Create `Line` for dimension line (DimensionLineStart → DimensionLineEnd)
  - [ ] Call `doc.Create.NewDimension(view, dimensionLine, referenceArray)`
  - [ ] Apply dimension type if specified: `dimension.DimensionType = GetDimensionType()`
- [ ] Commit transaction if all succeed, rollback if any fail
- [ ] Return Result<int> with success count and error messages
- [ ] Handle common errors: invalid references, locked views, missing dimension types

### **Task 5: Create DimensionTypeResolver Service** (AC: 2.6.6)
- [ ] Create `Services/Revit/DimensionTypeResolver.cs`
- [ ] Implement `GetDimensionType(Document doc, string? styleName) → DimensionType` method
- [ ] If styleName provided: query by name using `FilteredElementCollector`
- [ ] If not found or null: get project default from `doc.GetElement(doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType))`
- [ ] If no default: get first available LinearDimensionType
- [ ] Cache dimension types for performance (avoid repeated queries)
- [ ] Return null if no dimension types exist (edge case: empty template)

### **Task 6: Create Integration Facade (Orchestrator)** (AC: 2.6.7)
- [ ] Create `Services/DimensionOrchestrator.cs` (Layer 2 orchestrator)
- [ ] Implement `ExecuteDimensionCommand(DimensionCommand command, UIDocument uidoc) → Result` method
- [ ] Orchestrate full pipeline:
  1. Extract rooms (RevitRoomExtractor)
  2. Extract boundaries for each room (RevitBoundaryExtractor)
  3. Detect openings (RevitOpeningDetector)
  4. Analyze boundaries (RoomBoundaryAnalyzer - Layer 1)
  5. Plan dimension chains (DimensionChainPlanner - Layer 1)
  6. Create dimensions (RevitDimensionCreator)
- [ ] Add progress reporting (optional: progress bar for large room sets)
- [ ] Handle partial failures: continue with successful rooms, report failures
- [ ] Return summary: "Created 47 dimension chains in 9 of 12 rooms (3 failed: curved walls)"

### **Task 7: Add Simple Confirmation Dialog** (AC: 2.6.5)
- [ ] Create `UI/SimpleConfirmDialog.xaml` (basic WPF dialog)
- [ ] Show counts: "Create {chainCount} dimension chains in {roomCount} rooms?"
- [ ] Buttons: [Confirm] [Cancel]
- [ ] Call before transaction starts in DimensionOrchestrator
- [ ] If user cancels: return early, no transaction executed
- [ ] Note: This will be replaced by full preview in Story 2.4

### **Task 8: Create Integration Tests** (AC: 2.6.7)
- [ ] Create `tests/RevitAI.IntegrationTests/` project (requires Revit running)
- [ ] **Use existing test fixture:** `temp/test_rooms.rvt` (400MB real project, added 2025-11-17)
- [ ] Test 1: Extract rooms from test_rooms.rvt (verify RoomInfo conversion)
- [ ] Test 2: Extract boundaries from rectangular room (verify WallSegmentInfo)
- [ ] Test 3: Detect openings in room with door (verify OpeningInfo)
- [ ] Test 4: Create dimensions for single room (verify Dimension objects in doc)
- [ ] Test 5: End-to-end workflow (command → dimensions visible in model)
- [ ] Test 6: Transaction rollback on failure (verify undo works)
- [ ] Performance test: measure execution time for all rooms in test_rooms.rvt
- [ ] Document test results: room count, dimension count, execution time, geometry types found

### **Task 9: Update Layer 1 Services for Revit Data** (AC: 2.6.7)
- [ ] Verify RoomBoundaryAnalyzer handles real Revit boundary data (not just test factories)
- [ ] Verify DimensionChainPlanner handles real opening data
- [ ] Add validation: check for degenerate geometry from real rooms
- [ ] Add logging: log room names, wall counts, opening counts
- [ ] Test with exported boundary data from real Revit room (JSON serialization)

### **Task 10: Add LoggingService Integration** (AC: All)
- [ ] Add LoggingService to all new Revit services
- [ ] Log room extraction: "Extracted 12 rooms from Level 1"
- [ ] Log boundary extraction: "Analyzed 4 walls, 2 openings in 'Conference Room 101'"
- [ ] Log dimension creation: "Created 47 dimension chains (transaction committed)"
- [ ] Log errors: "Failed to create dimension: Invalid reference at (23.5, 15.2, 0)"
- [ ] Follow Story 1.6 LoggingService patterns

## Dev Notes

### Architectural Patterns

**Layer 2 Service Pattern:**
```csharp
// Layer 2 services convert Revit API → POCOs
public class RevitRoomExtractor
{
    public List<RoomInfo> ExtractRooms(Document doc, DimensionCommand command)
    {
        // Query Revit API
        var rooms = new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .WhereElementIsNotElementType()
            .Cast<Room>();

        // Convert to POCOs
        return rooms.Select(r => new RoomInfo
        {
            Id = r.Id.IntegerValue.ToString(),
            Name = r.Name,
            Level = r.Level.Name,
            BoundingBoxMin = (r.BoundingBox.Min.X, r.BoundingBox.Min.Y, r.BoundingBox.Min.Z),
            BoundingBoxMax = (r.BoundingBox.Max.X, r.BoundingBox.Max.Y, r.BoundingBox.Max.Z)
        }).ToList();
    }
}
```

**Orchestration Pattern:**
```csharp
// Orchestrator coordinates Layer 1 + Layer 2
public class DimensionOrchestrator
{
    private readonly RevitRoomExtractor _roomExtractor;
    private readonly RevitBoundaryExtractor _boundaryExtractor;
    private readonly RoomBoundaryAnalyzer _boundaryAnalyzer; // Layer 1
    private readonly DimensionChainPlanner _planner; // Layer 1
    private readonly RevitDimensionCreator _creator;

    public Result Execute(DimensionCommand command, UIDocument uidoc)
    {
        // Layer 2: Extract from Revit
        var rooms = _roomExtractor.ExtractRooms(uidoc.Document, command);
        var boundaries = rooms.Select(r => _boundaryExtractor.ExtractBoundary(r, uidoc.Document));

        // Layer 1: Process (already tested)
        var analyzedBoundaries = boundaries.Select(b => _boundaryAnalyzer.AnalyzeBoundary(b));
        var chains = analyzedBoundaries.SelectMany(b => _planner.PlanDimensions(b, command.Parameters));

        // Layer 2: Create in Revit
        return _creator.CreateDimensions(chains, uidoc.Document);
    }
}
```

### Revit API Reference Points

**Key Revit API Calls:**
- `Room.GetBoundarySegments()` - Extract wall curves
- `BoundarySegment.GetCurve()` - Get curve geometry
- `BoundarySegment.ElementId` - Get wall element (to filter separators)
- `Document.Create.NewDimension(view, line, referenceArray)` - Create dimension
- `ReferenceArray.Append(reference)` - Build dimension references
- `Wall.GetReferences(FaceSide.Interior)` - Get wall reference for dimensions

**Common Pitfalls:**
- Dimensions require a View context (cannot be created in 3D views)
- ReferenceArray order matters for continuous dimensions
- Room.GetBoundarySegments() can return nested loops (handle carefully)
- Opening detection requires spatial queries (bounding box intersection)

### Testing Strategy

**Unit Tests (Layer 1):** ✅ Already complete (73 tests, 152ms)

**Integration Tests (Layer 2):** Requires Revit running
- Create `conference_room_test.rvt` with known geometry
- Run tests via Revit Test Framework or manual execution
- Verify dimensions appear at expected positions
- Verify undo/redo works
- Performance target: < 5 seconds for 12-room test

**Manual Testing Checklist:**
1. Open sample project with 5-10 rooms
2. Run command: "Add dimensions to all rooms on Level 1"
3. Verify dimensions appear on all walls
4. Check opening gaps (doors/windows)
5. Test undo (Ctrl+Z) - all dimensions should disappear
6. Test with curved walls (should be skipped with warning)
7. Test with angled walls (should dimension along wall orientation)

### Project Structure

**New Files to Create:**
```
RevitAI.CSharp/
├── Services/
│   ├── Revit/                           # NEW FOLDER
│   │   ├── RevitRoomExtractor.cs        # NEW - Task 1
│   │   ├── RevitBoundaryExtractor.cs    # NEW - Task 2
│   │   ├── RevitOpeningDetector.cs      # NEW - Task 3
│   │   ├── RevitDimensionCreator.cs     # NEW - Task 4
│   │   └── DimensionTypeResolver.cs     # NEW - Task 5
│   └── DimensionOrchestrator.cs         # NEW - Task 6
├── UI/
│   └── SimpleConfirmDialog.xaml/cs      # NEW - Task 7
└── tests/
    └── RevitAI.IntegrationTests/        # NEW PROJECT - Task 8
        ├── DimensionWorkflowTests.cs
        ├── RevitRoomExtractorTests.cs
        └── Fixtures/
            └── conference_room_test.rvt

**Modified Files:**
RevitAI.CSharp/Commands/CopilotCommand.cs  # Wire up DimensionOrchestrator
RevitAI.CSharp/Services/ClaudeService.cs   # Add Revit context to prompts (Task 1 of Story 2.1)
```

### Definition of Done

**Code:**
- [ ] All 10 tasks implemented and compiling
- [ ] Layer 2 services follow SIL pattern (convert Revit → POCOs → Layer 1)
- [ ] Error handling for common Revit API failures
- [ ] LoggingService integrated throughout

**Testing:**
- [ ] Integration tests pass in Revit (requires manual execution)
- [ ] End-to-end test: Command → Dimensions visible in model
- [ ] Transaction rollback test: Failed creation doesn't corrupt document
- [ ] Performance test: 12 rooms in < 5 seconds

**Documentation:**
- [ ] XML comments on all public methods
- [ ] Dev notes updated with Revit API usage patterns
- [ ] Manual testing checklist completed

**Validation:**
- [ ] Code review by senior developer
- [ ] Tested with real project (not just synthetic test model)
- [ ] Dimensions are undo-able with Ctrl+Z
- [ ] Opening gaps verified (doors/windows create dimension breaks)

**Known Limitations (Document for Story 2.4-2.5):**
- Simple confirmation dialog (no visual preview yet)
- No curved wall support (skipped with warning)
- No detailed failure reporting (just count of failures)
- No progress bar for large room sets

## Dependencies

**Requires Complete (Layer 1):**
- ✅ Story 2.1: DimensionCommandParser (POCOs, parsing logic)
- ✅ Story 2.2: RoomBoundaryAnalyzer (boundary analysis logic)
- ✅ Story 2.3: DimensionChainPlanner (dimension planning logic)
- ✅ Epic 1: ClaudeService, SafetyValidator, LoggingService

**Enables (Layer 2):**
- 🔓 Story 2.4: Dimension Preview & Confirmation (needs dimension creation to preview)
- 🔓 Story 2.5: Edge Case Handling (needs real geometry to test edge cases)
- 🔓 End-to-end functional testing in Revit

## Risk Assessment

**Risks:**
1. **Revit API Complexity** - GetBoundarySegments() can return complex nested loops
   - Mitigation: Start with rectangular rooms, add complex geometry incrementally

2. **Reference Array Construction** - Building valid ReferenceArray is tricky
   - Mitigation: Use Wall.GetReferences() for reliable reference points

3. **Performance** - Large room sets (50+ rooms) might be slow
   - Mitigation: Add progress reporting, optimize FilteredElementCollector queries

4. **Transaction Failures** - Partial failures could corrupt document
   - Mitigation: Single transaction for all dimensions, rollback on any error

5. **View Context** - Dimensions require active view context
   - Mitigation: Use uidoc.ActiveView for dimension creation, validate view type

**Confidence Level:** 🟢 High
- Layer 1 logic is proven (73 tests passing)
- Revit API patterns are well-documented
- Similar services implemented in Epic 1 (ClaudeService, SafetyValidator)

## Success Metrics

**Functional:**
- ✅ Dimensions appear in Revit model at correct positions
- ✅ Opening gaps are correct (doors/windows)
- ✅ Dimensions are undo-able with Ctrl+Z
- ✅ Works with 12+ room test project

**Performance:**
- ✅ 12 rooms dimensioned in < 5 seconds
- ✅ No UI freezing during execution (use ExternalEvent if needed)

**Quality:**
- ✅ Zero document corruption (transaction safety)
- ✅ Clear error messages for failures
- ✅ Graceful handling of partial failures

## File List

**New Files:**
- `RevitAI.CSharp/Services/Revit/RevitRoomExtractor.cs`
- `RevitAI.CSharp/Services/Revit/RevitBoundaryExtractor.cs`
- `RevitAI.CSharp/Services/Revit/RevitOpeningDetector.cs`
- `RevitAI.CSharp/Services/Revit/RevitDimensionCreator.cs`
- `RevitAI.CSharp/Services/Revit/DimensionTypeResolver.cs`
- `RevitAI.CSharp/Services/DimensionOrchestrator.cs`
- `RevitAI.CSharp/UI/SimpleConfirmDialog.xaml`
- `RevitAI.CSharp/UI/SimpleConfirmDialog.xaml.cs`
- `RevitAI.CSharp/tests/RevitAI.IntegrationTests/RevitAI.IntegrationTests.csproj`
- `RevitAI.CSharp/tests/RevitAI.IntegrationTests/DimensionWorkflowTests.cs`

**Existing Test Fixture (Already Available):**
- `temp/test_rooms.rvt` (400MB real Revit project, added 2025-11-17) ✅

**Modified Files:**
- `RevitAI.CSharp/Commands/CopilotCommand.cs` - Wire up DimensionOrchestrator
- `RevitAI.CSharp/Services/ClaudeService.cs` - Add Revit context (levels, selections)

---

## Change Log

- 2025-11-17: Story drafted from deferred Layer 2 tasks in Stories 2.1, 2.2, 2.3
- Consolidates 5 deferred tasks into single cohesive integration story

---

**Created:** 2025-11-17
**Source:** Deferred Layer 2 tasks from Epic 2 Stories 2.1-2.3
**Author:** Analysis of testability matrix and Layer 1 completion
