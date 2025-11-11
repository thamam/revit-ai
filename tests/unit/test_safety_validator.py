"""
Unit tests for SafetyValidator

Tests the operation allowlist, blocklist, and scope validation logic.
"""

import pytest
import sys
import os

# Add lib to path
lib_path = os.path.join(os.path.dirname(__file__), '..', '..', '.extensions', 'RevitAI.extension', 'lib')
sys.path.insert(0, lib_path)

from safety_validator import SafetyValidator
from exceptions import ValidationError


class TestSafetyValidator:
    """Test suite for SafetyValidator"""

    def setup_method(self):
        """Set up test fixtures before each test"""
        self.validator = SafetyValidator()

    def test_allowed_operation_passes(self):
        """Test that allowed operations pass validation"""
        action = {
            "operation": "create_dimensions",
            "target": {
                "element_type": "rooms",
                "filters": {}
            },
            "params": {}
        }

        # Should not raise exception
        self.validator.validate_action(action)

    def test_blocked_operation_raises_error(self):
        """Test that blocked operations raise ValidationError"""
        action = {
            "operation": "delete_elements",
            "target": {
                "element_type": "walls",
                "filters": {}
            },
            "params": {}
        }

        with pytest.raises(ValidationError, match="Operation 'delete_elements' is not allowed"):
            self.validator.validate_action(action)

    def test_invalid_action_format_raises_error(self):
        """Test that invalid action format raises ValidationError"""
        # Not a dictionary
        with pytest.raises(ValidationError, match="Action must be a dictionary"):
            self.validator.validate_action("invalid")

        # Missing operation field
        with pytest.raises(ValidationError, match="Action missing 'operation' field"):
            self.validator.validate_action({"target": {}})

    def test_scope_too_large_raises_error(self):
        """Test that scope validation rejects too many elements"""
        action = {
            "operation": "create_dimensions",
            "target": {
                "element_type": "rooms",
                "filters": {}
            },
            "params": {}
        }

        # Mock element count > max allowed
        large_count = self.validator.max_elements + 100

        with pytest.raises(ValidationError, match="Operation scope too large"):
            self.validator._validate_scope(action, large_count)

    def test_create_dimensions_validation(self):
        """Test dimension-specific validation"""
        action = {
            "operation": "create_dimensions",
            "target": {
                "element_type": "rooms",
                "filters": {}
            },
            "params": {
                "offset_mm": 200
            }
        }

        # Valid action should pass
        self.validator._validate_create_dimensions(action, 10)

        # Too many dimensions should fail
        large_count = self.validator.max_dimensions + 1
        with pytest.raises(ValidationError, match="Too many dimensions"):
            self.validator._validate_create_dimensions(action, large_count)

    def test_create_tags_validation(self):
        """Test tag-specific validation"""
        action = {
            "operation": "create_tags",
            "target": {
                "element_type": "doors",
                "filters": {}
            },
            "params": {}
        }

        # Valid action should pass
        self.validator._validate_create_tags(action, 10)

        # Too many tags should fail
        large_count = self.validator.max_tags + 1
        with pytest.raises(ValidationError, match="Too many tags"):
            self.validator._validate_create_tags(action, large_count)

    def test_read_elements_validation(self):
        """Test read_elements validation"""
        action = {
            "operation": "read_elements",
            "target": {
                "element_type": "rooms",
                "filters": {}
            },
            "params": {
                "properties": ["Number", "Name"]
            }
        }

        # Valid action should pass
        self.validator._validate_read_elements(action, 10)

        # Too many elements should fail
        large_count = self.validator.max_elements + 1
        with pytest.raises(ValidationError, match="Too many elements to read"):
            self.validator._validate_read_elements(action, large_count)

    def test_all_allowed_operations_are_valid(self):
        """Test that all allowed operations have validators"""
        for operation in self.validator.allowed_operations:
            action = {
                "operation": operation,
                "target": {
                    "element_type": "test",
                    "filters": {}
                },
                "params": {}
            }

            # Should not raise exception for allowed operations
            # (may fail on specific validation but not on allowlist check)
            try:
                self.validator.validate_action(action)
            except ValidationError as e:
                # Acceptable if it's a specific validation error, not allowlist
                assert "not allowed" not in str(e).lower()

    def test_all_blocked_operations_are_rejected(self):
        """Test that all blocked operations are properly rejected"""
        for operation in self.validator.blocked_operations:
            action = {
                "operation": operation,
                "target": {
                    "element_type": "test",
                    "filters": {}
                },
                "params": {}
            }

            with pytest.raises(ValidationError, match=f"Operation '{operation}' is not allowed"):
                self.validator.validate_action(action)

    def test_unknown_operation_is_rejected(self):
        """Test that unknown operations are rejected"""
        action = {
            "operation": "unknown_operation_xyz",
            "target": {},
            "params": {}
        }

        with pytest.raises(ValidationError, match="Operation 'unknown_operation_xyz' is not allowed"):
            self.validator.validate_action(action)

    def test_validate_scope_with_zero_elements(self):
        """Test scope validation with zero elements"""
        action = {
            "operation": "create_dimensions",
            "target": {},
            "params": {}
        }

        # Zero elements should be valid (edge case)
        self.validator._validate_scope(action, 0)

    def test_validate_scope_at_boundary(self):
        """Test scope validation at exactly max_elements"""
        action = {
            "operation": "create_dimensions",
            "target": {},
            "params": {}
        }

        # Exactly max_elements should pass
        self.validator._validate_scope(action, self.validator.max_elements)

        # One more should fail
        with pytest.raises(ValidationError):
            self.validator._validate_scope(action, self.validator.max_elements + 1)


if __name__ == "__main__":
    pytest.main([__file__, "-v"])
