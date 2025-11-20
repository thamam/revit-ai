# 🚀 Machine Handoff Checklist

**From:** Development Machine 1 (Windows)
**To:** Development Machine 2 (Windows)
**Date:** November 20, 2025
**Latest Commit:** `e379f5c` - Session documentation

---

## ✅ Pre-Transfer Checklist (Machine 1 - COMPLETE)

- [x] All code changes committed to git
- [x] Discovery macros tested and working
- [x] Test fixture analysis report generated
- [x] Session documentation created
- [x] Working tree clean (no uncommitted changes)
- [x] Ready to push to remote repository

---

## 📋 Setup Checklist (Machine 2 - TO DO)

### 1. Pull Latest Code
```bash
cd C:\Users\[username]\projects\revit-ai
git pull origin main

# Verify you have these commits:
git log --oneline -3
# Should show:
# e379f5c Add comprehensive session documentation for discovery phase handoff
# fd41556 Add Phase 1 discovery macros and update IDimensionFactory interface
# 7f4d772 Add discovery task prompt for guided Revit testing
```

### 2. Install Required Software
- [ ] Revit 2026 installed and licensed
- [ ] .NET 8.0 SDK installed
  - Verify: `dotnet --version` should show 8.0.x
- [ ] Visual Studio 2022 OR VS Code with C# extension
- [ ] Git configured with your credentials

### 3. Configure API Key
```powershell
# Set Claude API key (user-level)
setx CLAUDE_API_KEY "sk-ant-api03-YOUR_KEY_HERE"

# Verify
$env:CLAUDE_API_KEY
# Should display your key

# **IMPORTANT:** Restart Revit after setting this
```

### 4. Restore NuGet Packages
```powershell
cd C:\Users\[username]\projects\revit-ai
dotnet restore RevitAI.CSharp/RevitAI.csproj
```

### 5. Build Project
```powershell
dotnet build RevitAI.CSharp/RevitAI.csproj

# Build should succeed and auto-deploy to:
# %APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\
```

### 6. Transfer Test Project File
**IMPORTANT:** Test file NOT in git (excluded by .gitignore)

**Option A - Copy from Machine 1:**
```
Source: C:\Users\[username]\projects\revit-ai\test_rooms_light_2026.rvt
Destination: C:\Users\[username]\projects\revit-ai\test_rooms_light_2026.rvt
```

**Option B - Verify existing file on Machine 2:**
- [ ] File exists at expected location
- [ ] File opens in Revit 2026 without errors
- [ ] Contains 4 rooms (ב-32, ב-33, ב-34, ב-35)
- [ ] Level: "קומת קרקע - פיתוח"

### 7. Verify Add-in Installation
```powershell
# Check manifest exists
dir "%APPDATA%\Autodesk\Revit\Addins\2026\RevitAI.addin"

# Check DLL exists
dir "%APPDATA%\Autodesk\Revit\Addins\2026\RevitAI\RevitAI.dll"

# If DLL is blocked, unblock it:
# Right-click DLL → Properties → Check "Unblock" → Apply
```

### 8. Test in Revit
- [ ] Launch Revit 2026
- [ ] Open `test_rooms_light_2026.rvt`
- [ ] Verify RevitAI ribbon tab appears
- [ ] Click "RevitAI Copilot" button
- [ ] Copilot dialog opens successfully
- [ ] Click "Test Claude API" button → Should connect successfully

### 9. Review Documentation
- [ ] Read `docs/session-2025-11-20-discovery-phase.md` (full session report)
- [ ] Review discovery report at `%APPDATA%\RevitAI\discovery\test_rooms_analysis.md`
- [ ] Check `CLAUDE.md` for coding patterns and conventions
- [ ] Review `docs/epics.md` for Epic 2 story breakdown

---

## 📊 Expected State After Setup

### Git Status
```
On branch main
Your branch is up to date with 'origin/main'.
nothing to commit, working tree clean
```

### File Structure
```
RevitAI.CSharp/
├── Services/Interfaces/IDimensionFactory.cs  ← Updated (Architecture namespace added)
├── Tools/
│   ├── DiscoveryMacro_Application.cs         ← NEW (375 lines)
│   └── DiscoveryMacro_Application_Clean.cs   ← NEW (349 lines)
docs/
└── session-2025-11-20-discovery-phase.md     ← NEW (651 lines)
```

### Discovery Report Location
```
%APPDATA%\RevitAI\discovery\test_rooms_analysis.md

Contents:
- 4 rooms analyzed
- 45 dimension styles cataloged
- Test recommendations
- C# test constants
```

### Revit Add-in Deployment
```
%APPDATA%\Autodesk\Revit\Addins\2026\
├── RevitAI.addin                  ← Manifest
└── RevitAI\
    ├── RevitAI.dll               ← Main assembly
    ├── Anthropic.SDK.dll         ← Dependencies
    └── [other dependencies]
```

---

## 🎯 Next Development Tasks

### Immediate: Review Phase
1. Read session documentation: `docs/session-2025-11-20-discovery-phase.md`
2. Review discovery report: `%APPDATA%\RevitAI\discovery\test_rooms_analysis.md`
3. Open test project in Revit and verify room inventory
4. Test RevitAI add-in loads correctly

### Next: Epic 2 Story 2.1 - Dimension Command Parser
**Estimated Effort:** 4-6 hours

**Tasks:**
1. Extend Claude system prompt in `Services/ClaudeService.cs`
2. Add dimension properties to `Models/RevitAction.cs`
3. Write unit tests for Hebrew dimension parsing
4. Test with prompts:
   - Hebrew: "תוסיף מידות לחדר ב-35"
   - English: "Add dimensions to room ב-33"

**Acceptance Criteria:**
- Parse Hebrew/English dimension commands
- Extract room identifiers (numbers or names)
- Validate against operation allowlist
- Return structured `RevitAction` with dimension parameters

**Key Files:**
- `RevitAI.CSharp/Services/ClaudeService.cs`
- `RevitAI.CSharp/Models/RevitAction.cs`
- `RevitAI.CSharp/Tests/Unit/ClaudeServiceTests.cs`

---

## 🆘 Troubleshooting

### Issue: Build fails with missing references
**Solution:**
```powershell
dotnet restore RevitAI.CSharp/RevitAI.csproj
dotnet clean RevitAI.CSharp/RevitAI.csproj
dotnet build RevitAI.CSharp/RevitAI.csproj
```

### Issue: Add-in not loading in Revit
**Solutions:**
1. Verify DLL not blocked (Properties → Unblock)
2. Check manifest path: `%APPDATA%\Autodesk\Revit\Addins\2026\RevitAI.addin`
3. Restart Revit completely
4. Check Windows Event Viewer for .NET errors

### Issue: API key not working
**Solutions:**
1. Verify env var: `$env:CLAUDE_API_KEY` in PowerShell
2. Restart Revit after setting env var
3. Check API key format: `sk-ant-api03-...`
4. Test connection in Copilot dialog

### Issue: Test project file missing
**Solutions:**
1. Copy from Machine 1 (not in git)
2. Recreate rooms manually in Revit:
   - Create 4 rooms on level "קומת קרקע - פיתוח"
   - Room numbers: ב-32, ב-33, ב-34, ב-35
   - 2 rectangular, 1 U-shaped, 1 complex polygon
3. Run discovery macro to verify setup

---

## 📞 Contact & Resources

**Documentation:**
- Main README: `README.md`
- Development Guide: `CLAUDE.md`
- Architecture: `docs/architecture.md`
- Session Report: `docs/session-2025-11-20-discovery-phase.md`

**Git Repository:**
- Branch: `main`
- Latest commits: `e379f5c`, `fd41556`

**Test Data:**
- Discovery Report: `%APPDATA%\RevitAI\discovery\test_rooms_analysis.md`
- Test Project: `test_rooms_light_2026.rvt` (copy from Machine 1)

**Logs:**
- Location: `%APPDATA%\RevitAI\logs\revit_ai.log`
- View in dialog: Click "View Logs" button in Copilot

---

## ✅ Completion Checklist

### Machine 2 Setup Complete When:
- [ ] Git repository cloned/pulled with latest commits
- [ ] .NET 8.0 SDK installed and verified
- [ ] Revit 2026 installed and licensed
- [ ] Claude API key configured in environment variable
- [ ] NuGet packages restored successfully
- [ ] Project builds without errors
- [ ] Add-in loads in Revit (ribbon tab visible)
- [ ] Test project opens with 4 rooms visible
- [ ] Copilot dialog opens and API connection works
- [ ] Session documentation reviewed
- [ ] Discovery report reviewed
- [ ] Ready to start Story 2.1 development

---

**REMEMBER:**
- Always commit before ending work session
- Update session documentation for next handoff
- Test in Revit after each build (C# requires restart)
- Use discovery report test constants for integration tests

**Good luck with Epic 2 Story 2.1! 🚀**
