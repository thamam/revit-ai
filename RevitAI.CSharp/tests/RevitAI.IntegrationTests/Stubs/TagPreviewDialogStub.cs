using System.Collections.Generic;
using RevitAI.Models.Domain;

namespace RevitAI.UI
{
    /// <summary>
    /// Stub implementation of TagPreviewDialog for integration testing.
    /// This is a minimal stub that allows AutoTagWorkflow to compile without WPF dependencies.
    /// The actual WPF dialog cannot be tested in headless integration tests.
    /// </summary>
    public class TagPreviewDialog
    {
        private readonly List<TagPlacement> _placements;
        private readonly string _category;

        public TagPreviewDialog(List<TagPlacement> placements, string category)
        {
            _placements = placements;
            _category = category;
        }

        /// <summary>
        /// Simulates showing the dialog.
        /// In integration tests, this will throw an exception because we can't actually show a WPF dialog.
        /// Tests should catch this and verify that Steps 1-5 executed correctly before this point.
        /// </summary>
        public bool? ShowDialog()
        {
            throw new System.InvalidOperationException(
                "TagPreviewDialog.ShowDialog() cannot be called in headless integration tests. " +
                "This is expected - tests verify workflow up to Step 5 (placement calculation). " +
                "Preview dialog and tag creation (Steps 6-7) require manual testing in Revit."
            );
        }
    }
}
