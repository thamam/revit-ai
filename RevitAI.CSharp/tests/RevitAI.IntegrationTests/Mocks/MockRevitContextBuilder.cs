using System;
using System.Threading.Tasks;
using RevitAI.Models;
using RevitAI.Services;

namespace RevitAI.IntegrationTests.Mocks
{
    /// <summary>
    /// Mock implementation of IRevitContextBuilder for integration testing
    /// Returns pre-configured mock data without requiring actual Revit API
    /// </summary>
    public class MockRevitContextBuilder : IRevitContextBuilder
    {
        private RevitContext _mockContext;

        /// <summary>
        /// Create mock context builder with default typical context
        /// </summary>
        public MockRevitContextBuilder()
        {
            _mockContext = Fixtures.MockRevitData.CreateTypicalContext();
        }

        /// <summary>
        /// Create mock context builder with custom context
        /// </summary>
        public MockRevitContextBuilder(RevitContext mockContext)
        {
            _mockContext = mockContext ?? throw new ArgumentNullException(nameof(mockContext));
        }

        /// <summary>
        /// Get tagging context (returns pre-configured mock data)
        /// </summary>
        public Task<RevitContext> GetTaggingContextAsync()
        {
            return Task.FromResult(_mockContext);
        }

        /// <summary>
        /// Get basic context (returns same mock data)
        /// </summary>
        public Task<RevitContext> GetBasicContextAsync()
        {
            return Task.FromResult(_mockContext);
        }

        /// <summary>
        /// Update the mock context (for testing context changes)
        /// </summary>
        public void SetMockContext(RevitContext context)
        {
            _mockContext = context ?? throw new ArgumentNullException(nameof(context));
        }

        /// <summary>
        /// Update element count for a specific category (for testing scope limits)
        /// </summary>
        public void SetElementCount(string category, int total, int untagged)
        {
            if (_mockContext.ElementSummary.ContainsKey(category))
            {
                _mockContext.ElementSummary[category] = new ElementCount
                {
                    Total = total,
                    Untagged = untagged
                };
            }
            else
            {
                _mockContext.ElementSummary.Add(category, new ElementCount
                {
                    Total = total,
                    Untagged = untagged
                });
            }
        }

        /// <summary>
        /// Add a tag type to available tag types (for testing tag type validation)
        /// </summary>
        public void AddTagType(string name, string category, string family)
        {
            _mockContext.AvailableTagTypes.Add(new TagTypeInfo
            {
                Name = name,
                Category = category,
                Family = family
            });
        }

        /// <summary>
        /// Clear all available tag types (for testing missing tag types)
        /// </summary>
        public void ClearTagTypes()
        {
            _mockContext.AvailableTagTypes.Clear();
        }
    }
}
