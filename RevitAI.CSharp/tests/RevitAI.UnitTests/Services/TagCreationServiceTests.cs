using System;
using System.Collections.Generic;
using NUnit.Framework;
using RevitAI.Models.Domain;
using RevitAI.Services;
using RevitAI.Services.Interfaces;

namespace RevitAI.UnitTests.Services;

/// <summary>
/// Unit tests for TagCreationService (Layer 2 Revit API bridge).
/// Uses mock implementations of IRevitDocument and ITransaction for cross-platform testing.
/// </summary>
[TestFixture]
[Category("Unit")]
[Category("Layer2")]
public class TagCreationServiceTests
{
    private TagCreationService _service = null!;
    private MockRevitDocument _mockDocument = null!;

    #pragma warning disable NUnit1032 // Mock transactions don't need disposal
    private MockTransaction _mockTransaction = null!;
    #pragma warning restore NUnit1032

    [SetUp]
    public void Setup()
    {
        _mockDocument = new MockRevitDocument();
        _mockTransaction = new MockTransaction("AI: Create Tags");
        _service = new TagCreationService(_mockDocument);
    }

    #region Constructor Tests

    [Test]
    public void Constructor_NullDocument_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new TagCreationService(null!));
    }

    [Test]
    public void Constructor_ValidDocument_CreatesInstance()
    {
        // Act
        var service = new TagCreationService(_mockDocument);

        // Assert
        Assert.IsNotNull(service);
    }

    #endregion

    #region CreateTags Success Tests

    [Test]
    public void CreateTags_AllSuccessfulPlacements_ReturnsSuccess()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0)),
            TagPlacement.CreateSuccess(1002, new XYZ(0, 20, 0)),
            TagPlacement.CreateSuccess(1003, new XYZ(0, 30, 0))
        };
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Should return success");
        Assert.AreEqual(3, result.CreatedCount, "Should create 3 tags");
        Assert.AreEqual(0, result.FailedCount, "Should have 0 failures");
        Assert.IsTrue(_mockTransaction.WasCommitted, "Transaction should be committed");
        Assert.IsFalse(_mockTransaction.WasRolledBack, "Transaction should not be rolled back");
    }

    [Test]
    public void CreateTags_SinglePlacement_CreatesOneTag()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0), hasLeader: true)
        };
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(1, result.CreatedCount);
        Assert.AreEqual(1, _mockDocument.CreatedTags.Count);
        Assert.AreEqual(1001, _mockDocument.CreatedTags[0].ElementId);
        Assert.IsTrue(_mockDocument.CreatedTags[0].HasLeader);
    }

    #endregion

    #region CreateTags Partial Success Tests

    [Test]
    public void CreateTags_MixedSuccessAndFailed_ReturnsPartialSuccess()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0)),
            TagPlacement.CreateFailed(1002, "Collision detected"),
            TagPlacement.CreateSuccess(1003, new XYZ(0, 30, 0)),
            TagPlacement.CreateFailed(1004, "Out of bounds")
        };
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(result.IsSuccess, "Should return success for partial");
        Assert.AreEqual(2, result.CreatedCount, "Should create 2 tags");
        Assert.AreEqual(2, result.FailedCount, "Should have 2 failures");
        Assert.AreEqual(2, result.FailureDetails.Count, "Should have 2 failure details");
        Assert.IsTrue(_mockTransaction.WasCommitted, "Transaction should be committed");
    }

    [Test]
    public void CreateTags_AllFailedPlacements_ReturnsFailure()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateFailed(1001, "Collision detected"),
            TagPlacement.CreateFailed(1002, "Out of bounds")
        };
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsFalse(result.IsSuccess, "Should return failure");
        Assert.AreEqual(0, result.CreatedCount, "Should create 0 tags");
        Assert.AreEqual(2, result.FailedCount, "Should have 2 failures");
        Assert.IsTrue(_mockTransaction.WasRolledBack, "Transaction should be rolled back");
    }

    #endregion

    #region CreateTags Validation Tests

    [Test]
    public void CreateTags_NullPlacements_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.CreateTags(null!, tagTypeId: 5001, _mockTransaction));
    }

    [Test]
    public void CreateTags_NullTransaction_ThrowsArgumentNullException()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _service.CreateTags(placements, tagTypeId: 5001, null!));
    }

    [Test]
    public void CreateTags_InvalidTagTypeId_ThrowsArgumentException()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateTags(placements, tagTypeId: 0, _mockTransaction));
    }

    [Test]
    public void CreateTags_NegativeTagTypeId_ThrowsArgumentException()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _service.CreateTags(placements, tagTypeId: -1, _mockTransaction));
    }

    #endregion

    #region CreateTags Element Validation Tests

    [Test]
    public void CreateTags_NonExistentElement_SkipsElement()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0)),
            TagPlacement.CreateSuccess(9999, new XYZ(0, 20, 0)), // Non-existent
            TagPlacement.CreateSuccess(1003, new XYZ(0, 30, 0))
        };
        int tagTypeId = 5001;

        _mockDocument.ExistingElementIds = new HashSet<int> { 1001, 1003 }; // 9999 not in set

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(2, result.CreatedCount, "Should create 2 tags (skip non-existent)");
        Assert.AreEqual(1, result.FailedCount, "Should have 1 failure");
        Assert.That(result.FailureDetails[0], Does.Contain("9999").And.Contains("not found"));
    }

    #endregion

    #region CreateTags Transaction Tests

    [Test]
    public void CreateTags_StartsTransaction()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };
        int tagTypeId = 5001;

        // Act
        _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(_mockTransaction.WasStarted, "Transaction should be started");
    }

    [Test]
    public void CreateTags_SuccessfulOperation_CommitsTransaction()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };
        int tagTypeId = 5001;

        // Act
        _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(_mockTransaction.WasCommitted);
        Assert.IsFalse(_mockTransaction.WasRolledBack);
    }

    [Test]
    public void CreateTags_NoSuccessfulTags_RollsBackTransaction()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateFailed(1001, "Collision")
        };
        int tagTypeId = 5001;

        // Act
        _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsFalse(_mockTransaction.WasCommitted);
        Assert.IsTrue(_mockTransaction.WasRolledBack);
    }

    [Test]
    public void CreateTags_DocumentException_RollsBackTransaction()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };
        int tagTypeId = 5001;

        _mockDocument.ThrowOnCreateTag = true; // Force exception

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsFalse(result.IsSuccess);
        Assert.IsTrue(_mockTransaction.WasRolledBack);
    }

    #endregion

    #region CreateTags Performance Tests

    [Test]
    public void CreateTags_RecordsExecutionTime()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0))
        };
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.GreaterOrEqual(result.ExecutionTimeMs, 0, "Should record execution time");
    }

    [Test]
    public void CreateTags_LargeBatch_CompletesInReasonableTime()
    {
        // Arrange
        var placements = new List<TagPlacement>();
        for (int i = 0; i < 100; i++)
        {
            placements.Add(TagPlacement.CreateSuccess(1000 + i, new XYZ(0, i * 10, 0)));
        }
        int tagTypeId = 5001;

        // Act
        var result = _service.CreateTags(placements, tagTypeId, _mockTransaction);

        // Assert
        Assert.IsTrue(result.IsSuccess);
        Assert.AreEqual(100, result.CreatedCount);
        Assert.Less(result.ExecutionTimeMs, 1000, "Should complete in < 1 second");
    }

    #endregion

    #region ValidateTagType Tests

    [Test]
    public void ValidateTagType_ExistingTagType_ReturnsTrue()
    {
        // Arrange
        int tagTypeId = 5001;
        _mockDocument.ExistingElementIds.Add(tagTypeId);

        // Act
        bool isValid = _service.ValidateTagType(tagTypeId);

        // Assert
        Assert.IsTrue(isValid);
    }

    [Test]
    public void ValidateTagType_NonExistentTagType_ReturnsFalse()
    {
        // Arrange
        int tagTypeId = 9999;

        // Act
        bool isValid = _service.ValidateTagType(tagTypeId);

        // Assert
        Assert.IsFalse(isValid);
    }

    [Test]
    public void ValidateTagType_ZeroId_ReturnsFalse()
    {
        // Act
        bool isValid = _service.ValidateTagType(0);

        // Assert
        Assert.IsFalse(isValid);
    }

    [Test]
    public void ValidateTagType_NegativeId_ReturnsFalse()
    {
        // Act
        bool isValid = _service.ValidateTagType(-1);

        // Assert
        Assert.IsFalse(isValid);
    }

    #endregion

    #region GetPreviewSummary Tests

    [Test]
    public void GetPreviewSummary_AllSuccessful_Returns100Percent()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0)),
            TagPlacement.CreateSuccess(1002, new XYZ(0, 20, 0)),
            TagPlacement.CreateSuccess(1003, new XYZ(0, 30, 0))
        };

        // Act
        string summary = TagCreationService.GetPreviewSummary(placements);

        // Assert
        Assert.That(summary, Does.Contain("3 tags"));
        Assert.That(summary, Does.Contain("100%"));
    }

    [Test]
    public void GetPreviewSummary_PartialSuccess_ShowsPercentage()
    {
        // Arrange
        var placements = new List<TagPlacement>
        {
            TagPlacement.CreateSuccess(1001, new XYZ(0, 10, 0)),
            TagPlacement.CreateFailed(1002, "Collision"),
            TagPlacement.CreateSuccess(1003, new XYZ(0, 30, 0))
        };

        // Act
        string summary = TagCreationService.GetPreviewSummary(placements);

        // Assert
        Assert.That(summary, Does.Contain("2 tags"));
        Assert.That(summary, Does.Contain("1 skipped"));
    }

    [Test]
    public void GetPreviewSummary_EmptyList_ReturnsNoTags()
    {
        // Arrange
        var placements = new List<TagPlacement>();

        // Act
        string summary = TagCreationService.GetPreviewSummary(placements);

        // Assert
        Assert.AreEqual("No tags to create", summary);
    }

    [Test]
    public void GetPreviewSummary_NullList_ReturnsNoTags()
    {
        // Act
        string summary = TagCreationService.GetPreviewSummary(null!);

        // Assert
        Assert.AreEqual("No tags to create", summary);
    }

    #endregion
}

#region Mock Implementations

/// <summary>
/// Mock implementation of IRevitDocument for testing.
/// </summary>
internal class MockRevitDocument : IRevitDocument
{
    public HashSet<int> ExistingElementIds { get; set; }

    public MockRevitDocument()
    {
        // Initialize with elements that MockElement.CreateGrid() will create (1000-1499)
        // Plus tag type 5001
        ExistingElementIds = new HashSet<int>();
        for (int i = 1000; i < 1500; i++)
        {
            ExistingElementIds.Add(i);
        }
        ExistingElementIds.Add(5001); // Tag type
    }

    public List<CreatedTag> CreatedTags { get; } = new List<CreatedTag>();
    public bool ThrowOnCreateTag { get; set; }

    public int CreateTag(int tagTypeId, int viewId, int elementId, bool addLeader, XYZ location)
    {
        if (ThrowOnCreateTag)
        {
            throw new InvalidOperationException("Mock exception: CreateTag failed");
        }

        int tagId = 9000 + CreatedTags.Count;
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
        return 1; // Default active view
    }

    public bool ElementExists(int elementId)
    {
        return ExistingElementIds.Contains(elementId);
    }
}

/// <summary>
/// Represents a created tag for test verification.
/// </summary>
internal class CreatedTag
{
    public int TagId { get; set; }
    public int TagTypeId { get; set; }
    public int ViewId { get; set; }
    public int ElementId { get; set; }
    public bool HasLeader { get; set; }
    public XYZ Location { get; set; } = new XYZ(0, 0, 0);
}

/// <summary>
/// Mock implementation of ITransaction for testing.
/// </summary>
internal class MockTransaction : ITransaction
{
    public string Name { get; }
    public bool IsActive { get; private set; }
    public bool WasStarted { get; private set; }
    public bool WasCommitted { get; private set; }
    public bool WasRolledBack { get; private set; }

    public MockTransaction(string name)
    {
        Name = name;
    }

    public void Start()
    {
        WasStarted = true;
        IsActive = true;
    }

    public void Commit()
    {
        if (!IsActive) throw new InvalidOperationException("Transaction not active");
        WasCommitted = true;
        IsActive = false;
    }

    public void RollBack()
    {
        if (!IsActive) throw new InvalidOperationException("Transaction not active");
        WasRolledBack = true;
        IsActive = false;
    }

    public void Dispose()
    {
        if (IsActive)
        {
            RollBack();
        }
    }
}

#endregion
