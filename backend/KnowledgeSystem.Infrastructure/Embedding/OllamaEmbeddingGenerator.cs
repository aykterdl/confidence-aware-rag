using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using KnowledgeSystem.Application.Interfaces;

namespace KnowledgeSystem.Infrastructure.Embedding;

/// <summary>
/// Adapter implementing embedding generation using Ollama's embedding API
/// This is the ONLY place where Ollama implementation details exist
/// 
/// ARCHITECTURE NOTES:
/// - Implements IEmbeddingGenerator port from Application layer
/// - Uses HttpClient to call Ollama API
/// - Model name and base URL come from IConfiguration
/// - Uses System.Text.Json for serialization
/// - Handles HTTP failures and invalid responses
/// </summary>
public sealed class OllamaEmbeddingGenerator : IEmbeddingGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private const string EmbeddingsEndpoint = "/api/embeddings";

    public OllamaEmbeddingGenerator(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Read model name from configuration
        _modelName = configuration["Ollama:Embeddings:Model"] 
            ?? throw new InvalidOperationException(
                "Ollama embedding model name not found in configuration. " +
                "Please set 'Ollama:Embeddings:Model' in appsettings.json");
    }

    /// <summary>
    /// Generate embedding for a single text input
    /// </summary>
    public async Task<float[]> GenerateEmbeddingAsync(
        string text,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Input text cannot be null or empty", nameof(text));

        // Prepare request
        var request = new OllamaEmbeddingRequest
        {
            Model = _modelName,
            Prompt = text
        };

        // Call Ollama API
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                EmbeddingsEndpoint,
                request,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Ollama embedding service. " +
                $"Ensure Ollama is running at {_httpClient.BaseAddress}",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Ollama embedding request timed out. The model might be downloading or the service is overloaded.",
                ex);
        }

        // Ensure success
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Ollama embedding request failed with status {response.StatusCode}. " +
                $"Response: {errorContent}");
        }

        // Deserialize response
        OllamaEmbeddingResponse? embeddingResponse;
        try
        {
            embeddingResponse = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Failed to deserialize Ollama embedding response. The response format might have changed.",
                ex);
        }

        // Validate response
        if (embeddingResponse?.Embedding == null || embeddingResponse.Embedding.Length == 0)
        {
            throw new InvalidOperationException(
                "Ollama returned an empty or null embedding vector.");
        }

        return embeddingResponse.Embedding;
    }

    /// <summary>
    /// Generate embeddings for multiple texts in batch
    /// Note: Ollama doesn't have native batch API, so we process sequentially
    /// This is acceptable for now as it maintains simplicity
    /// </summary>
    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(
        IEnumerable<string> texts,
        CancellationToken cancellationToken = default)
    {
        if (texts == null)
            throw new ArgumentNullException(nameof(texts));

        var textList = texts.ToList();
        
        if (textList.Count == 0)
            throw new ArgumentException("Texts collection cannot be empty", nameof(texts));

        if (textList.Any(string.IsNullOrWhiteSpace))
            throw new ArgumentException("Texts collection cannot contain null or empty strings", nameof(texts));

        // Process sequentially (Ollama doesn't support batch embeddings natively)
        var embeddings = new List<float[]>(textList.Count);

        foreach (var text in textList)
        {
            var embedding = await GenerateEmbeddingAsync(text, cancellationToken);
            embeddings.Add(embedding);
        }

        return embeddings.AsReadOnly();
    }

    // ============================================================================
    // OLLAMA API CONTRACTS (Infrastructure concern - NOT exposed outside)
    // ============================================================================

    /// <summary>
    /// Ollama embedding API request model
    /// </summary>
    private sealed class OllamaEmbeddingRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }
    }

    /// <summary>
    /// Ollama embedding API response model
    /// </summary>
    private sealed class OllamaEmbeddingResponse
    {
        [JsonPropertyName("embedding")]
        public required float[] Embedding { get; init; }
    }
}

