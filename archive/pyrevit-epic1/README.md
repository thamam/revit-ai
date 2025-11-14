# PyRevit Implementation Archive

**Archived Date:** 2025-11-15
**Status:** Superseded by C# SDK implementation

## Overview

This archive contains the initial PyRevit (Python/IronPython) implementation of RevitAI that was attempted during Epic 1. After over 1 hour of debugging PyRevit stability issues without success, the project pivoted to a C# Revit SDK implementation which worked immediately.

## What's Archived

- **`.extensions/RevitAI.extension/`** - Complete PyRevit extension structure
  - `lib/` - Python modules (claude_client, safety_validator, external_event, etc.)
  - `RevitAI.tab/` - Ribbon UI definitions
  - `config/` - YAML configuration files

- **`tests/`** - Python test suite
  - `unit/` - Unit tests using pytest
  - `integration/` - Integration tests with mocked Revit API
  - `fixtures/` - Test fixtures and mocks

- **`requirements.txt`** - Python dependencies (anthropic, pyyaml, requests)
- **`pytest.ini`** - Pytest configuration
- **`EPIC1_COMPLETE.md`** - Epic 1 completion summary (PyRevit version)
- **`HOW_TO_TEST.md`** - Testing guide for PyRevit implementation

## Why Archived

### Technical Decision (ADR-001 Revision)

**Original Decision:** Build with PyRevit (Python) instead of C# Revit SDK
**Revised Decision:** Build with C# Revit SDK (.NET 8.0) instead of PyRevit (IronPython)

**Rationale for Change:**
- **Stability Issues:** PyRevit/IronPython encountered persistent debugging issues (1+ hours with no resolution)
- **Immediate Success:** C# SDK worked on first attempt
- **Production Robustness:** Official Autodesk API with better stability
- **Modern Async Patterns:** .NET 8.0 async/await ideal for LLM API calls
- **Type Safety:** Compiler catches errors at build time vs runtime

**Outcome:**
The C# SDK implementation completed Epic 1 (all 7 stories) successfully and is deployed to Revit 2026 for testing.

## Salvageable Code

While the PyRevit implementation was not production-viable, some design patterns and logic are reusable:

1. **Safety Validator Logic** - Operation allowlist/blocklist patterns (`lib/safety_validator.py`)
2. **Claude API Integration** - Prompt engineering and response parsing concepts (`lib/claude_client.py`)
3. **Test Scenarios** - Test cases for validation and edge cases (`tests/`)

## Migration Notes

Key concepts successfully migrated to C# implementation:

| PyRevit Module | C# Equivalent | Status |
|----------------|---------------|--------|
| `claude_client.py` | `Services/ClaudeService.cs` | ✅ Migrated |
| `safety_validator.py` | `Services/SafetyValidator.cs` | ✅ Migrated |
| `external_event.py` | `Services/RevitEventHandler.cs` | ✅ Migrated (improved with TaskCompletionSource) |
| `logger.py` | `Services/LoggingService.cs` | ✅ Migrated (with file rotation) |
| `ui_dialogs.py` | `UI/CopilotDialog.cs`, `UI/PreviewConfirmDialog.cs` | ✅ Migrated to WPF |
| `config_manager.py` | Environment variables (Epic 1), YAML planned (Epic 2) | 🚧 Partial |

## Historical Value

This archive serves as:
- **Decision Documentation:** Evidence for ADR-001 revision
- **Learning Reference:** Understanding what didn't work and why
- **Design Inspiration:** Core patterns that informed the C# implementation

## Do Not Use

⚠️ **WARNING:** Do not attempt to use this PyRevit implementation. It has known stability issues and has been superseded by the production C# implementation in `RevitAI.CSharp/`.

For the current, working implementation, see the main project repository.

---

**See also:**
- [docs/architecture.md](../../docs/architecture.md) - ADR-001 (revised) and ADR-008
- [RevitAI.CSharp/](../../RevitAI.CSharp/) - Current C# implementation
- [README.md](../../README.md) - Main project documentation
