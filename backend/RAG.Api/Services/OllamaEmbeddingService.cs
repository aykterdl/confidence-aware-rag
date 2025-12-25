using Microsoft.Extensions.Options;
using RAG.Api.Configuration;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace RAG.Api.Services;

public class OllamaEmbeddingService : IOllamaEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OllamaSettings _settings;
    private readonly ILogger<OllamaEmbeddingService> _logger;

    public OllamaEmbeddingService(
        HttpClient httpClient,
        IOptions<OllamaSettings> settings,
        ILogger<OllamaEmbeddingService> logger)
    {
        _httpClient = httpClient;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        try
        {
            _logger.LogInformation("Generating embedding for text (length: {Length})", text.Length);

            var request = new OllamaEmbeddingRequest
            {
                Model = _settings.EmbeddingModel,
                Prompt = text
            };

            var response = await _httpClient.PostAsJsonAsync(
                "/api/embeddings",
                request,
                cancellationToken
            );

            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                cancellationToken: cancellationToken
            );

            if (result?.Embedding == null || result.Embedding.Length == 0)
            {
                throw new InvalidOperationException("Ollama returned empty embedding");
            }

            _logger.LogInformation("Successfully generated embedding with {Dimensions} dimensions", 
                result.Embedding.Length);

            return result.Embedding;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP error while calling Ollama API");
            throw new InvalidOperationException("Failed to connect to Ollama service", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating embedding");
            throw;
        }
    }

    // Request/Response DTOs
    private class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("prompt")]
        public string Prompt { get; set; } = string.Empty;
    }

    private class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}





