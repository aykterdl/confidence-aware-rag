using KnowledgeSystem.Application.UseCases.SemanticSearch;

namespace KnowledgeSystem.Application.UseCases.Prompting;

/// <summary>
/// Command to compose a prompt from user query and retrieved document chunks.
/// This is the input for Phase 4 - Step 2: Prompt Composition.
/// </summary>
public sealed record ComposePromptCommand
{
    /// <summary>
    /// The original user question/query.
    /// </summary>
    public required string Query { get; init; }

    /// <summary>
    /// Retrieved document chunks from semantic search (Phase 4 - Step 1).
    /// These chunks will be used as context for the LLM.
    /// 
    /// IMPORTANT:
    /// - Chunks are already ranked by relevance (highest similarity first)
    /// - Do NOT reorder or filter by similarity in this step
    /// - Preserve original chunk content (no summarization or merging)
    /// </summary>
    public required IReadOnlyCollection<ChunkMatch> RetrievedChunks { get; init; }

    /// <summary>
    /// Optional: specify the language for instructions (e.g., "tr" for Turkish, "en" for English).
    /// If null, defaults to English.
    /// </summary>
    public string? Language { get; init; }
}

