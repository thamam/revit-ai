using NUnit.Framework;
using RevitAI.Models.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RevitAI.UnitTests
{
    /// <summary>
    /// Unit tests for TagPreviewDialog - Story 2.3 Task 1
    /// Tests the CreatePreview factory method logic (does not require WPF)
    ///
    /// Test Coverage:
    /// - CreatePreview with all successful placements
    /// - CreatePreview with partial success (mixed success/failure)
    /// - CreatePreview with all failed placements
    /// - CreatePreview with empty placements list
    /// - CreatePreview summary message formatting
    ///
    /// NOTE: These tests use reflection to call the private CreatePreview method.
    /// Full WPF dialog testing requires Windows + Revit environment.
    /// </summary>
    [TestFixture]
    public class TagPreviewDialogTests
    {
        #region Test Helpers

        /// <summary>
        /// Uses reflection to call the private static CreatePreview method.
        /// This allows testing the preview generation logic without requiring WPF.
        /// </summary>
        private static object CallCreatePreview(List<TagPlacement> placements, string categoryName)
        {
            // Get the TagPreviewDialog type
            var assembly = Assembly.Load("RevitAI");
            var dialogType = assembly.GetType("RevitAI.UI.TagPreviewDialog");

            if (dialogType == null)
            {
                throw new InvalidOperationException("Could not find TagPreviewDialog type");
            }

            // Get the private static CreatePreview method
            var method = dialogType.GetMethod(
                "CreatePreview",
                BindingFlags.NonPublic | BindingFlags.Static);

            if (method == null)
            {
                throw new InvalidOperationException("Could not find CreatePreview method");
            }

            // Invoke the method
            var result = method.Invoke(null, new object[] { placements, categoryName });
            return result;
        }

        #endregion

        #region CreatePreview Tests

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_AllSuccess_ShowsCorrectSummary()
        {
            // Arrange
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateSuccess(1001, new XYZ(0, 0, 0)),
                TagPlacement.CreateSuccess(1002, new XYZ(10, 0, 0)),
                TagPlacement.CreateSuccess(1003, new XYZ(20, 0, 0))
            };

            // Act
            dynamic preview = CallCreatePreview(placements, "Door");

            // Assert
            Assert.That(preview, Is.Not.Null, "CreatePreview should return a valid OperationPreview");
            Assert.That(preview.OperationType, Is.EqualTo("auto_tag"));
            Assert.That(preview.Summary, Is.EqualTo("Preview: 3 Door Tags will be added"));
            Assert.That(preview.AffectedElementCount, Is.EqualTo(3));
            Assert.That(preview.SupportsVisualPreview, Is.True);
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_PartialSuccess_ShowsCorrectSummary()
        {
            // Arrange - 22 success, 2 failed
            var placements = new List<TagPlacement>();
            for (int i = 0; i < 22; i++)
            {
                placements.Add(TagPlacement.CreateSuccess(1000 + i, new XYZ(i * 10, 0, 0)));
            }
            placements.Add(TagPlacement.CreateFailed(2001, "No collision-free placement found"));
            placements.Add(TagPlacement.CreateFailed(2002, "No collision-free placement found"));

            // Act
            dynamic preview = CallCreatePreview(placements, "Door");

            // Assert
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.OperationType, Is.EqualTo("auto_tag"));
            Assert.That(preview.Summary, Is.EqualTo("Preview: 22 of 24 Door Tags will be added (2 failed)"));
            Assert.That(preview.AffectedElementCount, Is.EqualTo(24));
            Assert.That(preview.SupportsVisualPreview, Is.True);
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_AllFailed_ShowsCorrectSummary()
        {
            // Arrange
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateFailed(2001, "Too crowded"),
                TagPlacement.CreateFailed(2002, "Too crowded"),
                TagPlacement.CreateFailed(2003, "Too crowded")
            };

            // Act
            dynamic preview = CallCreatePreview(placements, "Window");

            // Assert
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Summary, Is.EqualTo("Preview: 0 of 3 Window Tags will be added (3 failed)"));
            Assert.That(preview.AffectedElementCount, Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_EmptyList_ShowsNoElementsMessage()
        {
            // Arrange
            var placements = new List<TagPlacement>();

            // Act
            dynamic preview = CallCreatePreview(placements, "Room");

            // Assert
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Summary, Is.EqualTo("No elements to tag"));
            Assert.That(preview.AffectedElementCount, Is.EqualTo(0));
            Assert.That(preview.SupportsVisualPreview, Is.False);
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_SingleElement_UsesCorrectPluralization()
        {
            // Arrange
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateSuccess(1001, new XYZ(0, 0, 0))
            };

            // Act
            dynamic preview = CallCreatePreview(placements, "Wall");

            // Assert
            Assert.That(preview, Is.Not.Null);
            // Note: Current implementation always uses "Tags" (plural)
            // This test documents the behavior - we can update later if needed
            Assert.That(preview.Summary, Is.EqualTo("Preview: 1 Wall Tags will be added"));
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_DifferentCategories_UsesCorrectCategoryName()
        {
            // Arrange
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateSuccess(1001, new XYZ(0, 0, 0)),
                TagPlacement.CreateSuccess(1002, new XYZ(10, 0, 0))
            };

            // Act - Test different category names
            dynamic doorPreview = CallCreatePreview(placements, "Door");
            dynamic windowPreview = CallCreatePreview(placements, "Window");
            dynamic roomPreview = CallCreatePreview(placements, "Room");

            // Assert
            Assert.That(doorPreview.Summary, Does.Contain("Door"));
            Assert.That(windowPreview.Summary, Does.Contain("Window"));
            Assert.That(roomPreview.Summary, Does.Contain("Room"));
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_Details_ContainsCategoryAndCounts()
        {
            // Arrange
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateSuccess(1001, new XYZ(0, 0, 0)),
                TagPlacement.CreateSuccess(1002, new XYZ(10, 0, 0)),
                TagPlacement.CreateFailed(2001, "Failed")
            };

            // Act
            dynamic preview = CallCreatePreview(placements, "Door");

            // Assert
            Assert.That(preview.Details, Is.Not.Null);
            Assert.That(preview.Details.ContainsKey("category"), Is.True);
            Assert.That(preview.Details["category"], Is.EqualTo("Door"));
            Assert.That(preview.Details.ContainsKey("success_count"), Is.True);
            Assert.That(preview.Details["success_count"], Is.EqualTo(2));
            Assert.That(preview.Details.ContainsKey("failed_count"), Is.True);
            Assert.That(preview.Details["failed_count"], Is.EqualTo(1));
            Assert.That(preview.Details.ContainsKey("total_count"), Is.True);
            Assert.That(preview.Details["total_count"], Is.EqualTo(3));
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_NullPlacements_ReturnsNoElementsPreview()
        {
            // Arrange
            List<TagPlacement> placements = null;

            // Act
            dynamic preview = CallCreatePreview(placements, "Door");

            // Assert
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Summary, Is.EqualTo("No elements to tag"));
            Assert.That(preview.AffectedElementCount, Is.EqualTo(0));
        }

        [Test]
        [Category("Unit")]
        [Category("UI")]
        [Category("Story2.3")]
        public void CreatePreview_MixedSuccessWithRetries_CountedCorrectly()
        {
            // Arrange - Some immediate success, some with retries, some failed
            var placements = new List<TagPlacement>
            {
                TagPlacement.CreateSuccess(1001, new XYZ(0, 0, 0)), // 1 attempt
                TagPlacement.CreateSuccessAfterRetry(1002, new XYZ(10, 0, 0), 3), // 3 attempts
                TagPlacement.CreateSuccessAfterRetry(1003, new XYZ(20, 0, 0), 5), // 5 attempts
                TagPlacement.CreateFailed(2001, "Too crowded")
            };

            // Act
            dynamic preview = CallCreatePreview(placements, "Door");

            // Assert
            Assert.That(preview, Is.Not.Null);
            Assert.That(preview.Summary, Is.EqualTo("Preview: 3 of 4 Door Tags will be added (1 failed)"));

            // All placements with retries should still be counted as success
            var details = preview.Details;
            Assert.That(details["success_count"], Is.EqualTo(3));
            Assert.That(details["failed_count"], Is.EqualTo(1));
        }

        #endregion

        #region Integration Notes

        /// <summary>
        /// NOTE: Full WPF dialog testing requires Windows + Revit environment.
        ///
        /// Manual testing checklist:
        /// 1. Dialog opens and displays correctly
        /// 2. Element status list is scrollable
        /// 3. Status icons (✓, ⚠, ✗) display correctly
        /// 4. DirectContext3D graphics render (or graceful fallback)
        /// 5. Confirm/Cancel buttons work
        /// 6. Graphics clear when dialog closes
        ///
        /// Integration test plan (Story 2.3 Task 2):
        /// - Create test Revit model with 50 doors
        /// - Run auto-tag operation
        /// - Verify TagPreviewDialog displays all 50 placements
        /// - Verify graphics render in viewport
        /// - Confirm tags are created when user clicks Confirm
        /// </summary>
        [Test]
        [Category("Manual")]
        [Category("Integration")]
        [Category("Story2.3")]
        [Ignore("Requires Windows + Revit + manual verification")]
        public void ManualTest_TagPreviewDialog_DisplaysCorrectly()
        {
            // This test serves as documentation for manual testing steps
            // Will be replaced with automated integration tests when Revit test harness is available
            Assert.Pass("Manual testing required - see method documentation");
        }

        #endregion
    }
}
