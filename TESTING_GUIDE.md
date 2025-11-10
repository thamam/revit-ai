# RevitAI - Testing Guide for Epic 1

**Status:** Epic 1 Complete - All 7 stories implemented
**Ready For:** Revit Integration Testing

---

## Epic 1 Summary

✅ **Story 1.1:** Project Setup & pyRevit Extension Scaffold
✅ **Story 1.2:** Claude API Integration & Secure Key Management
✅ **Story 1.3:** ExternalEvent Pattern for Thread-Safe Revit API Access
✅ **Story 1.4:** Operation Allowlist & Safety Validation Framework
✅ **Story 1.5:** Preview/Confirm UX Pattern
✅ **Story 1.6:** Logging & Diagnostics Infrastructure
✅ **Story 1.7:** Basic Ribbon UI with Text Input Dialog

All code is ready for Revit integration testing!

---

## Prerequisites for Testing

### 1. Revit Installation

- ✅ **Revit 2024** installed (latest from Autodesk website)
- Confirmed: You have this installed

### 2. pyRevit Installation

**Download and install pyRevit v5.1.0 or later:**

```bash
# Download from:
https://github.com/pyrevitlabs/pyRevit/releases/latest

# Run the installer
# Select: Install for all users (recommended)
# Revit 2024 should be auto-detected
```

**Verify installation:**
1. Launch Revit 2024
2. Look for "pyRevit" tab in ribbon
3. Click pyRevit → Settings to verify

### 3. Python Dependencies

**Install required Python packages:**

```bash
# Navigate to project directory
cd /home/thh3/personal/revit-ai

# Install dependencies
pip install -r requirements.txt

# This installs:
# - anthropic==0.72.0 (Claude API)
# - requests>=2.31.0
# - pyyaml>=6.0.1
# - keyring>=24.0.0 (API key storage)
```

### 4. Claude API Key

- ✅ **You have a Claude API key ready**
- Format: `sk-ant-...`
- Keep it secure - you'll configure it in Revit

---

## Installation Steps

### Step 1: Copy Extension to pyRevit

```bash
# Copy the RevitAI extension folder to pyRevit extensions directory
cp -r .extensions/RevitAI.extension %APPDATA%/pyRevit/Extensions/

# Alternative: Manual copy
# Source: /home/thh3/personal/revit-ai/.extensions/RevitAI.extension
# Destination: C:\Users\<YourUser>\AppData\Roaming\pyRevit\Extensions\
```

### Step 2: Create Configuration File

```bash
# Navigate to extension config folder
cd %APPDATA%/pyRevit/Extensions/RevitAI.extension/config

# Copy example config
copy firm_defaults.example.yaml firm_defaults.yaml

# Edit firm_defaults.yaml if needed (optional)
# Default settings work fine for testing
```

### Step 3: Reload pyRevit in Revit

1. Launch **Revit 2024**
2. Click **pyRevit** tab
3. Click **pyRevit** → **Reload**
4. Look for new **"RevitAI"** tab in ribbon

**Expected result:**
- New "RevitAI" tab appears
- "AI Copilot" panel visible
- Two buttons: "Copilot" and "Settings"

---

## Testing Checklist

### Test 1: Verify Extension Loads

**Steps:**
1. Open Revit 2024
2. Look for "RevitAI" tab in ribbon
3. Verify "AI Copilot" panel with two buttons

**Expected:**
✅ RevitAI tab visible
✅ Copilot button present
✅ Settings button present

**If failed:**
- Check pyRevit installation
- Reload pyRevit
- Check extension folder location

---

### Test 2: Configure API Key

**Steps:**
1. Click "Settings" button
2. Note "API Key: ✗ Not Configured"
3. Follow instructions to configure

**Option A: Environment Variable (Quick Test)**
```bash
# Windows Command Prompt
setx CLAUDE_API_KEY "sk-ant-your-key-here"

# Restart Revit for environment variable to take effect
```

**Option B: Python/Keyring (Recommended)**
```python
# From Python console or script
from lib.config_manager import get_config_manager
config = get_config_manager()
config.set_api_key("sk-ant-your-key-here")
```

**Expected:**
✅ API key stored securely
✅ Settings shows "API Key: ✓ Configured"

---

### Test 3: Test Claude API Connection

**Steps:**
1. Click "Settings" button
2. Click "Test API Connection"
3. Wait for response (should take 2-5 seconds)

**Expected Success:**
```
✓ Successfully connected to Claude API!

Model: claude-sonnet-4-20250514
Timeout: 10s
Max Retries: 3

Your API key is configured correctly.
```

**Expected Errors (and fixes):**

**Error:** "API key not configured"
→ Fix: Complete Test 2 first

**Error:** "Failed to connect to Claude API"
→ Fix: Check internet connection
→ Fix: Verify API key is valid
→ Fix: Check firewall settings

**Error:** "anthropic SDK not installed"
→ Fix: Run `pip install anthropic==0.72.0`

---

### Test 4: Open Copilot Dialog

**Steps:**
1. Open a Revit project (or start blank)
2. Click "Copilot" button in RevitAI tab
3. Read the welcome message

**Expected:**
```
AI Co-pilot for Revit

Welcome to RevitAI Co-pilot!

Epic 1 Complete: Foundation infrastructure ready
- ✓ Claude API Integration
- ✓ Safety validation (operation allowlist)
- ✓ Preview/confirm pattern
- ✓ Logging and diagnostics

Next: Epic 2 - Intelligent Dimension Automation

[Test Claude API Connection]
[View System Information]
[Close]
```

**If failed:**
- Check pyRevit console for errors
- Check logs: %APPDATA%/pyRevit/RevitAI/logs/revit_ai.log

---

### Test 5: View System Information

**Steps:**
1. From Copilot dialog, click "View System Information"
2. Review displayed information

**Expected:**
```
RevitAI System Information

Revit Context:
• Current View: [your view name]
• Levels: Level 1, Level 2, ...
• Rooms: [count]
• Walls: [count]
• Doors: [count]
• Selected: [count] elements

Configuration:
• API Key: ✓ Configured
• Model: claude-sonnet-4
• Language: en (or he)
• Max Elements: 500

Epic 1 Status:
✓ Project Setup Complete
✓ Claude API Integration
✓ ExternalEvent Pattern
✓ Operation Allowlist
✓ Preview/Confirm UX
✓ Logging Infrastructure
✓ Ribbon UI

Next Steps:
→ Implement Epic 2: Intelligent Dimension Automation
```

---

### Test 6: Verify Logging

**Steps:**
1. Open log directory: Settings → "View Logs"
2. Open `revit_ai.log` file
3. Verify log entries

**Expected log entries:**
```
2025-11-09 14:30:00 - RevitAI - INFO - RevitAI Co-pilot button clicked
2025-11-09 14:30:00 - RevitAI - INFO - Revit Version: 2024
2025-11-09 14:30:00 - RevitAI - INFO - Document: Project1
2025-11-09 14:30:05 - RevitAI - INFO - [START] test_claude_connection
2025-11-09 14:30:06 - RevitAI - INFO - [END] test_claude_connection | status=SUCCESS
```

**If no logs:**
- Check log directory exists: %APPDATA%/pyRevit/RevitAI/logs/
- Check write permissions
- Check logger initialization in code

---

### Test 7: Verify Safety Validator

**Steps (code test):**
1. Open pyRevit console: pyRevit → Developer → Python Shell
2. Run safety validator test:

```python
from lib.safety_validator import test_safety_validator
test_safety_validator()
```

**Expected output:**
```
Testing Safety Validator...
✓ Valid dimension operation accepted
✓ Blocked operation correctly rejected
✓ Large scope correctly rejected
✓ Unknown operation correctly rejected

Allowed operations: ['create_dimensions', 'create_tags', 'read_elements']
Blocked operations: ['close_project', 'delete_elements', 'export_data', ...]
```

---

## Troubleshooting

### Issue: RevitAI tab not appearing

**Possible causes:**
1. Extension not in correct folder
2. pyRevit not detecting extension
3. Folder naming incorrect

**Solutions:**
```bash
# Verify folder structure
dir %APPDATA%\pyRevit\Extensions\RevitAI.extension

# Should show:
# RevitAI.tab/
# lib/
# config/

# Reload pyRevit
# Revit → pyRevit → Reload

# Check pyRevit console for errors
# Revit → pyRevit → Developer → pyRevit Console
```

### Issue: Import errors in Python

**Error:** `ModuleNotFoundError: No module named 'anthropic'`

**Solution:**
```bash
# Install dependencies
pip install -r requirements.txt

# Verify installation
pip list | grep anthropic
# Should show: anthropic 0.72.0

# Check Python path
# In pyRevit console:
import sys
print("\n".join(sys.path))
```

### Issue: API key not storing

**Error:** `keyring library not available`

**Solution:**
```bash
# Install keyring
pip install keyring

# Or use environment variable instead
setx CLAUDE_API_KEY "sk-ant-..."
```

### Issue: Logs not appearing

**Check:**
1. Log directory exists: `%APPDATA%/pyRevit/RevitAI/logs/`
2. Write permissions
3. Logger initialization

**Create logs manually:**
```python
from lib.logger import test_logger
test_logger()
```

---

## Next Steps After Testing

Once all tests pass:

### 1. Commit Epic 1 Completion

```bash
git add .
git commit -m "Epic 1 Complete: Foundation & Core Infrastructure

All 7 stories implemented and tested:
- Project setup & pyRevit scaffold
- Claude API integration with secure key management
- ExternalEvent pattern for thread-safe Revit API
- Operation allowlist & safety validation
- Preview/confirm UX pattern
- Logging & diagnostics infrastructure
- Basic ribbon UI with dialogs

Ready for Epic 2: Intelligent Dimension Automation"
```

### 2. Start Epic 2

Epic 2 stories:
- Story 2.1: Dimension Command Parser (NLU for Dimensions)
- Story 2.2: Room Boundary Detection & Wall Analysis
- Story 2.3: Continuous Dimension Chain Generation
- Story 2.4: Dimension Preview & Confirmation Workflow
- Story 2.5: Edge Case Handling

### 3. Demo to Perry

Show:
1. Extension loading in Revit
2. API connection test
3. System information display
4. Logs and configuration
5. Explain architecture and safety features

Discuss:
- Timeline for Epic 2 (dimension automation)
- Any customization needs for firm
- Hebrew language UI priorities

---

## Support

**Logs location:** `%APPDATA%/pyRevit/RevitAI/logs/revit_ai.log`

**Configuration:** `%APPDATA%/pyRevit/Extensions/RevitAI.extension/config/firm_defaults.yaml`

**Architecture doc:** `docs/architecture.md`

**Epic breakdown:** `docs/epics.md`

---

**Status:** Ready for Revit Integration Testing ✅
**Next:** Test all checklist items above, then proceed to Epic 2
