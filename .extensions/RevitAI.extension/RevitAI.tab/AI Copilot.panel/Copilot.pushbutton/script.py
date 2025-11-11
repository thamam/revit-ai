"""
RevitAI Co-pilot - Main Entry Point
Launches the AI co-pilot dialog for natural language Revit automation
"""

# Standard library
import sys
import os

# Add lib to path for imports
lib_path = os.path.join(os.path.dirname(__file__), '..', '..', '..', 'lib')
if lib_path not in sys.path:
    sys.path.insert(0, lib_path)

# pyRevit imports
from pyrevit import script

# RevitAI imports
from ui_dialogs import show_copilot_dialog
from logger import get_logger

# Get current document
uidoc = __revit__.ActiveUIDocument  # noqa: F821
doc = uidoc.Document

# Initialize logger
logger = get_logger()


def main():
    """
    Main entry point for RevitAI Co-pilot button

    Epic 1 Complete: Foundation infrastructure ready
    - Story 1.1: ✓ Project Setup & pyRevit Extension Scaffold
    - Story 1.2: ✓ Claude API Integration & Secure Key Management
    - Story 1.3: ✓ ExternalEvent Pattern for Thread-Safe Revit API Access
    - Story 1.4: ✓ Operation Allowlist & Safety Validation Framework
    - Story 1.5: ✓ Preview/Confirm UX Pattern
    - Story 1.6: ✓ Logging & Diagnostics Infrastructure
    - Story 1.7: ✓ Basic Ribbon UI with Text Input Dialog

    Next: Epic 2 - Intelligent Dimension Automation
    """
    # Log button click
    logger.info("RevitAI Co-pilot button clicked")
    logger.info(f"Revit Version: {__revit__.Application.VersionNumber}")  # noqa: F821
    logger.info(f"pyRevit Version: {script.get_pyrevit_version()}")
    logger.info(f"Document: {doc.Title if doc else 'None'}")

    # Console output
    print("=" * 60)
    print("RevitAI Co-pilot - Epic 1 Complete!")
    print("=" * 60)
    print(f"Document: {doc.Title if doc else 'None'}")
    print(f"Revit Version: {__revit__.Application.VersionNumber}")  # noqa: F821
    print(f"pyRevit Version: {script.get_pyrevit_version()}")
    print("=" * 60)

    try:
        # Show main co-pilot dialog
        show_copilot_dialog(uidoc)

    except Exception as e:
        logger.error(f"Error in main entry point: {e}", exc_info=True)
        print(f"Error: {e}")
        # Show error in Revit
        from Autodesk.Revit.UI import TaskDialog
        TaskDialog.Show("RevitAI Error", f"An error occurred:\n{e}")


# Entry point when button is clicked
if __name__ == '__main__':
    main()
