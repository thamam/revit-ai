/*
 * REVIT APPLICATION MACRO VERSION
 *
 * This version works with ThisApplication (Application-level macro) instead of ThisDocument.
 *
 * HOW TO USE:
 * 1. Open Revit 2026
 * 2. Open test_rooms_light_2026.rvt (or any test file with rooms)
 * 3. Manage tab → Macro Manager
 * 4. Create an Application-level module named "DiscoveryModule"
 * 5. Replace ALL code in SharpDevelop with the code below
 * 6. Build (F8) and Run (F5)
 * 7. The macro will analyze the ACTIVE document and save a report
 *
 * OUTPUT:
 * - TaskDialog with summary statistics
 * - Full report at: %APPDATA%\RevitAI\discovery\test_rooms_analysis.md
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;

namespace DiscoveryModule
{
    [Autodesk.Revit.Attributes.Transaction(Autodesk.Revit.Attributes.TransactionMode.Manual)]
    [Autodesk.Revit.DB.Macros.AddInId("0AE5FB1E-24D8-413E-B55E-6CF8911D1991")]
    public partial class ThisApplication
    {
        private void Module_Startup(object sender, EventArgs e)
        {
        }

        private void Module_Shutdown(object sender, EventArgs e)
        {
        }

        #region Revit Macros generated code
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(Module_Startup);
            this.Shutdown += new System.EventHandler(Module_Shutdown);
        }
        #endregion

        public void RunDiscovery()
        {
            // Get the active document
            UIDocument uidoc = this.ActiveUIDocument;
            if (uidoc == null)
            {
                TaskDialog.Show("Error", "No active document. Please open a Revit file first.");
                return;
            }

            Document doc = uidoc.Document;

            try
            {
                // Data structures
                var rooms = new List<RoomInfo>();
                var openings = new List<OpeningInfo>();
                var dimStyles = new List<DimStyleInfo>();
                var levels = new List<LevelInfo>();

                // Test 1.1: Room Discovery
                var revitRooms = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Rooms)
                    .WhereElementIsNotElementType()
                    .Cast<Room>()
                    .Where(r => r.Area > 0)
                    .ToList();

                foreach (var room in revitRooms)
                {
                    var info = new RoomInfo
                    {
                        Name = room.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "Unnamed",
                        Number = room.Number ?? "",
                        LevelName = room.Level?.Name ?? "Unknown",
                        AreaSqFt = room.Area,
                        PerimeterFt = room.Perimeter
                    };

                    // Analyze boundaries
                    var options = new SpatialElementBoundaryOptions();
                    var segments = room.GetBoundarySegments(options);

                    if (segments != null && segments.Count > 0)
                    {
                        var outer = segments[0];
                        info.WallCount = outer.Count;

                        foreach (var seg in outer)
                        {
                            var curve = seg.GetCurve();
                            if (curve != null && !(curve is Line))
                                info.HasCurved = true;
                            if (seg.ElementId == ElementId.InvalidElementId)
                                info.HasSeparator = true;
                        }
                    }

                    // Classify geometry
                    info.GeometryType = info.WallCount switch
                    {
                        4 when !info.HasCurved => "Rectangular",
                        6 => "L-Shaped",
                        8 => "U-Shaped",
                        < 4 => "Invalid",
                        _ when info.HasCurved => "Curved",
                        _ => $"{info.WallCount}-Sided"
                    };

                    rooms.Add(info);
                }

                // Test 1.3: Openings
                var doors = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Doors)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .ToList();

                foreach (var door in doors)
                {
                    var width = door.get_Parameter(BuiltInParameter.DOOR_WIDTH)?.AsDouble() ??
                               door.LookupParameter("Width")?.AsDouble() ?? 0;
                    var roomName = door.FromRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ??
                                  door.ToRoom?.get_Parameter(BuiltInParameter.ROOM_NAME)?.AsString() ?? "";

                    openings.Add(new OpeningInfo
                    {
                        Type = "Door",
                        Family = door.Symbol?.Family?.Name ?? "Unknown",
                        Width = width,
                        RoomName = roomName
                    });

                    // Update room
                    var room = rooms.FirstOrDefault(r => r.Name == roomName);
                    if (room != null) room.DoorCount++;
                }

                var windows = new FilteredElementCollector(doc)
                    .OfCategory(BuiltInCategory.OST_Windows)
                    .WhereElementIsNotElementType()
                    .Cast<FamilyInstance>()
                    .ToList();

                foreach (var win in windows)
                {
                    var width = win.get_Parameter(BuiltInParameter.WINDOW_WIDTH)?.AsDouble() ??
                               win.LookupParameter("Width")?.AsDouble() ?? 0;
                    var host = win.Host as Wall;
                    var roomName = "";

                    openings.Add(new OpeningInfo
                    {
                        Type = "Window",
                        Family = win.Symbol?.Family?.Name ?? "Unknown",
                        Width = width,
                        RoomName = roomName
                    });
                }

                // Test 1.4: Dimension styles
                var defaultTypeId = doc.GetDefaultElementTypeId(ElementTypeGroup.LinearDimensionType);

                var dimTypeElements = new FilteredElementCollector(doc)
                    .OfClass(typeof(DimensionType))
                    .Cast<DimensionType>()
                    .ToList();

                foreach (var dt in dimTypeElements)
                {
                    dimStyles.Add(new DimStyleInfo
                    {
                        Name = dt.Name,
                        Family = dt.get_Parameter(BuiltInParameter.SYMBOL_FAMILY_NAME_PARAM)?.AsString() ?? "",
                        IsDefault = dt.Id == defaultTypeId
                    });
                }

                // Test 1.5: Levels
                var levelElements = new FilteredElementCollector(doc)
                    .OfClass(typeof(Level))
                    .Cast<Level>()
                    .OrderBy(l => l.Elevation)
                    .ToList();

                foreach (var l in levelElements)
                {
                    levels.Add(new LevelInfo
                    {
                        Name = l.Name,
                        Elevation = l.Elevation,
                        RoomCount = rooms.Count(r => r.LevelName == l.Name)
                    });
                }

                // Generate report
                var report = GenerateReport(rooms, openings, dimStyles, levels, doc.Title);

                // Save report
                string dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "RevitAI", "discovery");
                Directory.CreateDirectory(dir);
                string path = Path.Combine(dir, "test_rooms_analysis.md");
                File.WriteAllText(path, report);

                // Show summary
                var sb = new StringBuilder();
                sb.AppendLine("PHASE 1 DISCOVERY COMPLETE\n");
                sb.AppendLine($"Rooms: {rooms.Count}");
                sb.AppendLine($"Doors: {openings.Count(o => o.Type == "Door")}");
                sb.AppendLine($"Windows: {openings.Count(o => o.Type == "Window")}");
                sb.AppendLine($"Levels: {levels.Count(l => l.RoomCount > 0)}");
                sb.AppendLine($"Dim styles: {dimStyles.Count}");
                sb.AppendLine("\nGeometry:");

                foreach (var g in rooms.GroupBy(r => r.GeometryType).OrderByDescending(x => x.Count()))
                {
                    sb.AppendLine($"  {g.Key}: {g.Count()}");
                }

                sb.AppendLine($"\nReport saved to:\n{path}");

                TaskDialog.Show("Discovery", sb.ToString());
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", ex.ToString());
            }
        }

        private string GenerateReport(List<RoomInfo> rooms, List<OpeningInfo> openings,
            List<DimStyleInfo> dimStyles, List<LevelInfo> levels, string title)
        {
            var sb = new StringBuilder();

            sb.AppendLine($"# Test Fixture Analysis: {title}");
            sb.AppendLine();
            sb.AppendLine($"**Generated:** {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            sb.AppendLine();

            // Summary
            sb.AppendLine("## 1. Summary Statistics");
            sb.AppendLine();
            sb.AppendLine($"- **Total Rooms:** {rooms.Count}");
            sb.AppendLine($"- **Total Doors:** {openings.Count(o => o.Type == "Door")}");
            sb.AppendLine($"- **Total Windows:** {openings.Count(o => o.Type == "Window")}");
            sb.AppendLine($"- **Dimension Styles:** {dimStyles.Count}");
            sb.AppendLine();

            // Rooms by level
            sb.AppendLine("### Rooms by Level");
            foreach (var level in levels.Where(l => l.RoomCount > 0))
            {
                sb.AppendLine($"- **{level.Name}** ({level.Elevation:F2} ft): {level.RoomCount} rooms");
            }
            sb.AppendLine();

            // Geometry breakdown
            sb.AppendLine("### Geometry Types");
            foreach (var g in rooms.GroupBy(r => r.GeometryType).OrderByDescending(x => x.Count()))
            {
                sb.AppendLine($"- **{g.Key}:** {g.Count()} rooms");
            }
            sb.AppendLine();

            // Room table
            sb.AppendLine("## 2. Room Inventory");
            sb.AppendLine();
            sb.AppendLine("| Room Name | Number | Level | Area (sqft) | Walls | Type | Doors | Win |");
            sb.AppendLine("|-----------|--------|-------|-------------|-------|------|-------|-----|");

            foreach (var r in rooms.OrderBy(r => r.LevelName).ThenBy(r => r.Name))
            {
                sb.AppendLine($"| {r.Name} | {r.Number} | {r.LevelName} | {r.AreaSqFt:F0} | {r.WallCount} | {r.GeometryType} | {r.DoorCount} | {r.WindowCount} |");
            }
            sb.AppendLine();

            // Dimension styles
            sb.AppendLine("## 3. Dimension Styles");
            sb.AppendLine();
            sb.AppendLine("| Name | Family | Default |");
            sb.AppendLine("|------|--------|---------|");
            foreach (var s in dimStyles.OrderBy(s => s.Name))
            {
                sb.AppendLine($"| {s.Name} | {s.Family} | {(s.IsDefault ? "✓" : "")} |");
            }
            sb.AppendLine();

            // Test recommendations
            sb.AppendLine("## 4. Test Recommendations");
            sb.AppendLine();

            var rect = rooms.FirstOrDefault(r => r.GeometryType == "Rectangular" && r.DoorCount == 0);
            if (rect != null)
            {
                sb.AppendLine($"**Simple Test:** {rect.Name} (#{rect.Number}) - {rect.WallCount} walls, no openings");
            }

            var withDoor = rooms.FirstOrDefault(r => r.GeometryType == "Rectangular" && r.DoorCount > 0);
            if (withDoor != null)
            {
                sb.AppendLine($"**Opening Test:** {withDoor.Name} (#{withDoor.Number}) - {withDoor.DoorCount} doors");
            }

            var lShaped = rooms.FirstOrDefault(r => r.GeometryType == "L-Shaped");
            if (lShaped != null)
            {
                sb.AppendLine($"**Complex Test:** {lShaped.Name} (#{lShaped.Number}) - L-shaped");
            }
            sb.AppendLine();

            // Expected values
            sb.AppendLine("```csharp");
            sb.AppendLine("// Story 2.6 Test Constants:");
            sb.AppendLine($"const int EXPECTED_ROOMS = {rooms.Count};");
            sb.AppendLine($"const int EXPECTED_DOORS = {openings.Count(o => o.Type == "Door")};");
            sb.AppendLine($"const int EXPECTED_WINDOWS = {openings.Count(o => o.Type == "Window")};");
            sb.AppendLine($"const int RECTANGULAR_ROOMS = {rooms.Count(r => r.GeometryType == "Rectangular")};");
            sb.AppendLine("```");
            sb.AppendLine();

            return sb.ToString();
        }

        // Data classes
        private class RoomInfo
        {
            public string Name { get; set; }
            public string Number { get; set; }
            public string LevelName { get; set; }
            public double AreaSqFt { get; set; }
            public double PerimeterFt { get; set; }
            public int WallCount { get; set; }
            public bool HasCurved { get; set; }
            public bool HasSeparator { get; set; }
            public int DoorCount { get; set; }
            public int WindowCount { get; set; }
            public string GeometryType { get; set; }
        }

        private class OpeningInfo
        {
            public string Type { get; set; }
            public string Family { get; set; }
            public double Width { get; set; }
            public string RoomName { get; set; }
        }

        private class DimStyleInfo
        {
            public string Name { get; set; }
            public string Family { get; set; }
            public bool IsDefault { get; set; }
        }

        private class LevelInfo
        {
            public string Name { get; set; }
            public double Elevation { get; set; }
            public int RoomCount { get; set; }
        }
    }
}
