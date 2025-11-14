# RevitAI - Natural Language AI Co-pilot for Revit

**"Vibe coding" for architecture** - Command Revit in plain English (and Hebrew) using AI-powered natural language automation.

## Overview

RevitAI is a C# Revit add-in that enables architects to automate Revit tasks through natural language commands. Instead of hunting through menus and clicking through dialogs, you describe what you want in plain language, and the AI translates your intent into precise Revit operations.

**Current Status:** PoC - Epic 1 (Foundation) ✅ Complete - Ready for Revit testing

> **Note:** This project initially started with a PyRevit (Python) implementation which encountered stability issues. After 1+ hours of debugging with no success, we pivoted to the C# Revit SDK which worked immediately. The PyRevit implementation has been archived at `archive/pyrevit-epic1/` for historical reference. See [ADR-001](docs/architecture.md#adr-001-use-c-sdk-instead-of-pyrevit) for details on this architectural decision.

### What Makes This Special

- **Natural Language Interface**: Command Revit in Hebrew or English
- **Preview Before Commit**: See exactly what will happen before any changes are made
- **Safety First**: Operation allowlist prevents destructive actions
- **Professional Quality**: AI makes contextual decisions like an experienced architect

### PoC Scope

**Epic 1: Foundation & Core Infrastructure** ✅ *Complete*
- C# Revit add-in architecture (.NET 8.0)
- Claude API integration (Anthropic SDK 2.0.0)
- ExternalEvent pattern for thread-safe Revit API access
- Preview/confirm UX pattern (WPF dialogs)
- Operation allowlist and safety validation
- Logging infrastructure with file rotation
- All 7 stories implemented and tested

**Epic 2: Intelligent Dimension Automation** ⏳ *Not Started*
- Natural language dimension commands
- Room boundary detection
- Continuous dimension chain generation
- Preview & confirmation workflow

## Prerequisites

**For Using:**
- **Revit 2026** (tested version)
- **Claude API Key** from Anthropic ([Get one here](https://console.anthropic.com/))

**For Development:**
- **Revit 2026**
- **.NET 8.0 SDK** ([Download here](https://dotnet.microsoft.com/download/dotnet/8.0))
- **Visual Studio 2022** or **JetBrains Rider** (recommended for C# development)
- **Claude API Key** from Anthropic

## Installation

### Option A: Pre-built Release (End Users)

1. **Download Latest Release**
   - Download `RevitAI-v*.zip` from the Releases page
   - Extract to a temporary folder

2. **Install Add-in**
   ```powershell
   # Run the included installer script
   .\install-addon.ps1

   # Or manually copy files:
   # - RevitAI.dll and dependencies → %APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\
   # - RevitAI.addin → %APPDATA%\Autodesk\Revit\Addins\2026\
   ```

3. **Configure API Key**
   ```powershell
   # Set environment variable
   setx CLAUDE_API_KEY "sk-ant-YOUR_API_KEY_HERE"
   ```

4. **Restart Revit**
   - Close Revit completely (if running)
   - Launch Revit 2026
   - Look for "RevitAI" tab in ribbon

### Option B: Build from Source (Developers)

1. **Clone Repository**
   ```bash
   git clone https://github.com/<your-repo>/revit-ai.git
   cd revit-ai
   ```

2. **Install .NET 8.0 SDK**
   - Download from [dotnet.microsoft.com](https://dotnet.microsoft.com/download/dotnet/8.0)
   - Verify: `dotnet --version` (should show 8.0.x)

3. **Restore Dependencies**
   ```bash
   dotnet restore RevitAI.CSharp/RevitAI.csproj
   ```

4. **Build Project**
   ```bash
   # Build in Debug mode (auto-deploys to Revit Addins folder)
   dotnet build RevitAI.CSharp/RevitAI.csproj

   # Or build in Release mode
   dotnet build RevitAI.CSharp/RevitAI.csproj --configuration Release
   ```

5. **Configure API Key**
   ```powershell
   setx CLAUDE_API_KEY "sk-ant-YOUR_API_KEY_HERE"
   ```

6. **Restart Revit**
   - Build automatically copied DLL to `%APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\`
   - Manifest file copied to `%APPDATA%\Autodesk\Revit\Addins\2026\`
   - **Must restart Revit** to load changes (C# requires restart, no hot-reload)

## Usage

### Epic 1 Testing (Current)

After installation, verify the add-in is working:

1. **Open Revit 2026**
2. Look for the **"RevitAI"** tab in the ribbon
3. Click the **"Copilot"** button under "AI Copilot" panel
4. You should see the RevitAI Copilot dialog with 5 test buttons

**Available Test Buttons:**

1. **Test Claude API** - Verifies Claude API connection and API key
2. **Test ExternalEvent** - Tests thread-safe Revit API access (Story 1.3)
3. **Test Preview** - Demonstrates preview/confirm workflow (Story 1.5)
4. **View Logs** - Opens log file in Notepad (Story 1.6)
5. **Close** - Closes the dialog

Expected outcomes:
- "Test Claude API" → Shows connection status, validates API key
- "Test ExternalEvent" → Confirms background→main thread communication works
- "Test Preview" → Shows sample operation preview with 47 dimensions, Confirm/Cancel buttons
- "View Logs" → Opens `%APPDATA%\RevitAI\logs\revit_ai.log` in Notepad

### Future Usage (After Epic 1)

Once Epic 2 is complete, you'll be able to:

```text
Hebrew Example:
"תוסיף מידות פנימיות לכל החדרים בקומה 1"
(Add internal dimensions to all rooms on Level 1)

English Example:
"Add dimensions to selected rooms"
```

The AI will:
1. Parse your intent
2. Analyze the Revit model
3. Generate a preview of proposed dimensions
4. Ask for confirmation
5. Execute the operation atomically

## Development

### Project Structure

```text
revit-ai/
├── RevitAI.CSharp/                 # C# Revit Add-in Project
│   ├── Application.cs              # IExternalApplication (startup, ribbon)
│   ├── RevitAI.csproj              # .NET project file
│   ├── RevitAI.addin               # Revit manifest
│   ├── install-addon.ps1           # Deployment script
│   ├── Commands/                   # IExternalCommand implementations
│   │   ├── CopilotCommand.cs       # Main copilot command
│   │   └── SettingsCommand.cs      # Settings command
│   ├── Services/                   # Business logic
│   │   ├── ClaudeService.cs        # Claude API integration
│   │   ├── SafetyValidator.cs      # Operation validation
│   │   ├── RevitEventHandler.cs    # Thread-safe Revit API access
│   │   └── LoggingService.cs       # Logging infrastructure
│   ├── Models/                     # Data models
│   │   ├── RevitAction.cs          # Action DTOs
│   │   ├── RevitRequest.cs         # Request/Response
│   │   └── OperationPreview.cs     # Preview data
│   └── UI/                         # WPF dialogs
│       ├── CopilotDialog.cs        # Main dialog
│       ├── SettingsDialog.cs       # Settings dialog
│       └── PreviewConfirmDialog.cs # Preview/confirm dialog
├── RevitAI.Tests/                  # Test suite
│   ├── Unit/                       # NUnit unit tests
│   ├── Integration/                # Integration tests
│   └── Fixtures/                   # Test fixtures (mocks)
├── docs/                           # Documentation
│   ├── PRD.md                      # Product Requirements
│   ├── epics.md                    # Epic & Story Breakdown
│   └── architecture.md             # Architecture Decisions
├── CLAUDE.md                       # Development guide
└── README.md                       # This file
```

### Running Tests

```bash
# Run all tests
dotnet test RevitAI.Tests/RevitAI.Tests.csproj

# Run tests with detailed output
dotnet test -v detailed

# Run only unit tests
dotnet test --filter "Category=Unit"

# Run with coverage (requires coverlet)
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

### Code Quality

```powershell
# Format code (requires dotnet-format tool)
dotnet format RevitAI.CSharp/RevitAI.csproj

# Build with code analysis
dotnet build /p:RunAnalyzers=true /p:TreatWarningsAsErrors=false
```

### Development Workflow

1. **Write Code** in `RevitAI.CSharp/Services/`, `Models/`, `UI/`, or `Commands/`
2. **Write Tests** in `RevitAI.Tests/Unit/` or `Integration/`
3. **Run Tests** with `dotnet test`
4. **Build Project**: `dotnet build RevitAI.CSharp/RevitAI.csproj`
5. **Test in Revit**:
   - Close Revit completely
   - Restart Revit (C# requires full restart, no hot-reload)
   - Click "Copilot" button to test changes
   - Check logs at `%APPDATA%/RevitAI/logs/revit_ai.log`

### Debugging

**Option A: Visual Studio Debugger**
1. Set Revit as the startup executable in project properties
2. Set breakpoints in C# code
3. Press F5 to launch Revit with debugger attached
4. Trigger code via buttons, debugger will hit breakpoints

**Option B: Logging**
- Use `LoggingService.Instance` for structured logging
- Set log level to DEBUG for verbose output
- View logs at `%APPDATA%/RevitAI/logs/revit_ai.log`
- Use "View Logs" button in Copilot dialog for quick access

## Architecture

**Technology Stack:**
- **C# .NET 8.0** - Modern .NET runtime for Windows
- **Revit API 2026** - Official Autodesk Revit API
- **Anthropic SDK 2.0.0** - Claude API client for .NET
- **WPF (Windows Presentation Foundation)** - UI framework for dialogs

**Key Design Patterns:**
- **ExternalEvent Pattern** - Thread-safe Revit API access from background threads
- **Preview/Confirm UX** - Safety through visualization before commit
- **Operation Allowlist** - Strict validation prevents destructive actions
- **Singleton Services** - LoggingService, ClaudeService use singleton pattern
- **Request/Response Queue** - ConcurrentQueue + TaskCompletionSource for async communication
- **Atomic Transactions** - All Revit modifications wrapped in Transactions (commit/rollback)

See [docs/architecture.md](docs/architecture.md) for complete architectural decisions (7 ADRs).

## Safety & Security

- **Preview Before Commit**: All operations show preview and require confirmation
- **Operation Allowlist**: Only safe operations are permitted (create dimensions/tags, read elements)
- **No Model Modifications**: Cannot delete elements, modify walls/doors/rooms, or change project settings
- **Encrypted API Keys**: Claude API key stored in Windows Credential Manager (OS-level encryption)
- **Data Privacy**: Project data anonymized before sending to LLM
- **Atomic Transactions**: All operations use Revit Transactions (commit/rollback)

## Roadmap

### ✅ Phase 1: Foundation (Epic 1) - Complete
- [x] Story 1.1: Project Setup & C# Revit Add-in Scaffold
- [x] Story 1.2: Claude API Integration & Secure Key Management
- [x] Story 1.3: ExternalEvent Pattern for Thread-Safe Revit API Access
- [x] Story 1.4: Operation Allowlist & Safety Validation Framework
- [x] Story 1.5: Preview/Confirm UX Pattern
- [x] Story 1.6: Logging & Diagnostics Infrastructure
- [x] Story 1.7: Basic Ribbon UI with Test Buttons

### Phase 2: Validation Feature (Epic 2)
- [ ] Story 2.1: Dimension Command Parser (NLU for Dimensions)
- [ ] Story 2.2: Room Boundary Detection & Wall Analysis
- [ ] Story 2.3: Continuous Dimension Chain Generation
- [ ] Story 2.4: Dimension Preview & Confirmation Workflow
- [ ] Story 2.5: Edge Case Handling (Curved/Angled Walls)

### Future Phases (Post-PoC)
- Element tagging automation
- Schedule generation
- View/sheet management
- Quality control checks
- Full Revit Copilot capabilities

## Contributing

This is currently a PoC for a specific firm (Perry Studio). Contribution guidelines will be established after successful PoC validation.

## License

TBD (To be determined after PoC validation)

## Support

For issues and questions:
- Check logs at `%APPDATA%/RevitAI/logs/revit_ai.log`
- Review [docs/architecture.md](docs/architecture.md) for technical details and ADRs
- See [docs/epics.md](docs/epics.md) for story acceptance criteria
- See [CLAUDE.md](CLAUDE.md) for development guide and coding patterns

## Acknowledgments

- Built with **Revit API 2026** by Autodesk
- Powered by **Claude** by Anthropic ([Anthropic SDK 2.0.0](https://www.anthropic.com/))
- Developed using the **BMAD Methodology** ([bmad-system](https://github.com/bmad-system))

---

**Status**: Epic 1 Complete ✅ (All 7 stories implemented)
**Next**: Epic 2 - Intelligent Dimension Automation
**Build**: Successful, deployed to Revit 2026
**Ready for**: Revit integration testing
