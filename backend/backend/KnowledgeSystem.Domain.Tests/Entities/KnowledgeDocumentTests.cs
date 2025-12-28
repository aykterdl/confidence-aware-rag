using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;
using Xunit;

namespace KnowledgeSystem.Domain.Tests.Entities;

public class KnowledgeDocumentTests
{
    [Fact]
    public void Create_WithValidTitle_CreatesDocument()
    {
        // Act
        var document = KnowledgeDocument.Create("Test Document");

        // Assert
        Assert.NotNull(document);
        Assert.Equal("Test Document", document.Title);
        Assert.NotEqual(default(DocumentId), document.Id);
        Assert.Equal(0, document.SectionCount);
        Assert.False(document.IsValid()); // No sections yet
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Create_WithEmptyTitle_ThrowsException(string? invalidTitle)
    {
        // Act & Assert
        Assert.Throws<ArgumentException>(() => KnowledgeDocument.Create(invalidTitle!));
    }

    [Fact]
    public void AddSection_WithValidSection_AddsSuccessfully()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section = ContentSection.CreateGeneric(document.Id, 0, "Content");

        // Act
        document.AddSection(section);

        // Assert
        Assert.Equal(1, document.SectionCount);
        Assert.True(document.IsValid());
    }

    [Fact]
    public void AddSection_WithDuplicateIndex_ThrowsException()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section1 = ContentSection.CreateGeneric(document.Id, 0, "Content 1");
        var section2 = ContentSection.CreateGeneric(document.Id, 0, "Content 2");
        document.AddSection(section1);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => document.AddSection(section2));
    }

    [Fact]
    public void AddSection_WithWrongDocumentId_ThrowsException()
    {
        // Arrange
        var document1 = KnowledgeDocument.Create("Doc 1");
        var document2 = KnowledgeDocument.Create("Doc 2");
        var section = ContentSection.CreateGeneric(document2.Id, 0, "Content");

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => document1.AddSection(section));
    }

    [Fact]
    public void IsReadyForSearch_WithNoSections_ReturnsFalse()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");

        // Act & Assert
        Assert.False(document.IsReadyForSearch());
    }

    [Fact]
    public void IsReadyForSearch_WithSectionsWithoutEmbeddings_ReturnsFalse()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section = ContentSection.CreateGeneric(document.Id, 0, "Content");
        document.AddSection(section);

        // Act & Assert
        Assert.False(document.IsReadyForSearch());
    }

    [Fact]
    public void IsReadyForSearch_WithAllSectionsHavingEmbeddings_ReturnsTrue()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section = ContentSection.CreateGeneric(document.Id, 0, "Content");
        section.SetEmbedding(new float[] { 0.1f, 0.2f, 0.3f });
        document.AddSection(section);

        // Act & Assert
        Assert.True(document.IsReadyForSearch());
    }

    [Fact]
    public void GetSectionByIndex_WithExistingIndex_ReturnsSection()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section = ContentSection.CreateGeneric(document.Id, 5, "Content");
        document.AddSection(section);

        // Act
        var retrieved = document.GetSectionByIndex(5);

        // Assert
        Assert.NotNull(retrieved);
        Assert.Equal(5, retrieved.Index);
    }

    [Fact]
    public void GetSectionByIndex_WithNonExistingIndex_ReturnsNull()
    {
        // Arrange
        var document = KnowledgeDocument.Create("Test");
        var section = ContentSection.CreateGeneric(document.Id, 0, "Content");
        document.AddSection(section);

        // Act
        var retrieved = document.GetSectionByIndex(99);

        // Assert
        Assert.Null(retrieved);
    }
}

