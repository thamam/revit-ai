# Story 2.1: Dimension Command Parser (NLU for Dimensions)

**Epic:** Epic 2: Intelligent Dimension Automation
**Status:** drafted
**Priority:** High
**Estimated Effort:** 3-4 days

## Story

As a developer,
I want to parse natural language dimension prompts into structured actions,
so that the system understands the user's intent and scope for dimensioning operations.

## Acceptance Criteria

### AC-2.1.1: Hebrew/English Command Parsing
- [ ] Hebrew prompt "תוסיף מידות לכל החדרים בקומה 1" correctly parsed to structured action
- [ ] English prompt "Add dimensions to all rooms on Level 1" correctly parsed to same structure
- [ ] Both languages produce identical action schema with same target and parameters

### AC-2.1.2: Structured Action Schema
- [ ] Parser returns JSON structure: `{operation: "create_dimensions", target: {...}, parameters: {...}}`
- [ ] Operation type correctly identified (create_dimensions vs create_tags vs read_elements)
- [ ] Target scope includes: element_type, filter_criteria, level_name (if specified)
- [ ] Parameters include: dimension_style, offset, placement preferences

### AC-2.1.3: Scope Recognition
- [ ] "all rooms" → targets all rooms in project
- [ ] "selected rooms" → targets only currently selected rooms
- [ ] "rooms on Level 1" → filters by level name
- [ ] "current view" → limits to elements visible in active view
- [ ] "all rooms except corridors" → supports exclusion filters

### AC-2.1.4: Ambiguity Resolution
- [ ] Ambiguous prompts trigger clarifying questions (e.g., "Which rooms do you mean?")
- [ ] Missing level name prompts for clarification
- [ ] Invalid element types are rejected with helpful suggestions
- [ ] Multi-step resolution supported (ask → respond → refine)

### AC-2.1.5: Error Handling
- [ ] Invalid/malformed prompts return clear error messages
- [ ] Network failures handled gracefully with retry option
- [ ] Context-missing errors provide guidance (e.g., "No rooms found on Level 2")

## Tasks / Subtasks

- [ ] **Task 1: Design NLU System Prompt** (AC: 2.1.1, 2.1.2)
  - [ ] Create system prompt with Revit context (available levels, element types)
  - [ ] Define action schema with TypeScript-style types for validation
  - [ ] Include Hebrew/English examples in prompt
  - [ ] Add firm standard defaults (dimension offset, style preferences)

- [ ] **Task 2: Implement DimensionCommandParser Service** (AC: 2.1.2, 2.1.3)
  - [ ] Create POCO models for DimensionCommand, TargetScope, DimensionParameters
  - [ ] Implement parsing logic with JSON deserialization
  - [ ] Add scope recognition (all rooms, selected, level-filtered, current view)
  - [ ] Follow DimensionPlanningService pattern (Layer 1 pure logic where possible)

- [ ] **Task 3: Integrate with ClaudeService** (AC: 2.1.1, 2.1.5)
  - [ ] Add ParseDimensionCommand method to ClaudeService
  - [ ] Include Revit context in API call (levels, current view, selection)
  - [ ] Handle API errors and network failures
  - [ ] Implement retry logic for transient failures

- [ ] **Task 4: Implement Ambiguity Resolution Flow** (AC: 2.1.4)
  - [ ] Design clarification question schema
  - [ ] Track conversation state for multi-turn resolution
  - [ ] Validate user responses and refine action
  - [ ] Add timeout/cancel for stuck conversations

- [ ] **Task 5: Create Unit Tests** (AC: All)
  - [ ] Test Hebrew command parsing (minimum 5 test cases)
  - [ ] Test English command parsing (minimum 5 test cases)
  - [ ] Test scope recognition (all rooms, selected, level, view)
  - [ ] Test ambiguity detection and question generation
  - [ ] Test error handling paths
  - [ ] Follow Layer 1 pattern: Use POCOs, test in milliseconds

## Dev Notes

### Architectural Patterns

- **Layer 1 SIL Pattern:** Create POCOs for command models (DimensionCommand, TargetScope) that can be unit tested without Claude API
- **Service Separation:** Keep NLU logic (prompt building, response parsing) separate from Revit operations
- **Schema-First Design:** Define action schema first, then implement parsing around it
- **Dependency Injection:** Parser should accept configurable context (levels, styles) for testability

### Testing Standards (from Story 0)

- Use NUnit + Moq for Layer 1 tests
- Tests must run in < 1 second on Linux
- Follow Arrange-Act-Assert pattern
- Create POCO test data factories (like RoomInfo.CreateRectangular())
- Living specification tests tie PRD examples to executable code

### Project Structure Notes

**New Files to Create:**
```
RevitAI.CSharp/
├── Models/
│   └── Commands/
│       ├── DimensionCommand.cs        # POCO for parsed command
│       ├── TargetScope.cs             # POCO for scope filtering
│       └── DimensionParameters.cs     # POCO for dim settings
├── Services/
│   └── NLU/
│       ├── DimensionCommandParser.cs  # Parse text → structured command
│       └── PromptTemplates.cs         # System prompts for Claude
└── tests/
    └── RevitAI.UnitTests/
        └── NLU/
            └── DimensionCommandParserTests.cs
```

**Reuse from Story 0:**
- `ClaudeService.cs` for API communication
- `DimensionPlanningService.cs` for downstream planning logic
- POCO patterns (RoomInfo, WallInfo) for modeling

### Learnings from Previous Story

**From Story 0-0-sil-foundation (Status: in-progress)**

- **POCO Pattern Critical:** Business logic must use POCOs, not Revit types. DimensionCommandParser should return POCO DimensionCommand, not Revit-specific types.
- **Cross-Platform Testing:** Test project configured for net8.0 (not net8.0-windows) enables Linux testing. New tests should follow same pattern.
- **Service Isolation:** DimensionPlanningService demonstrates pure logic pattern. DimensionCommandParser should follow same: parse text → POCOs, no Revit API calls.
- **Test Performance:** 13 tests run in 23ms. Maintain this speed by using POCOs and avoiding external dependencies.

[Source: stories/0-0-sil-foundation.md#Dev-Agent-Record]

### References

- [Source: docs/epics.md#Story-2.1] - Original story specification with acceptance criteria
- [Source: docs/PRD.md] - Hebrew/English example prompts, user workflows
- [Source: docs/architecture.md#ADR-002] - Claude Sonnet 4.5 model selection rationale
- [Source: docs/architecture.md#ADR-007] - Operation allowlist pattern
- [Source: CLAUDE.md#Testing-Pattern] - NUnit + Moq setup instructions
- [Source: RevitAI.CSharp/Services/DimensionPlanningService.cs] - Layer 1 pure logic pattern
- [Source: RevitAI.CSharp/tests/RevitAI.UnitTests/] - Test project structure and patterns

## Definition of Done

- [ ] All acceptance criteria validated with passing tests
- [ ] Hebrew and English commands parse to identical structures
- [ ] Minimum 15 unit tests covering parsing, scope, ambiguity, errors
- [ ] Tests run in < 1 second (Layer 1 pattern)
- [ ] Integration with ClaudeService verified (can make actual API calls)
- [ ] Code follows POCO pattern established in Story 0
- [ ] No regressions in existing 13 tests
- [ ] Documentation updated with NLU schema

## Dependencies

- Story 1.2: Claude API Integration (Complete - Epic 1)
- Story 1.7: Basic Ribbon UI (Complete - Epic 1)
- Story 0: SIL Foundation (In-Progress - provides testing patterns)

## Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Hebrew parsing accuracy | High | Extensive test cases, Claude Sonnet has good Hebrew support |
| Ambiguous prompts complexity | Medium | Start with simple clarification flow, expand iteratively |
| API latency affecting UX | Medium | Async pattern, show progress indicator |
| Schema versioning | Low | Define schema in code with TypeScript-style types, version control |

---

## Dev Agent Record

### Context Reference

<!-- Path(s) to story context XML will be added here by context workflow -->

### Agent Model Used

Claude Sonnet 4.5 (claude-sonnet-4-5-20250929)

### Debug Log References

<!-- Implementation notes, decisions, and debugging info -->

### Completion Notes List

<!-- Summary of what was implemented -->

### File List

<!-- Files created, modified, or deleted -->

---

## Change Log

- 2025-11-16: Story drafted from Epic 2 specifications

---

**Created:** 2025-11-16
**Source:** Epic 2: Intelligent Dimension Automation - Story 2.1 from epics.md
**Author:** Generated by create-story workflow
