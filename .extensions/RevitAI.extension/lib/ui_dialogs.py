"""
UI Dialogs
User interface dialogs for RevitAI
"""

import sys
import os

# Add lib to path
lib_path = os.path.dirname(os.path.abspath(__file__))
if lib_path not in sys.path:
    sys.path.insert(0, lib_path)

from typing import Optional, Dict, Any
import threading

# Revit API imports
try:
    from Autodesk.Revit.UI import TaskDialog, TaskDialogCommonButtons, TaskDialogResult
    REVIT_API_AVAILABLE = True
except ImportError:
    REVIT_API_AVAILABLE = False

# RevitAI imports
from claude_client import ClaudeClient
from safety_validator import validate_action
from config_manager import get_config_manager
from logger import get_logger, OperationLogger
from revit_helpers import build_revit_context, show_message_dialog
from preview_graphics import PreviewManager
from exceptions import APIError, ValidationError, ConfigurationError

logger = get_logger()


def show_copilot_dialog(uidoc):
    """
    Show the main AI Co-pilot dialog

    Args:
        uidoc: Revit UIDocument
    """
    # For now, use simple TaskDialog
    # Future: Create WPF form with text input, RTL support, example prompts

    if not REVIT_API_AVAILABLE:
        print("Revit API not available - cannot show dialog")
        return

    try:
        # Check API key configuration
        config = get_config_manager()
        if not config.validate_api_key():
            show_api_key_required_dialog()
            return

        # Show main dialog
        dialog = TaskDialog("RevitAI Co-pilot")
        dialog.MainInstruction = "AI Co-pilot for Revit"
        dialog.MainContent = """Welcome to RevitAI Co-pilot!

**Epic 1 Complete:** Foundation infrastructure ready
- ✓ Claude API Integration
- ✓ Safety validation (operation allowlist)
- ✓ Preview/confirm pattern
- ✓ Logging and diagnostics

**Next:** Epic 2 - Intelligent Dimension Automation

**For full natural language interface:**
Run: `/bmad:bmm:workflows:dev-story` to implement Epic 2

**Example Commands (coming in Epic 2):**
• Hebrew: "תוסיף מידות פנימיות לכל החדרים בקומה 1"
• English: "Add internal dimensions to all rooms on Level 1"

**For now:** Test the foundation by clicking "Test Connection" below."""

        dialog.AddCommandLink(TaskDialogCommonButtons.CommandLink1, "Test Claude API Connection")
        dialog.AddCommandLink(TaskDialogCommonButtons.CommandLink2, "View System Information")
        dialog.CommonButtons = TaskDialogCommonButtons.Close

        result = dialog.Show()

        if result == TaskDialogCommonButtons.CommandLink1:
            test_claude_connection(uidoc)
        elif result == TaskDialogCommonButtons.CommandLink2:
            show_system_info(uidoc)

    except Exception as e:
        logger.error(f"Failed to show co-pilot dialog: {e}", exc_info=True)
        show_error_dialog("Error", f"Failed to show dialog: {e}")


def test_claude_connection(uidoc):
    """
    Test connection to Claude API

    Args:
        uidoc: Revit UIDocument
    """
    with OperationLogger("test_claude_connection") as op:
        op.start()

        try:
            # Create Claude client
            client = ClaudeClient()

            # Test connection
            if client.test_connection():
                show_message_dialog(
                    "Connection Test",
                    "✓ Successfully connected to Claude API!\n\n"
                    f"Model: {client.model}\n"
                    f"Timeout: {client.timeout}s\n"
                    f"Max Retries: {client.max_retries}\n\n"
                    "Your API key is configured correctly."
                )
                op.end(success=True)
            else:
                show_error_dialog(
                    "Connection Test Failed",
                    "✗ Failed to connect to Claude API\n\n"
                    "Please check:\n"
                    "• Internet connection\n"
                    "• API key is valid\n"
                    "• Anthropic service is available"
                )
                op.end(success=False)

        except ConfigurationError as e:
            show_error_dialog("Configuration Error", str(e))
            op.error(e)
        except APIError as e:
            show_error_dialog("API Error", str(e))
            op.error(e)
        except Exception as e:
            show_error_dialog("Unexpected Error", f"Failed to test connection: {e}")
            op.error(e)


def show_system_info(uidoc):
    """
    Show system information dialog

    Args:
        uidoc: Revit UIDocument
    """
    try:
        # Get Revit context
        context = build_revit_context(uidoc)

        # Get configuration
        config = get_config_manager()
        has_api_key = config.validate_api_key()

        # Build info message
        info = f"""**RevitAI System Information**

**Revit Context:**
• Current View: {context.get('current_view', 'None')}
• Levels: {', '.join(context.get('levels', []))}
• Rooms: {context.get('element_counts', {}).get('Room', 0)}
• Walls: {context.get('element_counts', {}).get('Wall', 0)}
• Doors: {context.get('element_counts', {}).get('Door', 0)}
• Selected: {context.get('selected_elements', 0)} elements

**Configuration:**
• API Key: {'✓ Configured' if has_api_key else '✗ Not configured'}
• Model: {config.get('api_settings.model', 'claude-sonnet-4')}
• Language: {config.get('language', 'en')}
• Max Elements: {config.get('safety.max_elements_per_operation', 500)}

**Epic 1 Status:**
✓ Project Setup Complete
✓ Claude API Integration
✓ ExternalEvent Pattern
✓ Operation Allowlist
✓ Preview/Confirm UX
✓ Logging Infrastructure
✓ Ribbon UI

**Next Steps:**
→ Implement Epic 2: Intelligent Dimension Automation
→ Run: `/bmad:bmm:workflows:dev-story` to continue"""

        show_message_dialog("System Information", info)

    except Exception as e:
        logger.error(f"Failed to show system info: {e}", exc_info=True)
        show_error_dialog("Error", f"Failed to get system info: {e}")


def show_api_key_required_dialog():
    """Show dialog prompting user to configure API key"""
    if not REVIT_API_AVAILABLE:
        print("API key required")
        return

    dialog = TaskDialog("API Key Required")
    dialog.MainInstruction = "Claude API Key Not Configured"
    dialog.MainContent = """RevitAI requires a Claude API key to function.

**To configure your API key:**

**Option 1: Environment Variable**
Set environment variable: CLAUDE_API_KEY=sk-...

**Option 2: Windows Credential Manager (Recommended)**
Install keyring: pip install keyring
Run from Python:
```python
from lib.config_manager import get_config_manager
config = get_config_manager()
config.set_api_key("sk-...")
```

**Option 3: Configuration File**
Create: .extensions/RevitAI.extension/config/firm_defaults.yaml
(Copy from firm_defaults.example.yaml)

**Get an API Key:**
Visit: https://console.anthropic.com/
Create an account and generate an API key."""

    dialog.CommonButtons = TaskDialogCommonButtons.Ok
    dialog.Show()


def show_error_dialog(title: str, message: str):
    """
    Show error dialog

    Args:
        title: Dialog title
        message: Error message
    """
    if not REVIT_API_AVAILABLE:
        print(f"[ERROR] {title}: {message}")
        return

    dialog = TaskDialog(title)
    dialog.MainInstruction = "Error"
    dialog.MainContent = message
    dialog.CommonButtons = TaskDialogCommonButtons.Ok
    dialog.Show()


def test_ui_dialogs():
    """Test function for UI dialogs (requires Revit context)"""
    print("Testing UI Dialogs...")

    if not REVIT_API_AVAILABLE:
        print("✗ Revit API not available")
        print("Note: Run from within Revit to test dialogs")
        return False

    print("Note: Full tests require Revit context")
    return True


if __name__ == '__main__':
    # Run test when module is executed directly
    test_ui_dialogs()
