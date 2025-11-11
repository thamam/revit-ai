using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAI.UI;

namespace RevitAI.Commands
{
    /// <summary>
    /// Settings Command
    /// Opens the settings dialog for API key configuration
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class SettingsCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                // Show Settings dialog
                SettingsDialog dialog = new SettingsDialog();
                dialog.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("RevitAI Error", $"Settings command failed:\n{ex.Message}");
                return Result.Failed;
            }
        }
    }
}
