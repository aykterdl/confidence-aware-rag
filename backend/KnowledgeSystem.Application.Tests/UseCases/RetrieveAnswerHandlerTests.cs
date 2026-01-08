using FluentValidation;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Application.UseCases.RetrieveAnswer;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;
using Moq;
using Xunit;

namespace KnowledgeSystem.Application.Tests.UseCases;

public class RetrieveAnswerHandlerTests
{
    private readonly Mock<IEmbeddingGenerator> _mockEmbeddingGenerator;
    private readonly Mock<IVectorSearchEngine> _mockVectorSearchEngine;
    private readonly Mock<ILanguageModel> _mockLanguageModel;
    private readonly IValidator<RetrieveAnswerQuery> _validator;
    private readonly ConfidencePolicy _policy;
    private readonly RetrieveAnswerHandler _handler;

    public RetrieveAnswerHandlerTests()
    {
        _mockEmbeddingGenerator = new Mock<IEmbeddingGenerator>();
        _mockVectorSearchEngine = new Mock<IVectorSearchEngine>();
        _mockLanguageModel = new Mock<ILanguageModel>();
        _validator = new RetrieveAnswerQueryValidator();
        _policy = ConfidencePolicy.Default; // 0.04 min, 0.06 low

        _handler = new RetrieveAnswerHandler(
            _mockEmbeddingGenerator.Object,
            _mockVectorSearchEngine.Object,
            _mockLanguageModel.Object,
            _validator,
            _policy);
    }

    [Fact]
    public async Task HandleAsync_WithLowConfidence_DoesNotInvokeLlm()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "Test question?" };
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        var documentId = DocumentId.New();
        var searchResults = new List<SectionSearchResult>
        {
            new()
            {
                Section = ContentSection.CreateGeneric(documentId, 0, "Low relevance content"),
                SimilarityScore = 0.02, // Below MinAcceptableThreshold (0.04)
                DocumentId = documentId,
                DocumentTitle = "Test Document"
            }
        };

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingAsync(query.Question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorSearchEngine
            .Setup(v => v.SearchAsync(queryEmbedding, query.TopK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConfidenceLevel.None, result.ConfidenceLevel);
        Assert.False(result.LlmInvoked);
        Assert.Contains("cannot answer", result.Answer, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(result.ConfidenceExplanation);

        // Verify LLM was NOT called
        _mockLanguageModel.Verify(
            l => l.GenerateAnswerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithHighConfidence_InvokesLlm()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "What is the answer?" };
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        var documentId = DocumentId.New();
        var section = ContentSection.CreateGeneric(documentId, 0, "Highly relevant content");
        section.SetEmbedding(new float[] { 0.15f, 0.25f, 0.35f });

        var searchResults = new List<SectionSearchResult>
        {
            new()
            {
                Section = section,
                SimilarityScore = 0.15, // Above LowConfidenceThreshold (0.06) → High confidence
                DocumentId = documentId,
                DocumentTitle = "Test Document"
            }
        };

        var llmAnswer = "This is the answer from LLM.";

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingAsync(query.Question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorSearchEngine
            .Setup(v => v.SearchAsync(queryEmbedding, query.TopK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLanguageModel
            .Setup(l => l.GenerateAnswerAsync(
                query.Question,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmAnswer);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConfidenceLevel.High, result.ConfidenceLevel);
        Assert.True(result.LlmInvoked);
        Assert.Equal(llmAnswer, result.Answer);
        Assert.Equal(1, result.RelevantSectionsCount);
        Assert.NotEmpty(result.ConfidenceExplanation);

        // Verify LLM was called once
        _mockLanguageModel.Verify(
            l => l.GenerateAnswerAsync(
                query.Question,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithMediumConfidence_InvokesLlmWithCautiousMode()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "What is this about?" };
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        var documentId = DocumentId.New();
        var section = ContentSection.CreateGeneric(documentId, 0, "Moderately relevant content");
        section.SetEmbedding(new float[] { 0.12f, 0.22f, 0.32f });

        var searchResults = new List<SectionSearchResult>
        {
            new()
            {
                Section = section,
                SimilarityScore = 0.05, // Between MinAcceptable (0.04) and Low (0.06) → Low confidence
                DocumentId = documentId,
                DocumentTitle = "Test Document"
            }
        };

        var llmAnswer = "Based on the available information...";
        string? capturedSystemPrompt = null;

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingAsync(query.Question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorSearchEngine
            .Setup(v => v.SearchAsync(queryEmbedding, query.TopK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLanguageModel
            .Setup(l => l.GenerateAnswerAsync(
                query.Question,
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, string, string?, CancellationToken>((q, ctx, prompt, ct) => 
            {
                capturedSystemPrompt = prompt;
            })
            .ReturnsAsync(llmAnswer);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConfidenceLevel.Low, result.ConfidenceLevel);
        Assert.True(result.LlmInvoked);
        Assert.Equal(llmAnswer, result.Answer);
        
        // Verify cautious mode system prompt was used (low confidence prompts with conditional language)
        Assert.NotNull(capturedSystemPrompt);
        Assert.Contains("conditional language", capturedSystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("partial or uncertain information", capturedSystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task HandleAsync_WithNoSearchResults_ReturnsLowConfidence()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "Unknown topic?" };
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var emptySearchResults = new List<SectionSearchResult>();

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingAsync(query.Question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorSearchEngine
            .Setup(v => v.SearchAsync(queryEmbedding, query.TopK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptySearchResults);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ConfidenceLevel.None, result.ConfidenceLevel);
        Assert.False(result.LlmInvoked);
        Assert.Equal(0, result.RelevantSectionsCount);
        Assert.NotEmpty(result.ConfidenceExplanation);

        // Verify LLM was NOT called
        _mockLanguageModel.Verify(
            l => l.GenerateAnswerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithEmptyQuestion_ThrowsValidationException()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "" };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_WithShortQuestion_ThrowsValidationException()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "Hi" }; // Less than 3 characters

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_WithInvalidTopK_ThrowsValidationException()
    {
        // Arrange
        var query = new RetrieveAnswerQuery 
        { 
            Question = "Valid question?",
            TopK = 0 // Invalid: must be >= 1
        };

        // Act & Assert
        await Assert.ThrowsAsync<ValidationException>(() => _handler.HandleAsync(query));
    }

    [Fact]
    public async Task HandleAsync_WithMultipleSections_UsesAllForContext()
    {
        // Arrange
        var query = new RetrieveAnswerQuery { Question = "Test question?" };
        var queryEmbedding = new float[] { 0.1f, 0.2f, 0.3f };

        var documentId = DocumentId.New();
        var section1 = ContentSection.CreateGeneric(documentId, 0, "Content 1");
        section1.SetEmbedding(new float[] { 0.1f, 0.2f, 0.3f });
        
        var section2 = ContentSection.CreateGeneric(documentId, 1, "Content 2");
        section2.SetEmbedding(new float[] { 0.2f, 0.3f, 0.4f });

        var searchResults = new List<SectionSearchResult>
        {
            new()
            {
                Section = section1,
                SimilarityScore = 0.10,
                DocumentId = documentId,
                DocumentTitle = "Test Document"
            },
            new()
            {
                Section = section2,
                SimilarityScore = 0.08,
                DocumentId = documentId,
                DocumentTitle = "Test Document"
            }
        };

        var llmAnswer = "Answer based on multiple sections.";

        _mockEmbeddingGenerator
            .Setup(e => e.GenerateEmbeddingAsync(query.Question, It.IsAny<CancellationToken>()))
            .ReturnsAsync(queryEmbedding);

        _mockVectorSearchEngine
            .Setup(v => v.SearchAsync(queryEmbedding, query.TopK, It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLanguageModel
            .Setup(l => l.GenerateAnswerAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(llmAnswer);

        // Act
        var result = await _handler.HandleAsync(query);

        // Assert
        Assert.Equal(2, result.RelevantSectionsCount);
        Assert.Equal(ConfidenceLevel.High, result.ConfidenceLevel);
        Assert.True(result.LlmInvoked);
        Assert.Equal(llmAnswer, result.Answer);
    }
}

