using KnowledgeSystem.Application.UseCases.Prompting;

namespace KnowledgeSystem.Application.UseCases.GenerateAnswer;

/// <summary>
/// Command to generate an LLM-based answer from a composed prompt.
/// This is the input for Phase 4 - Step 3: LLM Answer Generation.
/// </summary>
public sealed record GenerateAnswerCommand
{
    /// <summary>
    /// The fully composed prompt ready to be sent to an LLM.
    /// This comes from Phase 4 - Step 2 (Prompt Composition).
    /// </summary>
    public required ComposedPrompt Prompt { get; init; }

    /// <summary>
    /// Optional: maximum number of tokens to generate.
    /// If null, uses LLM default or configured value.
    /// </summary>
    public int? MaxTokens { get; init; }

    /// <summary>
    /// Optional: temperature for LLM generation (0.0 = deterministic, 1.0 = creative).
    /// For Balanced strategy, prefer low values (0.1-0.3).
    /// If null, uses configured default.
    /// </summary>
    public double? Temperature { get; init; }
}

