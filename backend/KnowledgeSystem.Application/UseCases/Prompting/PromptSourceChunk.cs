namespace KnowledgeSystem.Application.UseCases.Prompting;

/// <summary>
/// Represents a single document chunk used as context in the prompt.
/// Contains metadata for traceability and debugging.
/// </summary>
public sealed class PromptSourceChunk
{
    /// <summary>
    /// Unique identifier of the content section (chunk).
    /// </summary>
    public required Guid ChunkId { get; init; }

    /// <summary>
    /// Unique identifier of the source document.
    /// </summary>
    public required Guid DocumentId { get; init; }

    /// <summary>
    /// Title of the source document (for citation and debugging).
    /// </summary>
    public required string DocumentTitle { get; init; }

    /// <summary>
    /// The actual text content of the chunk (will be included in LLM context).
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Cosine similarity score (0.0 to 1.0, higher = more relevant).
    /// Used for metadata/logging only - NOT visible to LLM.
    /// </summary>
    public required double SimilarityScore { get; init; }

    /// <summary>
    /// Section type (e.g., "Article", "Paragraph", "Generic").
    /// Optional metadata for richer context.
    /// </summary>
    public string? SectionType { get; init; }

    /// <summary>
    /// Article number (if this chunk is a legal article).
    /// Optional metadata.
    /// </summary>
    public string? ArticleNumber { get; init; }

    /// <summary>
    /// Article title (if this chunk is a legal article).
    /// Optional metadata.
    /// </summary>
    public string? ArticleTitle { get; init; }
}

