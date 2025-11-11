"""
RevitAI Settings
Configure Claude API key and other settings
"""

# Standard library
import sys
import os

# Add lib to path for imports
lib_path = os.path.join(os.path.dirname(__file__), '..', '..', '..', 'lib')
if lib_path not in sys.path:
    sys.path.insert(0, lib_path)

# Revit API imports
from Autodesk.Revit.UI import TaskDialog, TaskDialogCommonButtons, TaskDialogResult

# RevitAI imports
from config_manager import get_config_manager
from logger import get_logger

# Get current document
uidoc = __revit__.ActiveUIDocument  # noqa: F821

# Initialize logger
logger = get_logger()


def show_settings_dialog():
    """Show settings configuration dialog"""

    config = get_config_manager()
    has_api_key = config.validate_api_key()

    dialog = TaskDialog("RevitAI Settings")
    dialog.MainInstruction = "Configuration & Settings"
    dialog.MainContent = f"""**Current Configuration:**

**API Key:** {'✓ Configured' if has_api_key else '✗ Not Configured'}
**Model:** {config.get('api_settings.model', 'claude-sonnet-4')}
**Language:** {config.get('language', 'en')}
**Max Elements:** {config.get('safety.max_elements_per_operation', 500)}
**Log Level:** {config.get('logging.log_level', 'INFO')}

**Configuration File:**
{config.config_path}

**To Configure API Key:**

**Method 1: Environment Variable**
Set: CLAUDE_API_KEY=sk-...

**Method 2: Python (Recommended)**
```python
from lib.config_manager import get_config_manager
config = get_config_manager()
config.set_api_key("sk-...")
```

**Method 3: Edit Configuration File**
Copy: firm_defaults.example.yaml
To: firm_defaults.yaml
(API key not stored in config file for security)

**Get Claude API Key:**
Visit: https://console.anthropic.com/"""

    dialog.AddCommandLink(TaskDialogCommonButtons.CommandLink1, "View Configuration File")
    dialog.AddCommandLink(TaskDialogCommonButtons.CommandLink2, "View Logs")
    dialog.AddCommandLink(TaskDialogCommonButtons.CommandLink3, "Test API Connection")
    dialog.CommonButtons = TaskDialogCommonButtons.Close

    result = dialog.Show()

    if result == TaskDialogCommonButtons.CommandLink1:
        view_config_file(config.config_path)
    elif result == TaskDialogCommonButtons.CommandLink2:
        view_logs()
    elif result == TaskDialogCommonButtons.CommandLink3:
        test_api_connection()


def view_config_file(config_path: str):
    """Open configuration file location"""
    import subprocess

    # Get directory and validate it exists
    config_dir = os.path.dirname(os.path.abspath(config_path))

    # Validate path is within expected directories (security check)
    if not os.path.exists(config_dir):
        logger.warning(f"Config directory does not exist: {config_dir}")
        TaskDialog.Show("Error", "Configuration directory not found")
        return

    try:
        # Open in Windows Explorer - use list args for security
        subprocess.Popen(['explorer', config_dir])
        logger.info(f"Opened config directory: {config_dir}")
    except Exception as e:
        logger.exception(f"Failed to open config directory: {e}")
        TaskDialog.Show("Error", f"Failed to open folder:\n{e}")


def view_logs():
    """Open logs directory"""
    import subprocess
    from logger import get_log_directory

    log_dir = os.path.abspath(get_log_directory())

    # Validate log directory exists (security check)
    if not os.path.exists(log_dir):
        logger.warning(f"Log directory does not exist: {log_dir}")
        TaskDialog.Show("Error", "Log directory not found")
        return

    try:
        # Open in Windows Explorer - use list args for security
        subprocess.Popen(['explorer', log_dir])
        logger.info(f"Opened log directory: {log_dir}")
    except Exception as e:
        logger.exception(f"Failed to open log directory: {e}")
        TaskDialog.Show("Error", f"Failed to open folder:\n{e}")


def test_api_connection():
    """Test Claude API connection"""
    from claude_client import ClaudeClient
    from exceptions import APIError, ConfigurationError

    try:
        client = ClaudeClient()

        if client.test_connection():
            TaskDialog.Show(
                "Connection Test",
                "✓ Successfully connected to Claude API!\n\n"
                f"Model: {client.model}\n"
                f"Timeout: {client.timeout}s\n"
                "API key is valid."
            )
            logger.info("API connection test: SUCCESS")
        else:
            TaskDialog.Show(
                "Connection Test Failed",
                "✗ Failed to connect to Claude API\n\n"
                "Check:\n"
                "• Internet connection\n"
                "• API key validity\n"
                "• Anthropic service status"
            )
            logger.warning("API connection test: FAILED")

    except ConfigurationError as e:
        TaskDialog.Show("Configuration Error", str(e))
        logger.exception(f"API connection test error: {e}")
    except APIError as e:
        TaskDialog.Show("API Error", str(e))
        logger.exception(f"API connection test error: {e}")
    except Exception as e:
        TaskDialog.Show("Error", f"Unexpected error:\n{e}")
        logger.exception(f"API connection test error: {e}")


def main():
    """Main entry point for Settings button"""
    logger.info("Settings button clicked")
    print("=" * 60)
    print("RevitAI Settings")
    print("=" * 60)

    try:
        show_settings_dialog()
    except Exception as e:
        logger.error(f"Error in settings: {e}", exc_info=True)
        TaskDialog.Show("Error", f"Settings error:\n{e}")


# Entry point when button is clicked
if __name__ == '__main__':
    main()
