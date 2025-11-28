using System.Collections.Generic;
using RevitAI.Models;

namespace RevitAI.IntegrationTests.Fixtures
{
    /// <summary>
    /// Mock Revit data fixtures for integration testing
    /// Provides realistic test data without requiring actual Revit API objects
    /// </summary>
    public static class MockRevitData
    {
        /// <summary>
        /// Create a mock RevitContext with typical project data
        /// </summary>
        public static RevitContext CreateTypicalContext()
        {
            return new RevitContext
            {
                Levels = new List<string> { "Level 0", "Level 1", "Level 2" },
                CurrentView = "Level 1 - Floor Plan",
                Selection = new SelectionInfo
                {
                    Count = 0,
                    Types = new List<string>()
                },
                Project = new ProjectInfo
                {
                    Name = "Test Project",
                    Number = "2024-001"
                },
                AvailableTagTypes = CreateTypicalTagTypes(),
                ElementSummary = CreateTypicalElementSummary()
            };
        }

        /// <summary>
        /// Create a mock context with many elements (for scope limit testing)
        /// </summary>
        public static RevitContext CreateContextWithManyElements()
        {
            var context = CreateTypicalContext();
            context.ElementSummary = new Dictionary<string, ElementCount>
            {
                ["Doors"] = new ElementCount { Total = 600, Untagged = 600 },
                ["Walls"] = new ElementCount { Total = 800, Untagged = 800 },
                ["Windows"] = new ElementCount { Total = 1200, Untagged = 1200 }
            };
            return context;
        }

        /// <summary>
        /// Create a mock context with selection
        /// </summary>
        public static RevitContext CreateContextWithSelection(int selectedCount, string selectedType)
        {
            var context = CreateTypicalContext();
            context.Selection = new SelectionInfo
            {
                Count = selectedCount,
                Types = new List<string> { selectedType }
            };
            return context;
        }

        /// <summary>
        /// Create a mock context with missing tag types (for validation testing)
        /// </summary>
        public static RevitContext CreateContextWithMissingTagTypes()
        {
            var context = CreateTypicalContext();
            context.AvailableTagTypes = new List<TagTypeInfo>
            {
                new TagTypeInfo
                {
                    Name = "Door Tag",
                    Category = "Door Tags",
                    Family = "Door Tag"
                }
                // Missing Wall Tag, Room Tag, etc.
            };
            return context;
        }

        /// <summary>
        /// Create a mock context with partially tagged elements
        /// </summary>
        public static RevitContext CreateContextWithPartiallyTaggedElements()
        {
            var context = CreateTypicalContext();
            context.ElementSummary = new Dictionary<string, ElementCount>
            {
                ["Doors"] = new ElementCount { Total = 50, Untagged = 20 }, // 30 tagged, 20 untagged
                ["Walls"] = new ElementCount { Total = 100, Untagged = 60 }, // 40 tagged, 60 untagged
                ["Rooms"] = new ElementCount { Total = 30, Untagged = 0 },   // All tagged
                ["Windows"] = new ElementCount { Total = 40, Untagged = 40 } // None tagged
            };
            return context;
        }

        /// <summary>
        /// Create typical tag types available in a Revit project
        /// </summary>
        private static List<TagTypeInfo> CreateTypicalTagTypes()
        {
            return new List<TagTypeInfo>
            {
                new TagTypeInfo
                {
                    Name = "Door Tag",
                    Category = "Door Tags",
                    Family = "Door Tag"
                },
                new TagTypeInfo
                {
                    Name = "Wall Tag",
                    Category = "Wall Tags",
                    Family = "Wall Tag"
                },
                new TagTypeInfo
                {
                    Name = "Room Tag",
                    Category = "Room Tags",
                    Family = "Room Tag"
                },
                new TagTypeInfo
                {
                    Name = "Window Tag",
                    Category = "Window Tags",
                    Family = "Window Tag"
                },
                new TagTypeInfo
                {
                    Name = "Mechanical Equipment Tag",
                    Category = "Mechanical Equipment Tags",
                    Family = "M_Mechanical Equipment Tag"
                },
                new TagTypeInfo
                {
                    Name = "Electrical Equipment Tag",
                    Category = "Electrical Equipment Tags",
                    Family = "E_Electrical Equipment Tag"
                }
            };
        }

        /// <summary>
        /// Create typical element summary for a residential project
        /// </summary>
        private static Dictionary<string, ElementCount> CreateTypicalElementSummary()
        {
            return new Dictionary<string, ElementCount>
            {
                ["Doors"] = new ElementCount { Total = 47, Untagged = 47 },
                ["Walls"] = new ElementCount { Total = 120, Untagged = 85 },
                ["Rooms"] = new ElementCount { Total = 12, Untagged = 12 },
                ["Windows"] = new ElementCount { Total = 35, Untagged = 35 },
                ["Mechanical Equipment"] = new ElementCount { Total = 8, Untagged = 8 },
                ["Electrical Equipment"] = new ElementCount { Total = 15, Untagged = 15 }
            };
        }

        /// <summary>
        /// Create context for Hebrew language testing
        /// </summary>
        public static RevitContext CreateHebrewContext()
        {
            return new RevitContext
            {
                Levels = new List<string> { "קומה 0", "קומה 1", "קומה 2" },
                CurrentView = "קומה 1 - תוכנית קומה",
                Selection = new SelectionInfo
                {
                    Count = 0,
                    Types = new List<string>()
                },
                Project = new ProjectInfo
                {
                    Name = "פרויקט בדיקה",
                    Number = "2024-001"
                },
                AvailableTagTypes = CreateTypicalTagTypes(), // Tag types remain in English (Revit standard)
                ElementSummary = CreateTypicalElementSummary()
            };
        }

        /// <summary>
        /// Create empty context (no elements)
        /// </summary>
        public static RevitContext CreateEmptyContext()
        {
            return new RevitContext
            {
                Levels = new List<string> { "Level 1" },
                CurrentView = "Level 1 - Floor Plan",
                Selection = new SelectionInfo
                {
                    Count = 0,
                    Types = new List<string>()
                },
                Project = new ProjectInfo
                {
                    Name = "Empty Project",
                    Number = "TEST-EMPTY"
                },
                AvailableTagTypes = CreateTypicalTagTypes(),
                ElementSummary = new Dictionary<string, ElementCount>
                {
                    ["Doors"] = new ElementCount { Total = 0, Untagged = 0 },
                    ["Walls"] = new ElementCount { Total = 0, Untagged = 0 },
                    ["Rooms"] = new ElementCount { Total = 0, Untagged = 0 }
                }
            };
        }
    }
}
