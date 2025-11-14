using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using RevitAI.Models;

namespace RevitAI.UI
{
    /// <summary>
    /// Preview/Confirm Dialog
    /// Shows preview of proposed operation and requires user confirmation
    /// Story 1.5: Preview/Confirm UX Pattern
    /// </summary>
    public class PreviewConfirmDialog : Window
    {
        private readonly OperationPreview _preview;
        public bool UserConfirmed { get; private set; }

        public PreviewConfirmDialog(OperationPreview preview)
        {
            _preview = preview;
            UserConfirmed = false;

            // Window settings
            Title = "RevitAI - Preview & Confirm";
            Width = 600;
            Height = 450;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            ResizeMode = ResizeMode.NoResize;

            BuildUI();
        }

        private void BuildUI()
        {
            // Main grid
            Grid mainGrid = new Grid();
            mainGrid.Margin = new Thickness(20);

            // Define rows
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Preview area
            mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Header
            TextBlock headerText = new TextBlock
            {
                Text = "⚠️  Preview Proposed Changes",
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 20),
                Foreground = new SolidColorBrush(Color.FromRgb(0, 120, 215)) // Blue
            };
            Grid.SetRow(headerText, 0);

            // Preview area
            Border previewBorder = new Border
            {
                BorderBrush = new SolidColorBrush(Colors.LightGray),
                BorderThickness = new Thickness(1),
                Background = new SolidColorBrush(Color.FromRgb(250, 250, 250)),
                Padding = new Thickness(15),
                Margin = new Thickness(0, 0, 0, 20)
            };
            Grid.SetRow(previewBorder, 1);

            StackPanel previewContent = new StackPanel();

            // Operation type
            TextBlock operationTypeLabel = new TextBlock
            {
                Text = "Operation:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            TextBlock operationTypeValue = new TextBlock
            {
                Text = _preview.OperationType.Replace("_", " ").ToUpper(),
                FontSize = 14,
                Foreground = new SolidColorBrush(Color.FromRgb(0, 100, 0)), // Dark green
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Summary
            TextBlock summaryLabel = new TextBlock
            {
                Text = "What will happen:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            TextBlock summaryValue = new TextBlock
            {
                Text = _preview.Summary,
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Affected elements count
            TextBlock affectedLabel = new TextBlock
            {
                Text = "Affected elements:",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0, 0, 0, 5)
            };
            TextBlock affectedValue = new TextBlock
            {
                Text = $"{_preview.AffectedElementCount} elements",
                FontSize = 14,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Details (if any)
            if (_preview.Details != null && _preview.Details.Count > 0)
            {
                TextBlock detailsLabel = new TextBlock
                {
                    Text = "Details:",
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(0, 0, 0, 5)
                };

                StackPanel detailsPanel = new StackPanel
                {
                    Margin = new Thickness(10, 0, 0, 0)
                };

                foreach (var detail in _preview.Details)
                {
                    TextBlock detailItem = new TextBlock
                    {
                        Text = $"• {detail.Key}: {detail.Value}",
                        Margin = new Thickness(0, 0, 0, 3)
                    };
                    detailsPanel.Children.Add(detailItem);
                }

                previewContent.Children.Add(detailsLabel);
                previewContent.Children.Add(detailsPanel);
            }

            // Visual preview placeholder (Epic 2 feature)
            if (_preview.SupportsVisualPreview)
            {
                TextBlock visualPreviewNote = new TextBlock
                {
                    Text = "📊 Visual preview will be available in Epic 2",
                    FontStyle = FontStyles.Italic,
                    Foreground = new SolidColorBrush(Colors.Gray),
                    Margin = new Thickness(0, 15, 0, 0)
                };
                previewContent.Children.Add(visualPreviewNote);
            }

            // Add all to preview content
            previewContent.Children.Insert(0, operationTypeLabel);
            previewContent.Children.Insert(1, operationTypeValue);
            previewContent.Children.Insert(2, summaryLabel);
            previewContent.Children.Insert(3, summaryValue);
            previewContent.Children.Insert(4, affectedLabel);
            previewContent.Children.Insert(5, affectedValue);

            previewBorder.Child = previewContent;

            // Button panel
            StackPanel buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };

            Button confirmButton = new Button
            {
                Content = "✓ Confirm",
                Width = 120,
                Height = 35,
                FontSize = 14,
                Margin = new Thickness(0, 0, 10, 0),
                Background = new SolidColorBrush(Color.FromRgb(0, 120, 215)), // Blue
                Foreground = new SolidColorBrush(Colors.White),
                BorderThickness = new Thickness(0)
            };
            confirmButton.Click += ConfirmButton_Click;

            Button cancelButton = new Button
            {
                Content = "✗ Cancel",
                Width = 120,
                Height = 35,
                FontSize = 14
            };
            cancelButton.Click += CancelButton_Click;

            buttonPanel.Children.Add(confirmButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            // Add all to main grid
            mainGrid.Children.Add(headerText);
            mainGrid.Children.Add(previewBorder);
            mainGrid.Children.Add(buttonPanel);

            Content = mainGrid;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = true;
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            UserConfirmed = false;
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Show preview dialog and get user confirmation
        /// </summary>
        public static bool ShowPreview(OperationPreview preview)
        {
            var dialog = new PreviewConfirmDialog(preview);
            var result = dialog.ShowDialog();
            return result == true && dialog.UserConfirmed;
        }
    }
}
