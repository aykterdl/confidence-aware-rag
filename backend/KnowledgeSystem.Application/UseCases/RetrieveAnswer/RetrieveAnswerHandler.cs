using FluentValidation;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.UseCases.RetrieveAnswer;

/// <summary>
/// Use Case Handler: Orchestrates answer retrieval workflow using RAG
/// 
/// Workflow:
/// 1. Validate query
/// 2. Generate embedding for question
/// 3. Perform vector similarity search
/// 4. Calculate confidence using Domain logic (ConfidenceScore)
/// 5. Apply relevance gating:
///    - If confidence is None → return fallback message, NO LLM call
///    - If confidence is Low/High → proceed to LLM
/// 6. Build contextual prompt from relevant sections
/// 7. Generate answer via LLM (mode adjusted by confidence level)
/// 8. Return result with confidence metadata
/// </summary>
public sealed class RetrieveAnswerHandler
{
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IVectorSearchEngine _vectorSearchEngine;
    private readonly ILanguageModel _languageModel;
    private readonly IValidator<RetrieveAnswerQuery> _validator;
    private readonly ConfidencePolicy _policy;

    public RetrieveAnswerHandler(
        IEmbeddingGenerator embeddingGenerator,
        IVectorSearchEngine vectorSearchEngine,
        ILanguageModel languageModel,
        IValidator<RetrieveAnswerQuery> validator,
        ConfidencePolicy policy)
    {
        _embeddingGenerator = embeddingGenerator;
        _vectorSearchEngine = vectorSearchEngine;
        _languageModel = languageModel;
        _validator = validator;
        _policy = policy;
    }

    public async Task<RetrieveAnswerResult> HandleAsync(
        RetrieveAnswerQuery query,
        CancellationToken cancellationToken = default)
    {
        // STEP 1: Validate query
        await _validator.ValidateAndThrowAsync(query, cancellationToken);

        // STEP 2: Generate embedding for question
        var questionEmbedding = await _embeddingGenerator.GenerateEmbeddingAsync(
            query.Question,
            cancellationToken);

        // STEP 3: Perform vector similarity search
        var searchResults = await _vectorSearchEngine.SearchAsync(
            questionEmbedding,
            query.TopK,
            cancellationToken);

        // STEP 4: Calculate confidence using Domain logic
        var confidenceScore = CalculateConfidenceScore(searchResults);

        // STEP 5: Apply relevance gating (DOMAIN RULE: IsAcceptable)
        if (!confidenceScore.IsAcceptable())
        {
            // Confidence is None → return fallback, NO LLM invocation
            return CreateLowConfidenceResult(confidenceScore, searchResults.Count);
        }

        // STEP 6: Build context from relevant sections
        var context = BuildContext(searchResults);

        // STEP 7: Generate answer via LLM (mode based on confidence)
        var systemPrompt = BuildSystemPrompt(confidenceScore);
        var answer = await _languageModel.GenerateAnswerAsync(
            query.Question,
            context,
            systemPrompt,
            cancellationToken);

        // STEP 8: Return result
        return new RetrieveAnswerResult
        {
            Answer = answer,
            ConfidenceLevel = confidenceScore.Level,
            RelevantSectionsCount = searchResults.Count,
            ConfidenceExplanation = confidenceScore.GetExplanation(), // DOMAIN provides explanation
            LlmInvoked = true
        };
    }

    /// <summary>
    /// Calculate confidence score using DOMAIN LOGIC
    /// Application layer orchestrates, Domain layer calculates
    /// </summary>
    private ConfidenceScore CalculateConfidenceScore(IReadOnlyList<SectionSearchResult> searchResults)
    {
        if (searchResults.Count == 0)
        {
            // No results found → None confidence
            return ConfidenceScore.Calculate(
                maxSimilarity: 0.0,
                averageSimilarity: 0.0,
                _policy);
        }

        var maxSimilarity = searchResults.Max(r => r.SimilarityScore);
        var averageSimilarity = searchResults.Average(r => r.SimilarityScore);

        // Delegate to Domain for confidence calculation
        return ConfidenceScore.Calculate(
            maxSimilarity,
            averageSimilarity,
            _policy);
    }

    /// <summary>
    /// Build context string from search results
    /// Concatenates relevant section content
    /// </summary>
    private static string BuildContext(IReadOnlyList<SectionSearchResult> searchResults)
    {
        var contextParts = searchResults
            .Select((result, index) => 
            {
                var section = result.Section;
                var header = section.Type == Domain.Entities.SectionType.Article && section.ArticleNumber != null
                    ? $"[{section.ArticleNumber}] {section.ArticleTitle ?? ""}".Trim()
                    : $"[Section {index + 1}]";
                
                return $"{header}\n{section.Content}";
            });

        return string.Join("\n\n---\n\n", contextParts);
    }

    /// <summary>
    /// Build system prompt based on confidence level
    /// DOMAIN RULE: RequiresCaution determines LLM mode
    /// </summary>
    private static string BuildSystemPrompt(ConfidenceScore confidenceScore)
    {
        if (confidenceScore.RequiresCaution())
        {
            // Low confidence → Cautious mode
            return @"You are a helpful assistant. Answer the question based STRICTLY on the provided context.
If the context contains partial or uncertain information, acknowledge the limitations.
Use conditional language (e.g., 'based on the available information', 'it appears that').
If you cannot provide a complete answer, explain what information is available and what is missing.
Be concise but thorough. Answer in the same language as the question.";
        }
        else
        {
            // High confidence → Direct mode
            return @"You are a helpful assistant. Answer the question based STRICTLY on the provided context.
Provide a clear, direct, and concise answer.
Do not speculate or add information not present in the context.
If the answer is not in the context, say so clearly.
Answer in the same language as the question.";
        }
    }

    /// <summary>
    /// Create fallback result when confidence is None (no LLM call)
    /// </summary>
    private static RetrieveAnswerResult CreateLowConfidenceResult(
        ConfidenceScore confidenceScore,
        int sectionsCount)
    {
        return new RetrieveAnswerResult
        {
            Answer = "I cannot answer this question based on the available documents. " +
                     "The similarity between your question and the document content is too low.",
            ConfidenceLevel = confidenceScore.Level,
            RelevantSectionsCount = sectionsCount,
            ConfidenceExplanation = confidenceScore.GetExplanation(), // DOMAIN provides explanation
            LlmInvoked = false
        };
    }

}

