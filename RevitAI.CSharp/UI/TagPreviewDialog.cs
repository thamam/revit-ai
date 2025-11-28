using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using RevitAI.Models;
using RevitAI.Models.Domain;

namespace RevitAI.UI
{
    /// <summary>
    /// Enhanced Preview Dialog for Auto-Tagging Operations
    /// Displays tag placement status with detailed element-level feedback
    /// Story 2.3: Preview UI Integration (Task 1)
    /// </summary>
    /// <remarks>
    /// This dialog extends the base PreviewConfirmDialog to provide:
    /// - Tag placement summary with success/failure counts
    /// - Element-level status list (Success/Warning/Failed)
    /// - Temporary visual graphics in Revit viewport (with graceful fallback)
    /// - User confirmation workflow (Preview -> Confirm/Cancel)
    ///
    /// AC-2.3.1: Enhanced preview with element status and visual graphics
    /// AC-2.3.2: Graceful fallback if DirectContext3D unavailable
    /// </remarks>
    public class TagPreviewDialog : PreviewConfirmDialog
    {
        private readonly List<TagPlacement> _placements;
        private readonly string _categoryName;
        private int _previewGraphicsId = -1;

        /// <summary>
        /// Creates a new tag preview dialog with placement results.
        /// </summary>
        /// <param name="placements">List of calculated tag placements (success and failed)</param>
        /// <param name="categoryName">Category being tagged (e.g., "Door", "Window")</param>
        public TagPreviewDialog(List<TagPlacement> placements, string categoryName)
            : base(CreatePreview(placements, categoryName))
        {
            _placements = placements ?? throw new ArgumentNullException(nameof(placements));
            _categoryName = categoryName ?? "Element";

            // Resize window to accommodate status list
            Height = 600;
            Width = 700;

            // Add tag status list to the dialog
            AddTagStatusList();

            // Attempt to render temporary graphics
            // Graceful fallback: if DirectContext3D fails, dialog still works
            try
            {
                DrawTemporaryGraphics();
            }
            catch (Exception ex)
            {
                // Log warning but continue - user still sees status list
                Console.WriteLine($"Warning: Could not render preview graphics: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                // Note: In production, this would use LoggingService
                // logger.Warning($"Preview graphics unavailable: {ex.Message}", "TAG_PREVIEW");
            }
        }

        /// <summary>
        /// Factory method to create OperationPreview from tag placements.
        /// </summary>
        /// <remarks>
        /// Generates user-friendly summary:
        /// - All success: "Preview: 24 Door Tags will be added"
        /// - Partial success: "Preview: 22 of 24 Door Tags will be added (2 failed)"
        /// </remarks>
        private static OperationPreview CreatePreview(List<TagPlacement> placements, string categoryName)
        {
            if (placements == null || placements.Count == 0)
            {
                return new OperationPreview
                {
                    OperationType = "auto_tag",
                    Summary = "No elements to tag",
                    AffectedElementCount = 0,
                    SupportsVisualPreview = false
                };
            }

            int successCount = placements.Count(p => p.IsSuccess);
            int failCount = placements.Count - successCount;
            int totalCount = placements.Count;

            // Generate summary message
            string summary;
            if (failCount == 0)
            {
                summary = $"Preview: {successCount} {categoryName} Tags will be added";
            }
            else
            {
                summary = $"Preview: {successCount} of {totalCount} {categoryName} Tags will be added ({failCount} failed)";
            }

            return new OperationPreview
            {
                OperationType = "auto_tag",
                Summary = summary,
                AffectedElementCount = totalCount,
                SupportsVisualPreview = true,
                Details = new Dictionary<string, object>
                {
                    { "category", categoryName },
                    { "success_count", successCount },
                    { "failed_count", failCount },
                    { "total_count", totalCount }
                }
            };
        }

        /// <summary>
        /// Adds element status list to the preview area.
        /// Shows each element's placement result with status icon.
        /// </summary>
        /// <remarks>
        /// Status Icons:
        /// - ✓ Success: Placement found on first attempt
        /// - ⚠ Warning: Placement found after retries (collision avoidance)
        /// - ✗ Failed: No collision-free placement found
        /// </remarks>
        private void AddTagStatusList()
        {
            // Find the preview border in the base dialog
            // The base dialog structure: Grid > Border (row 1)
            if (Content is Grid mainGrid && mainGrid.Children.Count >= 2)
            {
                // Get the preview border (row 1)
                Border previewBorder = null;
                foreach (var child in mainGrid.Children)
                {
                    if (child is Border border && Grid.GetRow(border) == 1)
                    {
                        previewBorder = border;
                        break;
                    }
                }

                if (previewBorder != null && previewBorder.Child is StackPanel previewContent)
                {
                    // Add separator
                    Border separator = new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(Colors.LightGray),
                        Margin = new Thickness(0, 15, 0, 15)
                    };
                    previewContent.Children.Add(separator);

                    // Add status list header
                    TextBlock statusHeader = new TextBlock
                    {
                        Text = "Element Status:",
                        FontWeight = FontWeights.Bold,
                        Margin = new Thickness(0, 0, 0, 10)
                    };
                    previewContent.Children.Add(statusHeader);

                    // Create scrollable DataGrid for tag status
                    DataGrid statusGrid = new DataGrid
                    {
                        AutoGenerateColumns = false,
                        IsReadOnly = true,
                        CanUserAddRows = false,
                        CanUserDeleteRows = false,
                        CanUserReorderColumns = false,
                        CanUserResizeRows = false,
                        HeadersVisibility = DataGridHeadersVisibility.Column,
                        GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                        MaxHeight = 250,
                        VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                        HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                        BorderBrush = new SolidColorBrush(Colors.LightGray),
                        BorderThickness = new Thickness(1)
                    };

                    // Define columns
                    statusGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Status",
                        Binding = new System.Windows.Data.Binding("StatusIcon"),
                        Width = 60
                    });

                    statusGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Element ID",
                        Binding = new System.Windows.Data.Binding("ElementId"),
                        Width = 100
                    });

                    statusGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Status Text",
                        Binding = new System.Windows.Data.Binding("StatusText"),
                        Width = new DataGridLength(1, DataGridLengthUnitType.Star) // Fill remaining space
                    });

                    statusGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = "Details",
                        Binding = new System.Windows.Data.Binding("FailureReason"),
                        Width = new DataGridLength(1.5, DataGridLengthUnitType.Star) // Wider for failure reasons
                    });

                    // Populate with data
                    List<TagStatusItem> statusItems = _placements.Select(p => new TagStatusItem
                    {
                        ElementId = p.ElementId,
                        ElementName = $"Element {p.ElementId}", // TODO: Get actual element name from Revit
                        StatusIcon = GetStatusIcon(p),
                        StatusText = GetStatusText(p),
                        FailureReason = p.FailureReason ?? (p.AttemptsUsed > 1 ? $"Placed after {p.AttemptsUsed} attempts" : "Placed successfully")
                    }).ToList();

                    statusGrid.ItemsSource = statusItems;

                    // Add to preview content
                    previewContent.Children.Add(statusGrid);

                    // Add statistics summary
                    int successCount = _placements.Count(p => p.IsSuccess);
                    int warningCount = _placements.Count(p => p.IsSuccess && p.AttemptsUsed > 1);
                    int failCount = _placements.Count(p => !p.IsSuccess);

                    TextBlock statsText = new TextBlock
                    {
                        Text = $"✓ {successCount} Success  |  ⚠ {warningCount} Warnings  |  ✗ {failCount} Failed",
                        FontStyle = FontStyles.Italic,
                        Foreground = new SolidColorBrush(Colors.Gray),
                        Margin = new Thickness(0, 10, 0, 0),
                        HorizontalAlignment = HorizontalAlignment.Center
                    };
                    previewContent.Children.Add(statsText);
                }
            }
        }

        /// <summary>
        /// Gets status icon based on placement result.
        /// </summary>
        private static string GetStatusIcon(TagPlacement placement)
        {
            if (!placement.IsSuccess)
                return "✗"; // Failed

            if (placement.AttemptsUsed > 1)
                return "⚠"; // Warning (needed retries)

            return "✓"; // Success
        }

        /// <summary>
        /// Gets human-readable status text.
        /// </summary>
        private static string GetStatusText(TagPlacement placement)
        {
            if (!placement.IsSuccess)
                return "Failed";

            if (placement.AttemptsUsed > 1)
                return "Success (with retries)";

            return "Success";
        }

        /// <summary>
        /// Renders temporary graphics in Revit viewport.
        /// Shows small rectangles at tag locations with color coding.
        /// </summary>
        /// <remarks>
        /// Graphics:
        /// - Green rectangles: Successful placements
        /// - Yellow rectangles: Warnings (placed after retries)
        /// - Red rectangles: Failed placements
        /// - Leader lines: Shown if HasLeader = true
        ///
        /// IMPORTANT: DirectContext3D may not work on Linux or without Revit running.
        /// Caller must catch exceptions and handle gracefully.
        /// </remarks>
        private void DrawTemporaryGraphics()
        {
            // PLACEHOLDER: DirectContext3D implementation
            // This requires Revit API access and will fail on Linux
            // Graceful fallback is handled by caller's try/catch

            // TODO (Epic 2 - Story 2.3 - Task 1):
            // Implement DirectContext3D graphics:
            // 1. Create DirectContext3DServer instance
            // 2. For each placement:
            //    - Draw small rectangle (0.3' x 0.15') at placement.Location
            //    - Color based on status: Green (success), Yellow (warning), Red (failed)
            //    - Draw leader line if placement.HasLeader == true
            // 3. Register server with Revit view
            // 4. Store server ID in _previewGraphicsId for cleanup

            // Example pseudocode:
            // var server = new TagPreviewGraphicsServer(_placements);
            // _previewGraphicsId = DirectContext3DService.RegisterServer(server);

            // For now, throw NotImplementedException to trigger graceful fallback
            throw new NotImplementedException(
                "DirectContext3D graphics not implemented yet. " +
                "Dialog will display element status list only.");
        }

        /// <summary>
        /// Clears temporary graphics from Revit viewport.
        /// </summary>
        private void ClearTemporaryGraphics()
        {
            if (_previewGraphicsId != -1)
            {
                try
                {
                    // TODO (Epic 2 - Story 2.3 - Task 1):
                    // DirectContext3DService.UnregisterServer(_previewGraphicsId);
                    _previewGraphicsId = -1;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Could not clear preview graphics: {ex.Message}");
                    // Not critical - graphics will be cleared when view refreshes
                }
            }
        }

        /// <summary>
        /// Cleanup when dialog closes.
        /// </summary>
        protected override void OnClosed(EventArgs e)
        {
            ClearTemporaryGraphics();
            base.OnClosed(e);
        }

        /// <summary>
        /// Static helper to show tag preview dialog and get user confirmation.
        /// </summary>
        /// <param name="placements">Calculated tag placements</param>
        /// <param name="categoryName">Category being tagged</param>
        /// <returns>True if user confirmed, false if cancelled</returns>
        public static bool ShowTagPreview(List<TagPlacement> placements, string categoryName)
        {
            var dialog = new TagPreviewDialog(placements, categoryName);
            var result = dialog.ShowDialog();
            return result == true && dialog.UserConfirmed;
        }

        /// <summary>
        /// Inner class for DataGrid binding.
        /// Represents a single row in the element status list.
        /// </summary>
        private class TagStatusItem
        {
            public int ElementId { get; set; }
            public string ElementName { get; set; }
            public string StatusIcon { get; set; }
            public string StatusText { get; set; }
            public string FailureReason { get; set; }
        }
    }
}
