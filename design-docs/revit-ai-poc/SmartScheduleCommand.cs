using System;
using System.Collections.Generic;
using System.Linq;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;

namespace RevitAI.SmartSchedule
{
    /// <summary>
    /// Main command to extract schedule data and send to Claude API for intelligent formatting
    /// </summary>
    [Transaction(TransactionMode.ReadOnly)]
    [Regeneration(RegenerationOption.Manual)]
    public class SmartScheduleCommand : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            try
            {
                UIDocument uidoc = commandData.Application.ActiveUIDocument;
                Document doc = uidoc.Document;

                // Step 1: Extract all schedules from the document
                List<ScheduleData> allSchedules = ScheduleExtractor.ExtractAllSchedules(doc);

                if (allSchedules.Count == 0)
                {
                    TaskDialog.Show("No Schedules", "No schedules found in the current document.");
                    return Result.Cancelled;
                }

                // Step 2: Let user select which schedule to process
                ScheduleData selectedSchedule = ShowScheduleSelector(allSchedules);
                
                if (selectedSchedule == null)
                {
                    return Result.Cancelled;
                }

                // Step 3: Send to Claude API for intelligent formatting
                string formattedOutput = ClaudeAPIClient.FormatSchedule(selectedSchedule);

                // Step 4: Display results
                ShowResults(selectedSchedule, formattedOutput);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = $"Error: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
                return Result.Failed;
            }
        }

        private ScheduleData ShowScheduleSelector(List<ScheduleData> schedules)
        {
            // Simple dialog to select schedule
            // In real implementation, use a proper WPF dialog
            string scheduleList = string.Join("\n", schedules.Select((s, i) => $"{i + 1}. {s.Name}"));
            
            TaskDialog td = new TaskDialog("Select Schedule")
            {
                MainContent = "Select a schedule to process:\n\n" + scheduleList + "\n\n(This is POC - will be improved)"
            };
            
            // For now, just return the first one
            // TODO: Implement proper selection UI
            return schedules.FirstOrDefault();
        }

        private void ShowResults(ScheduleData original, string formatted)
        {
            TaskDialog td = new TaskDialog("Schedule Processing Results")
            {
                MainInstruction = $"Processed: {original.Name}",
                MainContent = $"Original rows: {original.Rows.Count}\n\nFormatted output:\n\n{formatted.Substring(0, Math.Min(500, formatted.Length))}...",
                ExpandedContent = formatted
            };
            
            td.Show();
        }
    }
}
