using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using Autodesk.Revit.UI;
using Autodesk.Revit.Attributes;

namespace RevitAI
{
    /// <summary>
    /// RevitAI Application Entry Point
    /// Implements IExternalApplication for Revit add-in initialization
    /// </summary>
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]
    public class Application : IExternalApplication
    {
        /// <summary>
        /// Called when Revit starts and loads the add-in
        /// </summary>
        public Result OnStartup(UIControlledApplication application)
        {
            try
            {
                // Create RevitAI ribbon tab (only if it doesn't exist)
                string tabName = "RevitAI";
                string panelName = "AI Copilot";

                try
                {
                    application.CreateRibbonTab(tabName);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // Tab already exists, which is fine
                }

                // Create ribbon panel (only if it doesn't exist)
                RibbonPanel panel;
                try
                {
                    panel = application.CreateRibbonPanel(tabName, panelName);
                }
                catch (Autodesk.Revit.Exceptions.ArgumentException)
                {
                    // Panel already exists, get existing one
                    panel = null;
                    foreach (RibbonPanel p in application.GetRibbonPanels(tabName))
                    {
                        if (p.Name == panelName)
                        {
                            panel = p;
                            break;
                        }
                    }
                    if (panel == null)
                    {
                        throw new Exception($"Panel '{panelName}' exists but could not be retrieved");
                    }
                }

                // Add Copilot button
                AddCopilotButton(panel);

                // Add Settings button
                AddSettingsButton(panel);

                return Result.Succeeded;
            }
            catch (Exception ex)
            {
                TaskDialog.Show("RevitAI Error", $"Failed to initialize RevitAI:\n{ex.Message}");
                return Result.Failed;
            }
        }

        /// <summary>
        /// Called when Revit shuts down
        /// </summary>
        public Result OnShutdown(UIControlledApplication application)
        {
            // Cleanup if needed
            return Result.Succeeded;
        }

        /// <summary>
        /// Add Copilot command button to ribbon
        /// </summary>
        private void AddCopilotButton(RibbonPanel panel)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "RevitAI_Copilot",
                "Copilot",
                assemblyPath,
                "RevitAI.Commands.CopilotCommand"
            );

            buttonData.ToolTip = "Open RevitAI Copilot";
            buttonData.LongDescription = "AI-powered assistant for Revit automation.\n\n" +
                                        "Uses Claude AI to understand natural language commands " +
                                        "and automate Revit tasks safely.";

            // Add button to panel
            PushButton button = panel.AddItem(buttonData) as PushButton;

            // Set button icon (if available)
            // button.LargeImage = GetEmbeddedImage("RevitAI.Resources.copilot_32x32.png");
        }

        /// <summary>
        /// Add Settings command button to ribbon
        /// </summary>
        private void AddSettingsButton(RibbonPanel panel)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;

            PushButtonData buttonData = new PushButtonData(
                "RevitAI_Settings",
                "Settings",
                assemblyPath,
                "RevitAI.Commands.SettingsCommand"
            );

            buttonData.ToolTip = "RevitAI Settings";
            buttonData.LongDescription = "Configure Claude API key and RevitAI settings.";

            // Add button to panel
            PushButton button = panel.AddItem(buttonData) as PushButton;

            // Set button icon (if available)
            // button.LargeImage = GetEmbeddedImage("RevitAI.Resources.settings_32x32.png");
        }

        /// <summary>
        /// Helper method to load embedded image resources
        /// </summary>
        private BitmapImage GetEmbeddedImage(string resourceName)
        {
            try
            {
                Assembly assembly = Assembly.GetExecutingAssembly();
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream != null)
                    {
                        BitmapImage image = new BitmapImage();
                        image.BeginInit();
                        image.StreamSource = stream;
                        image.EndInit();
                        return image;
                    }
                }
            }
            catch
            {
                // Fallback to no icon if image loading fails
            }

            return null;
        }
    }
}
