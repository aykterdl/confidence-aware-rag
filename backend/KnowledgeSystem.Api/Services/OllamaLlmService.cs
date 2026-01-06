using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using KnowledgeSystem.Api.Configuration;

namespace KnowledgeSystem.Api.Services;

/// <summary>
/// Ollama LLM ile text generation servisi
/// </summary>
public class OllamaLlmService : IOllamaLlmService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OllamaLlmService> _logger;
    private readonly string _modelName;

    public OllamaLlmService(
        IHttpClientFactory httpClientFactory,
        IOptions<OllamaSettings> options,
        ILogger<OllamaLlmService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(IOllamaLlmService));
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5); // LLM generation uzun sürebilir
        _logger = logger;
        _modelName = options.Value.LlmModel;
    }

    public async Task<string> GenerateAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(prompt))
        {
            throw new ArgumentException("Prompt cannot be empty", nameof(prompt));
        }

        _logger.LogInformation("Generating LLM response with model: {Model}", _modelName);
        _logger.LogDebug("Prompt length: {Length} characters", prompt.Length);

        try
        {
            var requestBody = new
            {
                model = _modelName,
                prompt = prompt,
                stream = false, // Non-streaming response
                options = new
                {
                    temperature = 0.7,
                    top_p = 0.9,
                    top_k = 40
                }
            };

            var json = JsonSerializer.Serialize(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            _logger.LogDebug("Sending request to Ollama API: /api/generate");
            var response = await _httpClient.PostAsync("/api/generate", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("Ollama API error: {StatusCode} - {Error}", 
                    response.StatusCode, error);
                throw new HttpRequestException($"Ollama API failed: {response.StatusCode}");
            }

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogDebug("Received response from Ollama, parsing...");

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("response", out var responseText))
            {
                _logger.LogError("Ollama response missing 'response' field");
                throw new InvalidOperationException("Invalid Ollama API response");
            }

            var generatedText = responseText.GetString() ?? string.Empty;
            _logger.LogInformation("✅ LLM response generated: {Length} characters", generatedText.Length);

            return generatedText.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Failed to generate LLM response");
            throw new InvalidOperationException("LLM generation failed", ex);
        }
    }
}

