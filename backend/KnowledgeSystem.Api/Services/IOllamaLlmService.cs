namespace KnowledgeSystem.Api.Services;

/// <summary>
/// Ollama LLM ile text generation için servis interface
/// </summary>
public interface IOllamaLlmService
{
    /// <summary>
    /// Verilen prompt için LLM cevabı üretir
    /// </summary>
    /// <param name="prompt">LLM'e gönderilecek tam prompt</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>LLM'in ürettiği cevap</returns>
    Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default);
}


