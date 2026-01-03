namespace KnowledgeSystem.Application.Interfaces;

/// <summary>
/// Port: Service for language model interaction (answer generation)
/// Implementation: Infrastructure layer (Ollama, OpenAI, etc.)
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Generate an answer based on context and question
    /// </summary>
    /// <param name="question">User question</param>
    /// <param name="context">Relevant context from documents</param>
    /// <param name="systemPrompt">System instructions (optional)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Generated answer text</returns>
    Task<string> GenerateAnswerAsync(
        string question,
        string context,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}

