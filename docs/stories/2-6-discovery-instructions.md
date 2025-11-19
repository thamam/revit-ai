# Phase 1 Discovery: Execution Instructions

**Purpose:** Run discovery tests on `temp/test_rooms.rvt` before implementing Story 2.6 Layer 2 integration.

---

## Quick Start (Recommended: Macro Method)

### Step 1: Open Revit and Test File

1. Launch **Revit 2026**
2. Open `temp/test_rooms.rvt` (the 400MB test fixture)
3. Wait for the file to fully load

### Step 2: Create and Run the Macro

1. Go to **Manage** tab → **Macro Manager** button

2. In the Macro Manager dialog:
   - Click **Module** button
   - Click **Create**
   - Select **Document** (or **Application** level - both work)
   - Name it: `DiscoveryModule`
   - Click **OK**

3. SharpDevelop IDE will open with a template file

4. **Replace ALL the code** with the contents of:
   ```
   RevitAI.CSharp/Tools/DiscoveryMacro.cs
   ```
   (Copy the entire file content)

5. **Build the macro:**
   - Press **F8** (or Build menu → Build Solution)
   - Verify "Build succeeded" in output window

6. **Run the macro:**
   - Press **F5** (or Run menu → Run)
   - OR close IDE and run from Macro Manager dialog

### Step 3: View Results

1. A **TaskDialog** will appear with summary statistics:
   - Total rooms, doors, windows
   - Geometry breakdown

2. Full report saved to:
   ```
   %APPDATA%\RevitAI\discovery\test_rooms_analysis.md
   ```

   On Windows this is typically:
   ```
   C:\Users\<YourName>\AppData\Roaming\RevitAI\discovery\test_rooms_analysis.md
   ```

3. **Copy the report** to the project documentation:
   ```bash
   copy "%APPDATA%\RevitAI\discovery\test_rooms_analysis.md" "docs\stories\2-6-test-fixture-analysis.md"
   ```

---

## Alternative: Run as External Command

If you prefer to compile and run as part of the RevitAI add-in:

### Step 1: Register the Command

Add to `RevitAI.CSharp/Application.cs` in `CreateCommandBinding()`:

```csharp
// Add discovery command button
PushButtonData discoveryBtn = new PushButtonData(
    "DiscoveryCommand",
    "Discovery",
    assemblyPath,
    "RevitAI.Tools.DiscoveryScript"
);
discoveryBtn.ToolTip = "Run Phase 1 discovery tests";
settingsPanel.AddItem(discoveryBtn);
```

### Step 2: Build and Deploy

```bash
dotnet build RevitAI.CSharp/RevitAI.csproj
```

### Step 3: Run in Revit

1. Restart Revit (required for C# add-ins)
2. Open `temp/test_rooms.rvt`
3. Click the **Discovery** button on the RevitAI ribbon tab

---

## Expected Output

### TaskDialog Summary

```
PHASE 1 DISCOVERY COMPLETE

Rooms: 12
Doors: 8
Windows: 15
Levels: 2
Dim styles: 5

Geometry:
  Rectangular: 9
  L-Shaped: 2
  Curved: 1

Report saved to:
C:\Users\...\AppData\Roaming\RevitAI\discovery\test_rooms_analysis.md
```

### Markdown Report Contents

The report includes:

1. **Summary Statistics**
   - Total counts for all element types
   - Rooms grouped by level
   - Geometry complexity breakdown

2. **Room Inventory Table**
   - Name, Number, Level, Area
   - Wall count and geometry type
   - Door/window counts per room

3. **Dimension Styles Available**
   - List of all LinearDimensionType names
   - Default style marked

4. **Test Recommendations**
   - Specific rooms to use for different test scenarios
   - Expected test constants for integration tests

---

## Troubleshooting

### Macro won't build

**Error:** "Could not load type..."

**Solution:**
1. Ensure you selected the correct module type (Document or Application)
2. Try creating a new module and copy-paste again
3. Check for typos in the `using` statements

### No rooms found (count = 0)

**Possible causes:**
1. File not fully loaded - wait for loading to complete
2. Rooms not placed in project (need room tags to activate)
3. Wrong Revit version (2025 vs 2026 API differences)

**Solution:**
1. Manually verify rooms exist: View → Floor Plans → select a level → see room tags
2. If no rooms visible, the test fixture needs room placement

### Report path doesn't exist

**Solution:**
1. Check `%APPDATA%` resolves correctly
2. Run as administrator if permission issues
3. Manually create folder: `%APPDATA%\RevitAI\discovery\`

### Performance slow

If discovery takes > 30 seconds:
1. 400MB file is large - first load may take 1-2 minutes
2. Close other Revit files
3. Subsequent runs will be faster (file cached)

---

## What to Document After Running

After running the discovery script, update these files:

### 1. Test Fixture Analysis

Save the generated report to:
```
docs/stories/2-6-test-fixture-analysis.md
```

### 2. Update Story 2.6 with Expected Values

In `docs/stories/2-6-layer-2-integration.md`, add the actual values from discovery:

```csharp
// Discovered values from test_rooms.rvt:
const int EXPECTED_ROOM_COUNT = <discovered>;
const int EXPECTED_DOOR_COUNT = <discovered>;
const int EXPECTED_WINDOW_COUNT = <discovered>;
```

### 3. Identify Test Candidates

Document which specific rooms to use for each test type:
- Simple rectangular room (baseline)
- Room with multiple openings (gap handling)
- L-shaped room (complex geometry)
- Curved wall room (edge case)

---

## Next Steps After Discovery

Once you have the test fixture analysis:

1. **Review geometry types** - Are there curved walls to handle?
2. **Select test rooms** - Pick specific rooms for integration tests
3. **Note dimension styles** - Which style to use as default?
4. **Update test constants** - Add discovered values to test code
5. **Proceed to Story 2.6** - Implement Layer 2 services

---

## Files Created

This task created:

| File | Purpose |
|------|---------|
| `RevitAI.CSharp/Tools/DiscoveryScript.cs` | Full IExternalCommand version |
| `RevitAI.CSharp/Tools/DiscoveryMacro.cs` | Macro version for easy execution |
| `docs/stories/2-6-discovery-instructions.md` | This file |

---

**Created:** 2025-11-19
**Story:** 2.6 - Layer 2 Revit API Integration
**Phase:** 1 - Discovery Tests
