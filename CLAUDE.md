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

## Research Findings & Strategic Direction

**Research Date:** 2025-11-20
**Reports:** Tasks 1-3 (MCP Server Landscape, PyRevit+LLM Integration, Testing Strategies)

### Key Findings Summary

RevitAI's development is now informed by comprehensive research into the Revit+AI ecosystem. Three parallel research tasks analyzed 14 existing projects, academic papers achieving 80% accuracy, and industry testing strategies. These findings validate our architectural choices and reveal critical market gaps we can exploit.

### 1. Market Landscape & Competitive Positioning

**Commercial Products Identified:**
- **ArchiLabs AI Copilot** ($99/month, 100+ brands) - Natural language automation, proprietary
- **BIMLOGIQ Copilot** ($35/month) - Code-generation LLM, 50+ public commands
- **DWD AI Assistant** (Autodesk App Store) - Requires user's OpenAI API key

**Open-Source Projects:**
- **revit-mcp-python** (49 GitHub stars) - 13 working tools, MCP implementation
- **RevitGeminiRAG** (experimental) - RAG approach with 8-step workflow, "still rough"

**RevitAI's Unique Position:**
> "The only open-source, production-ready AI assistant for Revit with MCP+RAG hybrid architecture, local LLM support, and comprehensive safety testing framework."

**Market Gaps We Address:**
1. **No open-source production solution** - All mature products are proprietary/expensive
2. **No local LLM support** - Privacy-conscious firms (government, military) excluded
3. **No MCP+RAG hybrid** - Others choose one approach (limited or risky)
4. **No comprehensive testing framework** - Academic benchmarks not published openly

### 2. Architectural Validation

Our C# "Live Session Agent" architecture (Node A pattern from research) is validated by:

**Three Architectural Nodes Identified:**
1. **Node A: Live Session Agent** (revit-mcp-python, RevitAI)
   - Runs inside Revit process via ExternalEvent
   - Pros: Zero latency, god-mode access, user context awareness
   - Cons: Threading complexity (STA model), session fragility
   - **Our approach ✓**

2. **Node B: File-Based** (MCP4IFC)
   - Operates on exported IFC files
   - Pros: Scalable, no license required
   - Cons: "Round-trip failure" - semantic degradation
   - **Strategic dead-end for detailed design**

3. **Node C: Platform-Hub** (Speckle MCP)
   - Cloud database with GraphQL API
   - Pros: True round-trip via native serialization
   - Cons: Async interaction (not real-time)
   - **Long-term strategic direction**

**Research Quote:**
> "The winning architecture will likely be a hybrid of Node A (Live) and Node C (Platform)."

### 3. Epic 2 Strategic Pivot

**Original Plan:** Dimension automation (geometric, technically difficult)

**Research-Informed Plan:** Auto-tagging (annotation, high value, low risk)

**Rationale from Research:**
- **User Pain Point #1:** "Users spend days manually tagging" (Task 2 findings)
- **Risk Mitigation:** "Read-Only/Annotation tasks have lower blast radius" (Task 3, Studio Tema case)
- **Market Validation:** All 3 commercial products prioritize tagging over dimensions
- **Trust Building:** Demonstrate value before requesting higher-risk permissions

**New Epic 2 Structure:**
- Stories 2.1-2.3: Auto-Tagging Implementation (NEW PRIORITY)
- Stories 2.4-2.6: Dimension Automation (DEFERRED to Epic 3)
- Story 2.7: MCP Compatibility Layer (STRATEGIC ENABLER)

See `docs/epic2-refactored.md` for full refactored epic.

### 4. Testing Strategy (SIL Architecture)

Research revealed the "Inverted Testing Pyramid" problem in Revit development:

**Traditional Pyramid:**
```
      /\
     /E2E\      10% - Slow, expensive
    /──────\
   / Integ. \   20% - Medium speed
  /──────────\
 /   Unit     \ 70% - Fast, cheap
/______________\
```

**Revit Reality (Before SIL):**
```
 ______________
\   Unit     /  10% - Hard to isolate
 \──────────/
  \ Integ. /    20% - Requires Revit
   \──────/
    \ E2E/      70% - Manual, 10min/test
     \──/
```

**Our Solution: SIL (Separation of Interface Layers):**
- **Layer 1:** Pure business logic (80% of code) - Unit tests in milliseconds
- **Layer 2:** Revit API wrapper (15% of code) - Integration tests with mocks
- **Layer 3:** E2E glue (5% of code) - Acceptance tests weekly

**Restored Pyramid:**
```
      /\
     /E2E\      5% - Weekly
    /──────\
   / Layer2 \  15% - Nightly
  /──────────\
 /  Layer 1   \ 80% - Every commit
/______________\
```

See `docs/testing-framework.md` for comprehensive strategy.

### 5. Benchmark-Driven Development

**Academic Validation:**
- **BIMCoder (2025):** 80% accuracy with 1,680-query dataset
- **Synergistic BIM Aligners (2024):** 80-query evaluation for Revit C# API
- **Natural Language BIM Retrieval (2025):** 80% accuracy across architectural/structural/MEP

**Our Approach:**
- 85-query benchmark dataset (auto-tagging, dimensioning, querying, parameters)
- 45 Hebrew + 40 English queries
- Difficulty levels: Easy (35), Medium (35), Hard (15)
- Target accuracy: **80%+ to match academic state-of-art**

**Benchmark Structure:**
```json
{
  "id": "AT-001",
  "prompt": "Tag all doors in Level 1",
  "expected_operation": "auto_tag",
  "difficulty": "easy"
}
```

**Continuous Tracking:**
```
Version  | Date       | Accuracy | Notes
---------|------------|----------|---------------------------
v0.1.0   | 2025-11-15 | 72%      | Epic 1 baseline
v0.2.0   | 2025-11-20 | 80%      | Epic 2: Auto-tagging
v0.3.0   | TBD        | 83%+     | Epic 3: RAG hybrid
```

### 6. Future Architecture Roadmap

**Immediate (Epic 2):**
- ✅ Auto-tagging implementation (high value, low risk)
- ✅ MCP compatibility layer (JSON-RPC, tool registry)
- ✅ Comprehensive testing framework (SIL architecture)

**Short-Term (Epic 3):**
- RAG Hybrid Architecture (MCP + code generation fallback)
- Local LLM support (Ollama, qwen2.5-coder:7b for privacy)
- Dimension automation with hybrid LLM+solver approach

**Medium-Term (Epic 4):**
- Platform-Hub integration (Speckle connector)
- Multi-agent architecture (planner + executor + auditor)
- Promotion path: RAG-generated code → hardened MCP tool

**Long-Term (Epic 5+):**
- Cloud-based testing (no Revit license required)
- Model fine-tuning on Revit API corpus
- Multi-modal capabilities (vision models for 2D/3D understanding)

### 7. Risk Mitigation (Studio Tema Case Study)

Research identified real-world risk aversion in AEC industry:

**Financial Reality:**
- Studio Tema quantified automation risk: **20,000 ILS insurance deductible**
- Cannot afford "runaway" scripts corrupting production models
- **Sandbox environment mandate** for all automation tools

**Our Response:**
1. **Human-Gated Automation:** AI suggests, human confirms (Preview/Confirm pattern)
2. **Audit Loop Strategy:** Validate model health before/after operations
3. **Operation Allowlist:** Only safe operations (read, annotate) initially
4. **Scope Limits:** Maximum 500 elements per operation (configurable)
5. **Transaction Safety:** All changes atomic (commit/rollback)

**Risk Quantification:**
- Auto-tagging: **Low risk** (metadata addition, reversible with Ctrl+Z)
- Dimensioning: **Medium risk** (annotation, but collision complexity)
- Geometry modification: **High risk** (deferred until trust established)

### 8. Competitive Differentiation Strategy

**vs. Commercial Products (ArchiLabs, BIMLOGIQ):**
- ✅ Open-source (free vs. $35-99/month)
- ✅ Customizable (source code available)
- ✅ Local LLM option (privacy for sensitive projects)
- ✅ MCP-native (interoperates with ecosystem)

**vs. Open-Source Projects (revit-mcp-python):**
- ✅ Production-ready (not experimental)
- ✅ Comprehensive documentation (vs. minimal README)
- ✅ Testing framework (80+ benchmark queries)
- ✅ Safety-first (preview/confirm, audit trail)

**vs. Academic Research (BIMCoder, BIMgent):**
- ✅ Practical focus (real-world deployment vs. proof-of-concept)
- ✅ Community-driven (open contribution model)
- ✅ User-validated (iterate based on architect feedback)

### 9. Key Research Citations

**Task 1: MCP Server Landscape Analysis**
- Identified Model Context Protocol as emerging standard
- "The winning architecture will likely be a hybrid of Node A (Live) and Node C (Platform)"
- Operations inventory: auto-tagging #1 priority across all implementations

**Task 2: PyRevit+LLM Integration Analysis**
- Market validation: 3 commercial products, 14 total findings
- Gap analysis: "Nobody has combined MCP + RAG hybrid approach"
- User pain points: "Users spend days manually tagging wall types and door numbers"

**Task 3: Revit API Testing Strategies**
- SIL Architecture: "80% of code (Layer 1) tests in milliseconds without Revit"
- Studio Tema case: "20,000 ILS insurance, sandbox mandate, human-gated automation"
- Recommendation: "Start with 'Read-Only/Annotation' tasks (lower blast radius)"

### 10. Updated Project Documentation

**New Documents:**
- `docs/research/Task-1-Revit-MCP-Server-Landscape-Analysis.md` (comprehensive MCP ecosystem analysis)
- `docs/research/Task-2-PyRevit-and-LLM-Integration-Analysis.md` (market validation + gap analysis)
- `docs/research/Task-3-Revit-API-Testing-Strategies-Research.md` (SIL architecture + benchmarking)
- `docs/epic2-refactored.md` (research-informed auto-tagging prioritization)
- `docs/testing-framework.md` (comprehensive testing strategy + 85-query benchmark)
- `docs/architecture.md` - Updated with ADR-009 (MCP), ADR-010 (RAG), ADR-011 (Local LLM)

**Usage:**
These research findings inform all future development decisions. When planning new features, reference:
1. Task 1 for MCP architectural patterns
2. Task 2 for market gaps and user needs
3. Task 3 for testing strategies and risk mitigation

---

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
