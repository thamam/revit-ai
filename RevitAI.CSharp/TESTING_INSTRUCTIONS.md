# RevitAI C# SDK - Testing Instructions

**For Agent on Revit Machine**

This guide provides step-by-step instructions to build, install, and test the RevitAI C# SDK implementation on a machine with Revit installed.

## Prerequisites Check

Before starting, verify your environment has these installed:

### Required Software

1. **Visual Studio 2022** (Community, Professional, or Enterprise)
   - Download: https://visualstudio.microsoft.com/downloads/
   - Required workload: ".NET desktop development"

2. **Revit 2024**
   - Ensure Revit 2024 is installed and licensed
   - Note: If using a different Revit version, update `.csproj` target version

3. **.NET Framework 4.8 SDK**
   - Usually included with Visual Studio 2022
   - Verify: Run `dotnet --info` and check for .NET Framework 4.8

4. **Claude API Key**
   - Get from: https://console.anthropic.com/
   - Required for testing Claude API integration

### Verify Prerequisites

```bash
# Check .NET Framework
dotnet --info

# Check Visual Studio installation
where msbuild

# Check Revit installation
dir "C:\Program Files\Autodesk\Revit 2024\Revit.exe"
```

## Step 1: Clone and Navigate to Project

```bash
# Clone repository (if not already cloned)
git clone https://github.com/thamam/revit-ai.git
cd revit-ai

# Switch to C# SDK branch
git checkout csharp-sdk-implementation

# Navigate to C# project
cd RevitAI.CSharp
```

## Step 2: Build the Project

### Option A: Build with dotnet CLI (Recommended)

```bash
# Restore NuGet packages
dotnet restore RevitAI.csproj

# Build in Debug mode
dotnet build RevitAI.csproj --configuration Debug

# Expected output:
# Build succeeded.
# 0 Warning(s)
# 0 Error(s)
```

### Option B: Build with Visual Studio

```bash
# Open project in Visual Studio
start RevitAI.csproj

# In Visual Studio:
# - Press Ctrl+Shift+B to build
# - Or: Build → Build Solution
```

### Verify Build Output

Check that the DLL was created:

```bash
# Debug build output
dir bin\Debug\net48\RevitAI.dll

# Should show RevitAI.dll with recent timestamp
```

## Step 3: Verify Auto-Deployment

The build process automatically copies files to Revit's Add-ins folder.

### Check Deployment Locations

```bash
# Check DLL location
dir "%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\RevitAI.dll"

# Check manifest location
dir "%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI.addin"

# Expected: Both files should exist with recent timestamps
```

### Manual Deployment (if auto-deploy failed)

If files are missing, copy manually:

```bash
# Create directory if it doesn't exist
mkdir "%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI"

# Copy DLL
copy "bin\Debug\net48\RevitAI.dll" "%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\"

# Copy manifest
copy "RevitAI.addin" "%APPDATA%\Autodesk\Revit\Addins\2024\"
```

## Step 4: Configure API Key

Set the Claude API key as an environment variable:

```cmd
# Set API key (replace with your actual key)
setx CLAUDE_API_KEY "sk-ant-your-api-key-here"

# Output should show: SUCCESS: Specified value was saved.
```

**IMPORTANT:** You must restart any open Command Prompt windows and Revit for the environment variable to take effect.

### Verify API Key

```cmd
# In a NEW Command Prompt window
echo %CLAUDE_API_KEY%

# Should display your API key (starting with sk-ant-)
```

## Step 5: Launch Revit and Load Add-in

### Start Revit

1. Close any running Revit instances
2. Launch Revit 2024
3. Open any project (or create a new one)

### Verify Add-in Loaded

Check for the RevitAI ribbon tab:

1. Look at the top ribbon in Revit
2. You should see a new tab called **"RevitAI"**
3. Click on the RevitAI tab
4. You should see a panel called **"AI Copilot"** with two buttons:
   - **Copilot** button
   - **Settings** button

### Troubleshooting Add-in Not Loading

If the RevitAI tab doesn't appear:

1. **Check Revit Journal file** for error messages:
   ```
   %LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\
   ```
   Open the most recent journal file and search for "RevitAI" or "error"

2. **Verify manifest path**:
   ```bash
   dir "%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI.addin"
   ```

3. **Verify DLL path in manifest**:
   Open `RevitAI.addin` in a text editor and check the Assembly path matches:
   ```xml
   <Assembly>%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\RevitAI.dll</Assembly>
   ```

4. **Check for missing dependencies**:
   - Ensure `Anthropic.SDK` NuGet package was restored
   - Check `bin\Debug\net48\` folder for all DLLs

## Step 6: Test Settings Dialog

### Open Settings

1. Click the **RevitAI** tab in Revit ribbon
2. Click the **Settings** button

### Expected Behavior

A dialog should appear showing:

```
RevitAI Settings - C# SDK Implementation

**Current Configuration:**

API Key: ✓ Configured (or ✗ Not Configured)
Model: claude-sonnet-4-20250514
Framework: Revit C# SDK
Status: Epic 1 Complete

**To Configure API Key:**
...
```

### Verify Settings Dialog

- [ ] Dialog opens without errors
- [ ] API Key status shows "✓ Configured" (if you set the environment variable)
- [ ] "Open Documentation" button is clickable
- [ ] "Close" button closes the dialog

### Test Documentation Link

1. Click "Open Documentation" button
2. Should open browser to: https://github.com/thamam/revit-ai

## Step 7: Test Copilot Dialog

### Open Copilot

1. Click the **RevitAI** tab in Revit ribbon
2. Click the **Copilot** button

### Expected Behavior

A dialog should appear showing:

```
RevitAI Copilot - C# SDK Implementation

Epic 1: Foundation & Core Infrastructure ✓

This is the C# SDK version of RevitAI.
Built with Revit's official .NET API for maximum stability.

Status: Ready for Epic 2 implementation

[Natural language commands will go here...]
(Epic 2 feature)

[Test Claude API] [Close]
```

### Verify Copilot Dialog

- [ ] Dialog opens without errors
- [ ] Status text displays correctly
- [ ] Prompt textbox is disabled (Epic 2 feature)
- [ ] "Test Claude API" button is enabled
- [ ] "Close" button works

## Step 8: Test Claude API Connection

This is the most critical test - verifies the entire integration works.

### Prerequisites

- API key must be set (Step 4)
- Internet connection required

### Run API Test

1. In the Copilot dialog, click **"Test Claude API"** button
2. Wait 2-5 seconds for the API call to complete

### Expected Success Behavior

Status text should update to:

```
✓ Claude API connection successful!

API key is valid and service is reachable.
Ready for Epic 2 implementation.
```

### Expected Failure Scenarios

**If API key not set:**
```
Dialog appears: "API Key Missing"
Message: "Please set CLAUDE_API_KEY environment variable."
```

**If internet connection fails:**
```
✗ Claude API connection failed.

Check your API key and internet connection.
```

**If API key is invalid:**
```
✗ Claude API connection failed.

Check your API key and internet connection.
```

### Troubleshooting API Connection

If the test fails:

1. **Verify API key is set**:
   ```cmd
   echo %CLAUDE_API_KEY%
   ```

2. **Test internet connectivity**:
   ```cmd
   ping api.anthropic.com
   ```

3. **Verify API key validity**:
   - Log in to https://console.anthropic.com/
   - Check that your API key is active
   - Verify you have API credits

4. **Check Revit Journal for errors**:
   - Look for exception messages related to "Anthropic" or "HttpClient"

## Step 9: Verification Checklist

Complete this checklist to verify the installation:

### Build & Installation
- [ ] Project builds without errors
- [ ] RevitAI.dll exists in `%APPDATA%\Autodesk\Revit\Addins\2024\RevitAI\`
- [ ] RevitAI.addin exists in `%APPDATA%\Autodesk\Revit\Addins\2024\`
- [ ] API key environment variable is set

### Revit Loading
- [ ] Revit starts without errors
- [ ] RevitAI tab appears in ribbon
- [ ] AI Copilot panel shows two buttons

### Settings Dialog
- [ ] Settings dialog opens
- [ ] API Key status shows "Configured"
- [ ] Documentation link works
- [ ] Dialog closes properly

### Copilot Dialog
- [ ] Copilot dialog opens
- [ ] Epic 1 status message displays
- [ ] Test Claude API button is functional

### Claude API Integration
- [ ] Test Claude API returns success
- [ ] Status updates to show connection success
- [ ] No errors in Revit journal

### Overall Stability
- [ ] No crashes or errors during testing
- [ ] Revit remains stable after dialog operations
- [ ] Can repeat all tests multiple times

## Step 10: Debug Mode (Optional)

For detailed troubleshooting, enable debug logging.

### Enable Debug Logging in C#

Edit `RevitAI.CSharp/Services/ClaudeService.cs` and add logging:

```csharp
// Add at the top
using System.Diagnostics;

// In TestConnectionAsync method, add:
Debug.WriteLine("Testing Claude API connection...");
Debug.WriteLine($"API Key: {_apiKey.Substring(0, 10)}...");
```

### View Debug Output

1. Open Visual Studio
2. Tools → Options → Debugging → Output Window
3. Enable "Debug Output"
4. Set Revit as startup program (Project Properties → Debug)
5. Press F5 to launch Revit with debugger attached
6. View debug output in Visual Studio's Output window

## Expected Results Summary

After completing all steps, you should have:

1. ✅ **Built the C# SDK project** without errors
2. ✅ **Deployed to Revit's Add-ins folder** automatically
3. ✅ **Configured Claude API key** as environment variable
4. ✅ **Loaded RevitAI add-in** in Revit 2024
5. ✅ **Opened Settings dialog** and verified configuration
6. ✅ **Opened Copilot dialog** and tested UI
7. ✅ **Successfully connected to Claude API** via Test button
8. ✅ **Verified stability** - no crashes or errors

## What This Tests

This testing verifies **Epic 1: Foundation & Core Infrastructure** is complete:

- ✅ C# Revit add-in loads and creates ribbon UI
- ✅ Claude API integration works (async/await pattern)
- ✅ Safety validator compiles and is available
- ✅ Data models (RevitAction, RevitContext) are defined
- ✅ WPF dialogs display correctly
- ✅ API key management via environment variable works
- ✅ Overall architecture is stable and ready for Epic 2

## Next Steps After Testing

Once all tests pass, the system is ready for:

**Epic 2: GUI Implementation (Intelligent Dimension Automation)**
- Natural language command parser
- Room boundary detection
- Dimension chain generation
- Preview & confirmation workflow

**Epic 3: CLI Automation (Revit Batch Processor)**
- Batch script integration
- Scheduled operations
- Unattended processing

## Support

If you encounter issues:

1. **Check Revit Journal file** first: `%LOCALAPPDATA%\Autodesk\Revit\Autodesk Revit 2024\Journals\`
2. **Review build output** for warnings or errors
3. **Verify all prerequisites** are installed
4. **Check GitHub Issues**: https://github.com/thamam/revit-ai/issues

## Testing Complete

If all steps pass, report:

```
✅ RevitAI C# SDK Implementation - Testing Complete

Epic 1: Foundation & Core Infrastructure - VERIFIED

- Build: Success
- Installation: Success
- Revit Loading: Success
- Settings Dialog: Success
- Copilot Dialog: Success
- Claude API Test: Success
- Stability: No crashes or errors

Status: Ready for Epic 2 (GUI Implementation)
```
