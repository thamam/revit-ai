"""
Mock Claude API for testing

Provides mocked Anthropic API responses for unit testing
without making actual API calls.
"""

import json
from typing import Dict, Any


class MockMessage:
    """Mock Anthropic Message response"""
    def __init__(self, content: str):
        self.content = [MockTextBlock(content)]


class MockTextBlock:
    """Mock text content block"""
    def __init__(self, text: str):
        self.text = text
        self.type = "text"


class MockAnthropicClient:
    """Mock Anthropic client for testing"""

    def __init__(self, api_key: str = "test_key"):
        self.api_key = api_key
        self.messages = MockMessages()


class MockMessages:
    """Mock messages API"""

    def create(self, model: str, max_tokens: int, system: str, messages: list, **kwargs):
        """Mock message creation - returns predefined responses based on prompt"""

        # Extract user prompt
        user_prompt = ""
        for msg in messages:
            if msg.get("role") == "user":
                user_prompt = msg.get("content", "").lower()
                break

        # Return mocked responses based on prompt content
        if "dimension" in user_prompt or "מידות" in user_prompt:
            response = {
                "operation": "create_dimensions",
                "target": {
                    "element_type": "rooms",
                    "filters": {"level": "Level 1"}
                },
                "params": {
                    "dimension_type": "interior",
                    "offset_mm": 200
                }
            }
        elif "tag" in user_prompt or "תיוג" in user_prompt:
            response = {
                "operation": "create_tags",
                "target": {
                    "element_type": "doors",
                    "filters": {}
                },
                "params": {
                    "tag_type": "door_tag"
                }
            }
        elif "read" in user_prompt or "קרא" in user_prompt:
            response = {
                "operation": "read_elements",
                "target": {
                    "element_type": "rooms",
                    "filters": {}
                },
                "params": {
                    "properties": ["Number", "Name", "Area"]
                }
            }
        else:
            # Default response
            response = {
                "operation": "read_elements",
                "target": {
                    "element_type": "all",
                    "filters": {}
                },
                "params": {}
            }

        # Return as mock message
        json_response = json.dumps(response, indent=2)
        return MockMessage(f"```json\n{json_response}\n```")


class MockAnthropicError(Exception):
    """Mock Anthropic error"""
    pass


def create_mock_claude_client(api_key: str = "test_key"):
    """
    Create a mock Claude client for testing

    Args:
        api_key: Test API key

    Returns:
        MockAnthropicClient instance
    """
    return MockAnthropicClient(api_key)


def get_sample_responses() -> Dict[str, Any]:
    """
    Get dictionary of sample Claude API responses for testing

    Returns:
        dict: Sample responses for different operations
    """
    return {
        "create_dimensions": {
            "operation": "create_dimensions",
            "target": {
                "element_type": "rooms",
                "filters": {"level": "Level 1"}
            },
            "params": {
                "dimension_type": "interior",
                "offset_mm": 200
            }
        },
        "create_tags": {
            "operation": "create_tags",
            "target": {
                "element_type": "doors",
                "filters": {}
            },
            "params": {
                "tag_type": "door_tag"
            }
        },
        "read_elements": {
            "operation": "read_elements",
            "target": {
                "element_type": "rooms",
                "filters": {}
            },
            "params": {
                "properties": ["Number", "Name", "Area"]
            }
        },
        "blocked_operation": {
            "operation": "delete_elements",
            "target": {
                "element_type": "walls",
                "filters": {}
            },
            "params": {}
        }
    }
