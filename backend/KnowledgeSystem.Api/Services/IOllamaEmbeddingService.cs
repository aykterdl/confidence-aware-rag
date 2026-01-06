namespace KnowledgeSystem.Api.Services;

public interface IOllamaEmbeddingService
{
    /// <summary>
    /// Metni Ollama embedding modeli ile vektöre dönüştürür
    /// </summary>
    /// <param name="text">Embedding oluşturulacak metin</param>
    /// <param name="cancellationToken">İptal token</param>
    /// <returns>768 boyutlu float array</returns>
    Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);
}





