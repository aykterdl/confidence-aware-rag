namespace KnowledgeSystem.Application.Interfaces;

/// <summary>
/// Port: Service for generating embeddings from text
/// Implementation: Infrastructure layer (Ollama, OpenAI, etc.)
/// </summary>
public interface IEmbeddingGenerator
{
    /// <summary>
    /// Generate embedding vector for a given text
    /// </summary>
    /// <param name="text">Input text</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Embedding vector (float array)</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// </summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default);
}

