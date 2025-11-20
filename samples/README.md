# Sample Revit Projects

This directory contains test Revit project files used for development and testing. These files are **not tracked in git** due to their large size (~100MB+).

## Available Samples

### test_rooms_light_2026.rvt
- **Location:** `samples/revit-projects/test_rooms_light_2026.rvt`
- **Size:** ~100MB
- **Revit Version:** 2026
- **Purpose:** Phase 1 discovery testing, Epic 2 dimension automation
- **Contents:**
  - 4 analyzed rooms (Room 1-4)
  - Multiple dimension styles (45+ cataloged)
  - Clean geometry for algorithm testing
  - See `docs/session-2025-11-20-discovery-phase.md` for analysis

### How to Get Sample Files

**Option 1: Copy from nelly (Windows machine)**
```bash
# If you have access to the original machine
scp nelly:C:/temp/test_rooms_light_2026.rvt samples/revit-projects/
```

**Option 2: Generate Discovery Data**
If you don't have the original file, you can run the discovery macro on any Revit project:
1. Open any Revit project with rooms
2. Open Revit Macro Manager (Alt+F8 or Manage tab → Macro Manager)
3. Copy contents from `RevitAI.CSharp/Tools/DiscoveryMacro.cs`
4. Create new macro, paste, and run
5. Discovery report will be generated at `%APPDATA%\RevitAI\discovery\`

**Option 3: Use Your Own Projects**
RevitAI is designed to work with any Revit project. You can test with your own .rvt files - just place them in `samples/revit-projects/` for easy access.

## Directory Structure

```
samples/
├── README.md (this file)
└── revit-projects/
    ├── test_rooms_light_2026.rvt (not in git)
    ├── simple_room.rvt (future - for Story 2.1-2.3 tagging tests)
    └── complex_geometry.rvt (future - for Story 2.5 edge cases)
```

## Test Recommendations

Based on discovery phase findings:

### For Auto-Tagging Tests (Epic 2 Stories 2.1-2.3)
- **Simple projects:** 10-50 elements
- **Categories needed:** Doors, Walls, Rooms, Windows
- **Test scenarios:**
  - Untagged elements (baseline)
  - Partially tagged (filter logic)
  - Crowded areas (collision detection)

### For Dimension Tests (Epic 2 Stories 2.4-2.6)
- **Use test_rooms_light_2026.rvt**
- **Test constants available:**
  ```csharp
  // From discovery phase (see session doc)
  const int EXPECTED_ROOM_COUNT = 4;
  const int EXPECTED_WALL_COUNT_ROOM1 = 4;
  const double ROOM1_AREA_SQM = 12.89;
  ```

### For Edge Case Tests (Epic 2 Story 2.5)
- Curved walls
- Angled walls (non-orthogonal)
- L-shaped rooms
- Complex geometry

## Storage Best Practices

1. **Never commit .rvt files to git** (already in .gitignore)
2. **Keep local backup** - these files are valuable test fixtures
3. **Document test constants** - if you create custom test files, document expected values
4. **Share via external storage** - if team members need files, use OneDrive/Dropbox/etc.

## Version Control Alternative

For version-controlling test geometry without large binary files, consider:
- **Dynamo scripts** - Generate test geometry programmatically
- **Revit API macros** - Create test projects on-the-fly
- **IFC exports** - Smaller file size, but loses parametric data (not recommended for testing)

## Future: Synthetic Test Generation

As part of the testing framework (see `docs/testing-framework.md`), we plan to:
- Auto-generate simple test projects via Revit API
- Create fixtures programmatically in test setup
- Eliminate dependency on large binary .rvt files

This is a long-term goal - for now, manual .rvt files in `samples/` work well.

---

**Note:** If you create new sample files, document them here with:
- File name and location
- Purpose / what it tests
- Key characteristics (element counts, complexity)
- Expected test constants (if applicable)
