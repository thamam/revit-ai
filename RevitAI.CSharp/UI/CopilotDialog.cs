using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;
using RevitAI.Models;

namespace RevitAI.UI
{
    /// <summary>
    /// Main Copilot Dialog
    /// Simple WPF dialog for AI Copilot interaction
    /// </summary>
    public class CopilotDialog : Window
    {
        private readonly UIDocument _uiDoc;
        private System.Windows.Controls.TextBox _promptTextBox;
        private System.Windows.Controls.TextBlock _statusTextBlock;

        public CopilotDialog(UIDocument uiDoc)
        {
            _uiDoc = uiDoc;

            // Window settings
            Title = "RevitAI Copilot";
            Width = 600;
            Height = 400;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Build UI
            BuildUI();
        }

        private void BuildUI()
        {
            // Main grid
            Grid mainGrid = new Grid();
            mainGrid.Margin = new Thickness(10);

            // Define rows
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Status text
            _statusTextBlock = new TextBlock
            {
                Text = "RevitAI Copilot - C# SDK Implementation\n\n" +
                       "Epic 1: Foundation & Core Infrastructure ✓\n\n" +
                       "This is the C# SDK version of RevitAI.\n" +
                       "Built with Revit's official .NET API for maximum stability.\n\n" +
                       "Status: Ready for Epic 2 implementation",
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_statusTextBlock, 0);

            // Prompt textbox
            _promptTextBox = new System.Windows.Controls.TextBox
            {
                Height = 100,
                TextWrapping = TextWrapping.Wrap,
                AcceptsReturn = true,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(0, 0, 0, 10),
                IsEnabled = false,  // Disabled until Epic 2
                Text = "Natural language commands will go here...\n(Epic 2 feature)"
            };
            Grid.SetRow(_promptTextBox, 1);

            // Button panel
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button testApiButton = new Button
            {
                Content = "Test Claude API",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            testApiButton.Click += TestApiButton_Click;

            Button testEventButton = new Button
            {
                Content = "Test ExternalEvent",
                Width = 140,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            testEventButton.Click += TestEventButton_Click;

            Button testPreviewButton = new Button
            {
                Content = "Test Preview",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            testPreviewButton.Click += TestPreviewButton_Click;

            Button viewLogsButton = new Button
            {
                Content = "View Logs",
                Width = 100,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            viewLogsButton.Click += ViewLogsButton_Click;

            Button closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(testApiButton);
            buttonPanel.Children.Add(testEventButton);
            buttonPanel.Children.Add(testPreviewButton);
            buttonPanel.Children.Add(viewLogsButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 2);

            // Add all to main grid
            mainGrid.Children.Add(_statusTextBlock);
            mainGrid.Children.Add(_promptTextBox);
            mainGrid.Children.Add(buttonPanel);

            // Set content
            Content = mainGrid;
        }

        private async void TestApiButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _statusTextBlock.Text = "Testing Claude API connection...";

                // Get API key from environment or config
                string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    TaskDialog.Show("API Key Missing",
                        "Please set CLAUDE_API_KEY environment variable.\n\n" +
                        "Get your API key from: https://console.anthropic.com/");
                    _statusTextBlock.Text = "API key not configured.";
                    return;
                }

                var claudeService = new Services.ClaudeService(apiKey);
                bool connected = await claudeService.TestConnectionAsync();

                if (connected)
                {
                    _statusTextBlock.Text = "✓ Claude API connection successful!\n\n" +
                        "API key is valid and service is reachable.\n" +
                        "Ready for Epic 2 implementation.";
                }
                else
                {
                    _statusTextBlock.Text = "✗ Claude API connection failed.\n\n" +
                        "Check your API key and internet connection.";
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Test failed:\n{ex.Message}");
                _statusTextBlock.Text = $"Error: {ex.Message}";
            }
        }

        private async void TestEventButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Close the dialog BEFORE awaiting ExternalEvent
                // This allows Revit's main thread to become idle and process the ExternalEvent callback
                Close();

                // Call from background thread (async) - ExternalEvent will marshal to Revit main thread
                var response = await Services.RevitEventHandler.TestEventHandlerAsync();

                // Show result in TaskDialog (since main dialog is closed)
                if (response.Success)
                {
                    TaskDialog.Show("ExternalEvent Test - SUCCESS",
                        "✓ ExternalEvent test successful!\n\n" +
                        $"Message: {response.Message}\n\n" +
                        "Story 1.3 (ExternalEvent Pattern) is working correctly.\n" +
                        "Background threads can now safely call Revit API operations.");
                }
                else
                {
                    TaskDialog.Show("ExternalEvent Test - FAILED",
                        "✗ ExternalEvent test failed.\n\n" +
                        $"Error: {response.Message}\n\n" +
                        $"Details: {response.ErrorDetails}");
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"ExternalEvent test failed:\n{ex.Message}\n\n{ex.StackTrace}");
            }
        }

        private void TestPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _statusTextBlock.Text = "Testing Preview/Confirm dialog...\n\n" +
                    "This demonstrates Story 1.5 (Preview/Confirm UX Pattern).";

                // Create sample preview for dimension operation
                var preview = OperationPreview.CreateCreationPreview(
                    operationType: "create_dimensions",
                    itemsToCreate: 47,
                    affectedElements: 12,
                    itemTypeName: "dimension chains");

                preview.Details["rooms"] = 12;
                preview.Details["walls"] = 48;
                preview.Details["level"] = "Level 1";

                // Show preview dialog and get user confirmation
                bool confirmed = PreviewConfirmDialog.ShowPreview(preview);

                if (confirmed)
                {
                    _statusTextBlock.Text = "✓ User CONFIRMED the operation!\n\n" +
                        "Story 1.5 (Preview/Confirm Pattern) is working correctly.\n\n" +
                        "In Epic 2, this confirmation will trigger actual Revit operations\n" +
                        "wrapped in Transactions for atomic commit/rollback.";
                }
                else
                {
                    _statusTextBlock.Text = "✗ User CANCELLED the operation.\n\n" +
                        "Story 1.5 (Preview/Confirm Pattern) is working correctly.\n\n" +
                        "Safety-first: No changes made to Revit model.";
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Preview test failed:\n{ex.Message}");
                _statusTextBlock.Text = $"Error: {ex.Message}\n\n{ex.StackTrace}";
            }
        }

        private void ViewLogsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var logger = Services.LoggingService.Instance;

                // Write test log entries
                logger.Info("User clicked View Logs button", "UI_TEST");
                logger.LogOperation("view_logs", "TRIGGERED", "Testing logging functionality");
                logger.Debug("This is a debug message", "UI_TEST");
                logger.Warning("This is a warning message", "UI_TEST");

                string logPath = logger.GetLogFilePath();

                _statusTextBlock.Text = "✓ Logging Infrastructure Working!\n\n" +
                    $"Log file location:\n{logPath}\n\n" +
                    "Story 1.6 (Logging Infrastructure) is complete.\n\n" +
                    "Features:\n" +
                    "• File logging to %APPDATA%/RevitAI/logs/\n" +
                    "• Rotating file handler (10MB, 5 backups)\n" +
                    "• Structured logging with timestamps\n" +
                    "• Operation context tracking\n\n" +
                    "Test log entries have been written to the file.";

                // Optionally open log file in default text editor
                if (System.IO.File.Exists(logPath))
                {
                    System.Diagnostics.Process.Start("notepad.exe", logPath);
                }
            }
            catch (Exception ex)
            {
                TaskDialog.Show("Error", $"Logging test failed:\n{ex.Message}");
                _statusTextBlock.Text = $"Error: {ex.Message}\n\n{ex.StackTrace}";
            }
        }
    }
}
