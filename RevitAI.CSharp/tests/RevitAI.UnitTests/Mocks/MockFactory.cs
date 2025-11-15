using System.Collections.Generic;
using Moq;
using RevitAI.Services.Interfaces;

namespace RevitAI.UnitTests.Mocks
{
    /// <summary>
    /// Factory for creating mock implementations of Revit service interfaces.
    ///
    /// IMPORTANT: This factory uses Moq for interface mocking but cannot fully mock
    /// Revit API types (Room, Wall, Document) as they are sealed classes.
    ///
    /// Layer 1 Testing Strategy:
    /// - For logic that doesn't need Revit types: Mock interface return values with POCOs
    /// - For logic that uses Revit types: Use this as scaffolding for Layer 2 tests
    /// - Future: Create POCO wrappers (RoomInfo, WallInfo) to fully decouple from Revit API
    /// </summary>
    public static class MockFactory
    {
        /// <summary>
        /// Creates a mock IRoomAnalyzer that returns empty collections.
        /// Suitable for testing error paths and edge cases.
        /// </summary>
        public static Mock<IRoomAnalyzer> CreateEmptyRoomAnalyzer()
        {
            var mock = new Mock<IRoomAnalyzer>();

            // Setup to return empty collections (no rooms found scenario)
            mock.Setup(r => r.GetAllRooms(It.IsAny<Autodesk.Revit.DB.Document>()))
                .Returns(new List<Autodesk.Revit.DB.Architecture.Room>());

            mock.Setup(r => r.GetRoomsByLevel(It.IsAny<Autodesk.Revit.DB.Document>(), It.IsAny<string>()))
                .Returns(new List<Autodesk.Revit.DB.Architecture.Room>());

            return mock;
        }

        /// <summary>
        /// Creates a mock IRoomAnalyzer configured with specific room count.
        /// NOTE: Cannot return actual Room objects without Revit SDK.
        /// This demonstrates the pattern - actual implementation requires Revit.
        /// </summary>
        /// <param name="roomCount">Number of rooms to simulate (for testing counts/limits).</param>
        public static Mock<IRoomAnalyzer> CreateRoomAnalyzerWithCount(int roomCount)
        {
            var mock = new Mock<IRoomAnalyzer>();

            // Create a list that would have 'roomCount' items
            // Note: Can't instantiate actual Room objects without Revit
            // This is a placeholder showing the intended pattern
            var mockRooms = new List<Autodesk.Revit.DB.Architecture.Room>();
            // In production: mockRooms would be populated with mock Room objects
            // For Layer 1 testing: We test the count/iteration logic, not Room internals

            mock.Setup(r => r.GetAllRooms(It.IsAny<Autodesk.Revit.DB.Document>()))
                .Returns(mockRooms);

            return mock;
        }

        /// <summary>
        /// Creates a mock IDimensionFactory that tracks created dimensions.
        /// Useful for verifying that dimension creation was called correctly.
        /// </summary>
        public static Mock<IDimensionFactory> CreateTrackingDimensionFactory()
        {
            var mock = new Mock<IDimensionFactory>();

            // Setup to return null dimensions but allow verification of calls
            mock.Setup(d => d.CreateLinearDimension(
                    It.IsAny<Autodesk.Revit.DB.View>(),
                    It.IsAny<Autodesk.Revit.DB.Line>(),
                    It.IsAny<Autodesk.Revit.DB.ReferenceArray>()))
                .Returns((Autodesk.Revit.DB.Dimension)null);

            mock.Setup(d => d.CreateRoomDimensions(
                    It.IsAny<Autodesk.Revit.DB.View>(),
                    It.IsAny<IEnumerable<Autodesk.Revit.DB.Architecture.Room>>(),
                    It.IsAny<double>()))
                .Returns(new List<Autodesk.Revit.DB.Dimension>());

            return mock;
        }

        /// <summary>
        /// Creates a mock IRevitDocumentWrapper with common defaults.
        /// </summary>
        public static Mock<IRevitDocumentWrapper> CreateDocumentWrapper()
        {
            var mock = new Mock<IRevitDocumentWrapper>();

            mock.Setup(d => d.GetDocumentTitle())
                .Returns("Test Project.rvt");

            mock.Setup(d => d.IsDocumentModifiable())
                .Returns(true);

            // Note: GetActiveDocument(), GetActiveView(), StartTransaction()
            // return Revit types that can't be mocked without Revit SDK

            return mock;
        }
    }
}
