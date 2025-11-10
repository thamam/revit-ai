"""
Revit API Helper Functions
Utility functions for common Revit API operations
"""

from typing import List, Optional, Dict, Any

# Revit API imports
try:
    from Autodesk.Revit.DB import (
        FilteredElementCollector,
        BuiltInCategory,
        Level,
        Room,
        View,
        Transaction,
        XYZ
    )
    from Autodesk.Revit.UI import TaskDialog, TaskDialogCommonButtons
    REVIT_API_AVAILABLE = True
except ImportError:
    REVIT_API_AVAILABLE = False
    print("Warning: Revit API not available (development mode)")

from exceptions import RevitAPIError


def get_all_levels(doc) -> List:
    """
    Get all levels in the document

    Args:
        doc: Revit Document

    Returns:
        List of Level elements
    """
    if not REVIT_API_AVAILABLE:
        return []

    try:
        collector = FilteredElementCollector(doc)
        levels = collector.OfClass(Level).ToElements()
        return list(levels)
    except Exception as e:
        raise RevitAPIError(f"Failed to get levels: {e}")


def get_all_rooms(doc, level: Optional[Level] = None) -> List:
    """
    Get all rooms in the document or on a specific level

    Args:
        doc: Revit Document
        level: Optional Level to filter by

    Returns:
        List of Room elements
    """
    if not REVIT_API_AVAILABLE:
        return []

    try:
        collector = FilteredElementCollector(doc)
        rooms = collector.OfCategory(BuiltInCategory.OST_Rooms).ToElements()

        if level:
            # Filter by level
            level_id = level.Id
            rooms = [r for r in rooms if r.LevelId == level_id]

        return list(rooms)
    except Exception as e:
        raise RevitAPIError(f"Failed to get rooms: {e}")


def get_current_view(uidoc) -> Optional[View]:
    """
    Get the current active view

    Args:
        uidoc: Revit UIDocument

    Returns:
        Current View or None
    """
    if not REVIT_API_AVAILABLE:
        return None

    try:
        return uidoc.ActiveView
    except Exception as e:
        raise RevitAPIError(f"Failed to get current view: {e}")


def get_selected_elements(uidoc) -> List:
    """
    Get currently selected elements

    Args:
        uidoc: Revit UIDocument

    Returns:
        List of selected elements
    """
    if not REVIT_API_AVAILABLE:
        return []

    try:
        doc = uidoc.Document
        selection = uidoc.Selection
        element_ids = selection.GetElementIds()

        elements = [doc.GetElement(id) for id in element_ids]
        return [e for e in elements if e is not None]

    except Exception as e:
        raise RevitAPIError(f"Failed to get selected elements: {e}")


def show_message_dialog(title: str, message: str) -> None:
    """
    Show a simple message dialog

    Args:
        title: Dialog title
        message: Message text
    """
    if not REVIT_API_AVAILABLE:
        print(f"[DIALOG] {title}: {message}")
        return

    try:
        dialog = TaskDialog(title)
        dialog.MainContent = message
        dialog.CommonButtons = TaskDialogCommonButtons.Ok
        dialog.Show()
    except Exception as e:
        print(f"Failed to show dialog: {e}")


def show_confirmation_dialog(title: str, message: str) -> bool:
    """
    Show a confirmation dialog (Yes/No)

    Args:
        title: Dialog title
        message: Message text

    Returns:
        True if user clicked Yes, False if No
    """
    if not REVIT_API_AVAILABLE:
        print(f"[DIALOG] {title}: {message}")
        return True  # Default to Yes in development mode

    try:
        dialog = TaskDialog(title)
        dialog.MainContent = message
        dialog.CommonButtons = TaskDialogCommonButtons.Yes | TaskDialogCommonButtons.No
        result = dialog.Show()

        return result == TaskDialogCommonButtons.Yes

    except Exception as e:
        print(f"Failed to show confirmation dialog: {e}")
        return False


def build_revit_context(uidoc) -> Dict[str, Any]:
    """
    Build context dictionary with Revit project information

    Args:
        uidoc: Revit UIDocument

    Returns:
        Dictionary with Revit context
    """
    if not REVIT_API_AVAILABLE:
        return {
            'current_view': 'Development Mode',
            'levels': ['Level 1', 'Level 2'],
            'element_counts': {'Room': 0, 'Wall': 0, 'Door': 0},
            'selected_elements': 0
        }

    doc = uidoc.Document

    try:
        # Current view
        current_view = uidoc.ActiveView
        view_name = current_view.Name if current_view else "None"

        # Levels
        levels = get_all_levels(doc)
        level_names = [level.Name for level in levels]

        # Element counts
        element_counts = {}

        # Count rooms
        rooms = get_all_rooms(doc)
        element_counts['Room'] = len(rooms)

        # Count walls
        walls = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Walls).ToElements()
        element_counts['Wall'] = len(list(walls))

        # Count doors
        doors = FilteredElementCollector(doc).OfCategory(BuiltInCategory.OST_Doors).ToElements()
        element_counts['Door'] = len(list(doors))

        # Selected elements
        selected = get_selected_elements(uidoc)

        # Build context
        context = {
            'current_view': view_name,
            'levels': level_names,
            'element_counts': element_counts,
            'selected_elements': len(selected),
            'firm_standards': {
                'dimension_offset': 200,
                'dimension_style': 'Continuous'
            }
        }

        return context

    except Exception as e:
        raise RevitAPIError(f"Failed to build Revit context: {e}")


def test_revit_helpers():
    """Test function for Revit helpers (requires Revit context)"""
    print("Testing Revit Helpers...")

    if not REVIT_API_AVAILABLE:
        print("✗ Revit API not available (requires Revit)")
        return False

    # These tests require Revit context
    print("Note: Full tests require Revit context")
    print("Run from within Revit to test properly")

    return True


if __name__ == '__main__':
    # Run test when module is executed directly
    test_revit_helpers()
