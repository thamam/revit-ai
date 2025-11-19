# Story 2.6 Testing Guide: Layer 2 Integration with test_rooms.rvt

**Date:** 2025-11-17
**Test Fixture:** `temp/test_rooms.rvt` (400MB)
**Story:** 2.6 - Layer 2 Revit API Integration

## Overview

This document provides a comprehensive testing strategy for validating the Layer 2 Revit API integration using the real architectural test project `test_rooms.rvt`.

## Test Fixture Details

**File:** `temp/test_rooms.rvt`
**Size:** 400MB (substantial real-world project)
**Added:** 2025-11-17
**Purpose:** End-to-end integration testing for dimension automation

### What We Know About the Fixture

The file is a real Revit project (not a minimal test case), which means it likely contains:
- ✅ Multiple rooms with real geometry
- ✅ Various room types (rectangular, L-shaped, complex)
- ✅ Doors and windows (openings for gap handling)
- ✅ Multiple levels (for level filtering tests)
- ✅ Real architectural annotations and families
- ✅ Actual dimension styles from the template

**Size Significance:** 400MB suggests this is a production-scale project with rich geometry, making it an excellent validation target for our algorithms.

## Testing Strategy

### Phase 1: Discovery Tests (No Code Changes)

**Before writing any Layer 2 code**, we need to understand what's IN the test project:

#### Test 1.1: Room Discovery
```csharp
// Open test_rooms.rvt in Revit
// Run this in Revit Python Shell or C# macro:

var rooms = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Rooms)
    .WhereElementIsNotElementType()
    .Cast<Room>()
    .ToList();

Console.WriteLine($"Found {rooms.Count} rooms");
foreach (var room in rooms)
{
    Console.WriteLine($"  - {room.Name} (Level: {room.Level.Name}, Area: {room.Area} sqft)");
}
```

**Expected Output:** List of all rooms with names, levels, and areas
**Document:** Write down room count and names for test assertions

#### Test 1.2: Boundary Complexity Analysis
```csharp
// For each room, analyze boundary complexity:
foreach (var room in rooms)
{
    var segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
    var segmentCount = segments[0].Count; // First loop
    var hasCurves = segments[0].Any(s => s.GetCurve() is Arc);

    Console.WriteLine($"{room.Name}: {segmentCount} segments, Curved: {hasCurves}");
}
```

**Expected Output:** Segment counts per room, curved wall identification
**Purpose:** Understand complexity for Layer 1 algorithm validation

#### Test 1.3: Opening Detection
```csharp
// Count doors and windows:
var doors = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Doors)
    .WhereElementIsNotElementType()
    .Count();

var windows = new FilteredElementCollector(doc)
    .OfCategory(BuiltInCategory.OST_Windows)
    .WhereElementIsNotElementType()
    .Count();

Console.WriteLine($"Doors: {doors}, Windows: {windows}");
```

**Expected Output:** Total door and window counts
**Purpose:** Validate opening detection will have real data

#### Test 1.4: Dimension Style Inventory
```csharp
// List available dimension types:
var dimTypes = new FilteredElementCollector(doc)
    .OfClass(typeof(DimensionType))
    .Cast<DimensionType>()
    .ToList();

Console.WriteLine($"Available dimension types ({dimTypes.Count}):");
foreach (var dt in dimTypes)
{
    Console.WriteLine($"  - {dt.Name} (Family: {dt.FamilyName})");
}
```

**Expected Output:** List of dimension style names
**Purpose:** Know what styles are available for testing

### Phase 2: Layer 2 Service Unit Tests (Isolated)

Once Layer 2 services are implemented, test each service in isolation:

#### Test 2.1: RevitRoomExtractor
```csharp
[Test]
public void ExtractRooms_AllRooms_ReturnsRoomInfoList()
{
    // Arrange
    var doc = OpenTestDocument("temp/test_rooms.rvt");
    var command = new DimensionCommand
    {
        TargetScope = new TargetScope { FilterType = "all_rooms" }
    };
    var extractor = new RevitRoomExtractor();

    // Act
    var roomInfos = extractor.ExtractRooms(doc, command);

    // Assert
    Assert.IsNotEmpty(roomInfos, "Should extract rooms from test_rooms.rvt");
    Assert.IsTrue(roomInfos.All(r => !string.IsNullOrEmpty(r.Name)), "All rooms should have names");
    Assert.IsTrue(roomInfos.All(r => !string.IsNullOrEmpty(r.Level)), "All rooms should have levels");

    // Document actual count for regression testing:
    Console.WriteLine($"Extracted {roomInfos.Count} rooms from test_rooms.rvt");
}
```

#### Test 2.2: RevitBoundaryExtractor
```csharp
[Test]
public void ExtractBoundary_RectangularRoom_ReturnsValidBoundaryInfo()
{
    // Arrange
    var doc = OpenTestDocument("temp/test_rooms.rvt");
    var room = GetFirstRectangularRoom(doc); // Helper method
    var extractor = new RevitBoundaryExtractor();

    // Act
    var boundaryInfo = extractor.ExtractBoundary(room, doc);

    // Assert
    Assert.AreEqual(4, boundaryInfo.WallSegments.Count, "Rectangular room should have 4 walls");
    Assert.IsTrue(boundaryInfo.WallSegments.All(w => w.Normal != (0, 0, 0)),
        "All walls should have valid normals");
    Assert.AreEqual(4, boundaryInfo.Corners.Count, "Rectangular room should have 4 corners");
}
```

#### Test 2.3: RevitOpeningDetector
```csharp
[Test]
public void DetectOpenings_RoomWithDoor_ReturnsOpeningInfo()
{
    // Arrange
    var doc = OpenTestDocument("temp/test_rooms.rvt");
    var room = GetRoomWithDoor(doc); // Helper: find room with door
    var boundaries = new RevitBoundaryExtractor().ExtractBoundary(room, doc);
    var detector = new RevitOpeningDetector();

    // Act
    var openings = detector.DetectOpenings(room, boundaries.WallSegments, doc);

    // Assert
    Assert.IsNotEmpty(openings, "Room with door should have detected openings");
    Assert.IsTrue(openings.All(o => o.Width > 0), "All openings should have positive width");
    Assert.IsTrue(openings.All(o => o.WallSegmentIndex >= 0),
        "All openings should be associated with a wall segment");
}
```

### Phase 3: Layer 1 Validation with Real Data

Test that Layer 1 algorithms work correctly with REAL Revit geometry (not synthetic test data):

#### Test 3.1: RoomBoundaryAnalyzer with Real Boundaries
```csharp
[Test]
public void RoomBoundaryAnalyzer_RealRevitBoundary_HandlesCorrectly()
{
    // Arrange
    var doc = OpenTestDocument("temp/test_rooms.rvt");
    var room = GetFirstRoom(doc);
    var boundaryInfo = new RevitBoundaryExtractor().ExtractBoundary(room, doc);
    var analyzer = new RoomBoundaryAnalyzer();

    // Act
    var analyzedBoundary = analyzer.AnalyzeBoundary(boundaryInfo.Room);

    // Assert
    Assert.IsNotNull(analyzedBoundary);
    Assert.AreEqual(boundaryInfo.WallSegments.Count, analyzedBoundary.WallSegments.Count,
        "Analyzer should preserve wall segment count");
    // Verify no crashes with real geometry
}
```

#### Test 3.2: DimensionChainPlanner with Real Openings
```csharp
[Test]
public void DimensionChainPlanner_RealOpenings_CreatesGapsCorrectly()
{
    // Arrange
    var doc = OpenTestDocument("temp/test_rooms.rvt");
    var room = GetRoomWithMultipleOpenings(doc);
    var boundary = GetCompleteBoundary(room, doc); // Extract + Analyze
    var planner = new DimensionChainPlanner();
    var parameters = new DimensionParameters { OffsetMm = 200 };

    // Act
    var chains = planner.PlanDimensions(boundary, parameters);

    // Assert
    var chainsWithOpenings = chains.Where(c => c.OpeningIndices.Count > 0).ToList();
    Assert.IsNotEmpty(chainsWithOpenings, "Should plan chains with opening gaps");

    // Verify reference points include opening edges
    foreach (var chain in chainsWithOpenings)
    {
        Assert.IsTrue(chain.ReferencePoints.Count > 2,
            "Chains with openings should have > 2 reference points (start, opening edges, end)");
    }
}
```

### Phase 4: End-to-End Integration Tests

Full workflow tests using the complete orchestrator:

#### Test 4.1: Single Room End-to-End
```csharp
[Test]
public void EndToEnd_SingleRectangularRoom_CreatesDimensions()
{
    // Arrange
    var uidoc = OpenTestDocument("temp/test_rooms.rvt");
    var room = GetFirstRectangularRoom(uidoc.Document);
    var command = CreateDimensionCommandForRoom(room);
    var orchestrator = new DimensionOrchestrator(/* dependencies */);

    // Act
    var result = orchestrator.Execute(command, uidoc);

    // Assert
    Assert.IsTrue(result.Success, $"Should succeed, but got: {result.ErrorMessage}");
    Assert.AreEqual(4, result.DimensionCount, "Rectangular room should create 4 dimension chains");

    // Verify dimensions exist in document
    var dimensions = new FilteredElementCollector(uidoc.Document)
        .OfClass(typeof(Dimension))
        .Cast<Dimension>()
        .Where(d => d.Name.Contains("AI:"))
        .ToList();
    Assert.AreEqual(4, dimensions.Count, "Should find 4 dimensions in document");
}
```

#### Test 4.2: Multiple Rooms Performance Test
```csharp
[Test]
public void EndToEnd_AllRoomsInTestProject_CompletesInReasonableTime()
{
    // Arrange
    var uidoc = OpenTestDocument("temp/test_rooms.rvt");
    var command = new DimensionCommand
    {
        TargetScope = new TargetScope { FilterType = "all_rooms" }
    };
    var orchestrator = new DimensionOrchestrator(/* dependencies */);
    var stopwatch = Stopwatch.StartNew();

    // Act
    var result = orchestrator.Execute(command, uidoc);
    stopwatch.Stop();

    // Assert
    Assert.IsTrue(result.Success);
    Assert.LessOrEqual(stopwatch.ElapsedMilliseconds, 10000,
        "Should complete all rooms in < 10 seconds");

    // Document results
    Console.WriteLine($"Processed {result.RoomCount} rooms in {stopwatch.ElapsedMilliseconds}ms");
    Console.WriteLine($"Created {result.DimensionCount} dimension chains");
    Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / result.RoomCount}ms per room");
}
```

#### Test 4.3: Transaction Rollback Test
```csharp
[Test]
public void EndToEnd_FailedDimensionCreation_RollsBackTransaction()
{
    // Arrange
    var uidoc = OpenTestDocument("temp/test_rooms.rvt");
    var dimensionCountBefore = CountDimensions(uidoc.Document);

    // Create command that will fail (e.g., invalid dimension style)
    var command = new DimensionCommand
    {
        Parameters = new DimensionParameters { DimensionStyle = "INVALID_STYLE" }
    };
    var orchestrator = new DimensionOrchestrator(/* dependencies */);

    // Act
    var result = orchestrator.Execute(command, uidoc);

    // Assert
    Assert.IsFalse(result.Success, "Should fail with invalid dimension style");

    var dimensionCountAfter = CountDimensions(uidoc.Document);
    Assert.AreEqual(dimensionCountBefore, dimensionCountAfter,
        "Failed transaction should not create any dimensions (rollback)");
}
```

#### Test 4.4: Undo/Redo Verification
```csharp
[Test]
public void EndToEnd_CreatedDimensions_AreUndoable()
{
    // Arrange
    var uidoc = OpenTestDocument("temp/test_rooms.rvt");
    var command = CreateSimpleCommand();
    var orchestrator = new DimensionOrchestrator(/* dependencies */);
    var dimensionCountBefore = CountDimensions(uidoc.Document);

    // Act
    orchestrator.Execute(command, uidoc);
    var dimensionCountAfter = CountDimensions(uidoc.Document);

    // Undo
    uidoc.Document.Undo();
    var dimensionCountAfterUndo = CountDimensions(uidoc.Document);

    // Assert
    Assert.Greater(dimensionCountAfter, dimensionCountBefore, "Dimensions should be created");
    Assert.AreEqual(dimensionCountBefore, dimensionCountAfterUndo,
        "Undo should remove all created dimensions");
}
```

### Phase 5: Manual Testing Checklist

After automated tests pass, perform manual validation in Revit:

#### Manual Test 1: Visual Inspection
- [ ] Open `temp/test_rooms.rvt` in Revit 2026
- [ ] Run dimension command: "Add dimensions to all rooms"
- [ ] **Verify:** Dimension chains appear on all walls
- [ ] **Verify:** Dimension lines are 200mm (0.656ft) from walls
- [ ] **Verify:** Dimensions are parallel to walls
- [ ] **Verify:** Opening gaps are visible (doors/windows)
- [ ] **Verify:** Dimension text is readable and correct
- [ ] **Verify:** No overlapping dimensions

#### Manual Test 2: Edge Cases
- [ ] Find room with curved wall → **Should skip with warning**
- [ ] Find room with angled wall → **Should dimension along wall angle**
- [ ] Find very small room → **Should handle without errors**
- [ ] Find L-shaped room → **Should dimension all wall segments**

#### Manual Test 3: Transaction Safety
- [ ] Run dimension command
- [ ] Press Ctrl+Z immediately
- [ ] **Verify:** All dimensions disappear (atomic undo)
- [ ] Press Ctrl+Y (redo)
- [ ] **Verify:** All dimensions reappear

#### Manual Test 4: Performance
- [ ] Time the operation: "Add dimensions to all rooms"
- [ ] **Target:** < 5 seconds for typical room count
- [ ] **Document:** Actual time, room count, dimension count

#### Manual Test 5: Error Handling
- [ ] Lock a view and try to add dimensions
- [ ] **Verify:** Clear error message (not crash)
- [ ] Try command with no rooms selected (selection mode)
- [ ] **Verify:** Helpful error message

## Test Results Documentation Template

After running all tests, document results in this format:

### Test Session Report

**Date:** [Date]
**Revit Version:** 2026
**Test Fixture:** temp/test_rooms.rvt
**Story:** 2.6 - Layer 2 Integration

#### Discovery Phase Results
- **Total Rooms:** [X] rooms
- **Total Doors:** [X] doors
- **Total Windows:** [X] windows
- **Room Types Found:** [List: Rectangular, L-shaped, etc.]
- **Curved Walls:** [Yes/No] - [Count if yes]
- **Levels:** [List of level names]
- **Available Dimension Styles:** [List]

#### Automated Test Results
- **Unit Tests Passed:** [X/Y]
- **Integration Tests Passed:** [X/Y]
- **Layer 1 Validation Passed:** [X/Y]
- **End-to-End Tests Passed:** [X/Y]

#### Performance Metrics
- **All Rooms Processing Time:** [X]ms ([Y] rooms)
- **Average Time Per Room:** [Z]ms
- **Total Dimensions Created:** [N] chains
- **Memory Usage:** [Peak MB]

#### Manual Testing Results
- **Visual Inspection:** [Pass/Fail] - [Notes]
- **Edge Cases:** [Pass/Fail] - [Notes]
- **Transaction Safety:** [Pass/Fail] - [Notes]
- **Error Handling:** [Pass/Fail] - [Notes]

#### Issues Found
1. [Issue description] - [Severity: High/Medium/Low]
2. [Issue description] - [Severity: High/Medium/Low]

#### Recommendations
- [Recommendation 1]
- [Recommendation 2]

## Helper Methods for Test Implementation

```csharp
// Test helper utilities
public static class TestHelpers
{
    public static UIDocument OpenTestDocument(string relativePath)
    {
        var fullPath = Path.Combine(ProjectRoot, relativePath);
        var uiapp = GetUIApplication();
        var uidoc = uiapp.OpenAndActivateDocument(fullPath);
        return uidoc;
    }

    public static Room GetFirstRectangularRoom(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfCategory(BuiltInCategory.OST_Rooms)
            .Cast<Room>()
            .FirstOrDefault(r => IsRectangular(r));
    }

    public static bool IsRectangular(Room room)
    {
        var segments = room.GetBoundarySegments(new SpatialElementBoundaryOptions());
        if (segments.Count == 0) return false;

        var firstLoop = segments[0];
        return firstLoop.Count == 4 &&
               firstLoop.All(s => s.GetCurve() is Line);
    }

    public static int CountDimensions(Document doc)
    {
        return new FilteredElementCollector(doc)
            .OfClass(typeof(Dimension))
            .GetElementCount();
    }

    public static RoomBoundaryInfo GetCompleteBoundary(Room room, Document doc)
    {
        var extractor = new RevitBoundaryExtractor();
        var detector = new RevitOpeningDetector();
        var analyzer = new RoomBoundaryAnalyzer();

        var rawBoundary = extractor.ExtractBoundary(room, doc);
        rawBoundary.Openings = detector.DetectOpenings(room, rawBoundary.WallSegments, doc);

        return analyzer.AnalyzeBoundary(rawBoundary.Room);
    }
}
```

## Success Criteria Summary

Story 2.6 is considered **COMPLETE** when:

✅ **Discovery Phase:** Documented room count, geometry types, available styles
✅ **Unit Tests:** All Layer 2 services tested in isolation (90%+ pass rate)
✅ **Layer 1 Validation:** Algorithms work correctly with real Revit data
✅ **Integration Tests:** End-to-end workflow creates visible dimensions
✅ **Performance:** All rooms processed in < 10 seconds
✅ **Transaction Safety:** Undo/redo works correctly
✅ **Manual Testing:** Visual inspection confirms correct placement
✅ **Documentation:** Test results documented in this format

## Next Steps After Testing

Once Story 2.6 testing is complete:

1. **Document findings** in test results report
2. **Update Layer 1 algorithms** if real geometry reveals edge cases
3. **Proceed to Story 2.4** (Preview/Confirm workflow)
4. **Proceed to Story 2.5** (Edge case handling: curved walls, etc.)

---

**Testing Status:** Not Started (Story 2.6 currently in "drafted" status)
**Last Updated:** 2025-11-17
