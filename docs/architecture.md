# Architecture - revit-ai

**Author:** Doc
**Date:** 2025-11-09
**Version:** 1.0

---

## Executive Summary

revit-ai uses a **pyRevit plugin architecture** with Claude API integration to enable natural language automation of Revit tasks. The architecture prioritizes **safety** (preview/confirm pattern, operation allowlist) and **consistency** (standardized folder structure, shared libraries, BDD testing) to ensure reliable AI-driven automation in a domain-critical environment where file corruption is unacceptable.

## Project Initialization

**First Implementation Story: Project Setup**

Create the pyRevit extension structure following standard conventions:

```bash
# Manual setup - pyRevit uses folder conventions, not CLI generators
mkdir -p .extensions/RevitAI.extension/lib
mkdir -p .extensions/RevitAI.extension/RevitAI.tab/AI\ Copilot.panel/Copilot.pushbutton
```

This establishes the base architecture with these decisions:
- pyRevit 4.8+ framework (folder-based extension discovery)
- Python 3.8+ (IronPython compatibility via pyRevit)
- Standard pyRevit naming conventions for automatic UI generation

---

## Decision Summary

| Category | Decision | Version | Affects Epics | Rationale |
| -------- | -------- | ------- | ------------- | --------- |
| Framework | pyRevit | v5.1.0.25094+1017 | Epic 1 | Provides Python execution environment within Revit, folder-based extension discovery, automatic UI generation |
| Python | Python (IronPython via pyRevit) | 3.8+ | All Epics | pyRevit compatibility requirement, supports modern SDK features |
| LLM SDK | anthropic | 0.72.0 | Epic 1, Epic 2 | Official Claude API client, supports Sonnet 4.5, type-safe, async capable |
| HTTP Client | requests | latest | Epic 1 | Fallback HTTP client if anthropic SDK has issues, well-tested library |
| Threading | ExternalEvent Pattern | Revit API | Epic 1 | Thread-safe Revit API access from background threads (LLM calls) |
| Configuration | YAML + Windows Credential Manager | pyyaml latest | Epic 1 | Firm settings in YAML, API keys encrypted in Windows Credential Manager |
| Logging | Python logging module | stdlib | Epic 1 | Rotating file handler, structured logs, configurable levels |
| Testing | pytest + Revit API mocking | pytest 7.x | Epic 1 | Unit tests for LLM parsing, integration tests with mocked Revit API |
| Code Quality | Black + Pylint | latest | All Epics | Code formatter (Black) for consistency, linter (Pylint) for quality checks |
| Preview Graphics | Revit DirectContext3D | Revit API | Epic 1, Epic 2 | Hardware-accelerated temporary graphics for dimension preview |
| Error Handling | Try-except with user-friendly messages | Python | All Epics | Catch all exceptions, log technical details, show friendly messages to users |

## Project Structure

```
revit-ai/
├── .extensions/
│   └── RevitAI.extension/
│       ├── lib/                          # Shared Python modules
│       │   ├── __init__.py
│       │   ├── claude_client.py          # Claude API integration
│       │   ├── revit_helpers.py          # Revit API wrappers
│       │   ├── external_event.py         # ExternalEvent handler
│       │   ├── safety_validator.py       # Operation allowlist
│       │   ├── preview_graphics.py       # DirectContext3D preview
│       │   ├── config_manager.py         # Configuration loading
│       │   └── logger.py                 # Logging setup
│       ├── hooks/                        # pyRevit event hooks (if needed)
│       ├── RevitAI.tab/                  # Creates "RevitAI" ribbon tab
│       │   └── AI Copilot.panel/         # Creates panel in tab
│       │       ├── Copilot.pushbutton/   # Main command button
│       │       │   ├── script.py         # Entry point - launches dialog
│       │       │   ├── icon.png          # Button icon (32x32)
│       │       │   └── bundle.yaml       # Optional metadata
│       │       └── Settings.pushbutton/  # API key configuration
│       │           ├── script.py
│       │           └── icon.png
│       ├── config/                       # Configuration files
│       │   └── firm_defaults.yaml        # Firm-specific settings
│       └── .python-version               # Pin Python version (3.8)
├── tests/                                # Test suite (outside .extensions)
│   ├── unit/
│   │   ├── test_claude_client.py
│   │   ├── test_safety_validator.py
│   │   └── test_dimension_parser.py
│   ├── integration/
│   │   └── test_revit_integration.py
│   └── fixtures/
│       └── mock_revit_api.py             # Revit API mocks
├── docs/                                 # Documentation
│   ├── PRD.md
│   ├── epics.md
│   ├── architecture.md (this file)
│   └── stories/                          # Individual story docs
├── .gitignore                            # Python, Revit, IDE files
├── requirements.txt                      # Python dependencies
├── pytest.ini                            # pytest configuration
└── README.md                             # Project overview, setup instructions
```

## Epic to Architecture Mapping

| Epic | Architecture Components | Key Modules |
| ---- | ----------------------- | ----------- |
| **Epic 1: Foundation & Core Infrastructure** | pyRevit extension structure, Claude API client, ExternalEvent handler, Safety validator, Preview graphics system, Logging infrastructure, Configuration management | `lib/claude_client.py`, `lib/external_event.py`, `lib/safety_validator.py`, `lib/preview_graphics.py`, `lib/logger.py`, `lib/config_manager.py` |
| **Epic 2: Intelligent Dimension Automation** | Dimension command parser, Room boundary analyzer, Dimension generator, Preview & confirmation dialog | `lib/dimension_parser.py` (new), `lib/room_analyzer.py` (new), `lib/dimension_generator.py` (new), Dialog UI in `script.py` |

## Technology Stack Details

### Core Technologies

**pyRevit Framework (v5.1.0.25094+1017)**
- Python execution environment within Revit
- Folder-based extension discovery (`.extension`, `.tab`, `.panel`, `.pushbutton`)
- Automatic ribbon UI generation
- Access to Revit API and .NET libraries
- Compatible with Revit 2022, 2023, 2024, 2025

**Python 3.8+ (IronPython via pyRevit)**
- Type hints supported
- Modern standard library features
- Compatible with anthropic SDK

**Claude API (Anthropic SDK 0.72.0)**
- Official Python SDK for Claude API
- Supports Claude Sonnet 4.5 model
- Type-safe request/response handling
- Async support (for future optimization)

**Revit API (2022/2023)**
- Document model access (elements, parameters, geometry)
- Transaction API for atomic operations
- DirectContext3D for preview graphics
- ExternalEvent for thread-safe API access

### Dependencies

**Production Dependencies** (`requirements.txt`):
```
anthropic==0.72.0
requests>=2.31.0
pyyaml>=6.0.1
```

**Development Dependencies**:
```
pytest>=7.4.0
pytest-cov>=4.1.0
black>=23.0.0
pylint>=3.0.0
```

### Integration Points

**Claude API Integration**
- Entry Point: User enters Hebrew/English prompt in dialog
- API Call: `lib/claude_client.py` sends prompt with Revit context to Claude
- Response: Structured action (JSON) returned with operation, targets, parameters
- Threading: API calls run on background thread, ExternalEvent handles Revit API operations

**Revit API Integration**
- All Revit API calls wrapped in `lib/revit_helpers.py`
- ExternalEvent pattern ensures main thread execution
- Transactions wrap all model modifications (atomic commit/rollback)
- Preview graphics use DirectContext3D (hardware-accelerated)

**Configuration Integration**
- `config/firm_defaults.yaml`: Dimension offsets, tag families, dimension styles
- Windows Credential Manager: Encrypted Claude API key storage
- `lib/config_manager.py`: Centralized config loading with validation

## Implementation Patterns

These patterns ensure consistent implementation across all AI agents:

### Naming Conventions

**Python Modules and Functions**
- Module names: `snake_case` (e.g., `claude_client.py`, `room_analyzer.py`)
- Class names: `PascalCase` (e.g., `ClaudeClient`, `SafetyValidator`)
- Function names: `snake_case` (e.g., `parse_dimension_prompt`, `create_dimension_chain`)
- Constants: `UPPER_SNAKE_CASE` (e.g., `MAX_ELEMENTS`, `API_TIMEOUT`)

**Revit Elements**
- Transaction names: `"AI: <operation>"` (e.g., `"AI: Add Dimensions"`, `"AI: Create Tags"`)
- Log entries: `"[OPERATION] action"` (e.g., `"[DIM] Generating 47 dimension chains"`)

**File Naming**
- Test files: `test_<module>.py` (e.g., `test_claude_client.py`)
- Config files: `<purpose>.yaml` (e.g., `firm_defaults.yaml`)

### Code Organization

**Module Structure**
- One class per file for major components (e.g., `ClaudeClient` in `claude_client.py`)
- Helper functions in same file as related class
- Shared utilities in separate files (e.g., `revit_helpers.py`)

**Test Organization**
- Unit tests in `tests/unit/` - test individual functions/classes
- Integration tests in `tests/integration/` - test multi-component flows
- Fixtures in `tests/fixtures/` - mock Revit API, sample data

**Import Organization**
- Standard library imports first
- Third-party imports second (anthropic, requests, yaml)
- Local imports third (from lib import ...)
- Use absolute imports: `from lib.claude_client import ClaudeClient`

### Error Handling

**Exception Hierarchy**
```python
class RevitAIError(Exception):
    """Base exception for all RevitAI errors"""
    pass

class APIError(RevitAIError):
    """Claude API errors (timeout, rate limit, network)"""
    pass

class ValidationError(RevitAIError):
    """Safety validation failures (operation not allowed)"""
    pass

class RevitAPIError(RevitAIError):
    """Revit API operation failures"""
    pass
```

**Error Handling Pattern**
```python
try:
    # Operation
    result = perform_operation()
except APIError as e:
    logger.error(f"API error: {e}", exc_info=True)
    show_user_message("Could not connect to AI service. Check internet.")
except ValidationError as e:
    logger.warning(f"Validation failed: {e}")
    show_user_message(f"Operation not allowed: {e}")
except RevitAPIError as e:
    logger.error(f"Revit error: {e}", exc_info=True)
    show_user_message("Could not modify Revit model. See logs.")
except Exception as e:
    logger.critical(f"Unexpected error: {e}", exc_info=True)
    show_user_message("An unexpected error occurred. See logs.")
```

**User-Facing Error Messages**
- Always friendly and actionable
- Hebrew support (using RTL formatting)
- Examples:
  - "לא ניתן להתחבר לשירות AI. בדוק את החיבור לאינטרנט." (Cannot connect to AI service. Check internet connection.)
  - "הפעולה לא מותרת: מחיקת אלמנטים חסומה." (Operation not allowed: deleting elements is blocked.)

### Logging Strategy

**Log Configuration**
```python
# lib/logger.py
import logging
from logging.handlers import RotatingFileHandler

def setup_logger(name, log_file, level=logging.INFO):
    """Create logger with rotating file handler"""
    logger = logging.getLogger(name)
    logger.setLevel(level)

    handler = RotatingFileHandler(
        log_file,
        maxBytes=10*1024*1024,  # 10MB
        backupCount=5
    )

    formatter = logging.Formatter(
        '%(asctime)s - %(name)s - %(levelname)s - %(message)s'
    )
    handler.setFormatter(formatter)
    logger.addHandler(handler)

    return logger
```

**Log Levels**
- DEBUG: Detailed information (LLM prompts, API responses, Revit API calls)
- INFO: General operations (dimension created, preview shown, user confirmed)
- WARNING: Validation failures, partial successes
- ERROR: API errors, Revit API failures
- CRITICAL: Unhandled exceptions, system failures

**Log Location**
- `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

**What to Log**
- Every user prompt and LLM response (anonymized project data)
- Every Revit API operation (success/failure)
- Performance metrics (LLM latency, Revit operation time)
- Errors with full stack traces
- **Do NOT log**: API keys, proprietary project names, user personal info

### Data Format Patterns

**LLM Action Schema** (JSON response from Claude)
```python
{
    "operation": "create_dimensions",  # Operation type
    "targets": {
        "element_type": "Room",
        "scope": "Level 1",  # or "current_view" or "selected"
        "filter": {}         # Optional additional filters
    },
    "parameters": {
        "offset_mm": 200,
        "style": "continuous",
        "dimension_type": "Linear"  # From firm config or default
    },
    "clarifications": []  # List of questions if ambiguous
}
```

**Configuration File Format** (`firm_defaults.yaml`)
```yaml
firm_name: "Perry Studio"
language: "he"  # Hebrew primary

dimension_defaults:
  offset_mm: 200
  style: "Continuous"
  type: "Linear"

tag_defaults:
  door_family: "Door Tag - Studio"
  window_family: "Window Tag - Studio"
  room_family: "Room Tag - Studio"
  placement_offset: 100

api_settings:
  model: "claude-sonnet-4"
  timeout_seconds: 10
  max_retries: 3
```

### Testing Patterns

**Unit Test Structure**
```python
# tests/unit/test_claude_client.py
import pytest
from lib.claude_client import ClaudeClient

class TestClaudeClient:
    def setup_method(self):
        """Setup before each test"""
        self.client = ClaudeClient(api_key="test_key")

    def test_parse_dimension_prompt_hebrew(self):
        """Test Hebrew prompt parsing"""
        prompt = "תוסיף מידות פנימיות לכל החדרים בקומה 1"
        context = {"levels": ["Level 1", "Level 2"]}

        action = self.client.parse_prompt(prompt, context)

        assert action["operation"] == "create_dimensions"
        assert action["targets"]["scope"] == "Level 1"
```

**Integration Test with Mocked Revit API**
```python
# tests/integration/test_dimension_flow.py
from tests.fixtures.mock_revit_api import MockDocument
from lib.dimension_generator import DimensionGenerator

def test_end_to_end_dimension_creation():
    """Test full flow: parse → analyze → generate → commit"""
    doc = MockDocument()
    doc.add_room("Room 1", level="Level 1")

    generator = DimensionGenerator(doc)
    result = generator.create_dimensions(scope="Level 1")

    assert result.success == True
    assert result.dimension_count == 4  # 4 walls
```

## Consistency Rules

### File Structure Consistency

**All agents MUST follow**:
- Shared code in `lib/` (never duplicate code across scripts)
- Tests in `tests/` directory (NOT inside .extensions)
- Configuration in `config/` directory
- One responsibility per module (single purpose)

### Import Pattern Consistency

**All imports MUST use**:
```python
# Standard library
import os
import logging
from typing import Dict, List, Optional

# Third-party
import yaml
from anthropic import Anthropic

# Local - absolute imports from lib
from lib.claude_client import ClaudeClient
from lib.revit_helpers import get_all_rooms
```

### Transaction Pattern Consistency

**All Revit modifications MUST use**:
```python
from Autodesk.Revit.DB import Transaction

with Transaction(doc, "AI: <operation name>") as t:
    t.Start()
    try:
        # Revit API operations
        dimension = doc.Create.NewDimension(...)
        t.Commit()
    except Exception as e:
        t.RollBack()
        raise
```

### Date/Time Handling

- Store dates in ISO 8601 format: `"2025-11-09T14:30:00Z"`
- Display dates according to firm locale (Hebrew format for Perry's firm)
- Use Python `datetime` module (never hardcode date formats)

### Hebrew Language Support

**All user-facing text MUST**:
- Support Hebrew (he) and English (en) based on `config/firm_defaults.yaml`
- Use RTL (right-to-left) formatting for Hebrew UI elements
- Store translations in separate dictionaries or files

```python
MESSAGES = {
    "he": {
        "api_error": "לא ניתן להתחבר לשירות AI",
        "success": "הפעולה הושלמה בהצלחה"
    },
    "en": {
        "api_error": "Cannot connect to AI service",
        "success": "Operation completed successfully"
    }
}
```

## Data Architecture

**No persistent database** - This is a Revit plugin that operates on Revit project files.

**Data Sources**:
1. **Revit Document** (read-only during analysis)
   - Elements: Rooms, Walls, Doors, Windows
   - Parameters: Dimensions, Tags, Levels
   - Geometry: Boundaries, Curves, Points

2. **Configuration Files**
   - `firm_defaults.yaml`: Firm-specific settings
   - Windows Credential Manager: Encrypted API key

3. **Log Files** (append-only)
   - `revit_ai.log`: Operation history, errors, performance metrics

**Data Flow**:
```
User Prompt (Hebrew/English)
  ↓
Claude API (structured action JSON)
  ↓
Revit API (query elements, geometry)
  ↓
Preview Graphics (temporary visualization)
  ↓
User Confirmation
  ↓
Revit Transaction (atomic commit)
  ↓
Revit Document (modified with dimensions/tags)
```

## API Contracts

### Claude API Integration

**Request Format**:
```python
{
    "model": "claude-sonnet-4",
    "max_tokens": 1024,
    "messages": [
        {
            "role": "user",
            "content": """<context>
Revit Project Context:
- Current View: Level 1 Floor Plan
- Available Levels: Level 1, Level 2, Roof
- Element Types: Room (12), Wall (48), Door (8)
- Firm Standards: dimension offset 200mm, continuous style
</context>

<prompt>
{user_prompt_hebrew_or_english}
</prompt>

Return JSON action schema:
{
  "operation": "create_dimensions | create_tags | ...",
  "targets": {...},
  "parameters": {...},
  "clarifications": [...]
}"""
        }
    ]
}
```

**Response Format**:
```python
{
    "operation": "create_dimensions",
    "targets": {
        "element_type": "Room",
        "scope": "Level 1"
    },
    "parameters": {
        "offset_mm": 200,
        "style": "continuous"
    },
    "clarifications": []  # Empty if no questions
}
```

### Revit API Operations

**Safe Operations (Allowlist)**:
- `Document.Create.NewDimension()` - Create dimensions ✅
- `IndependentTag.Create()` - Create tags ✅
- `FilteredElementCollector.OfCategory()` - Query elements ✅
- `Room.GetBoundarySegments()` - Get geometry ✅
- `Element.get_Parameter()` - Read parameters ✅

**Blocked Operations**:
- `Element.Delete()` - Delete elements ❌
- `Wall.Create()` - Create/modify model ❌
- `Document.Save()` - Save project ❌
- `Document.Close()` - Close project ❌

## Security Architecture

### API Key Management

**Storage**: Windows Credential Manager (encrypted by OS)
```python
# lib/config_manager.py
import keyring

def get_api_key():
    """Retrieve encrypted API key"""
    return keyring.get_password("RevitAI", "claude_api_key")

def set_api_key(api_key):
    """Store encrypted API key"""
    keyring.set_password("RevitAI", "claude_api_key", api_key)
```

**First-Run Setup**: Settings dialog prompts for API key on first launch

### Data Privacy

**Anonymization**:
- Element IDs anonymized before sending to LLM (e.g., `room_1`, `room_2` instead of Revit IDs)
- Project name replaced with `"project_X"`
- No user names or firm-specific data sent to Claude

**HTTPS**: All Claude API calls use HTTPS (enforced by anthropic SDK)

### Operation Safety

**Allowlist Enforcement**:
```python
# lib/safety_validator.py
ALLOWED_OPERATIONS = {
    "create_dimensions",
    "create_tags",
    "read_elements"
}

def validate_operation(action):
    if action["operation"] not in ALLOWED_OPERATIONS:
        raise ValidationError(f"Operation '{action['operation']}' not allowed")

    if action.get("targets", {}).get("element_count", 0) > MAX_ELEMENTS:
        raise ValidationError(f"Operation scope too large")
```

## Performance Considerations

### LLM Response Time
- **Target**: <5 seconds for prompt parsing
- **Strategy**: Use Claude Sonnet (fast, accurate balance)
- **Fallback**: Timeout after 10 seconds, show friendly error

### Dimension Generation
- **Target**: <30 seconds for 50+ rooms
- **Strategy**: Batch Revit API calls within single transaction
- **Optimization**: Pre-calculate geometry in Python before Revit API calls

### Preview Graphics
- **Target**: <2 seconds to render preview
- **Strategy**: DirectContext3D (hardware-accelerated) instead of temporary elements
- **Trade-off**: More complex code, but faster user experience

### Memory Management
- **Concern**: Large Revit projects (>500 rooms)
- **Strategy**: Process in batches, limit scope to current view by default
- **Warning**: Alert user if operation scope exceeds recommended limits

## Deployment Architecture

### Installation Steps

1. **Install pyRevit** (v5.1.0+)
   - Download from GitHub releases
   - Run installer (supports Revit 2022/2023)

2. **Clone/Copy Extension**
   ```bash
   cd %APPDATA%/pyRevit/Extensions
   git clone https://github.com/<repo>/revit-ai.git
   # Or copy .extensions/RevitAI.extension/ folder
   ```

3. **Install Python Dependencies**
   ```bash
   pip install -r requirements.txt
   ```

4. **Configure API Key**
   - Launch Revit
   - Open RevitAI Settings dialog
   - Enter Claude API key (stored encrypted)

5. **Load Extension**
   - pyRevit auto-discovers `.extension` folder
   - "RevitAI" tab appears in Revit ribbon

### Distribution Options

**Option 1: Git Repository** (for developers)
- Clone repo to pyRevit extensions folder
- Pull updates via git

**Option 2: Zip Package** (for end users)
- Download `.zip` with `.extensions/` folder
- Extract to `%APPDATA%/pyRevit/Extensions`
- Reload pyRevit

**Option 3: pyRevit Extension Manager** (future)
- Publish to pyRevit extension registry
- One-click install from pyRevit UI

## Development Environment

### Prerequisites

- **Revit 2022 or 2023** (Perry's firm version)
- **pyRevit v5.1.0+** installed
- **Python 3.8+** (provided by pyRevit)
- **Git** for version control
- **Visual Studio Code** (recommended IDE)

### Setup Commands

```bash
# 1. Clone repository
git clone <repo-url> revit-ai
cd revit-ai

# 2. Install Python dependencies (via pyRevit's Python)
pip install -r requirements.txt

# 3. Copy extension to pyRevit folder
cp -r .extensions/RevitAI.extension %APPDATA%/pyRevit/Extensions/

# 4. Configure development environment
# Create config/firm_defaults.yaml with test settings
cp config/firm_defaults.example.yaml config/firm_defaults.yaml

# 5. Run tests
pytest tests/
```

### Development Workflow

1. **Write Code** in `.extensions/RevitAI.extension/lib/`
2. **Write Tests** in `tests/unit/` or `tests/integration/`
3. **Run Tests** with `pytest`
4. **Format Code** with `black .`
5. **Lint Code** with `pylint lib/`
6. **Test in Revit**:
   - Open Revit
   - pyRevit reloads extension automatically (or manually reload)
   - Click "AI Co-pilot" button
   - Check logs at `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

### Debugging

- Use `print()` statements (output appears in pyRevit console)
- Use `logger.debug()` for detailed logging
- Enable verbose mode in `firm_defaults.yaml`: `log_level: DEBUG`
- Attach VS Code debugger to Revit process (advanced)

## Architecture Decision Records (ADRs)

### ADR-001: Use C# SDK Instead of PyRevit

**Decision**: Build with C# Revit SDK (.NET 8.0) instead of PyRevit (IronPython)

**Rationale**:
- **Stability issues with PyRevit**: After 1+ hours debugging PyRevit/IronPython with no success, C# worked immediately
- **Production-grade robustness**: C# SDK is official Autodesk-supported API with better stability
- **Modern async/await**: .NET 8.0 async patterns ideal for LLM API calls (Anthropic SDK 2.0.0)
- **Better threading support**: Native TaskCompletionSource + ExternalEvent for clean async-to-sync bridging
- **Type safety**: C# compiler catches errors at build time vs runtime
- **Performance**: Compiled code runs faster than interpreted Python

**Trade-offs**:
- No hot-reload: Requires full Revit restart for code changes (vs pyRevit auto-reload)
- Compilation step: Need to build before testing (but automated via post-build events)
- Initial setup: Requires .NET SDK installation (but standard for Windows development)

**Empirical Evidence**:
Initial PyRevit implementation attempt failed after extensive debugging. Switching to C# SDK, Epic 1 (all 7 stories) completed successfully with build deployed to Revit 2026 Addins folder.

**Status**: Accepted (Revised from original PyRevit decision)

---

### ADR-002: Claude Sonnet 4 for NLU

**Decision**: Use Claude Sonnet 4 model for natural language understanding

**Rationale**:
- **Bilingual support**: Excellent Hebrew tokenization and understanding
- **Structured output**: Can return JSON actions reliably
- **Speed**: Faster than Opus, accurate enough for dimension prompts
- **Cost**: More economical than Opus for high-frequency operations

**Trade-offs**:
- Requires internet connection (API-based)
- Depends on Anthropic API availability
- Monthly API costs (estimated ~$50-100/month for small firm)

**Future**: Consider local LLM option for offline capability

**Status**: Accepted

---

### ADR-003: ExternalEvent Pattern for Threading

**Decision**: Use Revit's ExternalEvent pattern for thread-safe API access

**Rationale**:
- **Revit API requirement**: Most Revit API calls must be on main thread
- **User experience**: Allows non-blocking LLM calls (UI doesn't freeze)
- **Safety**: Prevents "accessing Revit API from wrong thread" errors

**Implementation**:
```python
# Background thread (LLM call)
action = claude_client.parse_prompt(user_prompt)

# Signal main thread to execute Revit operation
external_event.raise_event(action)

# Main thread (via ExternalEvent handler)
execute_revit_operation(action)
```

**Status**: Accepted

---

### ADR-004: DirectContext3D for Preview Graphics

**Decision**: Use DirectContext3D for dimension preview instead of temporary elements

**Rationale**:
- **Performance**: Hardware-accelerated rendering, <2s render time
- **Clean**: No temporary elements polluting the model
- **Visual quality**: Better control over highlight colors and styles

**Trade-offs**:
- More complex implementation than temporary elements
- Requires understanding of 3D rendering pipeline

**Alternative Considered**: Create temporary dimension elements, delete after confirmation (simpler but slower)

**Status**: Accepted

---

### ADR-005: Windows Credential Manager for API Keys

**Decision**: Store Claude API key in Windows Credential Manager

**Rationale**:
- **Security**: OS-level encryption (better than plain text config files)
- **User-friendly**: Works with Windows authentication
- **Standard practice**: Recommended for desktop app credential storage

**Alternative Considered**: Encrypted config file (but managing encryption keys is complex)

**Status**: Accepted

---

### ADR-006: Preview/Confirm Pattern for All Operations

**Decision**: All AI operations MUST show preview and require user confirmation

**Rationale**:
- **Safety**: Prevents accidental or unwanted changes
- **Trust**: Builds user confidence in AI automation
- **Transparency**: User sees exactly what will happen
- **Undo**: Even with preview, all operations are undoable (Ctrl+Z)

**Non-Negotiable**: This is a core safety principle for domain-critical work

**Status**: Accepted

---

### ADR-007: Operation Allowlist for Safety

**Decision**: Implement strict allowlist of permitted operations

**Rationale**:
- **Safety**: Prevents AI from performing destructive operations
- **Trust**: Firm can audit allowed operations
- **Scope control**: Limits blast radius of any AI errors

**Allowlist**:
- ✅ Create dimensions
- ✅ Create tags
- ✅ Read element properties
- ❌ Delete elements
- ❌ Modify walls/doors/rooms
- ❌ Change project settings

**Status**: Accepted

---

### ADR-008: Epic 1 Staged Implementation (Foundation then Execution)

**Decision**: Implement Epic 1 in two stages - Foundation Layer first (Stories 1.1, 1.2, 1.4, 1.7), then Execution Layer (Stories 1.3, 1.5, 1.6)

**Rationale**:
- **Risk mitigation**: Validate Claude API integration and safety framework before tackling complex threading
- **Dependency management**: Foundation components (API client, validators, UI) needed before execution layer (ExternalEvent, preview, logging)
- **Early feedback**: Test buttons demonstrate each story completion in isolation
- **Learning curve**: Team gains C# SDK experience incrementally
- **Testability**: Foundation layer can be unit tested without Revit threading complexity

**Implementation Stages**:

**Stage 1: Foundation Layer** (Completed)
- Story 1.1: Project scaffold, ribbon UI, basic WPF dialogs
- Story 1.2: Claude API integration, Anthropic SDK 2.0.0
- Story 1.4: Safety validation framework, operation allowlist
- Story 1.7: Basic configuration (environment variables)

**Stage 2: Execution Layer** (Completed)
- Story 1.3: ExternalEvent pattern, request/response queue, TaskCompletionSource
- Story 1.5: Preview/Confirm UX, WPF preview dialog, OperationPreview model
- Story 1.6: Logging infrastructure, file rotation, structured logging

**Outcome**:
Both stages completed successfully. Epic 1 is 100% complete with all 7 stories implemented and tested. Build deployed to `%APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\` ready for integration testing.

**Status**: Accepted and Completed

---

_Generated by BMAD Decision Architecture Workflow v1.3.2_
_Date: 2025-11-09_
_For: Doc_

_Updated with ADR-008_
_Date: 2025-11-15_

---

### ADR-009: Modal Dialog Blocking Pattern for ExternalEvent

**Decision**: Close WPF modal dialogs before awaiting ExternalEvent responses

**Rationale**:
- **Root Cause**: WPF `ShowDialog()` blocks Revit's main thread event loop
- **Problem**: ExternalEvent callbacks require Revit's idle processing to invoke `Execute()`
- **Evidence**: Requests timed out after 30s but executed 10+ minutes later when dialog closed
- **Fix**: Call `Close()` on dialog before `await` on ExternalEvent response

**Technical Details**:

The ExternalEvent pattern requires Revit's main thread to be idle to process callbacks. When a WPF modal dialog (`ShowDialog()`) is open, it keeps the UI thread busy in its own message pump, preventing Revit from reaching its idle state.

**Before (Broken)**:
```csharp
private async void TestEventButton_Click(object sender, RoutedEventArgs e)
{
    _statusTextBlock.Text = "Testing..."; // Dialog still open
    var response = await RevitEventHandler.TestEventHandlerAsync(); // TIMES OUT!
    // Execute() never called because dialog blocks Revit's idle loop
}
```

**After (Fixed)**:
```csharp
private async void TestEventButton_Click(object sender, RoutedEventArgs e)
{
    Close(); // Release Revit's UI thread FIRST
    var response = await RevitEventHandler.TestEventHandlerAsync(); // Works!
    TaskDialog.Show("Result", response.Message); // Use Revit's dialog
}
```

**Key Insight**: The `await` in an async button handler yields control back to the message pump, but if that message pump is the WPF dialog's pump (not Revit's), the ExternalEvent callback is never invoked.

**Performance Improvement**:
- Before: 30+ second timeout (Execute never called)
- After: 0.5-0.7 seconds for Execute callback

**Log Evidence** (from debugging session):
```
01:25:40.909 Enqueued request, raising ExternalEvent
01:25:40.912 ExternalEvent.Raise() returned: Accepted
01:25:41.557 Execute() called by Revit, queue count: 1  // 0.645s
```

**Implications for Epic 2**:
- All async operations that use ExternalEvent must NOT block Revit's UI thread
- Consider modeless dialogs (`Show()`) instead of modal (`ShowDialog()`) for long operations
- Alternatively, close dialog before awaiting, show results in separate dialog

**Lesson Learned**:
Comprehensive diagnostic logging (added during debugging) made this root cause discoverable. Without the detailed timestamps showing Raise() success but Execute() absence, this would have been extremely difficult to diagnose.

**Status**: Accepted and Implemented (Commit 9bdd80f)

---

### ADR-010: Hybrid Testing Architecture for SIL (Software-in-the-Loop)

**Decision**: Implement three-layer testing architecture separating business logic from Revit API integration

**Rationale**:
- **Open-loop problem**: Current dev cycle is 5-10 minutes per iteration (manual Revit restart)
- **SIL value**: Fast feedback loops enable 10x development velocity
- **Long-term ROI**: Investment pays dividends across ALL future epics
- **Architecture quality**: Dependency Inversion enables testability AND portability

**Three-Layer Architecture**:

**Layer 1: Pure Business Logic (Millisecond Tests)**
- No Revit API dependencies
- Dimension placement algorithms
- Room analysis heuristics
- Safety validation logic
- Claude prompt parsing
- Mock implementations for unit testing

**Layer 2: Revit API Wrapper (CI-Automated Tests)**
- Thin integration layer
- `IRoomAnalyzer`, `IDimensionFactory` interfaces
- Real Revit API calls wrapped in abstractions
- Integration tests with Revit Test Framework
- Deterministic test fixtures (.rvt files with known geometry)

**Layer 3: End-to-End Acceptance (Semi-Automated)**
- Full workflow validation
- User-facing feature testing
- Run weekly or before releases

**Benefits**:
1. **80% of code** (Layer 1) tests in milliseconds without Revit
2. **15% of code** (Layer 2) needs Revit but is stable/thin
3. **5% of code** (Layer 3) is glue, validated by acceptance tests

**Interface Example**:
```csharp
public interface IRoomAnalyzer {
    IEnumerable<Room> GetAllRooms(Document doc);
    IEnumerable<Wall> GetBoundingWalls(Room room);
}
```

High-level logic depends on abstractions, not Revit API directly. Test doubles inject at these seams.

**Living Documentation**:
- Claude prompt→action tests serve as executable specs
- PRD examples become test cases
- Requirements traceable to tests

**Status**: Accepted (Pending Implementation in Story 0)

---

_Updated with ADR-009 and ADR-010_
_Date: 2025-11-15_
_Epic 1 Retrospective Outcomes_
