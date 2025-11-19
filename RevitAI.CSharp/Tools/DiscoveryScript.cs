/*
 * Phase 1 Discovery Script for test_rooms.rvt
 *
 * PURPOSE:
 * Analyze the test fixture to understand its contents before implementing Layer 2 integration.
 * This script extracts all rooms, boundaries, openings, and dimension styles.
 *
 * HOW TO RUN:
 * Option 1: Revit Macro (Recommended)
 *   1. Open Revit 2026
 *   2. Open temp/test_rooms.rvt
 *   3. Go to Manage tab → Macro Manager
 *   4. Create new module (Application or Document level)
 *   5. Copy Run() method content into the macro
 *   6. Run the macro
 *   7. Output appears in Revit's built-in output window
 *
 * Option 2: Add as Ribbon Command
 *   1. Add this file to RevitAI.CSharp project
 *   2. Register as IExternalCommand
 *   3. Build and restart Revit
 *   4. Run from ribbon
 *
 * OUTPUT:
 * Generates markdown report to: %APPDATA%/RevitAI/discovery/test_rooms_analysis.md
 * Also outputs summary to TaskDialog for immediate viewing
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace RevitAI.Tools
{
    [Transaction(TransactionMode.ReadOnly)]
    public class DiscoveryScript : IExternalCommand
    {
        // Data structures for discovery
        private class RoomDiscoveryInfo
        {
            public string Name { get; set; } = "";
            public string Number { get; set; } = "";
            public string LevelName { get; set; } = "";
            public double AreaSqFt { get; set; }
            public double PerimeterFt { get; set; }
            public (double X, double Y, double Z) BoundingMin { get; set; }
            public (double X, double Y, double Z) BoundingMax { get; set; }
            public int WallCount { get; set; }
            public bool HasCurvedWalls { get; set; }
            public bool HasRoomSeparators { get; set; }
            public int DoorCount { get; set; }
            public int WindowCount { get; set; }
            public string GeometryType { get; set; } = "Unknown";
            public ElementId ElementId { get; set; }
        }

        private class OpeningDiscoveryInfo
        {
            public string Type { get; set; } = "";
            public string FamilyName { get; set; } = "";
            public double WidthFt { get; set; }
            public double HeightFt { get; set; }
            public string HostRoomName { get; set; } = "";
            public (double X, double Y, double Z) Location { get; set; }
        }

        private class DimensionStyleInfo
        {
            public string Name { get; set; } = "";
            public string FamilyName { get; set; } = "";
            public bool IsDefault { get; set; }
        }

        private class LevelInfo
        {
            public string Name { get; set; } = "";
            public double ElevationFt { get; set; }
            public int RoomCount { get; set; }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Run all discovery tests
                var results = RunAllDiscoveryTests(doc);

                // Generate markdown report
                string reportPath = GenerateReport(results, doc.Title);

                // Show summary dialog
                ShowSummaryDialog(results, reportPath);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("Discovery Error", $"Error during discovery:\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
        }

        private (
            List<RoomDiscoveryInfo> rooms,
            List<OpeningDiscoveryInfo> openings,
            List<DimensionStyleInfo> dimStyles,
            List<LevelInfo> levels
        ) RunAllDiscoveryTests(Document doc)
        {
            // Test 1.1 & 1.2: Room Discovery & Boundary Complexity
            var rooms = DiscoverRooms(doc);

            // Test 1.3: Opening Detection
            var openings = DiscoverOpenings(doc, rooms);

            // Test 1.4: Dimension Style Inventory
            var dimStyles = DiscoverDimensionStyles(doc);

            // Test 1.5: Level Inventory
            var levels = DiscoverLevels(doc, rooms);

            return (rooms, openings, dimStyles, levels);
        }

        /// <summary>
        /// Test 1.1 & 1.2: Discover all rooms with boundary analysis
        /// </summary>
        private List<RoomDiscoveryInfo> DiscoverRooms(Document doc)
        {
            var roomInfos = new List<RoomDiscoveryInfo>();

            var rooms = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Rooms)
                .WhereElementIsNotElementType()
                .Cast<Room>()
                .Where(r => r.Area > 0) // Only placed rooms
                .ToList();

            foreach (var room in rooms)
            {
                var info = new RoomDiscoveryInfo
                {
                    ElementId = room.Id,
                    Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Unnamed",
                    Number = room.Number ?? "",
                    LevelName = room.Level?.Name ?? "Unknown",
                    AreaSqFt = room.Area, // Already in sqft in Revit internal units
                    PerimeterFt = room.Perimeter
                };

                // Get bounding box
                var bbox = room.get_BoundingBox(null);
                if (bbox != null)
                {
                    info.BoundingMin = (bbox.Min.X, bbox.Min.Y, bbox.Min.Z);
                    info.BoundingMax = (bbox.Max.X, bbox.Max.Y, bbox.Max.Z);
                }

                // Analyze boundaries
                AnalyzeRoomBoundaries(room, info);

                // Determine geometry type
                info.GeometryType = ClassifyRoomGeometry(info);

                roomInfos.Add(info);
            }

            return roomInfos;
        }

        /// <summary>
        /// Analyze room boundaries for complexity
        /// </summary>
        private void AnalyzeRoomBoundaries(Room room, RoomDiscoveryInfo info)
        {
            var options = new SpatialElementBoundaryOptions
            {
                SpatialElementBoundaryLocation = SpatialElementBoundaryLocation.Finish
            };

            var boundarySegments = room.GetBoundarySegments(options);
            if (boundarySegments == null || boundarySegments.Count == 0)
            {
                info.WallCount = 0;
                return;
            }

            // Analyze first (outer) boundary loop
            var outerLoop = boundarySegments[0];
            info.WallCount = outerLoop.Count;

            foreach (var segment in outerLoop)
            {
                var curve = segment.GetCurve();

                // Check if curved (Arc, Circle, Ellipse, etc.)
                if (curve != null && !(curve is Line))
                {
                    info.HasCurvedWalls = true;
                }

                // Check if room separator (ElementId.InvalidElementId means separator)
                if (segment.ElementId == ElementId.InvalidElementId)
                {
                    info.HasRoomSeparators = true;
                }
            }
        }

        /// <summary>
        /// Classify room geometry based on wall count and characteristics
        /// </summary>
        private string ClassifyRoomGeometry(RoomDiscoveryInfo info)
        {
            if (info.HasCurvedWalls)
                return "Curved";
            if (info.HasRoomSeparators)
                return "With Separators";
            if (info.WallCount == 4)
                return "Rectangular";
            if (info.WallCount == 6)
                return "L-Shaped";
            if (info.WallCount == 8)
                return "U-Shaped";
            if (info.WallCount > 8)
                return "Complex";
            if (info.WallCount < 4)
                return "Invalid/Open";

            return $"{info.WallCount}-Sided";
        }

        /// <summary>
        /// Test 1.3: Discover all doors and windows
        /// </summary>
        private List<OpeningDiscoveryInfo> DiscoverOpenings(Document doc, List<RoomDiscoveryInfo> rooms)
        {
            var openings = new List<OpeningDiscoveryInfo>();

            // Collect all doors
            var doors = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Doors)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var door in doors)
            {
                var opening = ExtractOpeningInfo(door, "Door", rooms);
                if (opening != null)
                {
                    openings.Add(opening);

                    // Update room door count
                    var room = rooms.FirstOrDefault(r => r.Name == opening.HostRoomName);
                    if (room != null) room.DoorCount++;
                }
            }

            // Collect all windows
            var windows = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_Windows)
                .WhereElementIsNotElementType()
                .Cast<FamilyInstance>()
                .ToList();

            foreach (var window in windows)
            {
                var opening = ExtractOpeningInfo(window, "Window", rooms);
                if (opening != null)
                {
                    openings.Add(opening);

                    // Update room window count
                    var room = rooms.FirstOrDefault(r => r.Name == opening.HostRoomName);
                    if (room != null) room.WindowCount++;
                }
            }

            return openings;
        }

        /// <summary>
        /// Extract opening info from FamilyInstance
        /// </summary>
        private OpeningDiscoveryInfo ExtractOpeningInfo(FamilyInstance instance, string type, List<RoomDiscoveryInfo> rooms)
        {
            var info = new OpeningDiscoveryInfo
            {
                Type = type,
                FamilyName = instance.Symbol?.Family?.Name ?? "Unknown"
            };

            // Get width parameter
            var widthParam = instance.get_Parameter(BuiltInParameter.DOOR_WIDTH) ??
                            instance.get_Parameter(BuiltInParameter.WINDOW_WIDTH) ??
                            instance.LookupParameter("Width");
            if (widthParam != null && widthParam.HasValue)
            {
                info.WidthFt = widthParam.AsDouble();
            }

            // Get height parameter
            var heightParam = instance.get_Parameter(BuiltInParameter.DOOR_HEIGHT) ??
                             instance.get_Parameter(BuiltInParameter.WINDOW_HEIGHT) ??
                             instance.LookupParameter("Height");
            if (heightParam != null && heightParam.HasValue)
            {
                info.HeightFt = heightParam.AsDouble();
            }

            // Get location
            var location = instance.Location as LocationPoint;
            if (location != null)
            {
                info.Location = (location.Point.X, location.Point.Y, location.Point.Z);
            }

            // Find host room (from/to room for doors)
            var fromRoom = instance.FromRoom;
            var toRoom = instance.ToRoom;

            if (fromRoom != null)
            {
                info.HostRoomName = fromRoom.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Unknown";
            }
            else if (toRoom != null)
            {
                info.HostRoomName = toRoom.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Unknown";
            }

            return info;
        }

        /// <summary>
        /// Test 1.4: Discover all dimension styles
        /// </summary>
        private List<DimensionStyleInfo> DiscoverDimensionStyles(Document doc)
        {
            var styles = new List<DimensionStyleInfo>();

            // Get default dimension type ID
            var defaultTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);

            var dimTypes = new FilteredElementCollector(doc)
                .OfClass(typeof(DimensionType))
                .Cast<DimensionType>()
                .ToList();

            foreach (var dt in dimTypes)
            {
                // Filter to linear dimension types only
                var styleType = dt.get_Parameter(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)?.AsString();

                styles.Add(new DimensionStyleInfo
                {
                    Name = dt.Name,
                    FamilyName = styleType ?? "Unknown",
                    IsDefault = dt.Id == defaultTypeId
                });
            }

            return styles;
        }

        /// <summary>
        /// Test 1.5: Discover all levels
        /// </summary>
        private List<LevelInfo> DiscoverLevels(Document doc, List<RoomDiscoveryInfo> rooms)
        {
            var levels = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .Cast<Level>()
                .OrderBy(l => l.Elevation)
                .Select(l => new LevelInfo
                {
                    Name = l.Name,
                    ElevationFt = l.Elevation,
                    RoomCount = rooms.Count(r => r.LevelName == l.Name)
                })
                .ToList();

            return levels;
        }

        /// <summary>
        /// Generate markdown report
        /// </summary>
        private string GenerateReport(
            (List<RoomDiscoveryInfo> rooms, List<OpeningDiscoveryInfo> openings,
             List<DimensionStyleInfo> dimStyles, List<LevelInfo> levels) results,
            string documentTitle)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# Test Fixture Analysis: {documentTitle}");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine($"**Purpose:** Phase 1 Discovery for Story 2.6 Layer 2 Integration");
            sb.AppendLine();

            // Section 1: Summary Statistics
            sb.AppendLine("## 1. Summary Statistics");
            sb.AppendLine();
            sb.AppendLine($"- **Total Rooms:** {results.rooms.Count}");
            sb.AppendLine($"- **Total Doors:** {results.openings.Count(o => o.Type == "Door")}");
            sb.AppendLine($"- **Total Windows:** {results.openings.Count(o => o.Type == "Window")}");
            sb.AppendLine($"- **Total Levels:** {results.levels.Count}");
            sb.AppendLine($"- **Dimension Styles Available:** {results.dimStyles.Count}");
            sb.AppendLine();

            // Rooms by level
            sb.AppendLine("### Rooms by Level");
            sb.AppendLine();
            foreach (var level in results.levels.Where(l => l.RoomCount > 0))
            {
                sb.AppendLine($"- **{level.Name}** (Elevation: {level.ElevationFt:F2} ft): {level.RoomCount} rooms");
            }
            sb.AppendLine();

            // Geometry breakdown
            sb.AppendLine("### Geometry Complexity Breakdown");
            sb.AppendLine();
            var geometryGroups = results.rooms.GroupBy(r => r.GeometryType)
                .OrderByDescending(g => g.Count());
            foreach (var group in geometryGroups)
            {
                sb.AppendLine($"- **{group.Key}:** {group.Count()} rooms");
            }
            sb.AppendLine();

            // Section 2: Room Inventory Table
            sb.AppendLine("## 2. Room Inventory");
            sb.AppendLine();
            sb.AppendLine("| Room Name | Number | Level | Area (sqft) | Wall Count | Geometry | Doors | Windows |");
            sb.AppendLine("|-----------|--------|-------|-------------|------------|----------|-------|---------|");

            foreach (var room in results.rooms.OrderBy(r => r.LevelName).ThenBy(r => r.Name))
            {
                sb.AppendLine($"| {room.Name} | {room.Number} | {room.LevelName} | {room.AreaSqFt:F0} | {room.WallCount} | {room.GeometryType} | {room.DoorCount} | {room.WindowCount} |");
            }
            sb.AppendLine();

            // Section 3: Opening Details
            sb.AppendLine("## 3. Opening Details");
            sb.AppendLine();

            if (results.openings.Any())
            {
                sb.AppendLine("| Type | Family | Width (ft) | Height (ft) | Host Room |");
                sb.AppendLine("|------|--------|------------|-------------|-----------|");

                foreach (var opening in results.openings.OrderBy(o => o.Type).ThenBy(o => o.HostRoomName))
                {
                    sb.AppendLine($"| {opening.Type} | {opening.FamilyName} | {opening.WidthFt:F2} | {opening.HeightFt:F2} | {opening.HostRoomName} |");
                }
            }
            else
            {
                sb.AppendLine("*No openings found in the project.*");
            }
            sb.AppendLine();

            // Section 4: Dimension Styles
            sb.AppendLine("## 4. Dimension Styles Available");
            sb.AppendLine();
            sb.AppendLine("| Style Name | Family | Default |");
            sb.AppendLine("|------------|--------|---------|");

            foreach (var style in results.dimStyles.OrderBy(s => s.FamilyName).ThenBy(s => s.Name))
            {
                var defaultMarker = style.IsDefault ? "✓" : "";
                sb.AppendLine($"| {style.Name} | {style.FamilyName} | {defaultMarker} |");
            }
            sb.AppendLine();

            // Section 5: Level Inventory
            sb.AppendLine("## 5. Level Inventory");
            sb.AppendLine();
            sb.AppendLine("| Level Name | Elevation (ft) | Room Count |");
            sb.AppendLine("|------------|----------------|------------|");

            foreach (var level in results.levels)
            {
                sb.AppendLine($"| {level.Name} | {level.ElevationFt:F2} | {level.RoomCount} |");
            }
            sb.AppendLine();

            // Section 6: Test Recommendations
            sb.AppendLine("## 6. Test Recommendations");
            sb.AppendLine();

            // Find good test candidates
            var rectangularRooms = results.rooms.Where(r => r.GeometryType == "Rectangular").ToList();
            var complexRooms = results.rooms.Where(r => r.GeometryType != "Rectangular").ToList();
            var roomsWithOpenings = results.rooms.Where(r => r.DoorCount > 0 || r.WindowCount > 0).ToList();

            sb.AppendLine("### Recommended Test Rooms");
            sb.AppendLine();

            // Simple rectangular room
            if (rectangularRooms.Any())
            {
                var simpleRoom = rectangularRooms
                    .Where(r => r.DoorCount == 0 && r.WindowCount == 0)
                    .OrderBy(r => r.AreaSqFt)
                    .FirstOrDefault();

                if (simpleRoom != null)
                {
                    sb.AppendLine($"**Simple Rectangular (Baseline Test):** {simpleRoom.Name} (#{simpleRoom.Number})");
                    sb.AppendLine($"  - Area: {simpleRoom.AreaSqFt:F0} sqft, {simpleRoom.WallCount} walls, no openings");
                    sb.AppendLine($"  - Expected: 4 dimension chains (one per wall)");
                    sb.AppendLine();
                }
            }

            // Room with openings
            var roomWithDoor = roomsWithOpenings
                .Where(r => r.GeometryType == "Rectangular")
                .OrderByDescending(r => r.DoorCount + r.WindowCount)
                .FirstOrDefault();

            if (roomWithDoor != null)
            {
                sb.AppendLine($"**Room with Openings (Gap Handling Test):** {roomWithDoor.Name} (#{roomWithDoor.Number})");
                sb.AppendLine($"  - Doors: {roomWithDoor.DoorCount}, Windows: {roomWithDoor.WindowCount}");
                sb.AppendLine($"  - Expected: Dimension chains with gaps at openings");
                sb.AppendLine();
            }

            // Complex geometry
            var lShapedRoom = results.rooms.FirstOrDefault(r => r.GeometryType == "L-Shaped");
            if (lShapedRoom != null)
            {
                sb.AppendLine($"**L-Shaped Room (Complex Geometry Test):** {lShapedRoom.Name} (#{lShapedRoom.Number})");
                sb.AppendLine($"  - Walls: {lShapedRoom.WallCount}");
                sb.AppendLine($"  - Expected: 6 dimension chains");
                sb.AppendLine();
            }

            // Curved wall (edge case)
            var curvedRoom = results.rooms.FirstOrDefault(r => r.HasCurvedWalls);
            if (curvedRoom != null)
            {
                sb.AppendLine($"**Curved Walls (Edge Case Test):** {curvedRoom.Name} (#{curvedRoom.Number})");
                sb.AppendLine($"  - Expected: Skip curved walls with warning");
                sb.AppendLine();
            }

            // Section 7: Expected Results for Automated Tests
            sb.AppendLine("### Expected Results for Automated Tests");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine("// Use these values in Story 2.6 integration tests:");
            sb.AppendLine($"const int EXPECTED_ROOM_COUNT = {results.rooms.Count};");
            sb.AppendLine($"const int EXPECTED_DOOR_COUNT = {results.openings.Count(o => o.Type == "Door")};");
            sb.AppendLine($"const int EXPECTED_WINDOW_COUNT = {results.openings.Count(o => o.Type == "Window")};");
            sb.AppendLine($"const int EXPECTED_LEVEL_COUNT = {results.levels.Count(l => l.RoomCount > 0)};");
            sb.AppendLine($"const int RECTANGULAR_ROOM_COUNT = {rectangularRooms.Count};");
            sb.AppendLine($"const int ROOMS_WITH_OPENINGS = {roomsWithOpenings.Count};");
            sb.AppendLine("```");
            sb.AppendLine();

            // Section 8: Known Issues/Warnings
            sb.AppendLine("## 7. Potential Issues for Layer 2 Implementation");
            sb.AppendLine();

            var curvedCount = results.rooms.Count(r => r.HasCurvedWalls);
            if (curvedCount > 0)
            {
                sb.AppendLine($"⚠️ **Curved Walls:** {curvedCount} rooms have curved walls - these will be skipped or need special handling");
            }

            var separatorCount = results.rooms.Count(r => r.HasRoomSeparators);
            if (separatorCount > 0)
            {
                sb.AppendLine($"ℹ️ **Room Separators:** {separatorCount} rooms have non-physical boundaries");
            }

            var invalidGeometry = results.rooms.Count(r => r.WallCount < 4);
            if (invalidGeometry > 0)
            {
                sb.AppendLine($"⚠️ **Invalid Geometry:** {invalidGeometry} rooms have fewer than 4 walls");
            }

            if (curvedCount == 0 && separatorCount == 0 && invalidGeometry == 0)
            {
                sb.AppendLine("✅ No obvious issues detected - test fixture looks clean for Layer 2 testing");
            }

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("*This report was generated by RevitAI Phase 1 Discovery Script for Story 2.6 testing.*");

            // Save report
            string reportDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RevitAI", "discovery");
            Directory.CreateDirectory(reportDir);

            string reportPath = Path.Combine(reportDir, "test_rooms_analysis.md");
            File.WriteAllText(reportPath, sb.ToString());

            return reportPath;
        }

        /// <summary>
        /// Show summary dialog in Revit
        /// </summary>
        private void ShowSummaryDialog(
            (List<RoomDiscoveryInfo> rooms, List<OpeningDiscoveryInfo> openings,
             List<DimensionStyleInfo> dimStyles, List<LevelInfo> levels) results,
            string reportPath)
        {
            var sb = new StringBuilder();

            sb.AppendLine("PHASE 1 DISCOVERY COMPLETE");
            sb.AppendLine();
            sb.AppendLine($"Rooms: {results.rooms.Count}");
            sb.AppendLine($"Doors: {results.openings.Count(o => o.Type == "Door")}");
            sb.AppendLine($"Windows: {results.openings.Count(o => o.Type == "Window")}");
            sb.AppendLine($"Levels with rooms: {results.levels.Count(l => l.RoomCount > 0)}");
            sb.AppendLine($"Dimension styles: {results.dimStyles.Count}");
            sb.AppendLine();
            sb.AppendLine("Geometry Types:");

            var groups = results.rooms.GroupBy(r => r.GeometryType);
            foreach (var g in groups.OrderByDescending(x => x.Count()))
            {
                sb.AppendLine($"  {g.Key}: {g.Count()}");
            }

            sb.AppendLine();
            sb.AppendLine($"Full report saved to:");
            sb.AppendLine(reportPath);

            TaskDialog.Show("Discovery Results", sb.ToString());
        }
    }
}
