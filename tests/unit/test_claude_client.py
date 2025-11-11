"""
Unit tests for ClaudeClient

Tests Claude API integration with mocked API responses.
"""

import pytest
import sys
import os
from unittest.mock import Mock, patch, MagicMock

# Add lib and fixtures to path
lib_path = os.path.join(os.path.dirname(__file__), '..', '..', '.extensions', 'RevitAI.extension', 'lib')
fixtures_path = os.path.join(os.path.dirname(__file__), '..', 'fixtures')
sys.path.insert(0, lib_path)
sys.path.insert(0, fixtures_path)

from claude_client import ClaudeClient
from exceptions import APIError, ConfigurationError
from mock_claude_api import MockAnthropicClient, get_sample_responses


class TestClaudeClient:
    """Test suite for ClaudeClient"""

    def setup_method(self):
        """Set up test fixtures before each test"""
        self.api_key = "test_api_key_12345"

    @patch('claude_client.Anthropic')
    def test_client_initialization_with_api_key(self, mock_anthropic):
        """Test ClaudeClient initialization with API key"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)

        client = ClaudeClient(api_key=self.api_key)

        assert client.api_key == self.api_key
        assert client.model == "claude-sonnet-4-20250514"
        mock_anthropic.assert_called_once()

    def test_client_initialization_without_api_key_raises_error(self):
        """Test that initialization without API key raises ConfigurationError"""
        # This test assumes no CLAUDE_API_KEY environment variable is set
        with patch.dict(os.environ, {}, clear=True):
            with patch('claude_client.get_config_manager') as mock_config:
                mock_config.return_value.get_api_key.return_value = None

                with pytest.raises(ConfigurationError, match="Claude API key not configured"):
                    ClaudeClient()

    @patch('claude_client.Anthropic')
    def test_parse_prompt_returns_structured_action(self, mock_anthropic):
        """Test that parse_prompt returns structured JSON action"""
        mock_client = MockAnthropicClient(self.api_key)
        mock_anthropic.return_value = mock_client

        client = ClaudeClient(api_key=self.api_key)

        # Test with dimension prompt
        result = client.parse_prompt("Add dimensions to all rooms", {})

        assert isinstance(result, dict)
        assert "operation" in result
        assert result["operation"] == "create_dimensions"

    @patch('claude_client.Anthropic')
    def test_parse_prompt_with_hebrew(self, mock_anthropic):
        """Test parsing Hebrew language prompts"""
        mock_client = MockAnthropicClient(self.api_key)
        mock_anthropic.return_value = mock_client

        client = ClaudeClient(api_key=self.api_key)

        # Test with Hebrew dimension prompt
        result = client.parse_prompt("תוסיף מידות לכל החדרים", {})

        assert isinstance(result, dict)
        assert "operation" in result
        assert result["operation"] == "create_dimensions"

    @patch('claude_client.Anthropic')
    def test_parse_prompt_with_context(self, mock_anthropic):
        """Test that context is included in API call"""
        mock_client = MockAnthropicClient(self.api_key)
        mock_anthropic.return_value = mock_client

        client = ClaudeClient(api_key=self.api_key)

        context = {
            "levels": ["Level 1", "Level 2"],
            "rooms": {"count": 5}
        }

        result = client.parse_prompt("Add tags to doors", context)

        assert isinstance(result, dict)
        assert "operation" in result

    @patch('claude_client.Anthropic')
    def test_parse_json_response_with_code_fence(self, mock_anthropic):
        """Test parsing JSON responses with markdown code fences"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)
        client = ClaudeClient(api_key=self.api_key)

        # Test with code fence
        response_text = '''```json
        {
            "operation": "create_tags",
            "target": {"element_type": "doors"},
            "params": {}
        }
        ```'''

        result = client._parse_json_response(response_text, response_text)

        assert result["operation"] == "create_tags"

    @patch('claude_client.Anthropic')
    def test_parse_json_response_without_code_fence(self, mock_anthropic):
        """Test parsing plain JSON responses"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)
        client = ClaudeClient(api_key=self.api_key)

        response_text = '{"operation": "read_elements", "target": {}, "params": {}}'

        result = client._parse_json_response(response_text, response_text)

        assert result["operation"] == "read_elements"

    @patch('claude_client.Anthropic')
    def test_parse_invalid_json_raises_api_error(self, mock_anthropic):
        """Test that invalid JSON raises APIError"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)
        client = ClaudeClient(api_key=self.api_key)

        invalid_json = "This is not JSON"

        with pytest.raises(APIError, match="Failed to parse JSON response"):
            client._parse_json_response(invalid_json, invalid_json)

    @patch('claude_client.Anthropic')
    def test_test_connection_success(self, mock_anthropic):
        """Test that test_connection returns True on success"""
        mock_client = MockAnthropicClient(self.api_key)
        mock_anthropic.return_value = mock_client

        client = ClaudeClient(api_key=self.api_key)

        result = client.test_connection()

        assert result is True

    @patch('claude_client.Anthropic')
    def test_test_connection_handles_api_error(self, mock_anthropic):
        """Test that test_connection handles API errors gracefully"""
        # Create a mock that raises an error
        mock_client = Mock()
        mock_client.messages.create.side_effect = Exception("API Error")
        mock_anthropic.return_value = mock_client

        client = ClaudeClient(api_key=self.api_key)

        result = client.test_connection()

        # Should return False on error, not raise exception
        assert result is False

    @patch('claude_client.Anthropic')
    def test_client_uses_configured_model(self, mock_anthropic):
        """Test that client uses model from configuration"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)

        with patch('claude_client.get_config_manager') as mock_config:
            mock_config.return_value.get.return_value = "claude-opus-4"

            client = ClaudeClient(api_key=self.api_key)

            # Should use configured model
            assert "claude" in client.model.lower()

    @patch('claude_client.Anthropic')
    def test_system_prompt_includes_allowed_operations(self, mock_anthropic):
        """Test that system prompt includes allowed operations"""
        mock_anthropic.return_value = MockAnthropicClient(self.api_key)

        client = ClaudeClient(api_key=self.api_key)

        system_prompt = client._get_system_prompt()

        # Should mention supported operations
        assert "create_dimensions" in system_prompt
        assert "create_tags" in system_prompt
        assert "read_elements" in system_prompt


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
