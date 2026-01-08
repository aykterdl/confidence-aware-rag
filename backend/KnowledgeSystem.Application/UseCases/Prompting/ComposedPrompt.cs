namespace KnowledgeSystem.Application.UseCases.Prompting;

/// <summary>
/// Represents a fully composed prompt ready to be sent to an LLM.
/// This is the output of Phase 4 - Step 2: Prompt Composition.
/// 
/// ARCHITECTURE NOTE:
/// - This class is in the Application layer and has NO knowledge of LLM APIs
/// - Infrastructure layer (OllamaLanguageModel, etc.) will consume this prompt
/// - Prompt is deterministic: same inputs → same prompt
/// - Prompt is serializable and loggable for debugging
/// </summary>
public sealed class ComposedPrompt
{
    /// <summary>
    /// System-level instructions for the LLM.
    /// Defines role, behavior, and guardrails (e.g., "Use provided sources only").
    /// </summary>
    public required string SystemPrompt { get; init; }

    /// <summary>
    /// User-facing prompt containing:
    /// - The original user question
    /// - Retrieved document chunks as context
    /// - Task instructions
    /// </summary>
    public required string UserPrompt { get; init; }

    /// <summary>
    /// Metadata: all source chunks used in the prompt.
    /// Used for:
    /// - Traceability (which documents were used)
    /// - Debugging (similarity scores, chunk IDs)
    /// - Citation generation (if needed later)
    /// 
    /// NOT included in the actual LLM prompt text.
    /// </summary>
    public required IReadOnlyCollection<PromptSourceChunk> Sources { get; init; }

    /// <summary>
    /// The original user query (for reference and logging).
    /// </summary>
    public required string OriginalQuery { get; init; }

    /// <summary>
    /// Number of source chunks included in the prompt.
    /// </summary>
    public int SourceCount => Sources.Count;

    /// <summary>
    /// Total character count of the user prompt (for token estimation).
    /// </summary>
    public int UserPromptLength => UserPrompt.Length;

    /// <summary>
    /// Estimated total token count (rough approximation: chars / 4).
    /// Useful for checking context window limits.
    /// </summary>
    public int EstimatedTokenCount => (SystemPrompt.Length + UserPrompt.Length) / 4;
}

