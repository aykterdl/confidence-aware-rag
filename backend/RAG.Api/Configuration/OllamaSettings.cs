namespace RAG.Api.Configuration;

public class OllamaSettings
{
    public const string SectionName = "Ollama";
    
    public string BaseUrl { get; set; } = "http://ollama:11434";
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string LlmModel { get; set; } = "llama3.2:1b";
    public int TimeoutSeconds { get; set; } = 600; // 10 dakika - büyük PDF'ler için
}




