namespace KnowledgeSystem.Application.UseCases.SemanticSearch;

/// <summary>
/// Query for performing semantic vector search against stored document chunks.
/// This is Phase 4 - Step 1: retrieval only, no LLM invocation.
/// </summary>
public sealed class SemanticSearchQuery
{
    /// <summary>
    /// The natural language query from the user.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Maximum number of chunks to return (default: 5).
    /// </summary>
    public int TopK { get; init; } = 5;

    /// <summary>
    /// Optional filter: only search within a specific document.
    /// If null, searches across all documents.
    /// </summary>
    public string? DocumentId { get; init; }
}

