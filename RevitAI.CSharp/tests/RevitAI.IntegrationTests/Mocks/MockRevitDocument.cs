using System.Collections.Generic;
using RevitAI.Models.Domain;
using RevitAI.Services.Interfaces;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock implementation of IRevitDocument for integration testing.
    /// Tracks all CreateTag calls and simulates Revit Document API.
    /// </summary>
    public class MockRevitDocument : IRevitDocument
    {
        private int _nextTagId = 5000;
        private readonly HashSet<int> _existingElements = new HashSet<int>();

        /// <summary>
        /// List of all tags created (for verification in tests).
        /// </summary>
        public List<CreatedTag> CreatedTags { get; } = new List<CreatedTag>();

        /// <summary>
        /// Current active view ID (default = 1).
        /// </summary>
        public int ActiveViewId { get; set; } = 1;

        /// <summary>
        /// Add elements to the mock document.
        /// </summary>
        public void AddElement(int elementId)
        {
            _existingElements.Add(elementId);
        }

        /// <summary>
        /// Add multiple elements to the mock document.
        /// </summary>
        public void AddElements(IEnumerable<int> elementIds)
        {
            foreach (var id in elementIds)
            {
                _existingElements.Add(id);
            }
        }

        public int CreateTag(int tagTypeId, int viewId, int elementId, bool addLeader, XYZ location)
        {
            // Simulate tag creation
            int tagId = _nextTagId++;

            CreatedTags.Add(new CreatedTag
            {
                TagId = tagId,
                TagTypeId = tagTypeId,
                ViewId = viewId,
                ElementId = elementId,
                HasLeader = addLeader,
                Location = location
            });

            return tagId;
        }

        public int GetActiveViewId()
        {
            return ActiveViewId;
        }

        public bool ElementExists(int elementId)
        {
            return _existingElements.Contains(elementId);
        }

        /// <summary>
        /// Clear all created tags (for test cleanup).
        /// </summary>
        public void Clear()
        {
            CreatedTags.Clear();
        }
    }

    /// <summary>
    /// Represents a tag that was created in the mock document.
    /// Used for verification in tests.
    /// </summary>
    public class CreatedTag
    {
        public int TagId { get; set; }
        public int TagTypeId { get; set; }
        public int ViewId { get; set; }
        public int ElementId { get; set; }
        public bool HasLeader { get; set; }
        public XYZ Location { get; set; }

        public override string ToString()
        {
            return $"Tag[{TagId}] for Element[{ElementId}] at {Location} (Leader={HasLeader})";
        }
    }
}
