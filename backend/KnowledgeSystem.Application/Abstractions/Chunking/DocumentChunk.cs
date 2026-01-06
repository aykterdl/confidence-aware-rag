namespace KnowledgeSystem.Application.Abstractions.Chunking;

/// <summary>
/// Represents a single chunk of document content
/// Produced by IChunkingStrategy implementations
/// </summary>
public sealed class DocumentChunk
{
    /// <summary>
    /// Zero-based index of this chunk within the document
    /// </summary>
    public required int Index { get; init; }

    /// <summary>
    /// Chunk text content (should be semantically meaningful standalone text)
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// Character count of the chunk
    /// </summary>
    public int CharacterCount => Content.Length;

    /// <summary>
    /// Source page range (if available from extraction)
    /// Format: "1-3" or "5" or null if not applicable
    /// </summary>
    public string? SourcePageRange { get; init; }

    /// <summary>
    /// Optional heading or section title associated with this chunk
    /// Used for semantic context
    /// </summary>
    public string? Heading { get; init; }

    /// <summary>
    /// Additional metadata for this chunk
    /// May include: chunk_type (paragraph, heading, list), language, etc.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }
}

