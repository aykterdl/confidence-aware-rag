namespace KnowledgeSystem.Application.Interfaces;

/// <summary>
/// Port: Service for language model interaction (answer generation).
/// Implementation: Infrastructure layer (Ollama, OpenAI, etc.).
/// 
/// ARCHITECTURE NOTE:
/// - This interface is framework-agnostic
/// - No knowledge of Ollama, OpenAI, or HTTP APIs
/// - Infrastructure layer implements the actual LLM communication
/// </summary>
public interface ILanguageModel
{
    /// <summary>
    /// Generate text using a language model with system and user prompts.
    /// This is the standard LLM invocation pattern (e.g., ChatGPT, Claude, Llama).
    /// </summary>
    /// <param name="systemPrompt">System-level instructions defining LLM behavior and constraints.</param>
    /// <param name="userPrompt">User-facing prompt containing query, context, and task instructions.</param>
    /// <param name="cancellationToken">Cancellation token for request timeout.</param>
    /// <returns>Generated text response from the LLM.</returns>
    Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default);
}

