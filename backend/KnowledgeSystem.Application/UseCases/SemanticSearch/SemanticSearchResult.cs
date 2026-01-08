namespace KnowledgeSystem.Application.UseCases.SemanticSearch;

/// <summary>
/// Result of semantic search query containing matched document chunks.
/// </summary>
public sealed class SemanticSearchResult
{
    /// <summary>
    /// The original query string.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// List of matching chunks, ordered by relevance (similarity score descending).
    /// </summary>
    public required IReadOnlyList<ChunkMatch> Results { get; init; }

    /// <summary>
    /// Total number of chunks found (same as Results.Count).
    /// </summary>
    public required int TotalMatches { get; init; }
}

/// <summary>
/// Represents a single chunk that matched the query.
/// </summary>
public sealed class ChunkMatch
{
    /// <summary>
    /// Unique identifier for the content section (chunk).
    /// </summary>
    public required string ChunkId { get; init; }

    /// <summary>
    /// ID of the document this chunk belongs to.
    /// </summary>
    public required string DocumentId { get; init; }

    /// <summary>
    /// Title of the source document.
    /// </summary>
    public required string DocumentTitle { get; init; }

    /// <summary>
    /// The actual text content of the chunk.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Cosine similarity score (0.0 to 1.0, higher = more relevant).
    /// </summary>
    public required double SimilarityScore { get; init; }

    /// <summary>
    /// Page number(s) where this chunk appears (if available).
    /// </summary>
    public IReadOnlyList<int>? SourcePageNumbers { get; init; }

    /// <summary>
    /// Section type (e.g., Article, Paragraph, etc.).
    /// </summary>
    public string? SectionType { get; init; }

    /// <summary>
    /// Article number (if this is a legal article section).
    /// </summary>
    public string? ArticleNumber { get; init; }

    /// <summary>
    /// Article title (if this is a legal article section).
    /// </summary>
    public string? ArticleTitle { get; init; }
}

