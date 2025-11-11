# revit-ai - Epic Breakdown

**Author:** Doc
**Date:** 2025-11-09
**Project Level:** 2
**Target Scale:** Small studio (10 users, single firm)

---

## Overview

This document provides the complete epic and story breakdown for revit-ai, decomposing the requirements from the [PRD](./PRD.md) into implementable stories.

### Epic Structure Overview

This PoC is organized into **2 epics** that establish the foundation and validate with one high-value feature:

**Epic 1: Foundation & Core Infrastructure**
Establish the technical foundation for AI-driven Revit automation. Creates the essential pyRevit plugin architecture, LLM integration, and safety mechanisms that enable all AI co-pilot features.

**Epic 2: Intelligent Dimension Automation**
Enable architects to dimension floor plans through natural language commands. Validates the complete User→LLM→pyRevit→Revit flow with a high-value feature that saves 3 days of manual work per project.

**Sequencing:** Foundation → Dimensions. Each epic builds on previous work and is independently valuable.

**PoC Success:** After Epic 2, we validate the technical foundation with a real-world feature. Based on results, we reassess and decide next steps (tagging, other features, or pivot).

**Deferred for Post-PoC Evaluation:**
- Smart Element Tagging
- Schedule Generation
- View/Sheet Management
- Additional automation features

---

## Epic 1: Foundation & Core Infrastructure

**Goal:** Establish the technical foundation for AI-driven Revit automation. Creates the pyRevit plugin architecture, Claude API integration, preview/confirm safety pattern, and logging infrastructure that all features depend on.

### Story 1.1: Project Setup & pyRevit Extension Scaffold

As a developer,
I want to set up the project repository and create a basic pyRevit extension,
So that I can load and run Python code within Revit.

**Acceptance Criteria:**

**Given** a clean development environment with Revit installed
**When** I set up the project structure and create a minimal pyRevit extension
**Then** the extension loads in Revit and displays a "Hello World" button in the ribbon

**And** clicking the button shows a message dialog confirming Python execution
**And** the repository has proper .gitignore for Python and Revit files
**And** README documents installation steps for the extension

**Prerequisites:** None (first story)

**Technical Notes:**
- Create folder structure: `{project-root}/.extensions/RevitAI.extension/RevitAI.tab/`
- Implement basic `script.py` with TaskDialog
- Use pyRevit naming conventions for auto-discovery
- Document pyRevit version requirements (4.8+)
- Test on Revit 2022/2023

---

### Story 1.2: Claude API Integration & Secure Key Management

As a developer,
I want to integrate the Claude API with secure key storage,
So that the extension can send prompts to the LLM and receive structured responses.

**Acceptance Criteria:**

**Given** a valid Claude API key
**When** I configure the API key securely and send a test prompt
**Then** the extension successfully calls Claude API and receives a response

**And** the API key is stored encrypted (not plain text)
**And** the extension warns users if API key is missing/invalid
**And** HTTP requests use HTTPS and proper timeouts (10s)
**And** rate limiting and error handling are implemented

**Prerequisites:** Story 1.1 (basic extension structure)

**Technical Notes:**
- Use `anthropic` Python SDK or `requests` library
- Store API key in Windows Credential Manager or encrypted config file
- Implement retry logic for transient failures
- Log API calls (anonymized) for debugging
- Handle network errors gracefully with user-friendly messages

---

### Story 1.3: ExternalEvent Pattern for Thread-Safe Revit API Access

As a developer,
I want to implement the ExternalEvent pattern,
So that LLM responses can safely trigger Revit API operations from background threads.

**Acceptance Criteria:**

**Given** the Claude API integration is working
**When** an LLM response requests a Revit API operation
**Then** the operation executes via ExternalEvent on the main Revit thread

**And** the background thread waits for the operation to complete
**And** results or errors are returned to the caller
**And** no "API access from wrong thread" exceptions occur

**Prerequisites:** Story 1.2 (Claude API integration)

**Technical Notes:**
- Implement `IExternalEventHandler` in C# or use pyRevit's threading helpers
- Create request/response queue for async communication
- Wrap all Revit API calls in ExternalEvent.Raise()
- Test with simple operations (e.g., get all rooms, create text note)
- Document threading model for future developers

---

### Story 1.4: Operation Allowlist & Safety Validation Framework

As an architect,
I want the system to prevent dangerous operations,
So that AI automation cannot corrupt my project files.

**Acceptance Criteria:**

**Given** the ExternalEvent pattern is implemented
**When** an LLM response requests an operation
**Then** the system validates it against an allowlist before execution

**And** only safe operations are permitted (create dimensions/tags, read elements)
**And** destructive operations are blocked (delete, modify model elements)
**And** blocked operations show clear error messages to the user
**And** all validation failures are logged for audit

**Prerequisites:** Story 1.3 (ExternalEvent pattern)

**Technical Notes:**
- Define allowlist: CREATE_DIMENSION, CREATE_TAG, READ_ELEMENT_PROPERTIES
- Implement validation pipeline with SecurityError exceptions
- Add scope validation (max element count, current view only)
- Document safety mechanisms for user trust
- Include examples of blocked operations in tests

---

### Story 1.5: Preview/Confirm UX Pattern

As an architect,
I want to see proposed changes before they happen,
So that I can verify the AI understood my intent correctly.

**Acceptance Criteria:**

**Given** an operation has been validated by the allowlist
**When** the system generates preview graphics
**Then** proposed changes are displayed in a highlight color (yellow for dimensions)

**And** a confirmation dialog shows a summary (e.g., "Adding 47 dimension chains")
**And** I can confirm or cancel before the transaction commits
**And** confirmed operations are committed atomically (all or nothing)
**And** I can undo operations with Ctrl+Z after confirmation

**Prerequisites:** Story 1.4 (safety validation)

**Technical Notes:**
- Use Revit DirectContext3D or temporary elements for preview graphics
- Implement confirmation dialog with [Confirm] [Cancel] buttons
- Wrap execution in Revit Transaction for atomic commit
- Support undo via transaction history
- Clear preview graphics after user decision

---

### Story 1.6: Logging & Diagnostics Infrastructure

As a developer,
I want comprehensive logging of all operations,
So that I can debug issues and track system behavior.

**Acceptance Criteria:**

**Given** the system is running operations
**When** any operation executes (success or failure)
**Then** it is logged with timestamp, user prompt, action, and result

**And** errors include full stack traces
**And** log files are written to a predictable location
**And** sensitive data (API keys, proprietary project info) is not logged
**And** verbose mode can be enabled for detailed debugging

**Prerequisites:** Story 1.3 (ExternalEvent pattern)

**Technical Notes:**
- Use Python `logging` module with rotating file handler
- Log location: `%APPDATA%/pyRevit/RevitAI/logs/`
- Include: timestamp, operation type, LLM latency, Revit API execution time
- Implement log level configuration (INFO, DEBUG)
- Add operation replay capability from logs (future debugging)

---

### Story 1.7: Basic Ribbon UI with Text Input Dialog

As an architect,
I want a simple UI to enter natural language commands,
So that I can interact with the AI co-pilot.

**Acceptance Criteria:**

**Given** the extension is loaded in Revit
**When** I click the "AI Co-pilot" ribbon button
**Then** a dialog opens with a text input field and example prompts

**And** the dialog supports Hebrew RTL text input
**And** example prompts are shown in both Hebrew and English
**And** I can submit prompts via Enter key or Submit button
**And** the dialog shows feedback while processing (spinner/progress indicator)

**Prerequisites:** Story 1.1 (basic extension), Story 1.5 (preview/confirm pattern)

**Technical Notes:**
- Create WPF dialog with RTL support via FlowDirection="RightToLeft"
- Example prompts: "תוסיף מידות פנימיות לכל החדרים בקומה 1"
- Show LLM interpretation before preview: "I understand you want to dimension all Level 1 rooms..."
- Use async/await to prevent UI freezing during LLM calls
- Dockable panel preferred over modal dialog (future enhancement)

---

## Epic 2: Intelligent Dimension Automation

**Goal:** Enable architects to dimension floor plans through natural language commands. Validates the complete User→LLM→pyRevit→Revit flow with a high-value feature that saves 3 days of manual work per project.

### Story 2.1: Dimension Command Parser (NLU for Dimensions)

As a developer,
I want to parse natural language dimension prompts into structured actions,
So that the system understands the user's intent and scope.

**Acceptance Criteria:**

**Given** a Hebrew or English dimension prompt
**When** the prompt is sent to Claude API with Revit context
**Then** a structured action is returned with operation type, targets, and parameters

**And** the parser identifies scope (all rooms, selected rooms, specific level, current view)
**And** ambiguous prompts trigger clarifying questions
**And** the LLM response follows a defined schema (JSON or structured text)
**And** Hebrew prompts are handled with same accuracy as English

**Prerequisites:** Story 1.2 (Claude API integration), Story 1.7 (UI for prompts)

**Technical Notes:**
- Design prompt template with context: available levels, current view, element types
- Define action schema: `{operation: "create_dimensions", targets: {...}, parameters: {...}}`
- Include firm standards in context (dimension offset, style preferences)
- Test with examples: "תוסיף מידות פנימיות לכל החדרים בקומה 1", "Dimension selected rooms"
- Handle edge cases: no rooms found, invalid level name

---

### Story 2.2: Room Boundary Detection & Wall Analysis

As a developer,
I want to extract room boundaries and analyze wall geometry,
So that I can calculate where to place dimension chains.

**Acceptance Criteria:**

**Given** a list of rooms from the parsed scope
**When** I analyze room boundaries
**Then** wall segments are extracted with start/end points and orientation

**And** corners, openings (doors/windows), and wall junctions are identified
**And** curved walls are detected and handled appropriately
**And** angled walls (non-orthogonal) are processed correctly

**Prerequisites:** Story 2.1 (dimension parser), Story 1.3 (Revit API access)

**Technical Notes:**
- Use `Room.GetBoundarySegments()` to extract wall curves
- Calculate wall normals for dimension offset direction
- Filter out room separators vs. actual walls
- Handle multi-segment boundaries (L-shaped rooms, etc.)
- Store geometry in data structure for dimension generation

---

### Story 2.3: Continuous Dimension Chain Generation

As an architect,
I want the system to create continuous dimension chains across room boundaries,
So that I get professionally formatted dimensions without manual placement.

**Acceptance Criteria:**

**Given** room boundaries have been analyzed
**When** I request dimension generation
**Then** continuous dimension chains are created along each wall

**And** dimensions use the firm's default dimension style
**And** dimension offset is applied (200mm from wall, or firm standard)
**And** dimensions are properly aligned and spaced
**And** the Revit Transaction commits all dimensions atomically

**Prerequisites:** Story 2.2 (room boundary analysis), Story 1.4 (safety validation)

**Technical Notes:**
- Use `Document.Create.NewDimension()` API
- Create `ReferenceArray` from wall references
- Apply firm's dimension type (from config or project template)
- Handle dimension creation failures gracefully
- Test with simple rectangular room first, then complex shapes

---

### Story 2.4: Dimension Preview & Confirmation Workflow

As an architect,
I want to preview all proposed dimensions before they're created,
So that I can verify the AI's placement decisions.

**Acceptance Criteria:**

**Given** dimension chains have been generated (in memory, not committed)
**When** the preview is displayed
**Then** proposed dimensions are shown in yellow/amber highlight

**And** a summary shows: "Preview: 47 dimension chains will be added to 12 rooms"
**And** I can confirm to commit or cancel to abort
**And** confirmed dimensions are created in a single transaction
**And** I can undo with Ctrl+Z after confirmation

**Prerequisites:** Story 2.3 (dimension generation), Story 1.5 (preview/confirm pattern)

**Technical Notes:**
- Generate temporary dimension graphics using DirectContext3D
- Integrate with preview/confirm dialog from Story 1.5
- Show count of dimension chains and affected rooms
- Clear preview graphics after user decision
- Test end-to-end flow: prompt → parse → analyze → generate → preview → confirm → commit

---

### Story 2.5: Edge Case Handling (Curved Walls, Angled Walls, Complex Geometry)

As an architect,
I want the system to handle complex room geometries,
So that it works on real projects, not just simple rectangular rooms.

**Acceptance Criteria:**

**Given** rooms with curved walls, angled walls, or complex shapes
**When** I request dimensions
**Then** the system handles these cases gracefully

**And** curved walls are either dimensioned with arc dimensions or skipped with warning
**And** angled walls are dimensioned along their orientation
**And** degenerate cases (invalid geometry) are detected and reported
**And** partial success is supported: "Dimensioned 9 of 12 rooms (3 skipped due to curved walls)"

**Prerequisites:** Story 2.4 (dimension preview workflow)

**Technical Notes:**
- Detect curved walls via `Curve.IsBound` and `Arc` type checking
- Implement arc dimension creation or warning message
- Handle non-orthogonal walls by calculating dimension line orientation
- Show detailed preview with problematic rooms highlighted differently
- Provide "Show Details" option to explain why rooms were skipped

---

---

_For implementation: Use the `create-story` workflow to generate individual story implementation plans from this epic breakdown._
