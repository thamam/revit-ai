using System;
using System.Windows;
using System.Windows.Controls;
using System.Diagnostics;

namespace RevitAI.UI
{
    /// <summary>
    /// Settings Dialog
    /// Configure RevitAI settings and API key
    /// </summary>
    public class SettingsDialog : Window
    {
        private TextBlock _statusTextBlock;

        public SettingsDialog()
        {
            // Window settings
            Title = "RevitAI Settings";
            Width = 500;
            Height = 350;
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

            // Status/Info text
            _statusTextBlock = new TextBlock
            {
                Text = GetSettingsInfo(),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };
            Grid.SetRow(_statusTextBlock, 0);

            // Button panel
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button docsButton = new Button
            {
                Content = "Open Documentation",
                Width = 150,
                Height = 30,
                Margin = new Thickness(0, 0, 10, 0)
            };
            docsButton.Click += DocsButton_Click;

            Button closeButton = new Button
            {
                Content = "Close",
                Width = 80,
                Height = 30
            };
            closeButton.Click += (s, e) => Close();

            buttonPanel.Children.Add(docsButton);
            buttonPanel.Children.Add(closeButton);
            Grid.SetRow(buttonPanel, 1);

            // Add all to main grid
            mainGrid.Children.Add(_statusTextBlock);
            mainGrid.Children.Add(buttonPanel);

            // Set content
            Content = mainGrid;
        }

        private string GetSettingsInfo()
        {
            string apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY");
            bool hasApiKey = !string.IsNullOrWhiteSpace(apiKey);

            return $@"RevitAI Settings - C# SDK Implementation

**Current Configuration:**

API Key: {(hasApiKey ? "✓ Configured" : "✗ Not Configured")}
Model: claude-sonnet-4-20250514
Framework: Revit C# SDK
Status: Epic 1 Complete

**To Configure API Key:**

1. Set Windows environment variable:
   CLAUDE_API_KEY=sk-ant-your-key-here

2. Restart Revit for changes to take effect

**Get Claude API Key:**
Visit: https://console.anthropic.com/

**Project Info:**
- GitHub: https://github.com/thamam/revit-ai
- Branch: csharp-sdk-implementation
- Architecture: C# .NET Framework 4.8
- Revit Version: 2024+";
        }

        private void DocsButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "https://github.com/thamam/revit-ai",
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open documentation:\n{ex.Message}", "Error");
            }
        }
    }
}
