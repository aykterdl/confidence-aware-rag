using System.Text;
using Microsoft.Extensions.Options;
using RAG.Api.Configuration;
using RAG.Api.Models;

namespace RAG.Api.Services;

/// <summary>
/// RAG (Retrieval Augmented Generation) implementasyonu
/// Vector search + LLM ile kaynaklı cevap üretimi
/// Multi-turn conversation + citation support
/// </summary>
public class RagAnswerService : IRagAnswerService
{
    private readonly IVectorSearchService _vectorSearchService;
    private readonly IOllamaLlmService _llmService;
    private readonly IConversationStore _conversationStore;
    private readonly ILogger<RagAnswerService> _logger;
    private readonly int _maxTurnsInContext;
    private readonly RagConfidenceSettings _confidenceSettings;

    public RagAnswerService(
        IVectorSearchService vectorSearchService,
        IOllamaLlmService llmService,
        IConversationStore conversationStore,
        ILogger<RagAnswerService> logger,
        IConfiguration configuration,
        IOptions<RagConfidenceSettings> confidenceSettings)
    {
        _vectorSearchService = vectorSearchService;
        _llmService = llmService;
        _conversationStore = conversationStore;
        _logger = logger;
        _maxTurnsInContext = configuration.GetValue<int>("Conversation:MaxTurnsInContext", 5);
        _confidenceSettings = confidenceSettings.Value;
    }

    public async Task<RagResponse> AskAsync(RagRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Question))
        {
            throw new ArgumentException("Question cannot be empty", nameof(request));
        }

        var topK = request.TopK ?? 5;
        var minSimilarity = request.MinSimilarity ?? 0.0;

        // STEP 0: Conversation handling
        var conversationId = request.ConversationId ?? _conversationStore.CreateConversation();
        var conversationHistory = _conversationStore.GetConversation(conversationId);
        
        if (conversationHistory == null && request.ConversationId.HasValue)
        {
            _logger.LogWarning("Conversation {ConversationId} not found, creating new one", request.ConversationId);
            conversationId = _conversationStore.CreateConversation();
            conversationHistory = _conversationStore.GetConversation(conversationId);
        }

        _logger.LogInformation("🔍 RAG Question: '{Question}' (convId={ConvId}, topK={TopK}, minSim={MinSim})",
            request.Question.Substring(0, Math.Min(100, request.Question.Length)), 
            conversationId, topK, minSimilarity);

        // Dil tespiti (prompt için gerekli)
        var isTurkish = DetectTurkish(request.Question);
        var language = isTurkish ? "tr" : "en";
        
        try
        {
            // STEP 1: Vector search - en ilgili chunk'ları bul
            _logger.LogInformation("Step 1: Performing vector search...");
            var searchResults = await _vectorSearchService.SearchAsync(
                request.Question,
                topK,
                minSimilarity,
                cancellationToken
            );

            // Fallback response (no context)
            var noContextAnswer = isTurkish
                ? "Bu soru, sağlanan belgelerden cevaplanamıyor."
                : "I don't have enough information to answer this question based on the provided documents.";

            if (searchResults.Count == 0)
            {
                _logger.LogWarning("⚠️ No relevant documents found for question");
                
                // Conversation'a ekle
                _conversationStore.AddTurn(conversationId, request.Question, noContextAnswer);
                
                return new RagResponse
                {
                    Question = request.Question,
                    Answer = noContextAnswer,
                    ConversationId = conversationId,
                    Language = language,
                    Confidence = new ConfidenceInfo
                    {
                        Level = "none",
                        MaxSimilarity = 0.0,
                        AverageSimilarity = 0.0,
                        Explanation = "No relevant documents found"
                    },
                    Sources = new List<SourceReference>(),
                    SourceCount = 0,
                    AverageSimilarity = 0.0
                };
            }

            _logger.LogInformation("✅ Found {Count} relevant chunks", searchResults.Count);

            // STEP 1.5: Confidence & Relevance Gating
            var maxSimilarity = searchResults.Max(r => r.SimilarityScore);
            var avgSimilarity = searchResults.Average(r => r.SimilarityScore);
            
            _logger.LogInformation("📊 Similarity Scores: Max={Max:F4}, Avg={Avg:F4}, MinThreshold={MinThreshold:F4}",
                maxSimilarity, avgSimilarity, _confidenceSettings.MinAnswerSimilarity);

            // Relevance Gate: Too low similarity → don't call LLM
            if (maxSimilarity < _confidenceSettings.MinAnswerSimilarity)
            {
                _logger.LogWarning("🚫 RELEVANCE GATE: MaxSimilarity ({Max:F4}) below threshold ({Threshold:F4}). Skipping LLM.",
                    maxSimilarity, _confidenceSettings.MinAnswerSimilarity);
                
                var lowRelevanceAnswer = isTurkish
                    ? "Bu soru, mevcut belgelerle yeterince ilgili değil. Daha spesifik bir soru sorabilir misiniz?"
                    : "This question doesn't seem sufficiently related to the available documents. Could you ask a more specific question?";
                
                _conversationStore.AddTurn(conversationId, request.Question, lowRelevanceAnswer);
                
                return new RagResponse
                {
                    Question = request.Question,
                    Answer = lowRelevanceAnswer,
                    ConversationId = conversationId,
                    Language = language,
                    Confidence = new ConfidenceInfo
                    {
                        Level = "none",
                        MaxSimilarity = Math.Round(maxSimilarity, 4),
                        AverageSimilarity = Math.Round(avgSimilarity, 4),
                        Explanation = $"Similarity too low (max: {maxSimilarity:F4}, threshold: {_confidenceSettings.MinAnswerSimilarity:F4})"
                    },
                    Sources = searchResults.Select(r => new SourceReference
                    {
                        ChunkId = r.ChunkId,
                        DocumentId = r.DocumentId,
                        DocumentTitle = r.DocumentTitle,
                        ChunkIndex = r.ChunkIndex,
                        SimilarityScore = r.SimilarityScore,
                        ContentPreview = r.Content.Length > 100 ? r.Content.Substring(0, 100) + "..." : r.Content
                    }).ToList(),
                    SourceCount = searchResults.Count,
                    AverageSimilarity = Math.Round(avgSimilarity, 4)
                };
            }

            // Determine confidence level
            var confidenceLevel = maxSimilarity >= _confidenceSettings.LowConfidenceThreshold ? "high" : "low";
            
            _logger.LogInformation("✅ Confidence Level: {Level} (MaxSim: {Max:F4}, LowThreshold: {Threshold:F4})",
                confidenceLevel, maxSimilarity, _confidenceSettings.LowConfidenceThreshold);

            // STEP 2: Context oluştur (chunks + conversation history)
            _logger.LogInformation("Step 2: Building context...");
            var documentContext = BuildContext(searchResults);
            var conversationContext = BuildConversationContext(conversationHistory);
            
            _logger.LogInformation("✅ Context built: {DocChars} chars from docs, {ConvTurns} conversation turns",
                documentContext.Length, conversationHistory?.Turns.Count ?? 0);

            // STEP 3: RAG prompt hazırla (citation-aware + conversation + confidence-aware)
            _logger.LogInformation("Step 3: Preparing RAG prompt (confidence: {ConfidenceLevel})...", confidenceLevel);
            var prompt = BuildRagPrompt(documentContext, conversationContext, request.Question, isTurkish, confidenceLevel);
            _logger.LogDebug("Prompt length: {Length} characters", prompt.Length);

            // STEP 4: LLM'e gönder
            _logger.LogInformation("Step 4: Sending to LLM...");
            var answer = await _llmService.GenerateAsync(prompt, cancellationToken);
            _logger.LogInformation("✅ LLM response received: {Length} characters", answer.Length);

            // STEP 5: Response oluştur + Conversation'a kaydet
            var sources = searchResults.Select(r => new SourceReference
            {
                ChunkId = r.ChunkId,
                DocumentId = r.DocumentId,
                DocumentTitle = r.DocumentTitle,
                ChunkIndex = r.ChunkIndex,
                SimilarityScore = r.SimilarityScore,
                ContentPreview = r.Content.Length > 200
                    ? r.Content.Substring(0, 200) + "..."
                    : r.Content
            }).ToList();

            // Conversation'a turn ekle
            _conversationStore.AddTurn(conversationId, request.Question, answer);
            _logger.LogDebug("Added turn to conversation {ConversationId}", conversationId);

            // Confidence explanation
            var confidenceExplanation = confidenceLevel == "high"
                ? $"High confidence based on strong similarity (max: {maxSimilarity:F4})"
                : $"Low confidence - answer may be incomplete (max: {maxSimilarity:F4}, threshold: {_confidenceSettings.LowConfidenceThreshold:F4})";

            var response = new RagResponse
            {
                Question = request.Question,
                Answer = answer,
                ConversationId = conversationId,
                Language = language,
                Confidence = new ConfidenceInfo
                {
                    Level = confidenceLevel,
                    MaxSimilarity = Math.Round(maxSimilarity, 4),
                    AverageSimilarity = Math.Round(avgSimilarity, 4),
                    Explanation = confidenceExplanation
                },
                Sources = sources,
                SourceCount = sources.Count,
                AverageSimilarity = Math.Round(avgSimilarity, 4) // backward compatibility
            };

            _logger.LogInformation("✅ RAG COMPLETED - Confidence: {Confidence}, MaxSim: {MaxSim:F4}, Answer: {Length} chars",
                confidenceLevel, maxSimilarity, answer.Length);

            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ RAG processing failed for question: '{Question}'",
                request.Question.Substring(0, Math.Min(100, request.Question.Length)));
            throw new InvalidOperationException("RAG answer generation failed", ex);
        }
    }

    /// <summary>
    /// Chunk'lardan context string oluşturur
    /// </summary>
    private string BuildContext(List<SearchResult> searchResults)
    {
        var sb = new StringBuilder();

        for (int i = 0; i < searchResults.Count; i++)
        {
            var result = searchResults[i];
            sb.AppendLine($"[Document {i + 1}: {result.DocumentTitle}]");
            sb.AppendLine(result.Content);
            sb.AppendLine(); // Boş satır
        }

        return sb.ToString().Trim();
    }

    /// <summary>
    /// Dil tespiti - Türkçe mi değil mi
    /// </summary>
    private bool DetectTurkish(string text)
    {
        // Türkçe karakterler
        if (text.Any(c => "çğıöşüÇĞİÖŞÜ".Contains(c)))
            return true;
        
        // Türkçe kelimeler (case-insensitive)
        var turkishKeywords = new[] { "nedir", "hakkinda", "hakkında", "madde", "nasil", "nasıl", 
                                       "neden", "ne", "mi", "mu", "mü", "hangi", "kim" };
        
        foreach (var keyword in turkishKeywords)
        {
            if (text.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        
        return false;
    }

    /// <summary>
    /// Conversation history'yi context string'e çevirir
    /// </summary>
    private string BuildConversationContext(ConversationHistory? history)
    {
        if (history == null || history.Turns.Count == 0)
            return string.Empty;

        var recentTurns = history.GetRecentTurns(_maxTurnsInContext);
        if (recentTurns.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("PREVIOUS CONVERSATION:");
        sb.AppendLine();

        foreach (var turn in recentTurns)
        {
            sb.AppendLine($"User: {turn.Question}");
            sb.AppendLine($"Assistant: {turn.Answer}");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// RAG prompt oluşturur - citation-aware + conversation + language-aware + confidence-aware
    /// </summary>
    private string BuildRagPrompt(string documentContext, string conversationContext, string question, bool isTurkish, string confidenceLevel)
    {
        var languageInstruction = isTurkish 
            ? "IMPORTANT: The question is in TURKISH. You MUST answer in TURKISH only."
            : "IMPORTANT: The question is in ENGLISH. You MUST answer in ENGLISH only.";
        
        var noAnswerTemplate = isTurkish
            ? "Bu soru, sağlanan belgelerden cevaplanamıyor."
            : "I don't have enough information to answer this question based on the provided documents.";

        var conversationSection = string.IsNullOrEmpty(conversationContext)
            ? string.Empty
            : $@"
{conversationContext}

---
";

        // Low confidence mode - extra caution instructions
        var confidenceInstructions = confidenceLevel == "low"
            ? @"

⚠️ LOW CONFIDENCE MODE:
- The similarity between the question and documents is LOW
- Answer CAUTIOUSLY and acknowledge uncertainty
- Use conditional language: ""may"", ""might"", ""appears to"", ""suggests that""
- Avoid absolute claims or strong assertions
- Clearly state if the answer is partial or uncertain
"
            : string.Empty;

        return $@"You are a helpful assistant that answers questions based on provided documents.

{languageInstruction}

STRICT RULES (CITATION MODE):
1. Answer ONLY using information from the DOCUMENT CONTEXT below
2. If you provide an answer, it MUST be supported by the documents
3. Do NOT use information from previous conversation to answer factual questions
4. Do NOT use external knowledge or assumptions
5. If the documents do NOT contain the answer, say: ""{noAnswerTemplate}""
6. Be clear, concise, and direct
7. You may reference which document number you're using (e.g., ""According to Document 1..."")
{confidenceInstructions}
{conversationSection}DOCUMENT CONTEXT:

{documentContext}

QUESTION: {question}

ANSWER (in {(isTurkish ? "TURKISH" : "ENGLISH")}):";
    }
}

