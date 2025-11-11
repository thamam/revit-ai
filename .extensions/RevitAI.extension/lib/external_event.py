"""
ExternalEvent Handler for Thread-Safe Revit API Access

This module implements the ExternalEvent pattern to allow background threads
(e.g., Claude API calls) to safely execute Revit API operations on the main thread.
"""

import threading
from typing import Callable, Any, Optional
from queue import Queue, Empty

# Revit API imports
try:
    from Autodesk.Revit.UI import IExternalEventHandler, ExternalEvent, Result
    from Autodesk.Revit.DB import Transaction
    REVIT_API_AVAILABLE = True
except ImportError:
    REVIT_API_AVAILABLE = False
    print("Warning: Revit API not available (development mode)")

# RevitAI imports
from logger import get_logger
from exceptions import RevitAPIError

logger = get_logger(__name__)


class RevitOperationRequest:
    """
    Represents a request to execute a Revit operation on the main thread

    Attributes:
        operation: Callable function to execute
        args: Positional arguments for the operation
        kwargs: Keyword arguments for the operation
        result_queue: Queue to receive the result
    """

    def __init__(self, operation: Callable, *args, **kwargs):
        self.operation = operation
        self.args = args
        self.kwargs = kwargs
        self.result_queue = Queue(maxsize=1)
        self.exception = None

    def execute(self, uidoc) -> Any:
        """
        Execute the operation and store result or exception

        Args:
            uidoc: Revit UIDocument
        """
        try:
            result = self.operation(uidoc, *self.args, **self.kwargs)
            self.result_queue.put(('success', result))
        except Exception as e:
            self.exception = e
            self.result_queue.put(('error', e))

    def wait_for_result(self, timeout: Optional[float] = None) -> Any:
        """
        Wait for the operation to complete and return result

        Args:
            timeout: Maximum time to wait in seconds (None = wait forever)

        Returns:
            Result from the operation

        Raises:
            RevitAPIError: If operation failed
            TimeoutError: If timeout exceeded
        """
        try:
            status, value = self.result_queue.get(timeout=timeout)

            if status == 'success':
                return value
            else:
                raise RevitAPIError(f"Revit operation failed: {value}") from None

        except Empty:
            raise TimeoutError(f"Revit operation timed out after {timeout} seconds") from None


class RevitExternalEventHandler(IExternalEventHandler):
    """
    IExternalEventHandler implementation for executing Revit operations

    This handler processes operation requests from the request queue
    and executes them on the Revit main thread.
    """

    def __init__(self):
        """Initialize the event handler with a request queue"""
        self.request_queue = Queue()
        self.current_request = None

    def Execute(self, uiapp):
        """
        Execute method called by Revit on the main thread

        Args:
            uiapp: Revit UIApplication

        Returns:
            Result.Succeeded or Result.Failed
        """
        try:
            # Get the current request
            if self.current_request is None:
                return Result.Failed

            # Execute the request
            uidoc = uiapp.ActiveUIDocument
            self.current_request.execute(uidoc)

            # Clear current request
            self.current_request = None

            return Result.Succeeded

        except Exception as e:
            print(f"Error in ExternalEvent.Execute: {e}")
            return Result.Failed

    def GetName(self):
        """Return name for debugging purposes"""
        return "RevitAI External Event Handler"

    def queue_operation(self, operation: Callable, *args, **kwargs) -> RevitOperationRequest:
        """
        Queue an operation for execution on the Revit main thread

        Args:
            operation: Callable that takes (uidoc, *args, **kwargs)
            *args: Positional arguments for the operation
            **kwargs: Keyword arguments for the operation

        Returns:
            RevitOperationRequest that can be used to wait for result
        """
        request = RevitOperationRequest(operation, *args, **kwargs)
        self.request_queue.put(request)
        self.current_request = request
        return request


class RevitEventManager:
    """
    Manager for RevitAI external events

    Provides a high-level interface for executing Revit operations
    from background threads (e.g., after LLM API calls).
    """

    def __init__(self, uiapp=None):
        """
        Initialize the event manager

        Args:
            uiapp: Revit UIApplication (optional, for initialization)
        """
        if not REVIT_API_AVAILABLE:
            print("Warning: Revit API not available. Event manager running in mock mode.")
            self.event_handler = None
            self.external_event = None
            return

        # Create event handler
        self.event_handler = RevitExternalEventHandler()

        # Create external event
        if uiapp is not None:
            self.external_event = ExternalEvent.Create(self.event_handler)
        else:
            # Defer creation until Revit context is available
            self.external_event = None

    def initialize(self, external_event):
        """
        Initialize with an already-created ExternalEvent

        Args:
            external_event: ExternalEvent instance
        """
        self.external_event = external_event

    def execute_on_main_thread(
        self,
        operation: Callable,
        timeout: float = 30.0,
        *args,
        **kwargs
    ) -> Any:
        """
        Execute a Revit operation on the main thread

        This method can be called from a background thread (e.g., after
        making an LLM API call). It will queue the operation and wait
        for it to complete on the Revit main thread.

        Args:
            operation: Callable that takes (uidoc, *args, **kwargs)
            timeout: Maximum time to wait for operation (seconds)
            *args: Positional arguments for the operation
            **kwargs: Keyword arguments for the operation

        Returns:
            Result from the operation

        Raises:
            RevitAPIError: If Revit operation fails
            TimeoutError: If operation times out
        """
        if not REVIT_API_AVAILABLE or self.external_event is None:
            # Development mode - execute directly (no threading)
            logger.warning(
                f"DEVELOPMENT MODE: Skipping Revit operation '{operation.__name__}' - "
                f"Revit API not available. This may cause unexpected behavior."
            )
            return None

        # Queue the operation
        request = self.event_handler.queue_operation(operation, *args, **kwargs)

        # Raise the external event to trigger execution
        self.external_event.Raise()

        # Wait for result
        return request.wait_for_result(timeout=timeout)

    def is_available(self) -> bool:
        """Check if event manager is ready to execute operations"""
        return REVIT_API_AVAILABLE and self.external_event is not None


# Global event manager instance
_event_manager = None


def get_event_manager() -> RevitEventManager:
    """Get global event manager instance (singleton pattern)"""
    global _event_manager
    if _event_manager is None:
        _event_manager = RevitEventManager()
    return _event_manager


def initialize_event_manager(external_event):
    """
    Initialize global event manager with ExternalEvent

    Call this during plugin initialization after creating the ExternalEvent.
    """
    manager = get_event_manager()
    manager.initialize(external_event)


# Example usage functions

def example_get_all_rooms(uidoc):
    """
    Example operation: Get all rooms in current document

    Args:
        uidoc: Revit UIDocument

    Returns:
        List of room elements
    """
    from Autodesk.Revit.DB import FilteredElementCollector, BuiltInCategory

    doc = uidoc.Document
    collector = FilteredElementCollector(doc)
    rooms = collector.OfCategory(BuiltInCategory.OST_Rooms).ToElements()

    return list(rooms)


def example_create_text_note(uidoc, text: str, location):
    """
    Example operation: Create a text note

    Args:
        uidoc: Revit UIDocument
        text: Text content
        location: XYZ location for the note

    Returns:
        Created TextNote element
    """
    from Autodesk.Revit.DB import Transaction, TextNote

    doc = uidoc.Document

    with Transaction(doc, "AI: Create Text Note") as t:
        t.Start()
        try:
            # Get active view
            view = uidoc.ActiveView

            # Create text note
            text_note = TextNote.Create(doc, view.Id, location, text, doc.GetDefaultElementTypeId(TextNote))

            t.Commit()
            return text_note

        except Exception as e:
            t.RollBack()
            raise RevitAPIError(f"Failed to create text note: {e}") from e


def test_external_event():
    """Test function for external event (requires Revit context)"""
    print("Testing ExternalEvent pattern...")

    manager = get_event_manager()

    if not manager.is_available():
        print("✗ Event manager not available (Revit context required)")
        return False

    try:
        # Test operation
        rooms = manager.execute_on_main_thread(example_get_all_rooms, timeout=10.0)
        print(f"✓ Got {len(rooms)} rooms from Revit")
        return True

    except Exception as e:
        print(f"✗ Test failed: {e}")
        return False


if __name__ == '__main__':
    # Run test when module is executed directly
    test_external_event()
