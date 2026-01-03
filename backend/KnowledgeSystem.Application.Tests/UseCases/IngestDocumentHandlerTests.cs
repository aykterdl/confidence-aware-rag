using FluentValidation;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Application.UseCases.IngestDocument;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.Services;
using KnowledgeSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace KnowledgeSystem.Application.Tests.UseCases;

public class IngestDocumentHandlerTests
{
    private readonly Mock<IDocumentRepository> _mockRepository;
    private readonly Mock<IContentSectioningStrategy> _mockStrategy;
    private readonly Mock<IEmbeddingGenerator> _mockEmbeddingGenerator;
    private readonly IValidator<IngestDocumentCommand> _validator;
    private readonly IngestDocumentHandler _handler;

    public IngestDocumentHandlerTests()
    {
        _mockRepository = new Mock<IDocumentRepository>();
        _mockStrategy = new Mock<IContentSectioningStrategy>();
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator>();
        _validator = new IngestDocumentCommandValidator();

        _handler = new IngestDocumentHandler(
            _mockRepository.Object,
            _mockStrategy.Object,
            _mockEmbeddingGenerator.Object,
            _validator);
    }

    [Fact]
    public async Task HandleAsync_WithValidCommand_CreatesDocumentSuccessfully()
    {
        // Arrange
        var command = new IngestDocumentCommand
        {
            Title = "Test Document",
            Content = "This is test content for ingestion."
        };

        var mockEmbeddings = new List<float[]>
        {
            new float[] { 0.1f, 0.2f, 0.3f },
            new float[] { 0.4f, 0.5f, 0.6f }
        };

        DocumentId? capturedDocumentId = null;

        // Setup strategy to create sections with the correct document ID
        _mockStrategy
            .Setup(s => s.SectionContent(It.IsAny<DocumentId>(), command.Content))
            .Returns((DocumentId docId, string content) =>
            {
                capturedDocumentId = docId;
                return new List<ContentSection>
                {
                    ContentSection.CreateGeneric(docId, 0, "Section 1"),
                    ContentSection.CreateGeneric(docId, 1, "Section 2")
                };
            });

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingsAsync(
                It.IsAny<IEnumerable<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mockEmbeddings);

        _mockRepository
            .Setup(r => r.SaveAsync(It.IsAny<KnowledgeDocument>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((KnowledgeDocument doc, CancellationToken ct) => doc.Id);

        // Act
        var result = await _handler.HandleAsync(command);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.DocumentId);
        Assert.Equal(expected: capturedDocumentId!.Value.Value, actual: result.DocumentId);
        Assert.Equal(2, result.SectionsCreated);
        Assert.Equal("Test Document", result.Title);

        _mockStrategy.Verify(
            s => s.SectionContent(It.IsAny<DocumentId>(), command.Content),
            Times.Once);

        _mockEmbeddingGenerator.Verify(
            e => e.GenerateEmbeddingsAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Once);

        _mockRepository.Verify(
            r => r.SaveAsync(It.IsAny<KnowledgeDocument>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyTitle_ThrowsValidationException()
    {
        // Arrange
        var command = new IngestDocumentCommand
        {
            Title = "",
            Content = "This is test content."
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WithEmptyContent_ThrowsValidationException()
    {
        // Arrange
        var command = new IngestDocumentCommand
        {
            Title = "Test Document",
            Content = ""
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }

    [Fact]
    public async Task HandleAsync_WithShortContent_ThrowsValidationException()
    {
        // Arrange
        var command = new IngestDocumentCommand
        {
            Title = "Test",
            Content = "Short" // Less than 10 characters
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(command));
    }
}

