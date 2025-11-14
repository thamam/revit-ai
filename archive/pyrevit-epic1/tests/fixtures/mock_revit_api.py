"""
Mock Revit API for testing

Provides mocked Revit API classes and objects for unit testing
without requiring Revit to be running.
"""

from typing import List, Optional, Any


class MockElementId:
    """Mock Revit ElementId"""
    def __init__(self, value: int):
        self.IntegerValue = value

    def __eq__(self, other):
        return self.IntegerValue == other.IntegerValue

    def __hash__(self):
        return hash(self.IntegerValue)


class MockLevel:
    """Mock Revit Level"""
    def __init__(self, name: str, elevation: float = 0.0):
        self.Name = name
        self.Elevation = elevation
        self.Id = MockElementId(hash(name))


class MockRoom:
    """Mock Revit Room"""
    def __init__(self, number: str, name: str, level: MockLevel):
        self.Number = number
        self.Name = name
        self.Level = level
        self.LevelId = level.Id
        self.Id = MockElementId(hash(f"{number}_{name}"))


class MockView:
    """Mock Revit View"""
    def __init__(self, name: str):
        self.Name = name
        self.Id = MockElementId(hash(name))


class MockDocument:
    """Mock Revit Document"""
    def __init__(self, title: str = "Test Project"):
        self.Title = title
        self._elements = []

    def GetElement(self, element_id: MockElementId):
        """Get element by ID"""
        for elem in self._elements:
            if elem.Id == element_id:
                return elem
        return None

    def add_element(self, element):
        """Helper method to add elements for testing"""
        self._elements.append(element)


class MockSelection:
    """Mock Revit Selection"""
    def __init__(self):
        self._element_ids = []

    def GetElementIds(self):
        """Get selected element IDs"""
        return self._element_ids

    def set_selection(self, element_ids: List[MockElementId]):
        """Helper method to set selection for testing"""
        self._element_ids = element_ids


class MockUIDocument:
    """Mock Revit UIDocument"""
    def __init__(self, document: MockDocument):
        self.Document = document
        self.ActiveView = MockView("Default 3D View")
        self.Selection = MockSelection()


class MockTransaction:
    """Mock Revit Transaction"""
    def __init__(self, doc: MockDocument, name: str):
        self.doc = doc
        self.name = name
        self._started = False
        self._committed = False

    def Start(self):
        """Start transaction"""
        self._started = True
        return True

    def Commit(self):
        """Commit transaction"""
        if not self._started:
            raise RuntimeError("Transaction not started")
        self._committed = True
        return True

    def RollBack(self):
        """Rollback transaction"""
        self._started = False
        self._committed = False

    def __enter__(self):
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        if exc_type is None and self._started and not self._committed:
            self.Commit()
        elif exc_type is not None and self._started:
            self.RollBack()
        return False


def create_mock_revit_context():
    """
    Create a complete mock Revit context for testing

    Returns:
        tuple: (mock_uidoc, mock_doc) for testing
    """
    # Create document
    doc = MockDocument("Test Project")

    # Add some test levels
    level_1 = MockLevel("Level 1", 0.0)
    level_2 = MockLevel("Level 2", 10.0)
    doc.add_element(level_1)
    doc.add_element(level_2)

    # Add some test rooms
    room_1 = MockRoom("101", "Office", level_1)
    room_2 = MockRoom("102", "Conference Room", level_1)
    room_3 = MockRoom("201", "Office", level_2)
    doc.add_element(room_1)
    doc.add_element(room_2)
    doc.add_element(room_3)

    # Create UIDocument
    uidoc = MockUIDocument(doc)

    return uidoc, doc
