using KnowledgeSystem.Application.UseCases.Prompting;
using KnowledgeSystem.Application.UseCases.SemanticSearch;

namespace KnowledgeSystem.Application.Tests.UseCases.Prompting;

/// <summary>
/// Unit tests for ComposePromptHandler (Phase 4 - Step 2).
/// 
/// These tests verify that:
/// - Prompt composition is deterministic
/// - System and user prompts are properly structured
/// - Edge cases (no chunks) are handled gracefully
/// - Chunk content is preserved without alteration
/// - Turkish and English language support works correctly
/// </summary>
public sealed class ComposePromptHandlerTests
{
    private readonly ComposePromptHandler _handler;

    public ComposePromptHandlerTests()
    {
        _handler = new ComposePromptHandler();
    }

    // ============================================================================
    // BASIC FUNCTIONALITY TESTS
    // ============================================================================

    [Fact]
    public void Handle_WithValidCommand_ProducesComposedPrompt()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "What is democracy?",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Democracy is a form of government...", "Turkish Constitution", 0.85),
                CreateChunkMatch("Citizens have the right to vote...", "Turkish Constitution", 0.72)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SystemPrompt);
        Assert.NotEmpty(result.UserPrompt);
        Assert.Equal(2, result.Sources.Count);
        Assert.Equal("What is democracy?", result.OriginalQuery);
    }

    [Fact]
    public void Handle_ProducedPrompt_ContainsSystemPrompt()
    {
        // Arrange
        var command = CreateBasicCommand("Test question");

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains("assistant", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("documents", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("sources", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handle_ProducedPrompt_ContainsUserQuery()
    {
        // Arrange
        var query = "What is the definition of democracy?";
        var command = CreateBasicCommand(query);

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains(query, result.UserPrompt);
        Assert.Contains("USER QUESTION:", result.UserPrompt);
    }

    [Fact]
    public void Handle_ProducedPrompt_ContainsAllRetrievedChunks()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Chunk 1 content about democracy", "Doc1", 0.9),
                CreateChunkMatch("Chunk 2 content about voting", "Doc2", 0.8),
                CreateChunkMatch("Chunk 3 content about elections", "Doc3", 0.7)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains("Chunk 1 content about democracy", result.UserPrompt);
        Assert.Contains("Chunk 2 content about voting", result.UserPrompt);
        Assert.Contains("Chunk 3 content about elections", result.UserPrompt);
        Assert.Contains("CONTEXT", result.UserPrompt);
    }

    [Fact]
    public void Handle_ProducedPrompt_PreservesChunkContentWithoutAlteration()
    {
        // Arrange
        var originalContent = "This is the exact content with special characters: @#$%^&*()";
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                CreateChunkMatch(originalContent, "Test Doc", 0.9)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert - Original content must appear unchanged
        Assert.Contains(originalContent, result.UserPrompt);
        Assert.Single(result.Sources);
        Assert.Equal(originalContent, result.Sources.First().Content);
    }

    // ============================================================================
    // EDGE CASE: NO RETRIEVED CHUNKS
    // ============================================================================

    [Fact]
    public void Handle_WithNoChunks_ProducesPromptWithEmptyContext()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "What is democracy?",
            RetrievedChunks = Array.Empty<ChunkMatch>()
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SystemPrompt);
        Assert.NotEmpty(result.UserPrompt);
        Assert.Empty(result.Sources);
        Assert.Contains("No relevant documents found", result.UserPrompt);
    }

    [Fact]
    public void Handle_WithNoChunks_SystemPromptAdvisesLackOfInformation()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = Array.Empty<ChunkMatch>()
        };

        // Act
        var result = _handler.Handle(command);

        // Assert - System prompt should instruct LLM about lack of documents
        Assert.Contains("No relevant source documents", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================================
    // DETERMINISM TEST
    // ============================================================================

    [Fact]
    public void Handle_WithSameInput_ProducesDeterministicOutput()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "What is democracy?",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Democracy is government by the people", "Doc1", 0.85)
            }
        };

        // Act - Call handler twice with identical input
        var result1 = _handler.Handle(command);
        var result2 = _handler.Handle(command);

        // Assert - Outputs must be identical
        Assert.Equal(result1.SystemPrompt, result2.SystemPrompt);
        Assert.Equal(result1.UserPrompt, result2.UserPrompt);
        Assert.Equal(result1.OriginalQuery, result2.OriginalQuery);
        Assert.Equal(result1.Sources.Count, result2.Sources.Count);
    }

    // ============================================================================
    // LANGUAGE SUPPORT TESTS
    // ============================================================================

    [Fact]
    public void Handle_WithTurkishLanguage_ProducesTurkishPrompt()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Demokrasi nedir?",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Demokrasi halk yönetimidir", "Anayasa", 0.9)
            },
            Language = "tr"
        };

        // Act
        var result = _handler.Handle(command);

        // Assert - Prompts should contain Turkish keywords
        Assert.Contains("KULLANICI SORUSU:", result.UserPrompt);
        Assert.Contains("BAĞLAM", result.UserPrompt);
        Assert.Contains("TALİMATLAR:", result.UserPrompt);
        Assert.Contains("asistan", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Handle_WithoutLanguage_ProducesEnglishPrompt()
    {
        // Arrange
        var command = CreateBasicCommand("What is democracy?");

        // Act
        var result = _handler.Handle(command);

        // Assert - Default to English
        Assert.Contains("USER QUESTION:", result.UserPrompt);
        Assert.Contains("CONTEXT", result.UserPrompt);
        Assert.Contains("INSTRUCTIONS:", result.UserPrompt);
    }

    [Fact]
    public void Handle_WithTurkishLanguage_AndNoChunks_ProducesTurkishNoContextPrompt()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test sorusu",
            RetrievedChunks = Array.Empty<ChunkMatch>(),
            Language = "tr"
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains("doküman bulunamadı", result.UserPrompt, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("yeterli kaynak doküman bulunamadı", result.SystemPrompt, StringComparison.OrdinalIgnoreCase);
    }

    // ============================================================================
    // METADATA TESTS
    // ============================================================================

    [Fact]
    public void Handle_ProducedPrompt_IncludesDocumentTitles()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Content 1", "Turkish Constitution", 0.9),
                CreateChunkMatch("Content 2", "Civil Law", 0.8)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains("Turkish Constitution", result.UserPrompt);
        Assert.Contains("Civil Law", result.UserPrompt);
    }

    [Fact]
    public void Handle_WithArticleMetadata_IncludesArticleNumberAndTitle()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                new ChunkMatch
                {
                    ChunkId = Guid.NewGuid().ToString(),
                    DocumentId = Guid.NewGuid().ToString(),
                    DocumentTitle = "Turkish Constitution",
                    Content = "The Republic of Turkey is a democratic state...",
                    SimilarityScore = 0.92,
                    SectionType = "Article",
                    ArticleNumber = "2",
                    ArticleTitle = "Characteristics of the Republic"
                }
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Contains("Article 2", result.UserPrompt);
        Assert.Contains("Characteristics of the Republic", result.UserPrompt);
    }

    [Fact]
    public void Handle_Sources_ContainSimilarityScores()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Content", "Doc", 0.87)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert - Similarity score should be in metadata (not in prompt text)
        var source = result.Sources.First();
        Assert.Equal(0.87, source.SimilarityScore);
        // But NOT in the user prompt itself (per requirements)
        Assert.DoesNotContain("0.87", result.UserPrompt);
    }

    [Fact]
    public void Handle_ComposedPrompt_HasCorrectSourceCount()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "Test query",
            RetrievedChunks = new[]
            {
                CreateChunkMatch("C1", "D1", 0.9),
                CreateChunkMatch("C2", "D2", 0.8),
                CreateChunkMatch("C3", "D3", 0.7)
            }
        };

        // Act
        var result = _handler.Handle(command);

        // Assert
        Assert.Equal(3, result.SourceCount);
        Assert.Equal(3, result.Sources.Count);
    }

    // ============================================================================
    // VALIDATION TESTS
    // ============================================================================

    [Fact]
    public void Handle_WithNullCommand_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => _handler.Handle(null!));
    }

    [Fact]
    public void Handle_WithEmptyQuery_ThrowsArgumentException()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "",
            RetrievedChunks = Array.Empty<ChunkMatch>()
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(command));
    }

    [Fact]
    public void Handle_WithWhitespaceQuery_ThrowsArgumentException()
    {
        // Arrange
        var command = new ComposePromptCommand
        {
            Query = "   ",
            RetrievedChunks = Array.Empty<ChunkMatch>()
        };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _handler.Handle(command));
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    private static ComposePromptCommand CreateBasicCommand(string query)
    {
        return new ComposePromptCommand
        {
            Query = query,
            RetrievedChunks = new[]
            {
                CreateChunkMatch("Sample content", "Sample Document", 0.8)
            }
        };
    }

    private static ChunkMatch CreateChunkMatch(string content, string documentTitle, double similarityScore)
    {
        return new ChunkMatch
        {
            ChunkId = Guid.NewGuid().ToString(),
            DocumentId = Guid.NewGuid().ToString(),
            DocumentTitle = documentTitle,
            Content = content,
            SimilarityScore = similarityScore,
            SectionType = "Paragraph",
            ArticleNumber = null,
            ArticleTitle = null,
            SourcePageNumbers = null
        };
    }
}

