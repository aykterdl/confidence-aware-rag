using KnowledgeSystem.Application.Interfaces;

namespace KnowledgeSystem.Application.UseCases.SemanticSearch;

/// <summary>
/// Handler for semantic search queries.
/// Phase 4 - Step 1: Query embedding + vector retrieval only (no LLM).
/// </summary>
public sealed class SemanticSearchHandler
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorSearchEngine _vectorSearchEngine;

    public SemanticSearchHandler(
        IEmbeddingGenerator embeddingGenerator,
        IVectorSearchEngine vectorSearchEngine)
    {
        _embeddingGenerator = embeddingGenerator;
        _vectorSearchEngine = vectorSearchEngine;
    }

    /// <summary>
    /// Execute semantic search: embed query, search vectors, return relevant chunks.
    /// </summary>
    public async Task<SemanticSearchResult> HandleAsync(
        SemanticSearchQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(query.Query))
        {
            return new SemanticSearchResult
            {
                Query = query.Query ?? string.Empty,
                Results = Array.Empty<ChunkMatch>(),
                TotalMatches = 0
            };
        }

        if (query.TopK <= 0)
        {
            throw new ArgumentException("TopK must be greater than 0", nameof(query));
        }

        // Step 1: Generate query embedding
        float[] queryEmbedding;
        
        try
        {
            queryEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(
                query.Query,
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to generate query embedding. Ensure embedding service is running.", ex);
        }

        // Step 2: Perform vector search
        IReadOnlyList<SectionSearchResult> searchResults;
        
        try
        {
            searchResults = await _vectorSearchEngine.SearchAsync(
                queryEmbedding,
                query.TopK,
                cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Vector search failed. Check database and vector index.", ex);
        }

        // Step 3: Map Domain results to Application DTOs
        var chunkMatches = searchResults.Select(result => new ChunkMatch
        {
            ChunkId = result.Section.Id.Value.ToString(),
            DocumentId = result.DocumentId.Value.ToString(),
            DocumentTitle = result.DocumentTitle,
            Content = result.Section.Content,
            SimilarityScore = result.SimilarityScore,
            SourcePageNumbers = result.SourcePageNumbers,
            SectionType = result.Section.Type.ToString(),
            ArticleNumber = result.Section.ArticleNumber,
            ArticleTitle = result.Section.ArticleTitle
        }).ToList();

        return new SemanticSearchResult
        {
            Query = query.Query,
            Results = chunkMatches,
            TotalMatches = chunkMatches.Count
        };
    }
}

