# Story 2.2: Tag Placement Engine with Spatial Intelligence

**Epic:** Epic 2: Intelligent Automation (Refactored - Auto-Tagging Phase)
**Status:** ready-for-dev
**Priority:** P0 - Critical Path
**Estimated Effort:** 5-7 days
**Dependencies:** Story 2.1 (Auto-Tag Parser) ✅ DONE

## Story

As a developer,
I want to implement intelligent tag placement logic with collision avoidance,
So that tags are positioned professionally without overlapping.

## Acceptance Criteria

### AC-2.2.1: Basic Tag Placement Calculation
- [ ] Given a list of target elements (e.g., 24 doors) from parsed scope
- [ ] When tag placement is calculated using `CalculatePlacements()` method
- [ ] Then each tag has a proposed `XYZ` location based on element center + configured offset
- [ ] And tag placement data includes: element reference, location, leader flag, status (success/failed)

### AC-2.2.2: Collision Detection & Avoidance
- [ ] Given calculated tag placements
- [ ] When collision detection runs against existing tags and elements
- [ ] Then collisions are identified using bounding box intersection (with buffer margin)
- [ ] And colliding tags trigger alternative placement attempts (up to 10 attempts)
- [ ] And placement algorithm tries different angles: 0°, 90°, 45°, 135°, 180°, 225°, 270°, 315°

### AC-2.2.3: Alternative Placement Strategy
- [ ] When preferred placement has collision
- [ ] Then algorithm tries radial offsets at different angles (8 directions)
- [ ] And offset distance increases with attempt number (1.0x, 1.25x, 1.5x, etc.)
- [ ] And leader line is automatically enabled when tag is placed far from element center
- [ ] And algorithm stops after 10 attempts, marking placement as failed

### AC-2.2.4: Success Rate Target
- [ ] Given 100 elements to tag in typical architectural view
- [ ] When placement algorithm runs
- [ ] Then at least 95% of tags are placed successfully (no collision)
- [ ] And failed placements are logged with clear reasons
- [ ] And partial success is supported: "Tagged 95 of 100 elements (5 failed)"

### AC-2.2.5: View Type Handling
- [ ] Tags are placed correctly in floor plans (2D XY plane)
- [ ] Tags are placed correctly in elevations (2D XZ plane)
- [ ] Tags are placed correctly in sections (2D plane perpendicular to section line)
- [ ] View-specific bounding boxes are used for placement calculations

### AC-2.2.6: Performance Target
- [ ] Calculate placements for 500 elements in under 5 seconds
- [ ] Collision detection algorithm has O(n²) worst-case but optimized with spatial indexing
- [ ] Memory usage remains reasonable (< 500MB for 500 elements)

## Tasks / Subtasks

- [ ] **Task 1: Design Tag Placement POCO Models** (AC: 2.2.1)
  - [ ] Create `TagPlacement.cs` POCO with element, location, leader flag, status
  - [ ] Create `TagPlacementCandidate.cs` POCO for collision testing
  - [ ] Create `PlacementResult.cs` POCO with success count, failed count, details
  - [ ] Add factory methods for test data (CreateSuccess, CreateFailed)
  - [ ] Follow Layer 1 SIL pattern: no Revit dependencies

- [ ] **Task 2: Implement Placement Strategy Interface** (AC: 2.2.1, 2.2.3)
  - [ ] Create `IPlacementStrategy` interface with GetPreferredPlacement, GetAlternativePlacement
  - [ ] Implement `CenterOffsetStrategy` class (default strategy)
  - [ ] GetPreferredPlacement: Returns element center + upward offset
  - [ ] GetAlternativePlacement: Returns radial offset at different angle based on attempt number
  - [ ] Configurable offset distance (injected via constructor)

- [ ] **Task 3: Implement Collision Detection Service** (AC: 2.2.2)
  - [ ] Create `ITagCollisionDetector` interface
  - [ ] Implement `SimpleBoundingBoxCollisionDetector` class
  - [ ] HasCollision method checks bounding box intersection
  - [ ] EstimateTagBounds method calculates tag size (0.3' wide x 0.15' tall typical)
  - [ ] Buffer margin: 0.1' (configurable)
  - [ ] Unit test with overlapping and non-overlapping scenarios

- [ ] **Task 4: Implement Tag Placement Service** (AC: 2.2.1-2.2.4)
  - [ ] Create `TagPlacementService.cs` in Services/ folder (Layer 1 pure logic)
  - [ ] Constructor injection: ITagCollisionDetector, IPlacementStrategy, LoggingService
  - [ ] CalculatePlacements method iterates elements, tries placements
  - [ ] Loop: Try preferred placement → Check collision → Try alternatives (up to 10)
  - [ ] Track existing tag bounds to prevent new collisions
  - [ ] Return PlacementResult with success/failed counts

- [ ] **Task 5: Add View Type Handling** (AC: 2.2.5)
  - [ ] Extend POCO models with ViewType enum (FloorPlan, Elevation, Section)
  - [ ] CenterOffsetStrategy adjusts offset direction based on view type
  - [ ] Floor plan: Offset upward (Y+)
  - [ ] Elevation: Offset to the right (X+)
  - [ ] Section: Offset perpendicular to section line
  - [ ] Unit tests for each view type

- [ ] **Task 6: Create Comprehensive Unit Tests** (AC: All)
  - [ ] Test CenterOffsetStrategy with 0° and 90° offsets
  - [ ] Test GetAlternativePlacement with increasing distance
  - [ ] Test collision detector with overlapping bounding boxes
  - [ ] Test TagPlacementService with 0, 10, 100, 500 elements
  - [ ] Test placement success rate (target 95%+)
  - [ ] Test performance (500 elements < 5 seconds)
  - [ ] Follow Layer 1 pattern: Use POCOs, test in milliseconds

- [ ] **Task 7: Add LoggingService Integration** (AC: All)
  - [ ] Log placement calculation start/completion
  - [ ] Log collision detection warnings (when >3 attempts needed)
  - [ ] Log failed placements with element ID and reason
  - [ ] Log performance metrics (calculation time, success rate)

- [ ] **Task 8: Performance Optimization** (AC: 2.2.6)
  - [ ] Implement spatial indexing for collision detection (if needed)
  - [ ] Profile CalculatePlacements with 500 elements
  - [ ] Optimize bounding box calculations (cache where possible)
  - [ ] Target: < 5 seconds for 500 elements

## Dev Notes

### Architectural Patterns

- **Layer 1 SIL Pattern**: Create POCOs for tag placements (TagPlacement, TagPlacementCandidate, PlacementResult) that can be unit tested without Revit API
- **Service Separation**:
  - Layer 1: TagPlacementService (pure logic, geometry calculations)
  - Layer 2: RevitTagCreationService (Revit API interaction - Story 2.3)
- **Strategy Pattern**: IPlacementStrategy enables different placement algorithms (center offset, grid-based, etc.)
- **Dependency Injection**: Service accepts interfaces for testability and flexibility

### Testing Standards (from Stories 0, 2.1)

- Use NUnit + Moq for Layer 1 tests
- Tests must run in < 1 second on Linux (target for unit tests)
- Follow Arrange-Act-Assert pattern
- Create POCO test data factories (TagPlacement.CreateSuccess(), CreateFailed())
- Performance test separate: [Category("Performance")] for 500-element benchmark

### Project Structure

**New Files to Create:**
```
RevitAI.CSharp/
├── Models/
│   └── Domain/
│       ├── TagPlacement.cs              # POCO for tag placement result
│       ├── TagPlacementCandidate.cs     # POCO for collision testing
│       └── PlacementResult.cs           # POCO for batch result summary
├── Services/
│   ├── TagPlacementService.cs           # Layer 1 pure logic for placement calculation
│   ├── Interfaces/
│   │   ├── IPlacementStrategy.cs        # Interface for placement algorithms
│   │   └── ITagCollisionDetector.cs     # Interface for collision detection
│   ├── CenterOffsetStrategy.cs          # Default placement strategy
│   └── SimpleBoundingBoxCollisionDetector.cs  # Collision detection implementation
└── tests/
    └── RevitAI.UnitTests/
        └── Services/
            ├── TagPlacementServiceTests.cs
            ├── CenterOffsetStrategyTests.cs
            └── CollisionDetectorTests.cs
```

**Reuse from Story 2.1:**
- `RevitAction.cs` and `AutoTagValidation.cs` - Provides parsed tag commands
- POCO patterns for modeling
- Factory method pattern for test data
- LoggingService integration pattern

### Learnings from Previous Stories

**From Story 2.1 (Auto-Tag Parser) - Status: done**

- **Parsing Complete**: DimensionCommandParser (now supports auto_tag operation)
- **Validation Complete**: SafetyValidator enforces scope limits (500 elements max)
- **This story consumes**: RevitAction with operation="auto_tag", targets={category, scope}, parameters={tag_type, placement, leader}

**Layer 1 SIL Pattern Critical**:
- POCOs must have NO Revit API dependencies
- TagPlacement, TagPlacementCandidate, PlacementResult should follow same pattern
- Test project configured for net8.0 (not net8.0-windows) enables Linux testing

**Factory Methods for Test Data**:
- TagPlacement.CreateSuccess(elementId, location, leader)
- TagPlacement.CreateFailed(elementId, reason)
- PlacementResult.CreateEmpty(), CreatePartialSuccess(successCount, failCount)

**Testing Performance**: Story 2.1 achieved 24 tests in 123ms
- Maintain this speed by using POCOs and avoiding external dependencies
- Target: < 1 second for all unit tests
- Performance test (500 elements) separate: [Category("Performance")]

**Architectural Deviations**: None - Story 2.1 followed all ADRs and patterns correctly

### References

- [Source: docs/epic2-refactored.md#Story-2.2] - Story specification with acceptance criteria
- [Source: docs/PRD.md#Epic-2] - Epic 2 goal: automate "Auto-tag elements with collision avoidance"
- [Source: docs/architecture.md#ADR-007] - Operation allowlist enforcement
- [Source: CLAUDE.md#Layer-1-SIL-Pattern] - POCOs with no Revit dependencies
- [Source: RevitAI.CSharp/Models/RevitAction.cs] - Existing POCO pattern to follow
- [Source: docs/research/Task-2-PyRevit-and-LLM-Integration-Analysis.md] - Market validation for auto-tagging priority

## Definition of Done

- [ ] All acceptance criteria validated with passing tests
- [ ] Tag placement algorithm achieves 95%+ success rate (no collision)
- [ ] Collision detection works with bounding box intersection
- [ ] Alternative placement strategy tries 10 attempts before failing
- [ ] View type handling (floor plan, elevation, section) implemented
- [ ] Performance target met: 500 elements < 5 seconds
- [ ] Minimum 15 unit tests covering all placement scenarios
- [ ] Tests run in < 1 second for unit tests (performance test separate)
- [ ] LoggingService integrated for placement operations
- [ ] Code follows POCO pattern established in Stories 0 and 2.1
- [ ] No regressions in existing tests (52 tests from Stories 0, 2.1, 2.2-old)
- [ ] Documentation updated with tag placement patterns

## Dependencies

- Story 2.1: Auto-Tag Parser (✅ DONE) - Provides RevitAction with auto_tag operation
- Story 1.6: Logging Infrastructure (✅ DONE in Epic 1) - LoggingService available
- Story 0: SIL Foundation (✅ DONE) - Provides POCO patterns and testing infrastructure

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Collision detection performance | Medium | Implement spatial indexing if O(n²) too slow for 500 elements |
| Placement success rate < 95% | High | Increase alternative placement attempts to 15-20, add more angles |
| View type complexity (elevation/section) | Medium | Start with floor plan (simplest), add view types incrementally |
| Tag size estimation inaccuracy | Low | Use conservative estimates (0.3' x 0.15'), add buffer margin |
| Leader line calculation complexity | Medium | Defer advanced leader routing to Story 2.3, use straight lines initially |

## Research Insights

**From Task 2 (PyRevit+LLM Integration Analysis):**
> "Users spend days manually tagging wall types and door numbers" - Auto-tagging is the #1 user pain point identified across commercial products (ArchiLabs, BIMLOGIQ) and user interviews (Studio Tema case study).

**From Task 3 (Testing Strategies Research):**
> "Read-Only/Annotation tasks have lower blast radius than geometric modifications" - Tag placement is annotation (metadata), making it safer than dimension placement (geometry collision complexity). Studio Tema case study shows firms quantify automation risk at 20,000 ILS insurance deductible.

**Strategic Alignment:**
This story delivers the core value proposition of Epic 2 by implementing intelligent tag placement that:
1. **Reduces manual work**: Automates days of tagging work
2. **Professional quality**: 95%+ collision-free placement
3. **Low risk**: Annotation-only operation, no geometry modification

---

## Dev Agent Record

### Context Reference

- `docs/stories/2-2-tag-placement-engine.context.xml` - To be created with story context

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

N/A - Story not started yet

### Completion Notes List

**Status:** Ready for development

**Next Steps:**
1. Create POCO models (TagPlacement, TagPlacementCandidate, PlacementResult)
2. Implement IPlacementStrategy interface and CenterOffsetStrategy
3. Implement ITagCollisionDetector interface and SimpleBoundingBoxCollisionDetector
4. Implement TagPlacementService with CalculatePlacements method
5. Create comprehensive unit tests (target 15+ tests)
6. Add LoggingService integration
7. Performance optimization (if needed for 500 elements < 5 seconds)

### File List

**Files to Create:**
- `RevitAI.CSharp/Models/Domain/TagPlacement.cs`
- `RevitAI.CSharp/Models/Domain/TagPlacementCandidate.cs`
- `RevitAI.CSharp/Models/Domain/PlacementResult.cs`
- `RevitAI.CSharp/Services/Interfaces/IPlacementStrategy.cs`
- `RevitAI.CSharp/Services/Interfaces/ITagCollisionDetector.cs`
- `RevitAI.CSharp/Services/CenterOffsetStrategy.cs`
- `RevitAI.CSharp/Services/SimpleBoundingBoxCollisionDetector.cs`
- `RevitAI.CSharp/Services/TagPlacementService.cs`
- `RevitAI.CSharp/tests/RevitAI.UnitTests/Services/TagPlacementServiceTests.cs`
- `RevitAI.CSharp/tests/RevitAI.UnitTests/Services/CenterOffsetStrategyTests.cs`
- `RevitAI.CSharp/tests/RevitAI.UnitTests/Services/CollisionDetectorTests.cs`

**Files to Modify:**
- `RevitAI.CSharp/tests/RevitAI.UnitTests/RevitAI.UnitTests.csproj` - Add new source file references

---

## Change Log

- 2025-11-21: Story created from Epic 2 refactored specifications (research-informed prioritization)

---

**Created:** 2025-11-21
**Source:** Epic 2: Intelligent Automation (Refactored) - Story 2.2 from epic2-refactored.md
**Author:** Generated by bmad start story 2.2 workflow
