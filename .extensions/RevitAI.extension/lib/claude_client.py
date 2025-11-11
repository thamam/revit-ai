"""
Claude API Client
Handles communication with Claude API for natural language understanding
"""

import json
from typing import Dict, Any, Optional, List

# Try to import anthropic SDK
try:
    from anthropic import Anthropic, AnthropicError
    ANTHROPIC_AVAILABLE = True
except ImportError:
    ANTHROPIC_AVAILABLE = False
    print("Warning: anthropic SDK not available. Please install: pip install anthropic")

from exceptions import APIError, ConfigurationError
from config_manager import get_config_manager


class ClaudeClient:
    """
    Client for interacting with Claude API

    Handles:
    - API authentication
    - Prompt formatting with Revit context
    - Response parsing to structured actions
    - Error handling and retries
    """

    def __init__(self, api_key: Optional[str] = None):
        """
        Initialize Claude API client

        Args:
            api_key: Claude API key (optional, will use config manager if not provided)

        Raises:
            ConfigurationError: If API key not found
            APIError: If anthropic SDK not available
        """
        if not ANTHROPIC_AVAILABLE:
            raise APIError(
                "Anthropic SDK not installed. "
                "Please run: pip install anthropic"
            )

        # Get API key from parameter or config
        if api_key is None:
            config = get_config_manager()
            api_key = config.get_api_key()

        if not api_key:
            raise ConfigurationError(
                "Claude API key not configured. "
                "Please set API key using Settings dialog or CLAUDE_API_KEY environment variable."
            )

        # Initialize Anthropic client
        try:
            self.client = Anthropic(api_key=api_key)
        except Exception as e:
            raise APIError(f"Failed to initialize Anthropic client: {e}") from e

        # Load config
        self.config = get_config_manager()
        self.model = self.config.get('api_settings.model', 'claude-sonnet-4-20250514')
        self.timeout = self.config.get('api_settings.timeout_seconds', 10)
        self.max_retries = self.config.get('api_settings.max_retries', 3)
        self.temperature = self.config.get('api_settings.temperature', 0.0)

    def parse_prompt(
        self,
        user_prompt: str,
        context: Optional[Dict[str, Any]] = None
    ) -> Dict[str, Any]:
        """
        Parse user's natural language prompt into structured action

        Args:
            user_prompt: User's natural language command (Hebrew or English)
            context: Revit project context (levels, elements, view, etc.)

        Returns:
            Dictionary with structured action:
            {
                "operation": "create_dimensions | create_tags | ...",
                "targets": {...},
                "parameters": {...},
                "clarifications": [...]
            }

        Raises:
            APIError: If API call fails
        """
        # Build context section
        context_str = self._build_context_string(context or {})

        # Build full prompt
        system_prompt = self._get_system_prompt()
        full_prompt = f"""{context_str}

<prompt>
{user_prompt}
</prompt>

Return JSON action schema as specified in the system prompt."""

        try:
            # Call Claude API
            response = self.client.messages.create(
                model=self.model,
                max_tokens=1024,
                temperature=self.temperature,
                system=system_prompt,
                messages=[
                    {"role": "user", "content": full_prompt}
                ]
            )

            # Extract response text
            response_text = response.content[0].text

            # Parse JSON response
            action = self._parse_json_response(response_text)

            return action

        except AnthropicError as e:
            raise APIError(f"Claude API error: {e}") from e
        except Exception as e:
            raise APIError(f"Unexpected error calling Claude API: {e}") from e

    def _get_system_prompt(self) -> str:
        """Get system prompt that defines Claude's behavior"""
        return """You are an AI assistant for Revit, helping architects automate tasks through natural language.

Your job is to parse the user's command and return a structured JSON action that describes what they want to do.

**Supported Operations:**
- create_dimensions: Add dimension chains to rooms or elements
- create_tags: Add tags to doors, windows, rooms, or other elements
- read_elements: Query element properties

**Action Schema:**
```json
{
  "operation": "create_dimensions | create_tags | read_elements",
  "targets": {
    "element_type": "Room | Wall | Door | Window | ...",
    "scope": "Level 1 | current_view | selected | all",
    "filter": {}  // Optional additional filters
  },
  "parameters": {
    // Operation-specific parameters
  },
  "clarifications": []  // Questions if prompt is ambiguous
}
```

**Important Rules:**
1. Always return valid JSON (no markdown code blocks, no extra text)
2. If the prompt is ambiguous, add clarifying questions to "clarifications" array
3. Support both Hebrew and English commands
4. Map level names accurately (e.g., "קומה 1" or "Level 1")
5. Default to "current_view" scope if not specified
6. Use firm standards from context when available

**Examples:**

Hebrew: "תוסיף מידות פנימיות לכל החדרים בקומה 1"
→ {"operation": "create_dimensions", "targets": {"element_type": "Room", "scope": "Level 1"}, "parameters": {"offset_mm": 200, "style": "continuous"}, "clarifications": []}

English: "Tag all doors on this floor"
→ {"operation": "create_tags", "targets": {"element_type": "Door", "scope": "current_view"}, "parameters": {}, "clarifications": []}

Ambiguous: "Add dimensions"
→ {"operation": "create_dimensions", "targets": {}, "parameters": {}, "clarifications": ["Which elements do you want to dimension?", "Which level or view?"]}
"""

    def _build_context_string(self, context: Dict[str, Any]) -> str:
        """Build context string from Revit project context"""
        context_parts = ["<context>", "Revit Project Context:"]

        # Current view
        if 'current_view' in context:
            context_parts.append(f"- Current View: {context['current_view']}")

        # Available levels
        if 'levels' in context:
            levels_str = ", ".join(context['levels'])
            context_parts.append(f"- Available Levels: {levels_str}")

        # Element counts
        if 'element_counts' in context:
            counts = context['element_counts']
            counts_str = ", ".join([f"{k} ({v})" for k, v in counts.items()])
            context_parts.append(f"- Element Types: {counts_str}")

        # Firm standards
        if 'firm_standards' in context:
            standards = context['firm_standards']
            if 'dimension_offset' in standards:
                context_parts.append(f"- Dimension Offset: {standards['dimension_offset']}mm")
            if 'dimension_style' in standards:
                context_parts.append(f"- Dimension Style: {standards['dimension_style']}")

        # Selected elements
        if 'selected_elements' in context:
            count = context['selected_elements']
            context_parts.append(f"- Selected Elements: {count}")

        context_parts.append("</context>")
        return "\n".join(context_parts)

    def _parse_json_response(self, response_text: str) -> Dict[str, Any]:
        """
        Parse JSON from Claude's response

        Handles cases where Claude wraps JSON in markdown code blocks
        """
        # Remove markdown code blocks if present
        text = response_text.strip()

        if text.startswith("```json"):
            text = text[7:]  # Remove ```json
        elif text.startswith("```"):
            text = text[3:]  # Remove ```

        if text.endswith("```"):
            text = text[:-3]  # Remove trailing ```

        text = text.strip()

        # Parse JSON
        try:
            action = json.loads(text)
            return action
        except json.JSONDecodeError as e:
            raise APIError(f"Failed to parse JSON response: {e}\nResponse: {response_text}") from e

    def test_connection(self) -> bool:
        """
        Test connection to Claude API

        Returns:
            True if connection successful, False otherwise
        """
        try:
            response = self.client.messages.create(
                model=self.model,
                max_tokens=50,
                messages=[
                    {"role": "user", "content": "Say 'hello' in JSON: {\"message\": \"hello\"}"}
                ]
            )
            return True
        except Exception:
            return False


def test_claude_client():
    """Test function for Claude client (for development)"""
    try:
        client = ClaudeClient()
        print("✓ Claude client initialized")

        # Test connection
        if client.test_connection():
            print("✓ API connection successful")
        else:
            print("✗ API connection failed")
            return False

        # Test prompt parsing
        context = {
            'current_view': 'Level 1 Floor Plan',
            'levels': ['Level 1', 'Level 2', 'Roof'],
            'element_counts': {'Room': 12, 'Wall': 48, 'Door': 8},
            'firm_standards': {'dimension_offset': 200, 'dimension_style': 'Continuous'}
        }

        action = client.parse_prompt(
            "תוסיף מידות פנימיות לכל החדרים בקומה 1",
            context
        )

        print("✓ Hebrew prompt parsed:")
        print(json.dumps(action, indent=2, ensure_ascii=False))

        return True

    except Exception as e:
        print(f"✗ Test failed: {e}")
        return False


if __name__ == '__main__':
    # Run test when module is executed directly
    test_claude_client()
