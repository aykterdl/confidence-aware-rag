using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.Interfaces;

/// <summary>
/// Search result containing section, similarity score, and document metadata
/// </summary>
public sealed class SectionSearchResult
{
    public required ContentSection Section { get; init; }
    public required double SimilarityScore { get; init; } // 0-1 range (cosine similarity)
    public required DocumentId DocumentId { get; init; }
    
    /// <summary>
    /// Title of the source document (for API/UI display)
    /// </summary>
    public required string DocumentTitle { get; init; }
    
    /// <summary>
    /// Page numbers where this section appears (if available from metadata)
    /// </summary>
    public IReadOnlyList<int>? SourcePageNumbers { get; init; }
}

/// <summary>
/// Port: Service for vector similarity search
/// Implementation: Infrastructure layer (pgvector, Pinecone, etc.)
/// </summary>
public interface IVectorSearchEngine
{
    /// <summary>
    /// Search for similar content sections using vector similarity
    /// </summary>
    /// <param name="queryEmbedding">Query embedding vector</param>
    /// <param name="topK">Number of results to return</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Ordered list of search results (highest similarity first)</returns>
    Task<IReadOnlyList<SectionSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default);
}

