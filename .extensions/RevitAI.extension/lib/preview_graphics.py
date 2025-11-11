"""
Preview Graphics
Handles visualization of proposed changes before committing
"""

from typing import List, Optional, Dict, Any

# Revit API imports
try:
    from Autodesk.Revit.DB import (
        Transaction,
        Color,
        XYZ
    )
    from Autodesk.Revit.UI import TaskDialog, TaskDialogCommonButtons, TaskDialogResult
    REVIT_API_AVAILABLE = True
except ImportError:
    REVIT_API_AVAILABLE = False
    print("Warning: Revit API not available (development mode)")

from exceptions import PreviewError
from logger import get_logger

logger = get_logger()


class PreviewManager:
    """
    Manages preview graphics for AI operations

    Shows proposed changes (dimensions, tags, etc.) before committing
    using temporary visualization.

    Note: Full DirectContext3D implementation will be added in future iteration.
    Current implementation uses simple dialog-based preview.
    """

    def __init__(self, uidoc):
        """
        Initialize preview manager

        Args:
            uidoc: Revit UIDocument
        """
        self.uidoc = uidoc
        self.doc = uidoc.Document if uidoc else None
        self.preview_elements = []

    def show_dimension_preview(
        self,
        action: Dict[str, Any],
        dimension_count: int
    ) -> bool:
        """
        Show preview of dimension operation

        Args:
            action: Action dictionary describing the operation
            dimension_count: Estimated number of dimensions to create

        Returns:
            True if user confirms, False if user cancels
        """
        targets = action.get('targets', {})
        element_type = targets.get('element_type', 'elements')
        scope = targets.get('scope', 'current view')

        # Build preview message
        message = f"""Preview: Dimension Operation

**Operation:** Create continuous dimensions
**Target:** {element_type} in {scope}
**Estimated Dimensions:** {dimension_count} dimension chains

This operation will:
• Analyze room/element boundaries
• Create dimension chains at 200mm offset
• Use firm's default dimension style

The operation is reversible with Ctrl+Z (Undo).

Do you want to proceed?"""

        return self._show_confirmation(
            title="Preview: Add Dimensions",
            message=message
        )

    def show_tag_preview(
        self,
        action: Dict[str, Any],
        tag_count: int
    ) -> bool:
        """
        Show preview of tag operation

        Args:
            action: Action dictionary describing the operation
            tag_count: Estimated number of tags to create

        Returns:
            True if user confirms, False if user cancels
        """
        targets = action.get('targets', {})
        element_type = targets.get('element_type', 'elements')
        scope = targets.get('scope', 'current view')

        # Build preview message
        message = f"""Preview: Tag Operation

**Operation:** Create element tags
**Target:** {element_type} in {scope}
**Estimated Tags:** {tag_count} tags

This operation will:
• Find all {element_type} elements
• Position tags using intelligent placement
• Apply firm's default tag family
• Avoid overlapping tags

The operation is reversible with Ctrl+Z (Undo).

Do you want to proceed?"""

        return self._show_confirmation(
            title="Preview: Add Tags",
            message=message
        )

    def show_generic_preview(
        self,
        operation: str,
        description: str,
        item_count: int
    ) -> bool:
        """
        Show generic preview dialog

        Args:
            operation: Operation name
            description: Operation description
            item_count: Number of items to be created/modified

        Returns:
            True if user confirms, False if user cancels
        """
        message = f"""Preview: {operation}

{description}

**Items to process:** {item_count}

The operation is reversible with Ctrl+Z (Undo).

Do you want to proceed?"""

        return self._show_confirmation(
            title=f"Preview: {operation}",
            message=message
        )

    def _show_confirmation(self, title: str, message: str) -> bool:
        """
        Show confirmation dialog

        Args:
            title: Dialog title
            message: Message text

        Returns:
            True if user confirmed, False otherwise
        """
        if not REVIT_API_AVAILABLE:
            print(f"[PREVIEW] {title}")
            print(message)
            logger.info(f"Preview shown (development mode): {title}")
            return True

        try:
            dialog = TaskDialog(title)
            dialog.MainInstruction = "Review and Confirm"
            dialog.MainContent = message
            dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
            dialog.DefaultButton = TaskDialogCommonButtons.No  # Safety: default to No

            result = dialog.Show()

            confirmed = (result == TaskDialogResult.Yes)

            logger.info(f"Preview confirmation: {title} - {'Confirmed' if confirmed else 'Cancelled'}")

            return confirmed

        except Exception as e:
            logger.exception(f"Failed to show preview dialog: {e}")
            raise PreviewError(f"Preview dialog failed: {e}") from e

    def clear_preview(self):
        """
        Clear any preview graphics

        This will be expanded when DirectContext3D is implemented.
        """
        # Future: Clear DirectContext3D preview graphics
        self.preview_elements = []
        logger.debug("Preview cleared")


def test_preview_manager():
    """Test function for preview manager (requires Revit context)"""
    print("Testing Preview Manager...")

    if not REVIT_API_AVAILABLE:
        print("✗ Revit API not available")
        print("Note: Run from within Revit to test preview dialogs")
        return False

    print("Note: Full tests require Revit context")
    return True


if __name__ == '__main__':
    # Run test when module is executed directly
    test_preview_manager()
