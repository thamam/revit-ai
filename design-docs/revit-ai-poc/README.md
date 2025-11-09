# Revit AI - Smart Schedule PoC

**Technical Validation**: Extract Revit schedule data → Send to Claude API → Get intelligent formatting feedback

## What This Does

1. **Extracts** all schedules from current Revit document
2. **Converts** schedule data to JSON
3. **Sends** to Claude API with intelligent prompting
4. **Receives** analysis and formatting suggestions
5. **Displays** results in Revit UI

This proves: **LLM can understand Revit schedule context and provide professional-quality feedback**

---

## Prerequisites

- **Revit 2024** (adjust version in .csproj if different)
- **Visual Studio 2019+** (with .NET Framework 4.8)
- **Claude API Key** from Anthropic
- **NuGet Package Manager**

---

## Setup Instructions

### 1. Get Claude API Key

1. Go to https://console.anthropic.com/
2. Create account / Sign in
3. Get API key from Settings
4. Copy the key (starts with `sk-ant-...`)

### 2. Configure Project

**Update API Key** in `ClaudeAPIClient.cs`:
```csharp
private static string API_KEY = "sk-ant-your-key-here";
```

**Update Revit Version** in `.csproj`:
```xml
<!-- Change these paths to match your Revit installation -->
<Reference Include="RevitAPI">
  <HintPath>C:\Program Files\Autodesk\Revit 2024\RevitAPI.dll</HintPath>
</Reference>
```

### 3. Build Plugin

```bash
# Open Visual Studio
# File → Open → Project/Solution → RevitAI.SmartSchedule.csproj

# Restore NuGet packages
Tools → NuGet Package Manager → Restore

# Build
Build → Build Solution (Ctrl+Shift+B)
```

Build output will auto-copy to:
```
%AppData%\Autodesk\Revit\Addins\2024\
```

### 4. Test in Revit

1. **Open Revit 2024**
2. **Open a project** with existing schedules (walls, doors, rooms, etc.)
3. **Go to**: Add-Ins tab → External Tools
4. **Click**: "Smart Schedule"
5. **Result**: Dialog shows Claude's analysis

---

## Project Structure

```
revit-ai-poc/
├── SmartScheduleCommand.cs       # Main Revit command
├── ScheduleExtractor.cs           # Extract schedule data from Revit
├── ClaudeAPIClient.cs             # Claude API integration
├── RevitAI.SmartSchedule.csproj   # Visual Studio project
├── RevitAI.SmartSchedule.addin    # Revit manifest
├── packages.config                # NuGet dependencies
└── README.md                      # This file
```

---

## How It Works

### Step 1: Extract Schedule Data

```csharp
// Get all schedules in document
FilteredElementCollector collector = new FilteredElementCollector(doc);
var schedules = collector.OfClass(typeof(ViewSchedule)).ToElements();

// Extract table data
TableData tableData = viewSchedule.GetTableData();
TableSectionData section = tableData.GetSectionData(SectionType.Body);

// Read cell by cell
for (int row = 0; row < numRows; row++)
{
    for (int col = 0; col < numCols; col++)
    {
        string cellText = viewSchedule.GetCellText(SectionType.Body, row, col);
    }
}
```

### Step 2: Send to Claude

```csharp
// Build prompt with schedule context
string prompt = @"
You are an expert in Revit scheduling.
Analyze this schedule data: {JSON}
Provide: Summary, Quality Assessment, Formatting Suggestions
";

// Call Claude API
HttpClient client = new HttpClient();
var response = await client.PostAsync(
    "https://api.anthropic.com/v1/messages",
    requestBody
);
```

### Step 3: Display Results

```csharp
TaskDialog td = new TaskDialog("Results")
{
    MainContent = claudeResponse
};
td.Show();
```

---

## Testing Plan

### Test 1: Basic Extraction
**Goal**: Verify API can read schedule data

1. Open Revit with sample project
2. Create simple door schedule
3. Run plugin
4. **Success**: Dialog shows door data

### Test 2: Claude Integration
**Goal**: Verify Claude receives and analyzes data

1. Check API key is valid
2. Run on wall schedule
3. **Success**: Claude provides analysis

### Test 3: Multiple Schedule Types
**Goal**: Test different schedule categories

Test on:
- [ ] Wall Schedule
- [ ] Door Schedule
- [ ] Room Schedule
- [ ] Window Schedule
- [ ] Material Takeoff

### Test 4: Data Quality
**Goal**: Claude identifies real issues

Create schedule with:
- Missing data (empty cells)
- Inconsistent naming
- **Success**: Claude flags these issues

---

## Expected Output Example

```
Schedule Processing Results

Processed: Wall Schedule

Summary:
This is a wall schedule showing 12 interior partition walls. 
All walls are Type 6 (likely 6" thick).

Data Quality Assessment:
- ISSUE: 3 walls have blank "Comments" field
- ISSUE: Wall marks use inconsistent format (W-01 vs W1)
- GOOD: All walls have type assignments

Professional Formatting Suggestions:
1. Standardize mark format to "W-01" pattern
2. Add column for Fire Rating if required by code
3. Group walls by type for better readability
4. Consider adding "Base Finish" column

Insights:
- All walls use same type - consider if variety needed
- Mark sequence has gap (W-08 missing) - intentional?
```

---

## Next Steps After PoC

### If This Works:
1. **Improve UI**: WPF dialog for schedule selection
2. **Add Writing**: Actually modify schedules in Revit
3. **Expand Features**: Tagging and dimensioning
4. **MCP Integration**: External AI chat interface

### If This Partially Works:
1. **Refine Prompts**: Better instructions to Claude
2. **Add Examples**: Few-shot learning with good schedules
3. **Domain Knowledge**: Encode architectural standards

### If This Fails:
1. **API Issues**: Check Claude connectivity
2. **Data Format**: Validate JSON structure
3. **Revit Version**: Check API compatibility

---

## Common Issues

### "Could not load file or assembly 'RevitAPI'"
**Fix**: Update .csproj paths to match your Revit installation

### "API Key Invalid"
**Fix**: Get new key from https://console.anthropic.com/

### "No schedules found"
**Fix**: Create at least one schedule in Revit first

### "Plugin doesn't appear in Revit"
**Fix**: Check .addin file copied to `%AppData%\Autodesk\Revit\Addins\2024\`

---

## Key Metrics (From Brief)

- [ ] **API Capability**: Can extract schedule data? 
- [ ] **LLM Effectiveness**: Does Claude add value vs simple formatting?
- [ ] **Accuracy**: Does analysis meet professional standards?
- [ ] **Development Velocity**: Built in < 2 weeks?

---

## Client Success Criteria (To Define)

Schedule must have:
- [ ] _Completeness criteria from architect_
- [ ] _Formatting standards from firm_
- [ ] _Accuracy requirements_
- [ ] _"Good enough to use" threshold_

---

## Technical Notes

### Revit API Limitations Found
- Can read schedule data cell-by-cell ✓
- Cannot directly modify schedule cells (need Transaction)
- Export to CSV also possible as alternative

### LLM Considerations
- Context window: ~200K tokens (plenty for schedules)
- Response time: 2-5 seconds typical
- Cost: ~$0.01 per schedule analysis

### Security
- API key should move to config file
- Consider environment variables
- Don't commit keys to git

---

## Resources

**Revit API**:
- [The Building Coder - Schedule API](https://thebuildingcoder.typepad.com/blog/2012/05/the-schedule-api-and-access-to-schedule-data.html)
- [Revit API Docs](https://www.revitapidocs.com/)

**Claude API**:
- [Anthropic Docs](https://docs.anthropic.com/)
- [API Reference](https://docs.anthropic.com/claude/reference/messages)

---

**Decision Point**: After testing this on real project data, assess whether to continue with tagging/dimensioning features or pivot based on findings.
