# revit-ai - Product Requirements Document

**Author:** Doc
**Date:** 2025-11-07
**Version:** 1.0

---

## Executive Summary

Revit-ai is a **natural language AI co-pilot for Revit** - enabling architects to work in Revit the way developers work with AI coding assistants like Cursor or Windsurf. Instead of hunting through menus and clicking through dialogs, architects command Revit in plain English (and Hebrew) using natural language prompts that the AI translates into precise Revit operations.

**The Vision**: "Vibe coding" for architecture - where you describe what you want and the AI handles the mechanical execution, all while keeping you in full control with preview-before-commit safety.

**This PoC**: A focused technical validation to prove the end-to-end flow works - from natural language prompt → LLM understanding → pyRevit execution → Revit model changes - using deliberately simple features (dimensions, tagging) as tractable test cases to validate the architecture before expanding capabilities.

### What Makes This Special

**The "Aha Moment"**: When an architect types "Add internal dimensions to all rooms" and watches 3 days of tedious manual work complete in 30 seconds - with full preview and the ability to undo - they realize AI automation isn't just faster scripts, it's intelligent assistance that understands their intent and executes with professional judgment.

Unlike rigid, rule-based Revit plugins, revit-ai uses LLM intelligence to make contextual decisions (dimension placement, tag positioning, schedule formatting) the way an experienced architect would - adapting to spatial context, firm standards, and professional conventions without being explicitly programmed for every scenario.

---

## Project Classification

**Technical Type:** Desktop Plugin/Add-in (pyRevit-based Revit integration)
**Domain:** Architecture/AEC (Architecture, Engineering, Construction)
**Complexity:** Medium-High

This is a **domain-critical** system where mistakes are costly. Perry's firm has:
- 2-3 week model building cycles
- 4-month construction documentation phases
- Zero tolerance for errors that corrupt project files

The complexity comes not from technical architecture but from:
1. **Domain expertise required**: Understanding architectural conventions, standards, spatial relationships
2. **Safety requirements**: Preview/confirm pattern, allowlist of safe operations, scope limits
3. **Quality standards**: Output must meet professional standards architects would produce manually
4. **Firm-specific adaptation**: Each office has unique naming conventions, templates, standards

### Domain Context

**AEC Industry Characteristics:**
- Highly regulated (building codes, safety standards)
- Long project lifecycles (months to years)
- Collaborative (multiple disciplines, stakeholders)
- Quality-critical (construction errors are expensive/dangerous)
- Conservative adoption (proven tools preferred over cutting-edge)

**Perry's Firm Context:**
- Small studio (10 people, 3-4 full Revit licenses)
- Mix of Revit skill levels (25-80% feature utilization)
- Hebrew language primary (English secondary)
- **PoC Validation Path**: Testing with dimensions and tagging because they're tractable, well-defined features that exercise the full user→LLM→pyRevit→Revit flow without introducing excessive complexity

**Project Timeline Context:**
- Need working demo by **Friday** (Nov 10) to maintain momentum
- Perry wants to see "something work" - proof of concept
- Full PoC validation: 2-4 weeks
- Production decision: After PoC results

---

## Product Scope

### The Full Vision (Future State)

A **comprehensive AI co-pilot for Revit** that handles the full spectrum of architectural work through natural language:

- **Design exploration**: "Try this room with a vaulted ceiling"
- **View management**: "Create all necessary CD sheets for this floor"
- **Standards compliance**: "Check all walls for fire rating requirements"
- **Documentation**: "Generate complete door schedule with hardware specs"
- **Quality control**: "Find all missing dimensions and tag placement issues"
- **Learning assistant**: "How do I create a custom wall type?" (Hebrew support)

Like Cursor/Windsurf for coding, but for architecture - the AI becomes an intelligent assistant that understands architectural intent and executes mechanical tasks while keeping the architect in creative control.

### MVP - Proof of Concept Scope

**PoC Goal**: Validate the technical foundation with minimal scope to prove end-to-end feasibility.

**Validation Features** (deliberately simple to test architecture):
1. **Internal Dimensions** - Placing continuous dimensions across rooms
   - *Why chosen*: Well-defined geometric task, clear success criteria, exercises spatial understanding
   - *Validates*: Revit API dimension creation, geometric analysis, preview/confirm UX

2. **Auto-Tagging** - Positioning element tags by firm standards
   - *Why chosen*: Tests parameter access, element selection, placement logic
   - *Validates*: Revit data model understanding, style application, user preferences

**Explicitly Out of Scope for PoC:**
- Schedule generation (was in original brief, but dimensions/tagging sufficient for validation)
- Model editing (wall/door creation, modifications)
- Multi-step workflows
- Complex natural language understanding
- Firm-specific template library
- Production UI/UX polish

**PoC Success = End-to-End Flow Works:**
```
User prompt (Hebrew/English)
  ↓
LLM parses intent
  ↓
pyRevit executes safe operations
  ↓
Preview shows changes
  ↓
User confirms
  ↓
Revit Transaction commits
```

### Post-PoC Growth Path (If Validated)

**Phase 2 - Expand Capabilities:**
- View/sheet management
- Schedule automation
- QA/standards checking
- Multi-step workflows

**Phase 3 - Intelligence Layer:**
- Firm-specific learning (templates, conventions)
- Contextual suggestions
- Error prevention
- Workflow optimization

**Phase 4 - Full Co-pilot:**
- Natural design exploration
- Complete documentation automation
- Real-time collaboration support
- Continuous learning from firm work

---

## Risks & Constraints

### Critical Risk: Scope Creep/Drift

**THE BIGGEST DANGER**: Trying to do too much too fast, introducing complexity that causes us to lose control and miss the fundamental goal of touching end-to-end.

**Symptoms to Watch For:**
- Adding "just one more feature" before validating current one
- Gold-plating the UI before core flow works
- Over-engineering the LLM prompting before basic parsing works
- Building configurability before proving the concept
- Premature optimization or abstraction

**Mitigation Strategy:**
- **Iron triangle for PoC**: Dimensions + Tagging + Preview/Confirm pattern. Nothing else until these work on real project.
- **Friday demo forcing function**: Must show SOMETHING working by Nov 10
- **EBD checkpoints**: After each feature, evaluate: Does this prove the concept? Can we expand or do we pivot?
- **"Does this touch end-to-end?"** test: If it doesn't exercise the full user→LLM→pyRevit→Revit flow, defer it

### Other Risks

**Technical Risks:**
- Revit API limitations (can we create dimensions programmatically?)
- pyRevit stability (iteration speed, debugging)
- LLM accuracy (does Claude understand architectural context?)
- Performance (Hebrew tokenization, API latency)

**Product Risks:**
- Perry doesn't see value (PoC doesn't impress)
- Safety concerns prevent adoption (preview/confirm not enough trust)
- Firm standards too complex to capture (every project is different)

**Execution Risks:**
- Revit installation/setup delays (need to install today)
- Multi-tasking context switching (need good tracking)
- Perfectionism over shipping (analysis paralysis)

---

## Success Criteria

### PoC Success = Technical Validation

**Primary Goal:** Prove LLM + Revit API can automate dimensions with professional quality

**Success means:**
1. **Works on real project**: Can dimension actual floor plan from Perry's firm (not demo/mock data)
2. **LLM adds value**: AI makes intelligent decisions beyond simple rule execution (handles spacing, alignment, continuous dimensions)
3. **Professional quality**: Perry (experienced architect) says "this is good enough to use" or "promising, needs refinement"
4. **Identifies challenges**: We learn what's hard, what's possible, what needs work

**Decision Criteria:**
- ✅ **CONTINUE**: Dimensions work, Perry sees value, architecture is sound → proceed to tagging
- 🔄 **REFINE**: Works but needs improvement → iterate on prompts/algorithm
- ❌ **PIVOT**: Fundamental blockers (API limits, accuracy issues, performance) → reassess approach

### Business Success Criteria (If PoC validates)

**For Perry's Firm:**
- **Time savings**: 3-day dimension task → 30 minutes (95% reduction)
- **Quality improvement**: Consistent formatting, no "floating" dimensions
- **Adoption**: Staff with 25% Revit utilization can access 80% through prompts
- **ROI**: License cost (~100K ₪/year) justified by productivity gains

**For Product (Future):**
- Hebrew language support validated
- pyRevit architecture proves viable for distribution
- Preview/confirm UX builds trust with architects
- Firm-specific customization model validated

---

## Functional Requirements

### PoC Feature 1: Internal Dimensions

**User Story:** As an architect, I want to automatically place continuous internal dimensions across room boundaries so that I can save 3 days of manual dimensioning work per project.

**Requirements:**

**F1.1 - Natural Language Input**
- System SHALL accept Hebrew and English text prompts
- System SHALL parse prompts like: "Add internal dimensions to all rooms on Level 1"
- System SHALL identify target scope (all rooms, selected rooms, current view, specific level)

**F1.2 - Dimension Generation**
- System SHALL create continuous dimension chains across room boundaries
- System SHALL align dimensions to walls and structural elements
- System SHALL apply standard dimension offsets (e.g., 200mm from wall)
- System SHALL use firm's default dimension style/type
- System SHALL handle corner cases: angled walls, curved walls, openings

**F1.3 - Preview & Confirmation**
- System SHALL generate preview showing all proposed dimensions in yellow/highlight
- System SHALL display summary: "Adding 47 dimension chains across 12 rooms"
- System SHALL allow user to confirm or cancel before committing
- User SHALL be able to undo operation with standard Ctrl+Z

**F1.4 - Safety Constraints**
- System SHALL NOT modify existing dimensions
- System SHALL NOT delete or move model elements
- System SHALL operate only in current view or user-specified scope
- System SHALL fail gracefully if Revit API limits are reached

### PoC Feature 2: Auto-Tagging

**User Story:** As an architect, I want to automatically tag elements according to firm standards so that I don't spend hours manually positioning tags.

**Requirements:**

**F2.1 - Element Selection**
- System SHALL accept prompts like: "Tag all doors on this floor"
- System SHALL support element types: doors, windows, rooms, walls, equipment
- System SHALL filter by category, type, level, or current view
- System SHALL respect user selection if elements are pre-selected

**F2.2 - Tag Placement**
- System SHALL position tags using intelligent placement algorithm:
  - Doors: offset to hinge side, avoid overlap with dimension
  - Windows: centered above or below, avoid dimension conflicts
  - Rooms: centered in room geometry
- System SHALL use firm's default tag family for each element type
- System SHALL avoid overlapping tags (collision detection)
- System SHALL align tags where possible (horizontal/vertical alignment)

**F2.3 - Preview & Confirmation**
- System SHALL show preview with all proposed tags in highlight color
- System SHALL display summary: "Adding 23 door tags, 15 window tags"
- System SHALL allow confirm/cancel before committing
- User SHALL be able to undo with Ctrl+Z

**F2.4 - Configuration (Optional for PoC)**
- System SHOULD allow user to specify tag offset distance
- System SHOULD allow user to override default tag family
- System SHOULD remember user preferences per project

### Core System Requirements

**F3.1 - pyRevit Plugin Interface**
- System SHALL integrate as pyRevit extension/plugin
- System SHALL appear in Revit ribbon with "AI Co-pilot" button
- System SHALL provide input dialog for natural language prompts
- System SHALL use Revit ExternalEvent pattern for thread-safe API access

**F3.2 - LLM Integration**
- System SHALL use Claude API (Sonnet or Opus) for prompt parsing
- System SHALL send structured context: element types, view info, project standards
- System SHALL receive structured commands: operation type, targets, parameters
- System SHALL handle API errors gracefully (timeout, rate limits, network issues)

**F3.3 - Logging & Diagnostics**
- System SHALL log all operations to file: timestamp, prompt, action, result
- System SHALL capture errors with stack traces for debugging
- System SHALL provide feedback on LLM interpretation: "I understand you want to dimension all Level 1 rooms"
- System SHALL track operation time (LLM latency, Revit API execution)

---

## Non-Functional Requirements

### Performance

**NFR-P1: Response Time**
- LLM parsing SHALL complete within 5 seconds for typical prompts
- Dimension generation SHALL process 50+ rooms within 30 seconds
- Tagging operation SHALL process 100+ elements within 20 seconds
- Preview rendering SHALL update within 2 seconds

**NFR-P2: Scalability (PoC limits)**
- System SHALL handle projects with up to 500 rooms (typical firm project size)
- System SHALL gracefully degrade or warn if operation exceeds limits
- System SHALL NOT attempt operations on entire multi-building projects (defer to Phase 2)

### Reliability

**NFR-R1: Data Safety**
- System SHALL NEVER corrupt Revit project files
- System SHALL wrap all operations in Revit Transactions (atomic commit/rollback)
- System SHALL validate all API calls before execution
- System SHALL implement allowlist of safe operations (no model deletion, no parameter modification beyond tags/dimensions)

**NFR-R2: Error Handling**
- System SHALL recover gracefully from LLM API failures (timeout, rate limit)
- System SHALL provide clear error messages to user: "Could not connect to AI service. Please check internet connection."
- System SHALL log all errors for post-mortem analysis
- System SHALL never crash Revit (exception handling at all boundaries)

### Usability

**NFR-U1: Ease of Use**
- Hebrew-speaking architect with 25% Revit knowledge SHALL successfully use system with <5 minutes training
- System SHALL provide conversational feedback: "I found 12 rooms on Level 1. Proceeding to dimension..."
- System SHALL use familiar Revit UI patterns (ribbon button, dialog, preview highlighting)
- System SHALL support Revit's native undo/redo

**NFR-U2: Discoverability**
- System SHALL provide example prompts in UI: "Try: 'Add dimensions to selected rooms'"
- System SHALL suggest corrections for ambiguous prompts: "Did you mean 'Level 2' instead of 'Floor 2'?"
- System SHALL explain what it's about to do before executing

### Security & Privacy

**NFR-S1: Data Privacy**
- System SHALL NOT send proprietary project data to LLM beyond minimal context needed
- System SHALL anonymize element IDs and project names in LLM requests
- System SHALL use HTTPS for all API communication
- System SHALL allow offline operation (future: local LLM option)

**NFR-S2: API Key Management**
- System SHALL store Claude API key securely (not in plain text)
- System SHALL allow firm admin to configure API key centrally
- System SHALL warn user if API key is missing/invalid

### Maintainability

**NFR-M1: Code Quality**
- Python code SHALL follow PEP 8 style guidelines
- System SHALL use type hints for all function signatures
- System SHALL have unit tests for LLM parsing logic
- System SHALL have integration tests for Revit API operations

**NFR-M2: Debugging Support**
- System SHALL provide verbose logging mode for development
- System SHALL allow developer to replay operations from logs
- System SHALL separate concerns: UI, LLM client, Revit operations

### Localization

**NFR-L1: Hebrew Support**
- System SHALL accept Hebrew text input (RTL)
- System SHALL display Hebrew feedback messages
- System SHALL handle Hebrew tokenization efficiently (Claude's Unicode support)
- System SHALL provide Hebrew help documentation

---

## UX Principles & Interaction Design

### Core UX Philosophy

**Preview Before Commit**: User must always see what will happen before it happens. No surprises.

**Conversational Assistance**: System talks like a colleague, not a machine. Explains intent, asks clarifying questions.

**Fail Safe**: Better to do nothing than to do the wrong thing. When in doubt, ask the user.

### User Flow: Dimension Operation

```
1. User clicks "AI Co-pilot" button in Revit ribbon
2. Dialog opens with text input and example prompts
3. User types: "תוסיף מידות פנימיות לכל החדרים בקומה 1" (Hebrew)
4. System responds: "אני מבין - אני אוסיף מידות רציפות ל-12 החדרים בקומה 1. רגע אחד..."
   (I understand - I'll add continuous dimensions to 12 rooms on Level 1. One moment...)
5. System highlights proposed dimensions in yellow
6. Dialog shows: "Preview: 47 dimension chains will be added. [Confirm] [Cancel] [Adjust]"
7. User clicks Confirm
8. System commits transaction, shows success: "Done! Added 47 dimensions. Use Ctrl+Z to undo if needed."
```

### Visual Design

**Preview Highlighting:**
- Proposed dimensions: Yellow/amber color
- Proposed tags: Light blue color
- Affected elements: Subtle outline

**Dialog Design:**
- Minimal, non-modal where possible (dockable panel preferred)
- RTL support for Hebrew text
- Large input field (multi-line for complex prompts)
- Clear action buttons: Confirm (green), Cancel (gray), Adjust (blue)

**Feedback Messages:**
- Success: Green checkmark + brief message
- Warning: Amber triangle + explanation + suggestion
- Error: Red X + user-friendly explanation + troubleshooting hint

### Error Handling UX

**Ambiguous Prompt:**
```
User: "Add dimensions"
System: "I need more information. Do you want to:
  • Dimension all rooms in current view
  • Dimension selected elements
  • Dimension specific level (which one?)"
```

**Operation Preview Shows Issues:**
```
System: "Preview shows 3 rooms cannot be dimensioned (curved walls).
Proceed with remaining 9 rooms? [Yes] [No] [Show Details]"
```

**API Error:**
```
System: "Could not connect to AI service. Would you like to:
  • Retry connection
  • Work offline (limited functionality)
  • Check network settings"
```

### Accessibility

- Keyboard shortcuts for common operations (Ctrl+Shift+A to open dialog)
- Screen reader support (future)
- High contrast mode support (follows Revit theme)
- Font size respects Windows DPI settings

---

## Technical Architecture Overview

### System Components

```
┌─────────────────────────────────────────────────────────┐
│                    Revit Application                    │
│  ┌───────────────────────────────────────────────────┐  │
│  │          pyRevit Extension (Python)               │  │
│  │  ┌─────────────┐  ┌──────────────┐  ┌─────────┐  │  │
│  │  │ UI Layer    │  │ LLM Client   │  │ Revit   │  │  │
│  │  │ (Dialog,    │→ │ (Claude API) │→ │ API     │  │  │
│  │  │  Ribbon)    │  │              │  │ Wrapper │  │  │
│  │  └─────────────┘  └──────────────┘  └─────────┘  │  │
│  │         ↓                ↓                ↓        │  │
│  │  ┌─────────────────────────────────────────────┐  │  │
│  │  │      ExternalEvent Handler                  │  │  │
│  │  │   (Thread-safe Revit API access)            │  │  │
│  │  └─────────────────────────────────────────────┘  │  │
│  └───────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────┘
                         ↕ HTTPS
              ┌──────────────────────┐
              │   Claude API         │
              │   (Anthropic)        │
              └──────────────────────┘
```

### Technology Stack

**Development Environment:**
- Python 3.8+ (pyRevit compatible)
- Revit 2022/2023 (Perry's firm version)
- Visual Studio Code (Python development)
- Git (version control)

**Core Dependencies:**
- pyRevit 4.8+ (Revit integration framework)
- anthropic-sdk (Claude API client)
- requests (HTTP client for API calls)
- pyyaml (configuration management)

**Revit API:**
- RevitAPI.dll (core Revit object model)
- RevitAPIUI.dll (UI and events)
- Transaction API (atomic operations)
- FilteredElementCollector (element queries)

### Data Flow

**Prompt → Action:**
```python
# 1. User input
prompt = "Add internal dimensions to Level 1 rooms"

# 2. LLM parsing
context = {
    "project": "anonymized",
    "current_view": "Level 1 Floor Plan",
    "available_elements": ["Room", "Wall", "Door"],
    "firm_standards": {"dimension_offset": 200}
}
action = claude_api.parse_intent(prompt, context)
# Returns: {
#   "operation": "create_dimensions",
#   "targets": {"element_type": "Room", "level": "Level 1"},
#   "parameters": {"style": "continuous", "offset": 200}
# }

# 3. Revit operation (preview mode)
preview = dimension_creator.preview(action)
# Generates temporary graphics

# 4. User confirmation
if user_confirms():
    with Transaction(doc, "AI: Add Dimensions"):
        dimension_creator.execute(action)
        transaction.Commit()
```

### Configuration Management

**Firm-Level Config** (`firmconfig.yaml`):
```yaml
firm_name: "Perry_Studio"
language: "he"  # Hebrew primary
dimension_defaults:
  offset_mm: 200
  style: "Continuous"
tag_defaults:
  door_family: "Door Tag - Studio"
  placement_offset: 100
api_settings:
  claude_model: "claude-sonnet-4"
  timeout_seconds: 10
```

**User-Level Preferences** (stored in Revit project):
```yaml
user_id: "architect_01"
preferred_language: "he"
confirmation_required: true
verbose_feedback: true
```

### Safety Mechanisms

**Operation Allowlist:**
- ✅ Create dimensions
- ✅ Create tags
- ✅ Modify tag position
- ✅ Read element properties
- ❌ Delete elements
- ❌ Modify walls/doors/rooms
- ❌ Change project settings
- ❌ Export/import files

**Validation Pipeline:**
```python
def execute_llm_action(action):
    # 1. Allowlist check
    if action.operation not in ALLOWED_OPERATIONS:
        raise SecurityError(f"Operation {action.operation} not permitted")

    # 2. Scope validation
    if action.target_count > MAX_ELEMENTS:
        raise ValidationError(f"Operation scope too large: {action.target_count}")

    # 3. Preview generation
    preview = generate_preview(action)

    # 4. User confirmation
    if not user_confirms(preview):
        return ActionCancelled()

    # 5. Transaction execution
    with Transaction(doc, f"AI: {action.operation}"):
        result = execute_safe(action)
        transaction.Commit()

    return result
```

---

