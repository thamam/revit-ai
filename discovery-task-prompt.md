# Phase 1 Discovery Task: Guide User Through Revit Macro Execution

## Your Role
You are helping the user run the Phase 1 discovery tests on test_rooms.rvt in Revit 2026.
Guide them step-by-step through the process. Expect that things may not go smoothly.

## Context
- Project: RevitAI - C# Revit add-in for dimension automation
- Current task: Run discovery macro to analyze test_rooms.rvt before implementing Layer 2
- Files already created and ready:
  - RevitAI.CSharp/Tools/DiscoveryMacro.cs (for Revit Macro Manager)
  - RevitAI.CSharp/Tools/DiscoveryScript.cs (IExternalCommand version)
  - docs/stories/2-6-discovery-instructions.md (step-by-step guide)

## Step-by-Step Guide for User

### Step 1: Open Revit and Test File
1. Launch Revit 2026
2. Open: temp/test_rooms.rvt (in this repo)
3. Wait for full load (400MB file)

### Step 2: Create Macro
1. Go to Manage tab → Macro Manager
2. Click Module → Create
3. Select Document level
4. Name it: DiscoveryModule
5. SharpDevelop IDE will open

### Step 3: Copy Macro Code
The user needs to copy the ENTIRE contents of:
RevitAI.CSharp/Tools/DiscoveryMacro.cs

Into the SharpDevelop IDE, replacing all existing code.

### Step 4: Build and Run
1. Press F8 to build
2. If build succeeds, press F5 to run
3. If errors occur, help debug them

### Step 5: View Results
Output will be:
- TaskDialog with summary
- Report saved to: %APPDATA%\RevitAI\discovery\test_rooms_analysis.md

## Common Issues to Watch For

1. **Build error about ThisDocument** - this is expected if they accidentally try to build the main project instead of as a macro

2. **Room count = 0** - Rooms might not be placed in the test file

3. **Missing references** - SharpDevelop might need Revit API references

4. **Report path issues** - May need to create the directory manually

## Your Approach
- Be patient and guide step-by-step
- Ask for error messages verbatim
- Suggest alternative approaches if primary fails
- The DiscoveryScript.cs can be used as an alternative (add as IExternalCommand to the project)

## After Discovery Completes
Copy the generated report to: docs/stories/2-6-test-fixture-analysis.md
This will document the actual contents of test_rooms.rvt for Story 2.6 implementation.
