# RevitAI - Natural Language AI Co-pilot for Revit

**"Vibe coding" for architecture** - Command Revit in plain English (and Hebrew) using AI-powered natural language automation.

## Overview

RevitAI is a pyRevit extension that enables architects to automate Revit tasks through natural language commands. Instead of hunting through menus and clicking through dialogs, you describe what you want in plain language, and the AI translates your intent into precise Revit operations.

**Current Status:** PoC - Epic 1 (Foundation) in progress

### What Makes This Special

- **Natural Language Interface**: Command Revit in Hebrew or English
- **Preview Before Commit**: See exactly what will happen before any changes are made
- **Safety First**: Operation allowlist prevents destructive actions
- **Professional Quality**: AI makes contextual decisions like an experienced architect

### PoC Scope

**Epic 1: Foundation & Core Infrastructure** ✓ *In Progress*
- pyRevit extension structure
- Claude API integration
- ExternalEvent pattern for thread-safe Revit API access
- Preview/confirm UX pattern
- Operation allowlist and safety validation
- Logging and diagnostics

**Epic 2: Intelligent Dimension Automation** (After Epic 1)
- Natural language dimension commands
- Room boundary detection
- Continuous dimension chain generation
- Preview & confirmation workflow

## Prerequisites

- **Revit 2022 or 2023** (tested versions)
- **pyRevit v5.1.0+** ([Download here](https://github.com/pyrevitlabs/pyRevit/releases))
- **Python 3.8+** (provided by pyRevit)
- **Claude API Key** from Anthropic ([Get one here](https://console.anthropic.com/))

## Installation

### 1. Install pyRevit

Download and install pyRevit from the [official releases](https://github.com/pyrevitlabs/pyRevit/releases):

```bash
# Download pyRevit installer
# Run the installer (supports Revit 2022/2023/2024/2025)
```

### 2. Clone or Copy Extension

**Option A: Git Clone** (for developers)
```bash
cd %APPDATA%/pyRevit/Extensions
git clone https://github.com/<your-repo>/revit-ai.git
```

**Option B: Manual Copy** (for end users)
```bash
# Copy the .extensions/RevitAI.extension/ folder to:
%APPDATA%/pyRevit/Extensions/RevitAI.extension/
```

### 3. Install Python Dependencies

```bash
# Navigate to project directory
cd revit-ai

# Install dependencies using pyRevit's Python
pip install -r requirements.txt
```

### 4. Configure Firm Settings (Optional)

Copy the example configuration and customize for your firm:

```bash
cp .extensions/RevitAI.extension/config/firm_defaults.example.yaml
   .extensions/RevitAI.extension/config/firm_defaults.yaml
```

Edit `firm_defaults.yaml` with your firm's preferences:
- Language (Hebrew/English)
- Dimension offsets and styles
- Tag families

### 5. Reload pyRevit

In Revit:
- pyRevit automatically discovers the extension
- Or manually reload: pyRevit → Extensions → Reload

### 6. Configure API Key

On first launch:
- Click "RevitAI" tab in Revit ribbon (should appear after reload)
- Click "Settings" button (if available)
- Enter your Claude API key (stored encrypted in Windows Credential Manager)

## Usage

### Hello World (Story 1.1)

After installation, verify the extension is working:

1. Open Revit
2. Look for the **"RevitAI"** tab in the ribbon
3. Click the **"Copilot"** button under "AI Copilot" panel
4. You should see a "Hello World" dialog confirming the extension loaded correctly

Expected output:
- Dialog showing Revit version, pyRevit version, Python version
- Confirmation that the extension is running
- Next steps for development

### Future Usage (After Epic 1)

Once Epic 2 is complete, you'll be able to:

```
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

```
revit-ai/
├── .extensions/
│   └── RevitAI.extension/          # pyRevit extension
│       ├── lib/                    # Shared Python modules
│       ├── RevitAI.tab/            # Ribbon tab
│       │   └── AI Copilot.panel/   # Ribbon panel
│       │       └── Copilot.pushbutton/  # Button
│       │           └── script.py   # Entry point
│       └── config/                 # Configuration files
├── tests/                          # Test suite
│   ├── unit/                       # Unit tests
│   ├── integration/                # Integration tests
│   └── fixtures/                   # Test fixtures (mocks)
├── docs/                           # Documentation
│   ├── PRD.md                      # Product Requirements
│   ├── epics.md                    # Epic & Story Breakdown
│   └── architecture.md             # Architecture Decisions
├── requirements.txt                # Python dependencies
└── README.md                       # This file
```

### Running Tests

```bash
# Install development dependencies
pip install pytest pytest-cov black pylint

# Run unit tests
pytest tests/unit/

# Run all tests with coverage
pytest tests/ --cov=lib --cov-report=html

# Format code
black .

# Lint code
pylint lib/
```

### Development Workflow

1. **Write Code** in `.extensions/RevitAI.extension/lib/`
2. **Write Tests** in `tests/`
3. **Run Tests** with `pytest`
4. **Test in Revit**:
   - pyRevit reloads extension automatically
   - Or manually reload from pyRevit menu
   - Click button to test changes
   - Check logs at `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

### Debugging

- Use `print()` statements (output appears in pyRevit console)
- Enable verbose logging in `firm_defaults.yaml`: `log_level: DEBUG`
- View logs at `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

## Architecture

Built using:
- **pyRevit v5.1.0+** - Python execution environment within Revit
- **Claude API (Anthropic SDK 0.72.0)** - Natural language understanding
- **Revit API 2022/2023** - Revit automation
- **Python 3.8+** (IronPython via pyRevit)

Key Patterns:
- **ExternalEvent Pattern** - Thread-safe Revit API access
- **Preview/Confirm UX** - Safety through visualization
- **Operation Allowlist** - Prevents destructive actions
- **DirectContext3D** - Hardware-accelerated preview graphics

See [docs/architecture.md](docs/architecture.md) for complete architectural decisions.

## Safety & Security

- **Preview Before Commit**: All operations show preview and require confirmation
- **Operation Allowlist**: Only safe operations are permitted (create dimensions/tags, read elements)
- **No Model Modifications**: Cannot delete elements, modify walls/doors/rooms, or change project settings
- **Encrypted API Keys**: Claude API key stored in Windows Credential Manager (OS-level encryption)
- **Data Privacy**: Project data anonymized before sending to LLM
- **Atomic Transactions**: All operations use Revit Transactions (commit/rollback)

## Roadmap

### ✓ Phase 1: Foundation (Epic 1)
- [x] Story 1.1: Project Setup & pyRevit Extension Scaffold
- [ ] Story 1.2: Claude API Integration & Secure Key Management
- [ ] Story 1.3: ExternalEvent Pattern for Thread-Safe Revit API Access
- [ ] Story 1.4: Operation Allowlist & Safety Validation Framework
- [ ] Story 1.5: Preview/Confirm UX Pattern
- [ ] Story 1.6: Logging & Diagnostics Infrastructure
- [ ] Story 1.7: Basic Ribbon UI with Text Input Dialog

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
- Full Revit co-pilot capabilities

## Contributing

This is currently a PoC for a specific firm (Perry Studio). Contribution guidelines will be established after successful PoC validation.

## License

TBD (To be determined after PoC validation)

## Support

For issues and questions:
- Check logs at `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`
- Review [docs/architecture.md](docs/architecture.md) for technical details
- See [docs/epics.md](docs/epics.md) for story acceptance criteria

## Acknowledgments

- Built with [pyRevit](https://github.com/pyrevitlabs/pyRevit) by Ehsan Iran-Nejad
- Powered by [Claude](https://www.anthropic.com/claude) by Anthropic
- Developed using the [BMAD Methodology](https://github.com/bmad-system)

---

**Status**: Epic 1, Story 1.1 Complete ✓
**Next**: Story 1.2 - Claude API Integration
**Target**: Working PoC by Nov 10, 2025
