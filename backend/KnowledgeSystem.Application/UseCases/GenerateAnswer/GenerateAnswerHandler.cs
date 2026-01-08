using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.UseCases.GenerateAnswer;

/// <summary>
/// Handler for generating LLM-based answers from composed prompts.
/// Phase 4 - Step 3: LLM Answer Generation with Balanced (B) strategy.
/// 
/// RESPONSIBILITIES:
/// - Validate input (composed prompt must be valid)
/// - Calculate confidence based on source similarity scores
/// - Invoke LLM if confidence is acceptable
/// - Return structured result with answer, sources, and confidence
/// 
/// ARCHITECTURE:
/// - Application layer: orchestration logic
/// - Domain layer: confidence calculation
/// - Infrastructure layer: actual LLM communication
/// </summary>
public sealed class GenerateAnswerHandler
{
    private readonly ILanguageModel _languageModel;
    private readonly ConfidencePolicy _policy;

    public GenerateAnswerHandler(
        ILanguageModel languageModel,
        ConfidencePolicy? policy = null)
    {
        _languageModel = languageModel ?? throw new ArgumentNullException(nameof(languageModel));
        _policy = policy ?? ConfidencePolicy.Default;
    }

    /// <summary>
    /// Generate an answer from a composed prompt.
    /// </summary>
    public async Task<GeneratedAnswerResult> HandleAsync(
        GenerateAnswerCommand command,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (command.Prompt == null)
            throw new ArgumentException("Prompt cannot be null", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Prompt.SystemPrompt))
            throw new ArgumentException("System prompt cannot be empty", nameof(command));

        if (string.IsNullOrWhiteSpace(command.Prompt.UserPrompt))
            throw new ArgumentException("User prompt cannot be empty", nameof(command));

        var prompt = command.Prompt;

        // Step 1: Calculate confidence from source similarity scores
        var confidenceScore = CalculateConfidenceScore(prompt.Sources);

        // Step 2: Determine if LLM should be invoked based on confidence
        if (!confidenceScore.IsAcceptable())
        {
            // Confidence too low - return without LLM invocation
            return new GeneratedAnswerResult
            {
                Answer = GenerateNoConfidenceMessage(prompt.OriginalQuery, confidenceScore),
                Sources = prompt.Sources.ToList(),
                ConfidenceLevel = confidenceScore.Level,
                ConfidenceExplanation = confidenceScore.GetExplanation(),
                OriginalQuery = prompt.OriginalQuery,
                LlmInvoked = false
            };
        }

        // Step 3: Invoke LLM to generate answer
        string generatedAnswer;
        try
        {
            generatedAnswer = await _languageModel.GenerateAsync(
                prompt.SystemPrompt,
                prompt.UserPrompt,
                cancellationToken);

            if (string.IsNullOrWhiteSpace(generatedAnswer))
            {
                throw new InvalidOperationException("LLM returned empty response");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to generate answer from language model. Ensure the service is running and accessible.",
                ex);
        }

        // Step 4: Return structured result
        return new GeneratedAnswerResult
        {
            Answer = generatedAnswer.Trim(),
            Sources = prompt.Sources.ToList(),
            ConfidenceLevel = confidenceScore.Level,
            ConfidenceExplanation = confidenceScore.GetExplanation(),
            OriginalQuery = prompt.OriginalQuery,
            LlmInvoked = true
        };
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    /// Calculate confidence score from source chunk similarity scores.
    /// Uses Domain layer ConfidenceScore logic.
    /// </summary>
    private ConfidenceScore CalculateConfidenceScore(IReadOnlyCollection<Prompting.PromptSourceChunk> sources)
    {
        if (sources.Count == 0)
        {
            // No sources → no confidence
            return ConfidenceScore.Calculate(
                maxSimilarity: 0.0,
                averageSimilarity: 0.0,
                _policy);
        }

        var maxSimilarity = sources.Max(s => s.SimilarityScore);
        var averageSimilarity = sources.Average(s => s.SimilarityScore);

        return ConfidenceScore.Calculate(
            maxSimilarity,
            averageSimilarity,
            _policy);
    }

    /// <summary>
    /// Generate a message for when confidence is too low to provide an answer.
    /// </summary>
    private static string GenerateNoConfidenceMessage(string query, ConfidenceScore confidenceScore)
    {
        // TODO: Detect language from query
        bool isTurkish = false;

        if (isTurkish)
        {
            return $"Üzgünüm, \"{query}\" sorusu için yeterince güvenilir bilgi bulamadım.\n\n" +
                   $"Benzerlik çok düşük (en yüksek: {confidenceScore.MaxSimilarity * 100:F1}%, " +
                   $"gerekli: {ConfidencePolicy.Default.MinAcceptableThreshold * 100:F0}%).\n\n" +
                   "Lütfen sorunuzu farklı kelimelerle yeniden deneyin veya " +
                   "ilgili dokümanların yüklendiğinden emin olun.";
        }
        else
        {
            return $"I'm sorry, I could not find sufficiently reliable information to answer: \"{query}\"\n\n" +
                   $"The similarity is too low (max: {confidenceScore.MaxSimilarity * 100:F1}%, " +
                   $"required: {ConfidencePolicy.Default.MinAcceptableThreshold * 100:F0}%).\n\n" +
                   "Please try rephrasing your question or ensure relevant documents have been uploaded.";
        }
    }
}

