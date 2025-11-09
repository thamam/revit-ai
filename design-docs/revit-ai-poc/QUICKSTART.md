# QUICK START: Test in 5 Minutes

## Option 1: Test Claude API First (Recommended)

**Without Revit** - Validate your API key works:

```bash
# 1. Edit test_claude_api.py
# 2. Add your API key: ANTHROPIC_API_KEY = "sk-ant-..."
# 3. Run test
python test_claude_api.py
```

**Expected output**: Claude's analysis of a sample wall schedule

**If this works**: Your API key is valid, proceed to build Revit plugin

**If this fails**: Check API key or network connection

---

## Option 2: Build & Test Full Plugin

### Prerequisites Checklist
- [ ] Revit 2024 installed
- [ ] Visual Studio 2019+ installed
- [ ] Claude API key obtained
- [ ] Sample Revit project with schedules

### Build Steps (5 min)

```bash
# 1. Update API key in ClaudeAPIClient.cs line 13
private static string API_KEY = "sk-ant-YOUR-KEY-HERE";

# 2. Open Visual Studio
# Open RevitAI.SmartSchedule.csproj

# 3. Restore NuGet packages
# Tools → NuGet Package Manager → Restore

# 4. Update Revit version if not 2024
# Edit .csproj RevitAPI paths

# 5. Build
# Build → Build Solution (Ctrl+Shift+B)
```

### Test in Revit (2 min)

```bash
# 1. Open Revit 2024
# 2. Open ANY project with schedules
# 3. Add-Ins → External Tools → "Smart Schedule"
# 4. See Claude's analysis
```

---

## What to Look For

### ✅ Success Indicators
- Plugin appears in Add-Ins menu
- Dialog shows schedule selection
- Claude provides intelligent analysis
- Analysis mentions specific issues in YOUR data

### ❌ Failure Indicators  
- Plugin not in menu → Check .addin file location
- "No schedules" error → Create a schedule first
- API error → Check API key
- No analysis → Check Claude response parsing

---

## First Test Cases

### Test 1: Simple Wall Schedule (2 min)
1. Create basic wall schedule
2. Add 5-10 walls
3. Run plugin
4. **Expected**: Claude identifies wall types, finds patterns

### Test 2: Missing Data (3 min)
1. Use existing schedule
2. Leave some cells blank
3. Run plugin  
4. **Expected**: Claude flags incomplete data

### Test 3: Real Project (5 min)
1. Open actual project
2. Run on production schedule
3. Compare Claude's suggestions to manual review
4. **Expected**: Claude catches real issues you'd find manually

---

## Troubleshooting

```bash
# Plugin doesn't show in Revit
Check: %AppData%\Autodesk\Revit\Addins\2024\
Files: RevitAI.SmartSchedule.dll
       RevitAI.SmartSchedule.addin

# Build errors
1. Restore NuGet packages
2. Check Revit API paths in .csproj
3. Verify .NET Framework 4.8 installed

# API errors
1. Test with test_claude_api.py first
2. Check API key format (starts with sk-ant-)
3. Verify network allows HTTPS to api.anthropic.com
```

---

## Success = You See This

```
┌─────────────────────────────────────────┐
│ Schedule Processing Results              │
├─────────────────────────────────────────┤
│ Processed: Wall Schedule                 │
│                                          │
│ Original rows: 12                        │
│                                          │
│ Summary:                                 │
│ This wall schedule shows 12 interior    │
│ partition walls primarily using Type 6. │
│                                          │
│ Data Quality Assessment:                 │
│ - ISSUE: 3 walls missing Comments       │
│ - ISSUE: Inconsistent mark numbering    │
│                                          │
│ Professional Formatting Suggestions:     │
│ 1. Standardize mark format              │
│ 2. Add Fire Rating column               │
│ 3. Group by type for clarity            │
│                                          │
│ [See full analysis...]                   │
└─────────────────────────────────────────┘
```

---

## Time Budget

| Task | Time | Cumulative |
|------|------|------------|
| Get API key | 2 min | 2 min |
| Configure project | 3 min | 5 min |
| Build in VS | 5 min | 10 min |
| First Revit test | 2 min | 12 min |
| Iterate & verify | 5 min | 17 min |

**Total: ~15-20 minutes to first working demo**

---

## Next: After First Success

1. **Document findings**: What worked, what didn't
2. **Test on 3+ schedule types**: Walls, doors, rooms
3. **Collect architect feedback**: Is analysis useful?
4. **Decision point**: Continue to tagging/dimensioning?

---

## Emergency Contacts

**If stuck**:
- Revit API: https://thebuildingcoder.typepad.com/
- Claude API: https://docs.anthropic.com/
- This project: See README.md for detailed troubleshooting
