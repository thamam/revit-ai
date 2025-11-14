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

### Code Organization

- **One class per file** for major components
- **PascalCase** for classes and public members: `ClaudeService`, `SafetyValidator`, `ParsePrompt()`
- **camelCase** for private fields and parameters: `_apiKey`, `elementCount`
- **UPPER_SNAKE_CASE** for constants: `MAX_ELEMENTS`, `ALLOWED_OPERATIONS`
- **Namespaces** match folder structure: `RevitAI.Services`, `RevitAI.Models`, `RevitAI.UI`

### Using Directives Pattern (MUST FOLLOW)

```csharp
// System namespaces first
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// Revit API second
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;

// Third-party libraries third
using Anthropic.SDK;

// Local namespaces last
using RevitAI.Models;
using RevitAI.Services;
```

### Error Handling Pattern

```csharp
using RevitAI.Exceptions;

try
{
    var result = PerformOperation();
}
catch (ApiException ex)
{
    logger.Error("API error", "OPERATION", ex);
    TaskDialog.Show("Error", "Could not connect to AI service. Check internet.");
}
catch (ValidationException ex)
{
    logger.Warning($"Validation failed: {ex.Message}", "OPERATION");
    TaskDialog.Show("Operation Not Allowed", ex.Message);
}
catch (RevitApiException ex)
{
    logger.Error("Revit error", "OPERATION", ex);
    TaskDialog.Show("Error", "Could not modify Revit model. See logs.");
}
```

**Custom exceptions** in `Exceptions/` namespace:
- `RevitAIException` - Base exception
- `ApiException` - Claude API failures
- `ValidationException` - Safety validation failures
- `RevitApiException` - Revit API operation failures
- `ConfigurationException` - Configuration issues

### Revit Transaction Pattern (REQUIRED)

All Revit modifications MUST use transactions:

```csharp
using Autodesk.Revit.DB;

using (Transaction trans = new Transaction(doc, "AI: <operation name>"))
{
    trans.Start();
    try
    {
        // Revit API operations here
        Dimension dimension = doc.Create.NewDimension(...);
        trans.Commit();
    }
    catch (Exception ex)
    {
        trans.RollBack();
        throw;
    }
}
```

### Logging Pattern

```csharp
using RevitAI.Services;

var logger = LoggingService.Instance;

// Standard logging
logger.Info("Operation started", "CONTEXT");
logger.Error("Operation failed", "CONTEXT", ex);
logger.Warning("Validation warning", "SAFETY");
logger.Debug("Detailed debug info", "DEBUG");

// Operation logging
logger.LogOperation("dimension_creation", "STARTED", "Processing 12 rooms");
// ... perform operation ...
logger.LogOperation("dimension_creation", "SUCCESS", $"Created {count} dimensions");
```

### Testing Pattern

Unit tests using NUnit framework in `RevitAI.Tests/`:

```csharp
using NUnit.Framework;
using RevitAI.Services;

namespace RevitAI.Tests.Unit
{
    [TestFixture]
    public class ClaudeServiceTests
    {
        private ClaudeService _service;

        [SetUp]
        public void Setup()
        {
            // Setup before each test
            _service = new ClaudeService("test_api_key");
        }

        [Test]
        public async Task ParseHebrewPrompt_ShouldReturnCreateDimensions()
        {
            // Arrange
            string prompt = "תוסיף מידות לכל החדרים";

            // Act
            var action = await _service.ParsePromptAsync(prompt, new Dictionary<string, object>());

            // Assert
            Assert.AreEqual("create_dimensions", action.Operation);
        }
    }
}
```

Integration tests use mocked Revit API interfaces for multi-component flows.

## Key Architectural Decisions

### ADR-001: C# SDK over PyRevit
**Decision:** Build with C# Revit SDK (.NET 8.0) instead of PyRevit (IronPython)
**Rationale:** PyRevit stability issues (1+ hour debugging with no success), C# worked immediately, official Revit API support, better async/await for LLM calls, production-grade robustness

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

**Current Configuration (Epic 1):**
- API Key: `CLAUDE_API_KEY` environment variable
- Settings: Hardcoded defaults in Services classes

**Future Configuration (Epic 2):**
Configuration file: `%APPDATA%/RevitAI/settings.yaml`

Planned settings:
- `language`: "he" (Hebrew) or "en" (English)
- `dimension_defaults.offset_mm`: 200 (dimension line offset)
- `api_settings.model`: "claude-sonnet-4-5"
- `safety.max_elements_per_operation`: 500
- `logging.log_level`: "INFO" (use "DEBUG" for verbose)

**API Key Setup (Windows):**
```powershell
# Set environment variable (system-wide, requires admin)
setx CLAUDE_API_KEY "sk-ant-..." /M

# Set environment variable (user-only)
setx CLAUDE_API_KEY "sk-ant-..."

# Restart Revit for changes to take effect
```

Or set via Windows Settings → System → Advanced → Environment Variables

## Hebrew Language Support

All user-facing text should support both Hebrew (he) and English (en) based on configuration:

```csharp
public static class Messages
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["he"] = new Dictionary<string, string>
        {
            ["api_error"] = "לא ניתן להתחבר לשירות AI",
            ["success"] = "הפעולה הושלמה בהצלחה"
        },
        ["en"] = new Dictionary<string, string>
        {
            ["api_error"] = "Cannot connect to AI service",
            ["success"] = "Operation completed successfully"
        }
    };

    public static string Get(string key, string language = "en")
    {
        return Translations[language][key];
    }
}

// Usage
string message = Messages.Get("success", "he");
```

Hebrew UI requires RTL (right-to-left) formatting - architecture is ready but Epic 2 will implement.

## Troubleshooting

### Add-in not loading in Revit
1. Check manifest location: `%APPDATA%/Autodesk/Revit/Addins/2026/RevitAI.addin`
2. Check DLL location: `%APPDATA%/Autodesk/Revit/Addins/2026/RevitAI/RevitAI.dll`
3. Verify DLL is not blocked (Right-click → Properties → Unblock)
4. **Restart Revit** (C# add-ins require full restart, no hot-reload)
5. Check Windows Event Viewer for .NET exceptions

### Build errors
```powershell
# Clean and rebuild
dotnet clean RevitAI.CSharp/RevitAI.csproj
dotnet build RevitAI.CSharp/RevitAI.csproj

# Check for missing dependencies
dotnet restore RevitAI.CSharp/RevitAI.csproj
```

### API key not working
1. Set environment variable: `setx CLAUDE_API_KEY "sk-ant-..."`
2. **Restart Revit** for env var to take effect (close completely, not just reload)
3. Test connection: "Test Claude API" button in Copilot dialog
4. Verify in PowerShell: `$env:CLAUDE_API_KEY`

### Logs not appearing
1. Check log location: `%APPDATA%/RevitAI/logs/revit_ai.log`
2. Verify logging initialized in Application.cs startup
3. Click "View Logs" button to open in Notepad

## Development Status

**Epic 1: Foundation & Core Infrastructure** ✅ Complete (C# SDK)
- All 7 stories implemented
- 15+ C# classes across Services, Models, UI, Commands (~1500 lines)
- Stories completed:
  - 1.1: Claude API Integration ✅
  - 1.2: Safety Validation Framework ✅
  - 1.3: ExternalEvent Pattern ✅
  - 1.4: Basic UI Scaffold ✅
  - 1.5: Preview/Confirm UX ✅
  - 1.6: Logging Infrastructure ✅
  - 1.7: Configuration System ✅
- Build successful, deployed to Revit 2026 Addins folder
- Ready for Revit integration testing

**Epic 2: Intelligent Dimension Automation** ⏳ Not Started
- 5 stories planned
- Dimension command parser (NLU)
- Room boundary detection
- Dimension chain generation
- Preview & confirmation workflow

See `docs/epics.md` for epic breakdown and acceptance criteria.

## Project Files Reference

**Core Implementation:**
- `RevitAI.CSharp/Services/` - Business logic (ClaudeService, SafetyValidator, RevitEventHandler, LoggingService)
- `RevitAI.CSharp/Models/` - Data models (RevitAction, RevitRequest, RevitResponse, OperationPreview)
- `RevitAI.CSharp/UI/` - WPF dialogs (CopilotDialog, SettingsDialog, PreviewConfirmDialog)
- `RevitAI.CSharp/Commands/` - IExternalCommand implementations (CopilotCommand, SettingsCommand)
- `RevitAI.CSharp/Application.cs` - IExternalApplication (startup, ribbon creation)

**Documentation:**
- `README.md` - Project overview, installation, usage
- `docs/architecture.md` - Technical decisions (857 lines, 7 ADRs)
- `docs/PRD.md` - Product requirements
- `docs/epics.md` - Epic & story breakdown
- `CLAUDE.md` - This file (development guide)

**Tests:**
- `RevitAI.Tests/Unit/` - NUnit unit tests (isolated, fast)
- `RevitAI.Tests/Integration/` - Integration tests (with Revit API mocks)
- `RevitAI.Tests/Fixtures/` - Test fixtures and mocks

**Configuration:**
- `RevitAI.CSharp/RevitAI.csproj` - .NET project file with dependencies
- `RevitAI.CSharp/RevitAI.addin` - Revit manifest file
- `RevitAI.CSharp/install-addon.ps1` - PowerShell deployment script
