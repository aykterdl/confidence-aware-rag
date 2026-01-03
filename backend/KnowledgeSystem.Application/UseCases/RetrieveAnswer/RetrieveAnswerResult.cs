using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.UseCases.RetrieveAnswer;

/// <summary>
/// Result of answer retrieval
/// Contains answer text, confidence information, and metadata
/// </summary>
public sealed class RetrieveAnswerResult
{
    public required string Answer { get; init; }
    
    /// <summary>
    /// Confidence level (None, Low, High)
    /// </summary>
    public required ConfidenceLevel ConfidenceLevel { get; init; }
    
    /// <summary>
    /// Number of relevant sections found
    /// </summary>
    public required int RelevantSectionsCount { get; init; }
    
    /// <summary>
    /// Human-readable confidence explanation
    /// </summary>
    public required string ConfidenceExplanation { get; init; }
    
    /// <summary>
    /// Whether LLM was invoked to generate answer
    /// </summary>
    public required bool LlmInvoked { get; init; }
}

