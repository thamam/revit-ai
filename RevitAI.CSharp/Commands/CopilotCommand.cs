using System;
using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitAI.UI;

namespace RevitAI.Commands
{
    /// <summary>
    /// Main Copilot Command
    /// Opens the AI Copilot dialog for natural language Revit automation
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class CopilotCommand : IExternalCommand
    {
        public Result Execute(
            ExternalCommandData commandData,
            ref string message,
            ElementSet elements)
        {
            try
            {
                UIApplication uiApp = commandData.Application;
                UIDocument uiDoc = uiApp.ActiveUIDocument;
                Document doc = uiDoc.Document;

                // Show Copilot dialog
                CopilotDialog dialog = new CopilotDialog(uiDoc);
                dialog.ShowDialog();

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                message = ex.Message;
                TaskDialog.Show("RevitAI Error", $"Copilot command failed:\n{ex.Message}\n\n{ex.StackTrace}");
                return Result.Failed;
            }
        }
    }
}
