"""
Configuration Manager
Handles loading firm settings and secure API key storage
"""

import os
import yaml
from typing import Dict, Any, Optional

# Try to import keyring for secure API key storage
try:
    import keyring
    KEYRING_AVAILABLE = True
except ImportError:
    KEYRING_AVAILABLE = False
    print("Warning: keyring not available. API key will be stored in environment variable.")

from exceptions import ConfigurationError


class ConfigManager:
    """
    Manages configuration loading and API key storage

    Configuration sources (in priority order):
    1. Environment variables (for API key override)
    2. Windows Credential Manager (for API key)
    3. firm_defaults.yaml (for all other settings)
    """

    # Keyring service name
    SERVICE_NAME = "RevitAI"
    API_KEY_NAME = "claude_api_key"

    def __init__(self, config_path: Optional[str] = None):
        """
        Initialize configuration manager

        Args:
            config_path: Path to firm_defaults.yaml (optional)
                        Defaults to config/firm_defaults.yaml in extension folder
        """
        if config_path is None:
            # Default path relative to this file
            lib_dir = os.path.dirname(os.path.abspath(__file__))
            extension_dir = os.path.dirname(lib_dir)
            config_path = os.path.join(extension_dir, 'config', 'firm_defaults.yaml')

        self.config_path = config_path
        self._config = None

    def load_config(self) -> Dict[str, Any]:
        """
        Load configuration from YAML file

        Returns:
            Dictionary with configuration settings

        Raises:
            ConfigurationError: If config file cannot be loaded
        """
        if self._config is not None:
            return self._config

        # Check if config file exists
        if not os.path.exists(self.config_path):
            # Try example file
            example_path = self.config_path.replace('.yaml', '.example.yaml')
            if os.path.exists(example_path):
                raise ConfigurationError(
                    f"Configuration file not found: {self.config_path}\n"
                    f"Please copy {example_path} to {self.config_path} and customize it."
                )
            else:
                raise ConfigurationError(
                    f"Configuration file not found: {self.config_path}"
                )

        # Load YAML
        try:
            with open(self.config_path, 'r', encoding='utf-8') as f:
                self._config = yaml.safe_load(f)

            if not self._config:
                raise ConfigurationError("Configuration file is empty")

            return self._config

        except yaml.YAMLError as e:
            raise ConfigurationError(f"Invalid YAML in configuration file: {e}") from e
        except Exception as e:
            raise ConfigurationError(f"Failed to load configuration: {e}") from e

    def get(self, key: str, default: Any = None) -> Any:
        """
        Get configuration value by key

        Args:
            key: Configuration key (supports dot notation: "api_settings.model")
            default: Default value if key not found

        Returns:
            Configuration value or default
        """
        config = self.load_config()

        # Support dot notation for nested keys
        keys = key.split('.')
        value = config

        for k in keys:
            if isinstance(value, dict) and k in value:
                value = value[k]
            else:
                return default

        return value

    def get_api_key(self) -> Optional[str]:
        """
        Retrieve Claude API key from secure storage

        Priority:
        1. Environment variable: CLAUDE_API_KEY
        2. Windows Credential Manager (keyring)
        3. None if not found

        Returns:
            API key string or None
        """
        # Check environment variable first
        api_key = os.environ.get('CLAUDE_API_KEY')
        if api_key:
            return api_key

        # Check Windows Credential Manager
        if KEYRING_AVAILABLE:
            try:
                api_key = keyring.get_password(self.SERVICE_NAME, self.API_KEY_NAME)
                return api_key
            except Exception as e:
                print(f"Warning: Failed to retrieve API key from keyring: {e}")
                return None

        return None

    def set_api_key(self, api_key: str) -> bool:
        """
        Store Claude API key in secure storage

        Args:
            api_key: The API key to store

        Returns:
            True if successful, False otherwise
        """
        if not api_key or not isinstance(api_key, str):
            raise ValueError("API key must be a non-empty string")

        # Validate API key format (basic check)
        if not api_key.startswith('sk-'):
            print("Warning: API key doesn't start with 'sk-'. This may not be a valid Anthropic API key.")

        # Store in Windows Credential Manager
        if KEYRING_AVAILABLE:
            try:
                keyring.set_password(self.SERVICE_NAME, self.API_KEY_NAME, api_key)
                return True
            except Exception as e:
                print(f"Error: Failed to store API key in keyring: {e}")
                print("Fallback: Set environment variable CLAUDE_API_KEY instead")
                return False
        else:
            print("Error: keyring library not available")
            print("Please install: pip install keyring")
            print("Or set environment variable: CLAUDE_API_KEY")
            return False

    def delete_api_key(self) -> bool:
        """
        Delete Claude API key from secure storage

        Returns:
            True if successful, False otherwise
        """
        if KEYRING_AVAILABLE:
            try:
                keyring.delete_password(self.SERVICE_NAME, self.API_KEY_NAME)
                return True
            except Exception:
                return False
        return False

    def validate_api_key(self) -> bool:
        """
        Check if API key is configured

        Returns:
            True if API key is available, False otherwise
        """
        api_key = self.get_api_key()
        return api_key is not None and len(api_key) > 0


# Global config manager instance
_config_manager = None


def get_config_manager() -> ConfigManager:
    """Get global configuration manager instance (singleton pattern)"""
    global _config_manager
    if _config_manager is None:
        _config_manager = ConfigManager()
    return _config_manager
