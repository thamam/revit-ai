# revit-ai - Epic Breakdown

**Author:** Doc
**Date:** 2025-11-20
**Project Level:** 2 (BMad Method)
**Target Scale:** Small studio (10 users, single firm)

---

## Overview

This document provides the complete epic and story breakdown for revit-ai, decomposing the requirements from the [PRD](./PRD.md) into implementable stories.

**Living Document Notice:** This version incorporates research findings from 2025-11-20 (Tasks 1-3: MCP Server Landscape, PyRevit+LLM Integration, Testing Strategies) that informed Epic 2 refactoring to prioritize auto-tagging over dimension automation.

### Epic Structure Overview

This project is organized into **2 epics** that establish the foundation and validate the AI-automation flow with market-validated features:

**Epic 1: Foundation & Core Infrastructure**
Establish the technical foundation for AI-driven Revit automation. Creates the C# .NET SDK architecture, Claude API integration, and safety mechanisms that enable all AI co-pilot features.

**Epic 2: Intelligent Automation (Research-Informed)**
Enable architects to automate tedious Revit tasks through natural language. Prioritizes auto-tagging (Stories 2.1-2.3, high value/low risk) before dimension automation (Stories 2.4-2.6, deferred to Phase 2), based on research showing auto-tagging is the #1 user pain point and has lower blast radius.

**Sequencing:** Foundation → Auto-Tagging → Dimensions. Each phase builds on previous work and delivers incremental value.

**Research Rationale:** Task 2 findings show "users spend days manually tagging" and commercial products (ArchiLabs, BIMLOGIQ) prioritize annotation tasks. Task 3 (Studio Tema case study) shows firms quantify automation risk at 20,000 ILS and prefer starting with "read-only/annotation" tasks.

---

## Functional Requirements Inventory

From PRD.md, extracted functional requirements:

### PoC Feature 1: Auto-Tagging (Epic 2, Phase 1)
- **F2.1**: Element selection via natural language (doors, windows, rooms, walls)
- **F2.2**: Intelligent tag placement (collision avoidance, alignment, firm standards)
- **F2.3**: Preview & confirmation workflow
- **F2.4**: Configuration support (tag offset, family override, preferences)

### PoC Feature 2: Internal Dimensions (Epic 2, Phase 2 - Deferred)
- **F1.1**: Natural language input (Hebrew/English)
- **F1.2**: Dimension generation (continuous chains, alignment, offsets)
- **F1.3**: Preview & confirmation
- **F1.4**: Safety constraints (no deletion, scope limits)

### Core System Requirements (Epic 1)
- **F3.1**: C# Revit Add-in interface (ribbon, dialog, ExternalEvent pattern)
- **F3.2**: LLM integration (Claude API, structured context, error handling)
- **F3.3**: Logging & diagnostics (operation logs, errors, performance metrics)

### Non-Functional Requirements (All Epics)
- **NFR-P1**: Response time (<5s LLM, <30s execution)
- **NFR-R1**: Data safety (transactions, allowlist, validation)
- **NFR-R2**: Error handling (graceful recovery, clear messages)
- **NFR-U1**: Ease of use (<5min training, conversational feedback)
- **NFR-S1**: Data privacy (minimal LLM context, HTTPS, anonymization)
- **NFR-M1**: Code quality (type hints, unit tests, integration tests)
- **NFR-L1**: Hebrew support (RTL, Unicode, localized messages)

---

## FR Coverage Map

| Epic | FR Covered | User Value Delivered |
|------|------------|---------------------|
| **Epic 1: Foundation** | F3.1, F3.2, F3.3, NFR-R1, NFR-S1, NFR-M1 | Technical foundation that enables all automation features |
| **Epic 2 Phase 1: Auto-Tagging** | F2.1, F2.2, F2.3, F2.4, NFR-P1, NFR-U1, NFR-L1 | Save days of manual tagging work (HIGH VALUE) |
| **Epic 2 Phase 2: Dimensions** | F1.1, F1.2, F1.3, F1.4 | Automate 3 days of dimensioning per project (DEFERRED) |

---

## Epic 1: Foundation & Core Infrastructure

**Goal:** Establish the technical foundation for AI-driven Revit automation. Creates the C# .NET 8.0 add-in architecture, Claude API integration, preview/confirm safety pattern, and logging infrastructure that all features depend on.

**FR Coverage:** F3.1 (Plugin interface), F3.2 (LLM integration), F3.3 (Logging), NFR-R1 (Safety), NFR-S1 (Security), NFR-M1 (Quality)

**Status:** ✅ COMPLETED (2025-11-15) - All 7 stories implemented, tested in Revit 2026

---

### Story 1.1: Claude API Integration

**As a developer,**
I want to integrate the Claude API with secure key storage,
So that the extension can send prompts to the LLM and receive structured responses.

**Acceptance Criteria:**

**Given** a valid Claude API key stored in environment variable
**When** I configure the API and send a test prompt
**Then** the extension successfully calls Claude API (Sonnet 4.5) and receives JSON response

**And** the API key is stored securely (environment variable or Windows Credential Manager)
**And** HTTP requests use HTTPS with proper timeouts (10s)
**And** rate limiting and error handling are implemented
**And** API errors show user-friendly messages ("Could not connect to AI service. Check internet.")

**Prerequisites:** None (first story)

**Technical Notes:**
- Use Anthropic SDK 2.0.0 for C# .NET
- System prompt defines operation schema (auto_tag, read_elements, create_dimensions)
- Parse JSON responses into RevitAction data model
- Async/await for non-blocking LLM calls
- See: `RevitAI.CSharp/Services/ClaudeService.cs`

**Status:** ✅ Complete

---

### Story 1.2: Safety Validation Framework

**As a developer,**
I want to implement an operation allowlist and scope validator,
So that AI operations are constrained to safe, approved actions only.

**Acceptance Criteria:**

**Given** a parsed RevitAction from Claude API
**When** the action is validated
**Then** only allowlisted operations pass (auto_tag, read_elements, create_dimensions)

**And** destructive operations are blocked (delete_elements, modify_walls, etc.)
**And** scope limits are enforced (max 500 elements per operation, configurable)
**And** validation errors provide clear explanations ("Operation 'delete_walls' not permitted")
**And** validation results are structured (IsValid, ErrorMessage, WarningMessage)

**Prerequisites:** Story 1.1 (Claude API integration)

**Technical Notes:**
- Implement `SafetyValidator.cs` with operation allowlist
- Scope validation checks element count before execution
- Return `ValidationResult` with pass/fail + messages
- See: `RevitAI.CSharp/Services/SafetyValidator.cs`

**Status:** ✅ Complete

---

### Story 1.3: ExternalEvent Pattern for Thread-Safe API Access

**As a developer,**
I want to implement the ExternalEvent pattern,
So that LLM responses can safely trigger Revit API operations from background threads.

**Acceptance Criteria:**

**Given** an async Claude API call running on background thread
**When** the LLM response arrives
**Then** Revit API operations execute on the main thread via ExternalEvent

**And** background thread for LLM calls does not block UI
**And** ExternalEvent handler queues requests and processes them atomically
**And** errors in either thread are caught and logged
**And** operations complete without "wrong thread" exceptions

**Prerequisites:** Story 1.1 (Claude integration), Story 1.2 (Safety validation)

**Technical Notes:**
- Implement `IExternalEventHandler` for Revit API access
- Background thread handles async LLM calls (non-blocking)
- Main thread executes Revit operations via `ExternalEvent.Raise()`
- Request/response queue for thread communication
- See: `RevitAI.CSharp/Services/RevitEventHandler.cs`

**Status:** ✅ Complete

---

### Story 1.4: Basic UI Scaffold (Ribbon & Dialog)

**As an architect,**
I want a simple UI to enter natural language prompts,
So that I can interact with the AI co-pilot from within Revit.

**Acceptance Criteria:**

**Given** Revit is open with RevitAI installed
**When** I navigate to the Add-ins ribbon
**Then** I see a "RevitAI" tab with "Copilot" button

**And** clicking the button opens a WPF dialog with text input field
**And** dialog displays example prompts ("Try: 'Tag all doors in current view'")
**And** dialog has Send, Cancel, and Settings buttons
**And** dialog remains non-modal (can interact with Revit while open)
**And** Hebrew RTL text input is supported

**Prerequisites:** Story 1.1 (Claude integration), Story 1.3 (ExternalEvent)

**Technical Notes:**
- Create `IExternalCommand` for ribbon button
- Implement WPF `CopilotDialog.xaml` with text input
- Support RTL for Hebrew input
- Use ExternalEvent to send prompts from dialog to handler
- See: `RevitAI.CSharp/Commands/CopilotCommand.cs`, `UI/CopilotDialog.xaml`

**Status:** ✅ Complete

---

### Story 1.5: Preview/Confirm UX Pattern

**As an architect,**
I want to preview proposed changes before they're committed,
So that I can verify AI actions before modifying my model.

**Acceptance Criteria:**

**Given** a validated RevitAction ready for execution
**When** preview is generated
**Then** proposed changes are shown in WPF preview dialog

**And** dialog displays operation summary ("Will tag 23 doors, 15 windows")
**And** dialog has Confirm (green), Cancel (gray), and Adjust (blue) buttons
**And** clicking Confirm commits Revit transaction atomically
**And** clicking Cancel discards action with no model changes
**And** preview shows counts, element types, and affected areas

**Prerequisites:** Story 1.3 (ExternalEvent), Story 1.4 (UI scaffold)

**Technical Notes:**
- Create `PreviewConfirmDialog.xaml` with operation summary
- Use WPF data binding for dynamic content
- Wrap execution in Revit `Transaction` (atomic commit/rollback)
- Show preview counts before full execution
- See: `RevitAI.CSharp/UI/PreviewConfirmDialog.xaml`

**Status:** ✅ Complete

---

### Story 1.6: Logging Infrastructure

**As a developer,**
I want comprehensive logging for all operations,
So that I can debug issues and track system usage.

**Acceptance Criteria:**

**Given** any RevitAI operation executes
**When** the operation runs
**Then** logs are written to `%APPDATA%/RevitAI/logs/revit_ai.log`

**And** logs include: timestamp, operation type, prompt, result, errors
**And** log levels are supported (DEBUG, INFO, WARNING, ERROR)
**And** rotating file handler prevents logs from growing unbounded
**And** errors include stack traces for debugging
**And** logs are structured (can be parsed for analytics)

**Prerequisites:** Story 1.1 (Claude integration), Story 1.3 (ExternalEvent)

**Technical Notes:**
- Use `Microsoft.Extensions.Logging` framework
- Rotating file handler with 10MB max size
- Structured log format: `[timestamp] [level] [operation] message`
- Log all LLM requests/responses (anonymized)
- See: `RevitAI.CSharp/Services/LoggingService.cs`

**Status:** ✅ Complete

---

### Story 1.7: Configuration System

**As a developer,**
I want a YAML-based configuration system,
So that firm-specific settings can be managed without code changes.

**Acceptance Criteria:**

**Given** RevitAI is installed
**When** the system loads configuration
**Then** settings are read from `%APPDATA%/RevitAI/settings.yaml`

**And** configuration includes: language preference, dimension offsets, safety limits
**And** missing config file creates default settings automatically
**And** invalid YAML shows user-friendly error message
**And** API key is stored separately (environment variable, not in YAML)
**And** configuration can be reloaded without restarting Revit

**Prerequisites:** Story 1.1 (Claude integration), Story 1.2 (Safety validation)

**Technical Notes:**
- Use `YamlDotNet` library for parsing
- Default config embedded as resource
- Support firm-level and user-level overrides
- Configuration schema validation
- See: `RevitAI.CSharp/Services/ConfigurationService.cs`

**Status:** ✅ Complete

---

## Epic 2: Intelligent Automation (Research-Informed)

**Goal:** Enable architects to automate tedious Revit tasks through natural language commands, prioritizing high-value, low-risk operations that demonstrate the complete User→LLM→Revit flow.

**FR Coverage:**
- **Phase 1 (Stories 2.1-2.3):** F2.1, F2.2, F2.3, F2.4, NFR-P1, NFR-U1, NFR-L1
- **Phase 2 (Stories 2.4-2.6):** F1.1, F1.2, F1.3, F1.4
- **Phase 3 (Story 2.7):** Strategic MCP compatibility enabler

**Strategic Pivot:** Research (2025-11-20) validated prioritizing auto-tagging before dimensions. Rationale:
1. **User Pain Point #1:** "Users spend days manually tagging" (Task 2: PyRevit+LLM Integration Analysis)
2. **Market Validation:** All 3 commercial products (ArchiLabs, BIMLOGIQ, DWD) prioritize tagging
3. **Risk Mitigation:** Annotation tasks have "lower blast radius" than geometric operations (Task 3: Studio Tema case - 20,000 ILS insurance)
4. **Trust Building:** Demonstrate value before requesting higher-risk permissions

**Status:** 🚧 IN PROGRESS - Phase 1 (Auto-Tagging) ready for development

---

### Story 2.1: Auto-Tagging Command Parser & Safety Validation

**As an architect,**
I want to auto-tag elements using natural language commands,
So that I can save days of manual tagging work while ensuring safe, preview-confirmed operations.

**Acceptance Criteria:**

**Given** a Hebrew or English tagging prompt (e.g., "תייג את כל הדלתות בקומה 1", "Tag all walls in current view")
**When** the prompt is sent to Claude API with Revit context
**Then** a structured action is returned with:
- Operation type: `auto_tag`
- Target elements: category (Walls/Doors/Rooms/Windows), scope (current view, level, selection)
- Tag parameters: tag type, placement strategy (leader/no leader), offset

**And** ambiguous prompts trigger clarifying questions:
- "Which tag type? [Door Tag | Door Number | Custom]"
- "Tag all doors or only untagged doors?"

**And** safety validation enforces:
- Maximum 500 elements per operation (configurable)
- Only "read + annotate" operations allowed (no geometry modification)
- Tags are metadata additions (reversible with Ctrl+Z)

**And** Hebrew and English prompts handled equally (>90% accuracy on benchmark queries)

**Prerequisites:**
- Story 1.1: Claude API integration
- Story 1.2: Safety validation framework
- Story 1.4: Basic UI scaffold
- Story 1.7: Configuration system

**Technical Notes:**
- Enhance Claude system prompt with auto_tag operation schema
- Revit context: current view, available levels, element counts (tagged/untagged), available tag types
- Parse JSON response into `RevitAction` with auto_tag operation
- Safety validator checks: operation allowlist, scope limits (<500 elements), tag type exists
- Hebrew tokenization validation (Claude Sonnet 4.5 native Unicode support)
- **Testing:** Unit tests (Layer 1 pure logic), Integration tests (Layer 2 mocked Revit API)
- See: `docs/epic2-refactored.md` Story 2.1 for complete implementation details

**Definition of Done:**
- [ ] Claude parses Hebrew and English tagging prompts accurately (>90% on 30-query subset)
- [ ] Safety validator enforces scope limits and operation allowlist
- [ ] Unit tests pass (parsing + validation logic) with 80%+ Layer 1 coverage
- [ ] Integration tests pass with mocked Revit API
- [ ] Code reviewed and merged to main branch
- [ ] Documentation updated (architecture.md, CLAUDE.md, testing-framework.md)

**Status:** 📋 READY FOR DEV (Story marked ready-for-dev in sprint-status.yaml)

---

### Story 2.2: Tag Placement Engine with Spatial Intelligence

**As a developer,**
I want to implement intelligent tag placement logic,
So that tags don't overlap and follow professional standards.

**Acceptance Criteria:**

**Given** a list of target elements (e.g., 24 doors) from parsed scope
**When** tag placement is calculated
**Then** each tag is positioned:
- At element center (default) or user-specified offset
- With collision avoidance (95%+ collision-free placement after 10 attempts)
- With appropriate leader line (if element is small or obscured)
- Respecting firm standards (offset distance, orientation)

**And** placement algorithm handles:
- Elements in different views (plan, elevation, section)
- Rotated elements (angled walls, diagonal doors)
- Elements with existing tags (skip or replace based on user preference)

**And** placement failures are logged:
- "Could not place tag for Door ID 12345: No valid placement found"
- Partial success supported: "Tagged 22 of 24 doors (2 failed)"

**And** collision detection uses bounding box geometry (accurate to 10mm)

**Prerequisites:**
- Story 2.1: Auto-tagging parser and validation
- Story 1.3: ExternalEvent pattern (thread-safe Revit API access)

**Technical Notes:**
- Implement `TagPlacementService.cs` with placement strategies
- `IPlacementStrategy` interface: `GetPreferredPlacement()`, `GetAlternativePlacement()`
- Default strategy: CenterOffsetStrategy (element center + offset)
- Collision detection: `ITagCollisionDetector` using bounding box overlap
- 10-attempt algorithm: try 8 directions (0°, 45°, 90°, 135°, 180°, 225°, 270°, 315°) + increase distance
- Leader line enabled if placed >50% element width from center
- **Testing:** Unit tests for placement logic (no Revit API), Integration tests for geometry calculations
- See: `docs/epic2-refactored.md` Story 2.2 for complete implementation with code snippets

**Definition of Done:**
- [ ] Placement algorithm achieves 95%+ collision-free rate on test dataset (45 style test project from nelly)
- [ ] Unit tests pass (placement strategies, collision detection) with 80%+ Layer 1 coverage
- [ ] Integration tests validate geometry calculations with mocked Revit elements
- [ ] Edge cases handled (rotated elements, small elements, existing tags)
- [ ] Code reviewed and merged to main branch
- [ ] Performance validated (<2s for 100 elements)

**Status:** 📝 DRAFTED (Story documented in epic2-refactored.md)

---

### Story 2.3: Auto-Tagging Execution with Preview & Audit Trail

**As an architect,**
I want to preview and confirm tag placement before committing,
So that I can verify AI decisions and maintain full control over my model.

**Acceptance Criteria:**

**Given** tag placements calculated by placement engine
**When** preview is generated
**Then** WPF preview dialog shows:
- Operation summary ("Will tag 23 doors, 15 windows, 2 failed")
- Visual preview of tag positions in Revit (highlight overlay)
- List of elements to be tagged (expandable tree view)

**And** clicking Confirm:
- Wraps operation in Revit Transaction (atomic commit/rollback)
- Creates tags using calculated placements
- Logs operation to audit trail: timestamp, prompt, element count, result
- Shows success message ("Tagged 38 elements. Use Ctrl+Z to undo.")

**And** clicking Cancel:
- Discards all placements with no model changes
- Returns to copilot dialog

**And** audit trail includes:
- Full operation history (CSV format): timestamp, user, prompt, operation, element_count, success/failure
- Accessible via "View Audit Trail" button in Settings dialog

**Prerequisites:**
- Story 2.2: Tag placement engine
- Story 1.5: Preview/Confirm UX pattern
- Story 1.6: Logging infrastructure

**Technical Notes:**
- Extend `PreviewConfirmDialog.xaml` with tag-specific preview
- Visual preview: Use Revit `DirectShape` temporary graphics (yellow highlight for tags)
- Transaction wrapper: `using (Transaction t = new Transaction(doc, "AI: Auto-Tag Elements"))`
- Audit trail CSV: `%APPDATA%/RevitAI/audit/operations.csv`
- Include undo instructions in success message
- **Testing:** E2E acceptance tests (requires Revit), transaction safety tests (rollback on error)
- See: `docs/epic2-refactored.md` Story 2.3 for complete implementation

**Definition of Done:**
- [ ] Preview dialog shows accurate tag placement preview
- [ ] Transaction commits atomically (all tags or none)
- [ ] Transaction rollback works correctly on error/cancel
- [ ] Audit trail CSV captures all operations
- [ ] E2E acceptance tests pass on real Revit project
- [ ] Undo (Ctrl+Z) works as expected
- [ ] Code reviewed and merged to main branch
- [ ] User documentation created (with screenshots)

**Status:** 📝 DRAFTED (Story documented in epic2-refactored.md)

---

### Story 2.4: Dimension Command Parser (NLU for Dimensions)

**As an architect,**
I want to parse natural language dimensioning prompts,
So that I can specify dimension operations conversationally.

**Acceptance Criteria:**

**Given** a Hebrew or English dimension prompt (e.g., "הוסף מידות פנימיות לכל החדרים", "Add internal dimensions to all rooms")
**When** the prompt is sent to Claude API with Revit context
**Then** a structured action is returned with:
- Operation type: `create_dimensions`
- Target elements: category (Rooms), scope (current view, level, all)
- Dimension parameters: style (continuous/chain), offset (200mm default), orientation (horizontal/vertical/both)

**And** ambiguous prompts trigger clarifying questions

**Prerequisites:**
- Story 1.1: Claude API integration
- Story 1.2: Safety validation framework

**Technical Notes:**
- Layer 1 pure logic (parsing, validation) - COMPLETE from discovery phase on nelly
- Layer 2 Revit API integration - DEFERRED to Phase 2
- See: `docs/session-2025-11-20-discovery-phase.md` for discovery findings

**Status:** ✅ Layer 1 COMPLETE (from nelly discovery), 🔜 Layer 2 DEFERRED to Phase 2

---

### Story 2.5: Room Boundary Detection & Dimension Chain Generation

**As a developer,**
I want to detect room boundaries and generate dimension chains,
So that dimensions are placed accurately across wall segments.

**Acceptance Criteria:**

**Given** a room element from parsed scope
**When** boundary analysis runs
**Then** room boundary segments are identified (walls, openings, corners)

**And** dimension chains are generated:
- Continuous dimensions across boundary segments
- Proper offset from walls (200mm default, configurable)
- Alignment to architectural grid lines (if available)

**Prerequisites:**
- Story 2.4: Dimension parser
- Story 1.3: ExternalEvent pattern

**Technical Notes:**
- Layer 1 pure geometry logic (boundary detection, placement algorithms) - COMPLETE from discovery
- Layer 2 Revit API integration - DEFERRED to Phase 2
- Discovery data: 4 test rooms, 45 dimension styles cataloged on nelly machine
- See: `docs/session-2025-11-20-discovery-phase.md` for test constants

**Status:** ✅ Layer 1 COMPLETE (from nelly discovery), 🔜 Layer 2 DEFERRED to Phase 2

---

### Story 2.6: Dimension Preview & Hybrid LLM+Solver Approach

**As an architect,**
I want to preview dimension placements before committing,
So that I can verify accuracy and adjust if needed.

**Acceptance Criteria:**

**Given** dimension chains generated for target rooms
**When** preview is displayed
**Then** proposed dimensions are shown in yellow highlight overlay

**And** preview dialog shows summary and allows confirm/cancel
**And** hybrid approach: LLM parses intent, geometric solver calculates precise placement

**Prerequisites:**
- Story 2.5: Dimension generation logic
- Story 1.5: Preview/Confirm UX pattern

**Technical Notes:**
- **Research Insight:** Use hybrid architecture (Task 1: MCP findings)
  - LLM: Parse user intent, understand "internal dimensions", "all rooms", etc.
  - Solver: Calculate exact dimension placement using geometry algorithms
  - Benefit: LLM flexibility + solver precision
- Layer 3 E2E acceptance tests - DEFERRED to Phase 2
- See: `docs/epic2-refactored.md` for hybrid architecture details

**Status:** 📋 BACKLOG (Not started, awaits Phase 2)

---

### Story 2.7: MCP Compatibility Layer

**As a developer,**
I want to expose RevitAI operations via MCP JSON-RPC interface,
So that the system integrates with the emerging MCP ecosystem.

**Acceptance Criteria:**

**Given** RevitAI add-in is running
**When** MCP client connects to `http://localhost:48884`
**Then** MCP JSON-RPC server responds with available tools

**And** MCP tools registry includes:
- `get_revit_model_info`: Returns project metadata
- `list_levels`: Returns all levels and elevations
- `auto_tag_elements`: Creates tags for specified elements
- `create_dimensions`: Generates dimension chains

**And** MCP requests are validated and executed via existing services
**And** MCP responses follow JSON-RPC 2.0 specification
**And** errors are returned in standardized MCP error format

**Prerequisites:**
- Story 2.3: Auto-tagging execution (operations to expose)
- Story 2.6: Dimension preview (operations to expose)

**Technical Notes:**
- **Research Context:** MCP is emerging standard (49 GitHub stars for revit-mcp-python, Task 1)
- Positions RevitAI as "MCP-native" for future ecosystem integration
- Implements after tagging + dimensions to expose completed operations
- Wrap existing `ClaudeService` and `RevitEventHandler` with MCP JSON-RPC interface
- HTTP server on localhost:48884 (non-privileged port)
- See: `docs/architecture.md` ADR-009 for MCP architecture

**Definition of Done:**
- [ ] MCP server runs on localhost:48884
- [ ] Tool registry returns all available operations
- [ ] MCP requests execute operations successfully
- [ ] Error handling follows MCP specification
- [ ] Integration tests validate MCP protocol compliance
- [ ] Documentation includes MCP client examples

**Status:** 📋 BACKLOG (Strategic enabler, implements after Phase 1+2 complete)

---

## FR Coverage Matrix

| Functional Requirement | Epic | Story | User Value |
|------------------------|------|-------|-----------|
| F2.1: Element Selection (NLU) | Epic 2 | 2.1 | Parse "tag all doors" prompts |
| F2.2: Tag Placement (Spatial) | Epic 2 | 2.2 | Intelligent collision-free positioning |
| F2.3: Tag Preview & Confirm | Epic 2 | 2.3 | Safety + control before committing |
| F2.4: Tag Configuration | Epic 2 | 2.1, 2.3 | Custom offsets, tag families, preferences |
| F1.1: Dimension NL Input | Epic 2 | 2.4 | Parse "add dimensions" prompts |
| F1.2: Dimension Generation | Epic 2 | 2.5 | Room boundary analysis, continuous chains |
| F1.3: Dimension Preview | Epic 2 | 2.6 | Visual preview before commit |
| F1.4: Safety Constraints | Epic 2 | 2.4 | Scope limits, no deletion, atomic transactions |
| F3.1: Plugin Interface | Epic 1 | 1.4 | Ribbon, dialog, ExternalEvent |
| F3.2: LLM Integration | Epic 1 | 1.1 | Claude API, structured context, error handling |
| F3.3: Logging | Epic 1 | 1.6 | Operation logs, diagnostics, performance tracking |
| NFR-P1: Performance | All | All | <5s LLM, <30s execution |
| NFR-R1: Data Safety | Epic 1 | 1.2, 1.5 | Transactions, allowlist, validation |
| NFR-U1: Usability | Epic 1 | 1.4, 1.5 | <5min training, conversational feedback |
| NFR-S1: Security | Epic 1 | 1.1, 1.7 | Secure API keys, HTTPS, anonymization |
| NFR-M1: Maintainability | Epic 1 | All | Type hints, unit tests, SIL architecture |
| NFR-L1: Hebrew Support | Epic 1, 2 | 1.4, 2.1 | RTL input, Unicode, localized messages |

**Coverage Validation:** ✅ All FRs from PRD mapped to stories. No orphaned requirements.

---

## Summary

**Epic 1 Status:** ✅ COMPLETED (2025-11-15)
- 7 stories implemented
- C# .NET 8.0 migration from pyRevit (ADR update)
- Revit 2026 compatibility validated
- 15+ C# classes (~1500 lines)
- Build deployed, tested successfully

**Epic 2 Status:** 🚧 IN PROGRESS
- **Phase 1 (Auto-Tagging):** Stories 2.1-2.3 ready for development (PRIORITY)
- **Phase 2 (Dimensions):** Stories 2.4-2.6 deferred (Layer 1 complete from nelly discovery)
- **Phase 3 (MCP):** Story 2.7 backlog (strategic enabler)

**Research Impact:**
- Task 1 (MCP Landscape): Validated MCP compatibility as strategic differentiator
- Task 2 (PyRevit+LLM): Identified auto-tagging as #1 user pain point, market-validated priority
- Task 3 (Testing Strategies): SIL architecture enables 80% unit test coverage (Layer 1), 85-query benchmark targeting 80% accuracy

**Next Action:** Begin Story 2.1 (Auto-Tag Parser) using `/bmad:bmm:workflows:story-context` and `/bmad:bmm:workflows:dev-story`

---

_For implementation: Use the `story-context` workflow to assemble dynamic context, then `dev-story` workflow to execute stories from this epic breakdown._

_This document incorporates context from PRD, Architecture, Product Brief, and Research Findings (2025-11-20)._
