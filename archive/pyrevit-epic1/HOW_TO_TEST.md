# How to Test RevitAI C# Feature

## Setup Complete!

The RevitAI C# add-in has been successfully:
- ✅ Updated to support Revit 2026 (.NET 8.0)
- ✅ Built with all dependencies
- ✅ Installed to Revit's add-ins folder

## Location of Files

**Add-in DLL**: `C:\Users\nelly\AppData\Roaming\Autodesk\Revit\Addins\2026\RevitAI\RevitAI.dll`
**Manifest**: `C:\Users\nelly\AppData\Roaming\Autodesk\Revit\Addins\2026\RevitAI.addin`

## How to Test in Revit 2026

### Step 1: Start Revit 2026
1. Launch Revit 2026
2. Open any project or create a new project

### Step 2: Look for the RevitAI Ribbon Tab
Once Revit loads, you should see a new ribbon tab called **"RevitAI"** in the top menu bar.

If you don't see it:
- Check if Revit showed any error messages on startup
- Go to Revit's Add-Ins tab → External Tools to see if RevitAI loaded

### Step 3: Test the Features

The RevitAI ribbon tab should have two buttons:

#### Button 1: "Copilot"
Click this button to open the Copilot dialog.

**What you'll see:**
- A window titled "RevitAI Copilot"
- Status text showing "Status: Ready for Epic 2 implementation"
- A "Test Claude API" button
- A disabled text box (will be enabled in Epic 2)

**What to test:**
1. Click "Test Claude API" button
   - If you have CLAUDE_API_KEY environment variable set, it will test the connection
   - You should see a message indicating success or failure
   - If successful, it means the Claude AI integration is working

#### Button 2: "Settings"
Click this button to open the Settings dialog.

**What you'll see:**
- A window titled "RevitAI Settings"
- Your Claude API configuration
- Model being used: `claude-sonnet-4-20250514`
- Link to GitHub repository

## Setting Up Claude API Key (Optional)

To test the Claude API integration:

1. Get a Claude API key from https://console.anthropic.com/
2. Set it as an environment variable:
   ```
   setx CLAUDE_API_KEY "sk-ant-your-key-here"
   ```
3. **Restart Revit** (environment variables are loaded at startup)
4. Try the "Test Claude API" button again

## What Works Now (Epic 1 - Foundation)

- ✅ Revit ribbon integration
- ✅ UI dialogs (Copilot & Settings)
- ✅ Claude API connection testing
- ✅ Safety validation system
- ✅ .NET 8.0 / Revit 2026 support

## What's Coming Next (Epic 2)

- Natural language command input
- AI-powered command parsing
- Actual Revit operations (dimensions, tags, element queries)

## Troubleshooting

### RevitAI tab doesn't appear
- Check `C:\Users\nelly\AppData\Roaming\Autodesk\Revit\Addins\2026\` for the RevitAI.addin file
- Look for error messages when Revit starts
- Check Revit's Journal file for loading errors

### "Test Claude API" always fails
- Make sure CLAUDE_API_KEY environment variable is set
- Restart Revit after setting the environment variable
- Check that your API key is valid and active

### Buttons don't do anything
- Check the Revit status bar for error messages
- Try restarting Revit

## Rebuilding the Add-in

If you make changes to the code:

```powershell
cd C:\Users\nelly\Documents\projects\revit-ai\RevitAI.CSharp
dotnet build --configuration Release
.\install-addon.ps1
```

Then restart Revit to load the new version.

## Success!

You now have a working RevitAI add-in for Revit 2026! 🎉

The foundation is complete and ready for Epic 2 development where natural language commands and AI-powered operations will be implemented.
