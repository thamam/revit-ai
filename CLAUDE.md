# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

RevitAI is a C# Revit add-in that enables architects to automate Revit tasks through natural language commands in Hebrew or English. It uses Claude API for natural language understanding and implements safety-first architecture with preview/confirm patterns.

**Technology Stack:** C# .NET 8.0, Revit 2026 API, Anthropic SDK, WPF

**Current Status:** Epic 1 Foundation Complete (API Integration, Safety Validation, UI Scaffold) - Completing Execution Layer (ExternalEvent, Preview/Confirm, Logging)

## Common Development Commands

### Building

```bash
# Restore NuGet packages
dotnet restore RevitAI.CSharp/RevitAI.csproj

# Build project (Debug)
dotnet build RevitAI.CSharp/RevitAI.csproj

# Build for Release
dotnet build RevitAI.CSharp/RevitAI.csproj --configuration Release

# Build automatically copies DLL to:
# %APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\
```

### Testing

```bash
# Run all UI tests (requires Revit running)
dotnet test RevitAI.CSharp/tests/RevitAI.UITests/RevitAI.UITests.csproj

# Run specific test class
dotnet test --filter "ClassName=APIConnectionTests"

# Run with detailed output
dotnet test -v detailed
```

### Code Quality

```bash
# Visual Studio Code Analysis (on build)
dotnet build /p:RunAnalyzers=true

# Format code (requires dotnet-format tool)
dotnet format RevitAI.CSharp/RevitAI.csproj
```

### Development Workflow

1. Make code changes in `RevitAI.CSharp/Services/`, `Commands/`, `UI/`, or `Models/`
2. Build project: `dotnet build` (auto-deploys to Revit Addins folder)
3. **Close and restart Revit** to load new DLL (C# requires restart, unlike pyRevit)
4. Test changes in Revit
5. Check logs at `%APPDATA%/RevitAI/logs/` (once logging is implemented)

### Installation for Revit Testing

**Option 1: Automatic (via Build)**
```bash
# Build automatically deploys
dotnet build RevitAI.CSharp/RevitAI.csproj
# Restart Revit to load
```

**Option 2: Manual (PowerShell Script)**
```powershell
# Run installation script
.\RevitAI.CSharp\install-addon.ps1
# Restart Revit
```

**Option 3: Manual (Copy Files)**
```bash
# Copy DLL files
cp RevitAI.CSharp/bin/Debug/* %APPDATA%/Autodesk/Revit/Addins/2026/RevitAI/

# Copy manifest
cp RevitAI.CSharp/RevitAI.addin %APPDATA%/Autodesk/Revit/Addins/2026/

# Restart Revit
```

## Architecture Overview

### C# Add-in Structure

This project uses **Revit's official C# API** with standard .NET project structure:

```
RevitAI.CSharp/
├── Application.cs            → IExternalApplication (ribbon creation)
├── RevitAI.addin             → Revit manifest file
├── RevitAI.csproj            → .NET project file
├── Commands/
│   ├── CopilotCommand.cs     → IExternalCommand (main copilot)
│   └── SettingsCommand.cs    → IExternalCommand (settings)
├── Services/
│   ├── ClaudeService.cs      → Claude API integration
│   ├── SafetyValidator.cs    → Operation validation
│   ├── RevitEventHandler.cs  → ExternalEvent handler (Story 1.3 - IN PROGRESS)
│   └── LoggingService.cs     → Logging infrastructure (Story 1.6 - PLANNED)
├── UI/
│   ├── CopilotDialog.cs      → WPF main dialog
│   ├── SettingsDialog.cs     → WPF settings dialog
│   └── PreviewDialog.cs      → WPF preview/confirm (Story 1.5 - PLANNED)
├── Models/
│   └── RevitAction.cs        → Data transfer objects
└── tests/
    └── RevitAI.UITests/      → Selenium-based UI tests
```

**CRITICAL:** All shared logic goes in `Services/`, UI code in `UI/`, commands stay thin.

### Core Components

1. **Claude API Integration** (`Services/ClaudeService.cs`) ✅ COMPLETE
   - Parses natural language (Hebrew/English) into structured JSON actions
   - Uses Anthropic SDK 2.0.0 with Claude Sonnet 4.5 model
   - System prompt defines operation schema and allowed operations
   - Async/await for non-blocking API calls

2. **Safety Validation** (`Services/SafetyValidator.cs`) ✅ COMPLETE
   - Operation allowlist: `create_dimensions`, `create_tags`, `read_elements`
   - Blocks destructive operations: `delete_elements`, `modify_walls`, etc.
   - Validates scope limits (max 500 elements by default)
   - Returns structured `ValidationResult` with clear error messages

3. **ExternalEvent Pattern** (`Services/RevitEventHandler.cs`) 🚧 IN PROGRESS (Story 1.3)
   - Thread-safe Revit API access (required by Revit)
   - Background thread for async Claude API calls (non-blocking UI)
   - Main thread for Revit operations via `IExternalEventHandler`
   - Request/response queue for async communication

4. **Preview/Confirm UX** (`UI/PreviewDialog.cs`) 📋 PLANNED (Story 1.5)
   - Shows WPF preview dialog before any changes
   - Displays proposed operations with counts (e.g., "47 dimension chains")
   - Requires user confirmation (Confirm/Cancel buttons)
   - All operations use Revit Transactions (atomic commit/rollback)

5. **Configuration** (Environment Variables + Future YAML)
   - API key: `CLAUDE_API_KEY` environment variable
   - Settings stored in `%APPDATA%/RevitAI/settings.yaml` (planned)
   - Firm defaults for dimension offsets, styles, etc. (Epic 2)

6. **Logging** (`Services/LoggingService.cs`) 📋 PLANNED (Story 1.6)
   - File logs: `%APPDATA%/RevitAI/logs/revit_ai.log`
   - Uses `Microsoft.Extensions.Logging` framework
   - Rotating file handler with size limits
   - Structured format with timestamps, operation context, and log levels

### Data Flow

```
User enters Hebrew/English prompt
    ↓
Claude API parses → structured JSON action
    ↓
Safety validator checks allowlist & limits
    ↓
Revit API queries elements (via ExternalEvent)
    ↓
Preview graphics generated
    ↓
User confirms → Transaction commits
    ↓
Revit document modified
```

## Coding Patterns & Conventions

### Module Organization

- **One class per file** for major components
- **Snake_case** for modules and functions: `claude_client.py`, `parse_prompt()`
- **PascalCase** for classes: `ClaudeClient`, `SafetyValidator`
- **UPPER_SNAKE_CASE** for constants: `MAX_ELEMENTS`, `ALLOWED_OPERATIONS`

### Import Pattern (MUST FOLLOW)

```python
# Standard library first
import os
import logging
from typing import Dict, List, Optional

# Third-party second
import yaml
from anthropic import Anthropic

# Local third - absolute imports from lib
from lib.claude_client import ClaudeClient
from lib.revit_helpers import get_all_rooms
```

### Error Handling Pattern

```python
from exceptions import APIError, ValidationError, RevitAPIError

try:
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
```

**Custom exceptions** in `lib/exceptions.py`:
- `RevitAIError` - Base exception
- `APIError` - Claude API failures
- `ValidationError` - Safety validation failures
- `RevitAPIError` - Revit API operation failures
- `ConfigurationError` - Configuration issues

### Revit Transaction Pattern (REQUIRED)

All Revit modifications MUST use transactions:

```python
from Autodesk.Revit.DB import Transaction

with Transaction(doc, "AI: <operation name>") as t:
    t.Start()
    try:
        # Revit API operations here
        dimension = doc.Create.NewDimension(...)
        t.Commit()
    except Exception as e:
        t.RollBack()
        raise
```

### Logging Pattern

```python
from lib.logger import get_logger, log_operation

logger = get_logger(__name__)

# Standard logging
logger.info("Operation started")
logger.error("Operation failed", exc_info=True)

# Operation logging with context
with log_operation("dimension_creation") as op:
    # Perform operation
    op.log_info(f"Created {count} dimensions")
    # Auto-logs success/failure and duration
```

### Testing Pattern

Unit tests in `tests/unit/` - test individual functions/classes:

```python
import pytest
from lib.claude_client import ClaudeClient

class TestClaudeClient:
    def setup_method(self):
        """Setup before each test"""
        self.client = ClaudeClient(api_key="test_key")

    def test_parse_hebrew_prompt(self):
        """Test Hebrew prompt parsing"""
        prompt = "תוסיף מידות לכל החדרים"
        action = self.client.parse_prompt(prompt, {})
        assert action["operation"] == "create_dimensions"
```

Integration tests in `tests/integration/` - test multi-component flows using mocked Revit API from `tests/fixtures/`.

## Key Architectural Decisions

### ADR-001: pyRevit vs C# Plugin
**Decision:** Build with pyRevit (Python) instead of C# Revit SDK
**Rationale:** Faster iteration, better Claude SDK support, easier LLM integration

### ADR-002: Claude Sonnet 4 for NLU
**Decision:** Use Claude Sonnet 4.5 model
**Rationale:** Excellent Hebrew support, structured JSON output, speed/cost balance

### ADR-003: ExternalEvent Pattern
**Decision:** Use ExternalEvent for thread-safe Revit API access
**Rationale:** Required by Revit API, prevents "wrong thread" errors, enables non-blocking LLM calls

### ADR-006: Preview/Confirm Pattern (NON-NEGOTIABLE)
**Decision:** ALL operations MUST show preview and require confirmation
**Rationale:** Safety-first for domain-critical work where file corruption is unacceptable

### ADR-007: Operation Allowlist
**Decision:** Strict allowlist of permitted operations
**Rationale:** Prevents AI from performing destructive operations, limits blast radius

See `docs/architecture.md` for complete ADR details.

## Safety & Security Requirements

### ALWAYS Enforce These Rules

1. **Operation Allowlist** - Only these operations permitted:
   - `create_dimensions` - Add dimension chains ✅
   - `create_tags` - Add element tags ✅
   - `read_elements` - Query properties ✅
   - Everything else is BLOCKED ❌

2. **Scope Validation** - Check limits before execution:
   - Max 500 elements per operation (configurable)
   - Max 1000 dimensions per operation
   - Fail early with clear error messages

3. **Preview Required** - NEVER skip preview/confirm:
   - Show what will change
   - Get explicit user confirmation
   - Support cancel at any point

4. **Atomic Transactions** - All Revit changes in transactions:
   - Use `Transaction` context manager
   - Rollback on any error
   - One operation = one transaction

5. **Secure API Keys**:
   - Store in Windows Credential Manager (encrypted)
   - Never log API keys
   - Support environment variable fallback

6. **Data Privacy**:
   - Anonymize project data before sending to LLM
   - Don't log proprietary information
   - Don't send user personal info to Claude

## Configuration

Configuration file: `.extensions/RevitAI.extension/config/firm_defaults.yaml`

Key settings:
- `language`: "he" (Hebrew) or "en" (English)
- `dimension_defaults.offset_mm`: 200 (dimension line offset)
- `api_settings.model`: "claude-sonnet-4"
- `safety.max_elements_per_operation`: 500
- `logging.log_level`: "INFO" (use "DEBUG" for verbose)

**API Key Setup:**
```python
from lib.config_manager import get_config_manager
config = get_config_manager()
config.set_api_key("sk-ant-...")  # Stores encrypted
```

Or use environment variable: `export CLAUDE_API_KEY="sk-ant-..."`

## Hebrew Language Support

All user-facing text should support both Hebrew (he) and English (en) based on `firm_defaults.yaml`:

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

# Usage
config = get_config_manager()
lang = config.get('language', 'en')
message = MESSAGES[lang]["success"]
```

Hebrew UI requires RTL (right-to-left) formatting - architecture is ready but Epic 2 will implement.

## Troubleshooting

### Extension not loading in Revit
1. Check folder location: `%APPDATA%/pyRevit/Extensions/RevitAI.extension/`
2. Verify folder structure: `RevitAI.tab/AI Copilot.panel/`
3. Reload pyRevit: Revit → pyRevit → Reload
4. Check pyRevit console for errors

### Import errors
```bash
# Install dependencies
pip install -r requirements.txt

# Verify in pyRevit console
import anthropic
print(anthropic.__version__)  # Should be 0.72.0
```

### API key not working
1. Try environment variable: `setx CLAUDE_API_KEY "sk-ant-..."`
2. Restart Revit for env var to take effect
3. Test connection: Settings button → Test API Connection

### Logs not appearing
Check: `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

## Development Status

**Epic 1: Foundation & Core Infrastructure** ✅ Complete
- All 7 stories implemented
- 12 Python modules (~2000 lines)
- Comprehensive testing framework
- Ready for Revit integration testing

**Epic 2: Intelligent Dimension Automation** ⏳ Not Started
- 5 stories planned
- Dimension command parser (NLU)
- Room boundary detection
- Dimension chain generation
- Preview & confirmation workflow

See `EPIC1_COMPLETE.md` and `TESTING_GUIDE.md` for details.

## Project Files Reference

**Core Implementation:**
- `.extensions/RevitAI.extension/lib/*.py` - All shared modules
- `.extensions/RevitAI.extension/RevitAI.tab/` - UI buttons

**Documentation:**
- `README.md` - Project overview, installation, usage
- `docs/architecture.md` - Technical decisions (857 lines, 7 ADRs)
- `docs/PRD.md` - Product requirements
- `docs/epics.md` - Epic & story breakdown
- `TESTING_GUIDE.md` - Integration testing checklist
- `EPIC1_COMPLETE.md` - Epic 1 completion summary

**Tests:**
- `tests/unit/` - Unit tests (isolated, fast)
- `tests/integration/` - Integration tests (with Revit API mocks)
- `tests/fixtures/` - Test fixtures and mocks
- `pytest.ini` - Test configuration

**Configuration:**
- `requirements.txt` - Python dependencies
- `.extensions/RevitAI.extension/config/firm_defaults.example.yaml` - Config template
