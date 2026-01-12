using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
    private readonly ILogger<OllamaLanguageModel> _logger;
    private const string GenerateEndpoint = "/api/generate";
    
    // Safety parameters (grounded, deterministic generation)
    private const double Temperature = 0.3; // Low temperature for factual answers
    private const int MaxTokens = 2048; // Reasonable limit for answer length

    public OllamaLanguageModel(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<OllamaLanguageModel> logger)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        if (configuration == null)
            throw new ArgumentNullException(nameof(configuration));

        // Read model name from configuration (REQUIRED)
        _modelName = configuration["Ollama:LanguageModel:Model"] 
            ?? throw new InvalidOperationException(
                "Ollama language model name not found in configuration. " +
                "Please set 'Ollama:LanguageModel:Model' in appsettings.json");
                
        _logger.LogInformation(
            "OllamaLanguageModel initialized: Model={ModelName}, Temperature={Temperature}, MaxTokens={MaxTokens}",
            _modelName, Temperature, MaxTokens);
    }

    /// <summary>
    /// Generate text using Ollama language model with system and user prompts.
    /// This matches the standard LLM invocation pattern (ChatGPT, Claude, Llama).
    /// </summary>
    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (string.IsNullOrWhiteSpace(systemPrompt))
            throw new ArgumentException("System prompt cannot be null or empty", nameof(systemPrompt));

        if (string.IsNullOrWhiteSpace(userPrompt))
            throw new ArgumentException("User prompt cannot be null or empty", nameof(userPrompt));

        // Build final prompt (combine system + user prompts for Ollama)
        // Note: Ollama doesn't have separate system/user roles, so we combine them
        var finalPrompt = BuildPrompt(systemPrompt, userPrompt);

        // Prepare Ollama API request
        var request = new OllamaGenerateRequest
        {
            Model = _modelName,
            Prompt = finalPrompt,
            Stream = false, // Single response (no streaming)
            Temperature = Temperature, // Grounded, deterministic
            NumPredict = MaxTokens // Limit response length
        };

        _logger.LogDebug(
            "Sending LLM generation request: Model={Model}, PromptLength={PromptLength}, Temperature={Temperature}",
            _modelName, finalPrompt.Length, Temperature);

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
            _logger.LogError(ex,
                "Failed to connect to Ollama language model service at {BaseAddress}",
                _httpClient.BaseAddress);
            throw new InvalidOperationException(
                $"Failed to connect to Ollama language model service. " +
                $"Ensure Ollama is running at {_httpClient.BaseAddress}",
                ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(ex,
                "Ollama language model request timed out after {Timeout}s",
                _httpClient.Timeout.TotalSeconds);
            throw new TimeoutException(
                "Ollama language model request timed out. " +
                "The model might be loading, or the response is taking longer than expected. " +
                "Consider increasing 'Ollama:LanguageModel:TimeoutSeconds' in configuration.",
                ex);
        }

        // Ensure success
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError(
                "Ollama language model request failed: StatusCode={StatusCode}, Response={ErrorContent}",
                response.StatusCode, errorContent);
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
            _logger.LogError(ex, "Failed to deserialize Ollama language model response");
            throw new InvalidOperationException(
                "Failed to deserialize Ollama language model response. " +
                "The response format might have changed.",
                ex);
        }

        // Validate response
        if (string.IsNullOrWhiteSpace(generateResponse?.Response))
        {
            _logger.LogWarning("Ollama returned an empty or null response");
            throw new InvalidOperationException(
                "Ollama returned an empty or null response. " +
                "The model might not have generated any output.");
        }

        var trimmedResponse = generateResponse.Response.Trim();
        _logger.LogInformation(
            "LLM generation completed successfully: ResponseLength={Length} characters",
            trimmedResponse.Length);
        
        return trimmedResponse;
    }

    // ============================================================================
    // PROMPT BUILDING (Infrastructure concern)
    // ============================================================================

    /// <summary>
    /// Combine system prompt and user prompt into final prompt for Ollama.
    /// 
    /// NOTE: Ollama's /api/generate endpoint doesn't have separate system/user role support
    /// like ChatGPT. We concatenate them into a single prompt.
    /// 
    /// The actual prompt structure (with context, instructions, etc.) is defined
    /// in the Application layer (ComposePromptHandler).
    /// </summary>
    private static string BuildPrompt(string systemPrompt, string userPrompt)
    {
        var promptBuilder = new System.Text.StringBuilder();

        // System instructions (behavior, guardrails, strategy)
        promptBuilder.AppendLine(systemPrompt);
        promptBuilder.AppendLine();
        promptBuilder.AppendLine("---");
        promptBuilder.AppendLine();

        // User prompt (query + context + task)
        promptBuilder.AppendLine(userPrompt);

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
        
        [JsonPropertyName("temperature")]
        public required double Temperature { get; init; }
        
        [JsonPropertyName("num_predict")]
        public required int NumPredict { get; init; }
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

