"""
Safety Validator
Implements operation allowlist and validates all AI actions before execution
"""

from typing import Dict, Any, List, Optional, Set

from exceptions import ValidationError
from config_manager import get_config_manager


# Allowlist of permitted operations
ALLOWED_OPERATIONS: Set[str] = {
    "create_dimensions",     # Create dimension chains
    "create_tags",          # Create element tags
    "read_elements",        # Query element properties (read-only)
}

# Blocked operations (explicitly forbidden)
BLOCKED_OPERATIONS: Set[str] = {
    "delete_elements",      # Delete model elements
    "modify_walls",         # Modify wall geometry
    "modify_doors",         # Modify door properties
    "modify_rooms",         # Modify room boundaries
    "save_project",         # Save the project file
    "close_project",        # Close the project
    "export_data",          # Export project data
    "import_data",          # Import external data
}

# Maximum limits for safety
DEFAULT_MAX_ELEMENTS = 500
DEFAULT_MAX_DIMENSIONS = 1000
DEFAULT_MAX_TAGS = 1000


class SafetyValidator:
    """
    Validates AI operations against safety rules

    Ensures:
    - Only allowed operations are executed
    - Operation scope is within acceptable limits
    - No destructive actions are performed
    """

    def __init__(self):
        """Initialize the safety validator"""
        config = get_config_manager()

        # Load safety limits from configuration
        self.max_elements = config.get(
            'safety.max_elements_per_operation',
            DEFAULT_MAX_ELEMENTS
        )
        self.max_dimensions = config.get(
            'safety.max_dimensions_per_operation',
            DEFAULT_MAX_DIMENSIONS
        )
        self.max_tags = config.get(
            'safety.max_tags_per_operation',
            DEFAULT_MAX_TAGS
        )

    def validate_action(self, action: Dict[str, Any]) -> None:
        """
        Validate an action before execution

        Args:
            action: Action dictionary from Claude API

        Raises:
            ValidationError: If action is not allowed or exceeds limits
        """
        # Check action structure
        if not isinstance(action, dict):
            raise ValidationError("Action must be a dictionary")

        if 'operation' not in action:
            raise ValidationError("Action missing 'operation' field")

        operation = action['operation']

        # Validate operation allowlist
        self._validate_operation_allowed(operation)

        # Validate operation-specific rules
        if operation == "create_dimensions":
            self._validate_dimension_operation(action)
        elif operation == "create_tags":
            self._validate_tag_operation(action)
        elif operation == "read_elements":
            self._validate_read_operation(action)

    def _validate_operation_allowed(self, operation: str) -> None:
        """
        Check if operation is in the allowlist

        Args:
            operation: Operation name

        Raises:
            ValidationError: If operation is not allowed
        """
        # Check blocked operations first
        if operation in BLOCKED_OPERATIONS:
            raise ValidationError(
                f"Operation '{operation}' is explicitly forbidden. "
                f"This operation could damage the project file."
            )

        # Check allowlist
        if operation not in ALLOWED_OPERATIONS:
            raise ValidationError(
                f"Operation '{operation}' is not allowed. "
                f"Permitted operations: {', '.join(sorted(ALLOWED_OPERATIONS))}"
            )

    def _validate_dimension_operation(self, action: Dict[str, Any]) -> None:
        """
        Validate dimension creation operation

        Args:
            action: Dimension action dictionary

        Raises:
            ValidationError: If operation exceeds limits
        """
        targets = action.get('targets', {})

        # Check element count if specified
        element_count = targets.get('element_count', 0)
        if element_count > self.max_elements:
            raise ValidationError(
                f"Operation scope too large: {element_count} elements "
                f"(maximum: {self.max_elements}). "
                f"Please narrow the scope or work in batches."
            )

        # Check dimension count if specified
        dimension_count = action.get('estimated_dimension_count', 0)
        if dimension_count > self.max_dimensions:
            raise ValidationError(
                f"Too many dimensions: {dimension_count} "
                f"(maximum: {self.max_dimensions}). "
                f"Please reduce the scope."
            )

        # Validate scope
        scope = targets.get('scope')
        if scope and scope not in ['current_view', 'selected', 'all'] and not scope.startswith('Level '):
            raise ValidationError(
                f"Invalid scope: {scope}. "
                f"Must be 'current_view', 'selected', 'all', or a level name like 'Level 1'."
            )

    def _validate_tag_operation(self, action: Dict[str, Any]) -> None:
        """
        Validate tag creation operation

        Args:
            action: Tag action dictionary

        Raises:
            ValidationError: If operation exceeds limits
        """
        targets = action.get('targets', {})

        # Check element count if specified
        element_count = targets.get('element_count', 0)
        if element_count > self.max_elements:
            raise ValidationError(
                f"Operation scope too large: {element_count} elements "
                f"(maximum: {self.max_elements})"
            )

        # Check tag count if specified
        tag_count = action.get('estimated_tag_count', 0)
        if tag_count > self.max_tags:
            raise ValidationError(
                f"Too many tags: {tag_count} "
                f"(maximum: {self.max_tags})"
            )

        # Validate element type
        element_type = targets.get('element_type')
        if element_type:
            allowed_types = ['Door', 'Window', 'Room', 'Wall', 'Floor', 'Ceiling']
            if element_type not in allowed_types:
                raise ValidationError(
                    f"Cannot tag element type '{element_type}'. "
                    f"Allowed types: {', '.join(allowed_types)}"
                )

    def _validate_read_operation(self, action: Dict[str, Any]) -> None:
        """
        Validate read-only operation

        Args:
            action: Read action dictionary

        Raises:
            ValidationError: If operation exceeds limits
        """
        targets = action.get('targets', {})

        # Check element count if specified
        element_count = targets.get('element_count', 0)
        if element_count > self.max_elements * 2:  # Allow more for read-only
            raise ValidationError(
                f"Query scope too large: {element_count} elements "
                f"(maximum: {self.max_elements * 2})"
            )

    def check_scope_limits(
        self,
        operation: str,
        element_count: int,
        estimated_output_count: Optional[int] = None
    ) -> None:
        """
        Check if operation scope is within acceptable limits

        Args:
            operation: Operation name
            element_count: Number of elements to process
            estimated_output_count: Estimated number of output items (dimensions/tags)

        Raises:
            ValidationError: If scope exceeds limits
        """
        if element_count > self.max_elements:
            raise ValidationError(
                f"Operation scope exceeds limit: {element_count} elements "
                f"(maximum: {self.max_elements})"
            )

        if estimated_output_count:
            if operation == "create_dimensions" and estimated_output_count > self.max_dimensions:
                raise ValidationError(
                    f"Estimated dimensions ({estimated_output_count}) exceeds limit ({self.max_dimensions})"
                )
            elif operation == "create_tags" and estimated_output_count > self.max_tags:
                raise ValidationError(
                    f"Estimated tags ({estimated_output_count}) exceeds limit ({self.max_tags})"
                )

    def get_allowed_operations(self) -> List[str]:
        """Get list of allowed operations"""
        return sorted(list(ALLOWED_OPERATIONS))

    def get_blocked_operations(self) -> List[str]:
        """Get list of explicitly blocked operations"""
        return sorted(list(BLOCKED_OPERATIONS))


# Global validator instance
_validator = None


def get_validator() -> SafetyValidator:
    """Get global safety validator instance (singleton pattern)"""
    global _validator
    if _validator is None:
        _validator = SafetyValidator()
    return _validator


def validate_action(action: Dict[str, Any]) -> None:
    """
    Convenience function to validate an action

    Args:
        action: Action dictionary to validate

    Raises:
        ValidationError: If action is not valid
    """
    validator = get_validator()
    validator.validate_action(action)


def test_safety_validator():
    """Test function for safety validator"""
    print("Testing Safety Validator...")

    validator = get_validator()

    # Test 1: Valid dimension operation
    try:
        valid_action = {
            "operation": "create_dimensions",
            "targets": {
                "element_type": "Room",
                "scope": "Level 1",
                "element_count": 12
            },
            "parameters": {},
            "estimated_dimension_count": 48
        }
        validator.validate_action(valid_action)
        print("✓ Valid dimension operation accepted")
    except ValidationError as e:
        print(f"✗ Valid operation rejected: {e}")
        return False

    # Test 2: Blocked operation
    try:
        blocked_action = {
            "operation": "delete_elements",
            "targets": {}
        }
        validator.validate_action(blocked_action)
        print("✗ Blocked operation was not rejected!")
        return False
    except ValidationError:
        print("✓ Blocked operation correctly rejected")

    # Test 3: Scope too large
    try:
        large_scope_action = {
            "operation": "create_dimensions",
            "targets": {
                "element_count": 1000  # Exceeds default limit of 500
            }
        }
        validator.validate_action(large_scope_action)
        print("✗ Large scope was not rejected!")
        return False
    except ValidationError:
        print("✓ Large scope correctly rejected")

    # Test 4: Unknown operation
    try:
        unknown_action = {
            "operation": "unknown_operation",
            "targets": {}
        }
        validator.validate_action(unknown_action)
        print("✗ Unknown operation was not rejected!")
        return False
    except ValidationError:
        print("✓ Unknown operation correctly rejected")

    print("\nAllowed operations:", validator.get_allowed_operations())
    print("Blocked operations:", validator.get_blocked_operations())

    return True


if __name__ == '__main__':
    # Run test when module is executed directly
    test_safety_validator()
