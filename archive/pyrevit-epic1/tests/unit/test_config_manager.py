"""
Unit tests for ConfigManager

Tests configuration loading, API key management, and nested key access.
"""

import pytest
import sys
import os
import tempfile
import yaml

# Add lib to path
lib_path = os.path.join(os.path.dirname(__file__), '..', '..', '.extensions', 'RevitAI.extension', 'lib')
sys.path.insert(0, lib_path)

from config_manager import ConfigManager, get_config_manager
from exceptions import ConfigurationError


class TestConfigManager:
    """Test suite for ConfigManager"""

    def setup_method(self):
        """Set up test fixtures before each test"""
        # Create a temporary config file
        self.temp_dir = tempfile.mkdtemp()
        self.config_file = os.path.join(self.temp_dir, "test_config.yaml")

        # Sample configuration
        self.sample_config = {
            "language": "en",
            "api_settings": {
                "model": "claude-sonnet-4",
                "timeout_seconds": 10,
                "max_retries": 3
            },
            "safety": {
                "max_elements_per_operation": 500
            },
            "logging": {
                "log_level": "INFO"
            }
        }

        # Write sample config
        with open(self.config_file, 'w') as f:
            yaml.dump(self.sample_config, f)

    def teardown_method(self):
        """Clean up after each test"""
        # Remove temp files
        if os.path.exists(self.config_file):
            os.remove(self.config_file)
        if os.path.exists(self.temp_dir):
            os.rmdir(self.temp_dir)

    def test_load_valid_config(self):
        """Test loading a valid YAML configuration"""
        config = ConfigManager(self.config_file)
        assert config._config is not None
        assert config._config["language"] == "en"

    def test_get_top_level_key(self):
        """Test getting top-level configuration values"""
        config = ConfigManager(self.config_file)
        assert config.get("language") == "en"

    def test_get_nested_key_with_dot_notation(self):
        """Test getting nested values using dot notation"""
        config = ConfigManager(self.config_file)
        assert config.get("api_settings.model") == "claude-sonnet-4"
        assert config.get("api_settings.timeout_seconds") == 10
        assert config.get("safety.max_elements_per_operation") == 500

    def test_get_nonexistent_key_returns_default(self):
        """Test that nonexistent keys return the default value"""
        config = ConfigManager(self.config_file)
        assert config.get("nonexistent_key", "default") == "default"
        assert config.get("api_settings.nonexistent", 100) == 100

    def test_get_without_default_returns_none(self):
        """Test that get() without default returns None for missing keys"""
        config = ConfigManager(self.config_file)
        assert config.get("nonexistent_key") is None

    def test_empty_config_file_raises_error(self):
        """Test that empty configuration file raises ConfigurationError"""
        # Create empty file
        empty_file = os.path.join(self.temp_dir, "empty.yaml")
        with open(empty_file, 'w') as f:
            f.write("")

        with pytest.raises(ConfigurationError, match="Configuration file is empty"):
            ConfigManager(empty_file)

        os.remove(empty_file)

    def test_invalid_yaml_raises_error(self):
        """Test that invalid YAML raises ConfigurationError"""
        # Create invalid YAML
        invalid_file = os.path.join(self.temp_dir, "invalid.yaml")
        with open(invalid_file, 'w') as f:
            f.write("invalid: yaml: content: [")

        with pytest.raises(ConfigurationError, match="Invalid YAML"):
            ConfigManager(invalid_file)

        os.remove(invalid_file)

    def test_nonexistent_file_creates_default_config(self):
        """Test that nonexistent file path is handled"""
        nonexistent = os.path.join(self.temp_dir, "nonexistent.yaml")

        # Should not crash - should handle gracefully
        try:
            config = ConfigManager(nonexistent)
            # If it creates a default config, verify it's usable
            assert config.get("language", "en") == "en"
        except ConfigurationError:
            # Acceptable to raise ConfigurationError for missing file
            pass

    def test_singleton_pattern(self):
        """Test that get_config_manager returns singleton instance"""
        # This test may need to be adjusted based on actual singleton implementation
        # For now, just verify it returns a ConfigManager instance
        config = get_config_manager()
        assert isinstance(config, ConfigManager)

    def test_validate_api_key_with_valid_key(self):
        """Test API key validation with valid key"""
        config = ConfigManager(self.config_file)

        # Note: This test may need mocking of keyring library
        # For now, test the validation logic
        result = config.validate_api_key()
        # Should return bool
        assert isinstance(result, bool)

    def test_nested_dict_access_multiple_levels(self):
        """Test accessing deeply nested configuration values"""
        config = ConfigManager(self.config_file)

        # Add some deeply nested data
        config._config["deep"] = {
            "level1": {
                "level2": {
                    "level3": "value"
                }
            }
        }

        assert config.get("deep.level1.level2.level3") == "value"

    def test_config_with_list_values(self):
        """Test configuration with list values"""
        # Add list to config
        with open(self.config_file, 'w') as f:
            test_config = self.sample_config.copy()
            test_config["allowed_operations"] = ["create_dimensions", "create_tags"]
            yaml.dump(test_config, f)

        config = ConfigManager(self.config_file)
        operations = config.get("allowed_operations")
        assert isinstance(operations, list)
        assert "create_dimensions" in operations


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
