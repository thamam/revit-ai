using System;
using System.Windows;
using System.Windows.Controls;
using Autodesk.Revit.UI;

namespace RevitAI.UI
{
    /// <summary>
    /// Main Copilot Dialog
    /// Simple WPF dialog for AI Copilot interaction
    /// </summary>
    public class CopilotDialog : Window
    {
        private readonly UIDocument _uiDoc;
        private TextBox _promptTextBox;
        private TextBlock _statusTextBlock;

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
            _promptTextBox = new TextBox
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

            Button testButton = new Button
            {
                Content = "Test Claude API",
                Width = 120,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            testButton.Click += TestButton_Click;

            Button closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(testButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 2);

            // Add all to main grid
            mainGrid.Children.Add(_statusTextBlock);
            mainGrid.Children.Add(_promptTextBox);
            mainGrid.Children.Add(buttonPanel);

            // Set content
            Content = mainGrid;
        }

        private async void TestButton_Click(object sender, RoutedEventArgs e)
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
    }
}
