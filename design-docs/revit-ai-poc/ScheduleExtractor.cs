using System;
using System.Collections.Generic;
using Autodesk.Revit.DB;

namespace RevitAI.SmartSchedule
{
    /// <summary>
    /// Data structure to hold schedule information
    /// </summary>
    public class ScheduleData
    {
        public string Name { get; set; }
        public string Category { get; set; }
        public List<string> Headers { get; set; }
        public List<List<string>> Rows { get; set; }
        public int RowCount => Rows?.Count ?? 0;
        public int ColumnCount => Headers?.Count ?? 0;

        public ScheduleData()
        {
            Headers = new List<string>();
            Rows = new List<List<string>>();
        }
    }

    /// <summary>
    /// Utility class to extract schedule data from Revit documents
    /// Based on best practices from The Building Coder and Revit API community
    /// </summary>
    public static class ScheduleExtractor
    {
        /// <summary>
        /// Extracts all schedules from the document
        /// </summary>
        public static List<ScheduleData> ExtractAllSchedules(Document doc)
        {
            List<ScheduleData> allSchedules = new List<ScheduleData>();

            // Get all ViewSchedule elements
            FilteredElementCollector collector = new FilteredElementCollector(doc);
            IList<Element> scheduleElements = collector.OfClass(typeof(ViewSchedule)).ToElements();

            foreach (Element element in scheduleElements)
            {
                ViewSchedule viewSchedule = element as ViewSchedule;
                
                if (viewSchedule == null || viewSchedule.IsTitleblockRevisionSchedule)
                    continue;

                try
                {
                    ScheduleData scheduleData = ExtractScheduleData(doc, viewSchedule);
                    
                    if (scheduleData.RowCount > 0)
                    {
                        allSchedules.Add(scheduleData);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue processing other schedules
                    System.Diagnostics.Debug.WriteLine($"Error extracting schedule {viewSchedule.Name}: {ex.Message}");
                }
            }

            return allSchedules;
        }

        /// <summary>
        /// Extracts data from a single schedule
        /// </summary>
        public static ScheduleData ExtractScheduleData(Document doc, ViewSchedule viewSchedule)
        {
            ScheduleData data = new ScheduleData
            {
                Name = viewSchedule.Name
            };

            // Get category name
            try
            {
                Category category = doc.Settings.Categories.get_Item(viewSchedule.Definition.CategoryId);
                data.Category = category?.Name ?? "Unknown";
            }
            catch
            {
                data.Category = "Unknown";
            }

            // Get table data
            TableData tableData = viewSchedule.GetTableData();
            TableSectionData sectionData = tableData.GetSectionData(SectionType.Body);

            int numRows = sectionData.NumberOfRows;
            int numCols = sectionData.NumberOfColumns;

            // Extract headers (first row)
            if (numRows > 0)
            {
                for (int col = 0; col < numCols; col++)
                {
                    string headerText = viewSchedule.GetCellText(SectionType.Body, 0, col);
                    data.Headers.Add(headerText);
                }
            }

            // Extract data rows (skip header row)
            for (int row = 1; row < numRows; row++)
            {
                List<string> rowData = new List<string>();
                
                for (int col = 0; col < numCols; col++)
                {
                    string cellText = viewSchedule.GetCellText(SectionType.Body, row, col);
                    rowData.Add(cellText);
                }
                
                data.Rows.Add(rowData);
            }

            return data;
        }

        /// <summary>
        /// Converts schedule data to JSON for LLM consumption
        /// </summary>
        public static string ToJson(ScheduleData schedule)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(schedule, Newtonsoft.Json.Formatting.Indented);
        }

        /// <summary>
        /// Converts schedule data to CSV format
        /// </summary>
        public static string ToCsv(ScheduleData schedule)
        {
            List<string> lines = new List<string>();
            
            // Add headers
            lines.Add(string.Join(",", schedule.Headers.Select(h => QuoteCsvField(h))));
            
            // Add rows
            foreach (var row in schedule.Rows)
            {
                lines.Add(string.Join(",", row.Select(cell => QuoteCsvField(cell))));
            }
            
            return string.Join("\n", lines);
        }

        private static string QuoteCsvField(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "\"\"";
            
            if (field.Contains(",") || field.Contains("\"") || field.Contains("\n"))
            {
                return "\"" + field.Replace("\"", "\"\"") + "\"";
            }
            
            return field;
        }
    }
}
