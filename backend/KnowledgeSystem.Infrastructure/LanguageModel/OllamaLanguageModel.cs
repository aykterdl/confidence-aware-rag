using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using KnowledgeSystem.Application.Interfaces;

namespace KnowledgeSystem.Infrastructure.LanguageModel;

/// <summary>
/// Adapter implementing answer generation using Ollama's language model API
/// This is the ONLY place where Ollama LLM implementation details exist
/// 
/// ARCHITECTURE NOTES:
/// - Implements ILanguageModel port from Application layer
/// - Uses HttpClient to call Ollama /api/generate endpoint
/// - Model name comes from IConfiguration
/// - Combines system prompt, context, and question into final prompt
/// - Uses System.Text.Json for serialization
/// - Handles HTTP failures and invalid responses
/// 
/// DESIGN DECISIONS:
/// - Single-turn generation (no conversation history)
/// - No streaming (simpler implementation)
/// - No retries (fail fast)
/// - System prompt strategy lives in Application layer
/// </summary>
public sealed class OllamaLanguageModel : ILanguageModel
{
    private readonly HttpClient _httpClient;
    private readonly string _modelName;
    private const string GenerateEndpoint = "/api/generate";

    public OllamaLanguageModel(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Read model name from configuration (REQUIRED)
        _modelName = configuration["Ollama:LanguageModel:Model"] 
            ?? throw new InvalidOperationException(
                "Ollama language model name not found in configuration. " +
                "Please set 'Ollama:LanguageModel:Model' in appsettings.json");
    }

    /// <summary>
    /// Generate an answer based on context and question
    /// Combines system prompt, context, and question into a single prompt
    /// </summary>
    public async Task<string> GenerateAnswerAsync(
        string question,
        string context,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(question))
            throw new ArgumentException("Question cannot be null or empty", nameof(question));

        if (string.IsNullOrWhiteSpace(context))
            throw new ArgumentException("Context cannot be null or empty", nameof(context));

        // Build final prompt (system instructions + context + question)
        var finalPrompt = BuildPrompt(systemPrompt, context, question);

        // Prepare Ollama API request
        var request = new OllamaGenerateRequest
        {
            Model = _modelName,
            Prompt = finalPrompt,
            Stream = false // Single response (no streaming)
        };

        // Call Ollama API
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.PostAsJsonAsync(
                GenerateEndpoint,
                request,
                cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"Failed to connect to Ollama language model service. " +
                $"Ensure Ollama is running at {_httpClient.BaseAddress}",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TimeoutException(
                "Ollama language model request timed out. " +
                "The model might be loading, or the response is taking longer than expected. " +
                "Consider increasing 'Ollama:TimeoutSeconds' in configuration.",
                ex);
        }

        // Ensure success
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException(
                $"Ollama language model request failed with status {response.StatusCode}. " +
                $"Response: {errorContent}");
        }

        // Deserialize response
        OllamaGenerateResponse? generateResponse;
        try
        {
            generateResponse = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                cancellationToken: cancellationToken);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                "Failed to deserialize Ollama language model response. " +
                "The response format might have changed.",
                ex);
        }

        // Validate response
        if (string.IsNullOrWhiteSpace(generateResponse?.Response))
        {
            throw new InvalidOperationException(
                "Ollama returned an empty or null response. " +
                "The model might not have generated any output.");
        }

        return generateResponse.Response.Trim();
    }

    // ============================================================================
    // PROMPT BUILDING (Infrastructure concern)
    // ============================================================================

    /// <summary>
    /// Combine system prompt, context, and question into final prompt
    /// This is a simple template - the semantic strategy lives in Application layer
    /// </summary>
    private static string BuildPrompt(string? systemPrompt, string context, string question)
    {
        var promptBuilder = new System.Text.StringBuilder();

        // Add system instructions if provided
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            promptBuilder.AppendLine(systemPrompt);
            promptBuilder.AppendLine();
        }

        // Add context
        promptBuilder.AppendLine("Context:");
        promptBuilder.AppendLine(context);
        promptBuilder.AppendLine();

        // Add question
        promptBuilder.AppendLine("Question:");
        promptBuilder.AppendLine(question);
        promptBuilder.AppendLine();

        // Explicit instruction to answer
        promptBuilder.AppendLine("Answer:");

        return promptBuilder.ToString();
    }

    // ============================================================================
    // OLLAMA API CONTRACTS (Infrastructure concern - NOT exposed outside)
    // ============================================================================

    /// <summary>
    /// Ollama /api/generate request model
    /// </summary>
    private sealed class OllamaGenerateRequest
    {
        [JsonPropertyName("model")]
        public required string Model { get; init; }

        [JsonPropertyName("prompt")]
        public required string Prompt { get; init; }

        [JsonPropertyName("stream")]
        public required bool Stream { get; init; }
    }

    /// <summary>
    /// Ollama /api/generate response model (non-streaming)
    /// </summary>
    private sealed class OllamaGenerateResponse
    {
        [JsonPropertyName("response")]
        public required string Response { get; init; }
    }
}

