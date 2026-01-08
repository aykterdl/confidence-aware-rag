using System.Text;

namespace KnowledgeSystem.Application.UseCases.Prompting;

/// <summary>
/// Handler for composing prompts from user queries and retrieved document chunks.
/// Phase 4 - Step 2: Prompt Composition (Balanced, Semantic-Aware, Guarded).
/// 
/// STRATEGY: BALANCED (B)
/// - Prefer answers grounded in retrieved document chunks
/// - Allow light general reasoning only if it helps connect facts
/// - Never hallucinate missing facts
/// - If documents lack sufficient information, explicitly state uncertainty
/// 
/// ARCHITECTURE:
/// - Application layer: defines prompt composition logic
/// - NO knowledge of LLM APIs (OpenAI, Ollama, etc.)
/// - Infrastructure layer will consume the composed prompt (Phase 4 - Step 3)
/// </summary>
public sealed class ComposePromptHandler
{
    /// <summary>
    /// Compose a prompt from user query and retrieved chunks.
    /// </summary>
    public ComposedPrompt Handle(ComposePromptCommand command)
    {
        if (command == null)
            throw new ArgumentNullException(nameof(command));

        if (string.IsNullOrWhiteSpace(command.Query))
            throw new ArgumentException("Query cannot be empty", nameof(command));

        // Determine language for instructions
        var isTurkish = command.Language?.StartsWith("tr", StringComparison.OrdinalIgnoreCase) == true;

        // Build system prompt (role + guardrails + balanced strategy)
        var systemPrompt = BuildSystemPrompt(isTurkish, command.RetrievedChunks.Count == 0);

        // Build user prompt (query + context + instructions)
        var userPrompt = BuildUserPrompt(command.Query, command.RetrievedChunks, isTurkish);

        // Build source metadata (for traceability)
        var sources = command.RetrievedChunks.Select(chunk => new PromptSourceChunk
        {
            ChunkId = Guid.Parse(chunk.ChunkId),
            DocumentId = Guid.Parse(chunk.DocumentId),
            DocumentTitle = chunk.DocumentTitle,
            Content = chunk.Content,
            SimilarityScore = chunk.SimilarityScore,
            SectionType = chunk.SectionType,
            ArticleNumber = chunk.ArticleNumber,
            ArticleTitle = chunk.ArticleTitle
        }).ToList();

        return new ComposedPrompt
        {
            SystemPrompt = systemPrompt,
            UserPrompt = userPrompt,
            Sources = sources,
            OriginalQuery = command.Query
        };
    }

    // ============================================================================
    // SYSTEM PROMPT CONSTRUCTION (ROLE + GUARDRAILS + BALANCED STRATEGY)
    // ============================================================================

    /// <summary>
    /// Build the system prompt that defines LLM behavior.
    /// </summary>
    private static string BuildSystemPrompt(bool isTurkish, bool noChunksRetrieved)
    {
        if (isTurkish)
        {
            return noChunksRetrieved
                ? BuildSystemPromptTurkishNoContext()
                : BuildSystemPromptTurkishWithContext();
        }
        else
        {
            return noChunksRetrieved
                ? BuildSystemPromptEnglishNoContext()
                : BuildSystemPromptEnglishWithContext();
        }
    }

    /// <summary>
    /// System prompt for Turkish with context chunks.
    /// </summary>
    private static string BuildSystemPromptTurkishWithContext()
    {
        return @"Sen yardımcı bir asistansın ve kullanıcının sorularını sağlanan dokümanlar temelinde yanıtlıyorsun.

GÖREV:
- Kullanıcının sorusunu verilen kaynak metinlerden yararlanarak yanıtla
- Kaynak metinlerde bulunan bilgileri öncelikli kullan
- Gerekirse bilgileri mantıksal olarak ilişkilendir, ancak asla bilgi uydurma
- Cevap kaynaklarda yeterince desteklenmiyorsa, eksikliği açıkça belirt

KURALLAR:
- Sadece sağlanan kaynaklardan bilgi kullan
- Kaynaklarda olmayan bilgileri ekleme
- Belirsiz durumlarda ""Verilen dokümanlarda bu bilgi yok"" de
- Net ve öz ol, gereksiz detaylara girme
- Eğer cevap veremiyorsan, ""Bilmiyorum"" diyebilirsin

STRATEJİ: BALANCED (Dengeli)
- Kaynak metinlere sadık kal
- Hafif çıkarım yapabilirsin (gerçekleri bağlamak için)
- Asla tahmin yürütme veya kaynak dışı bilgi ekleme";
    }

    /// <summary>
    /// System prompt for Turkish without context chunks.
    /// </summary>
    private static string BuildSystemPromptTurkishNoContext()
    {
        return @"Sen yardımcı bir asistansın.

DURUM:
Kullanıcının sorusu için yeterli kaynak doküman bulunamadı.

GÖREV:
- Kullanıcıya kibarca durumu açıkla
- Sorularını farklı kelimelerle tekrar denemelerini öner
- Mevcut dokümanlarda bu bilginin olmayabileceğini belirt

KURALLAR:
- Cevap uydurma
- Genel bilgi verme
- Sadece mevcut durumu açıkla";
    }

    /// <summary>
    /// System prompt for English with context chunks.
    /// </summary>
    private static string BuildSystemPromptEnglishWithContext()
    {
        return @"You are a helpful assistant answering questions based on provided documents.

TASK:
- Answer the user's question using the provided source texts
- Prioritize information found in the source materials
- You may logically connect facts if necessary, but never fabricate information
- If the answer is not fully supported by the sources, explicitly state the limitation

RULES:
- Use only the provided sources
- Do not add information not present in the sources
- In uncertain cases, say ""This information is not in the provided documents""
- Be clear and concise, avoid unnecessary details
- If you cannot answer, say ""I don't know""

STRATEGY: BALANCED
- Stay faithful to source texts
- Light inference is allowed (to connect facts)
- Never guess or add external knowledge";
    }

    /// <summary>
    /// System prompt for English without context chunks.
    /// </summary>
    private static string BuildSystemPromptEnglishNoContext()
    {
        return @"You are a helpful assistant.

SITUATION:
No relevant source documents were found for the user's question.

TASK:
- Politely explain the situation to the user
- Suggest they try rephrasing their question
- Note that this information may not be available in the current documents

RULES:
- Do not fabricate answers
- Do not provide general knowledge
- Only explain the current situation";
    }

    // ============================================================================
    // USER PROMPT CONSTRUCTION (QUERY + CONTEXT + INSTRUCTIONS)
    // ============================================================================

    /// <summary>
    /// Build the user prompt containing query, context chunks, and instructions.
    /// </summary>
    private static string BuildUserPrompt(
        string query,
        IReadOnlyCollection<SemanticSearch.ChunkMatch> chunks,
        bool isTurkish)
    {
        var builder = new StringBuilder();

        // Section 1: User Question
        builder.AppendLine(isTurkish ? "KULLANICI SORUSU:" : "USER QUESTION:");
        builder.AppendLine(query);
        builder.AppendLine();

        // Section 2: Context (if chunks exist)
        if (chunks.Count > 0)
        {
            builder.AppendLine(isTurkish ? "BAĞLAM (Kaynak Dokümanlar):" : "CONTEXT (Source Documents):");
            builder.AppendLine();

            int sourceIndex = 1;
            foreach (var chunk in chunks)
            {
                builder.AppendLine($"[{(isTurkish ? "Kaynak" : "Source")} {sourceIndex}]");
                builder.AppendLine($"{(isTurkish ? "Doküman" : "Document")}: {chunk.DocumentTitle}");

                // Include article metadata if present (for legal documents)
                if (!string.IsNullOrWhiteSpace(chunk.ArticleNumber))
                {
                    var articleLabel = chunk.ArticleTitle != null
                        ? $"{(isTurkish ? "Madde" : "Article")} {chunk.ArticleNumber} - {chunk.ArticleTitle}"
                        : $"{(isTurkish ? "Madde" : "Article")} {chunk.ArticleNumber}";
                    builder.AppendLine($"{articleLabel}");
                }

                builder.AppendLine($"{(isTurkish ? "İçerik" : "Content")}:");
                builder.AppendLine(chunk.Content);
                builder.AppendLine();

                sourceIndex++;
            }
        }
        else
        {
            // No chunks retrieved - inform LLM
            builder.AppendLine(isTurkish ? "BAĞLAM:" : "CONTEXT:");
            builder.AppendLine(isTurkish
                ? "(Soruyla ilgili doküman bulunamadı)"
                : "(No relevant documents found for this question)");
            builder.AppendLine();
        }

        // Section 3: Instructions
        builder.AppendLine(isTurkish ? "TALİMATLAR:" : "INSTRUCTIONS:");
        if (chunks.Count > 0)
        {
            if (isTurkish)
            {
                builder.AppendLine("- Yukarıdaki bağlamı kullanarak soruyu yanıtla");
                builder.AppendLine("- Cevap tamamen desteklenmiyorsa, sınırlamayı belirt");
                builder.AppendLine("- Verilen bağlam dışındaki bilgilere atıfta bulunma");
            }
            else
            {
                builder.AppendLine("- Answer the question using the context above");
                builder.AppendLine("- If the answer is not fully supported, state the limitation");
                builder.AppendLine("- Do not reference information outside the provided context");
            }
        }
        else
        {
            if (isTurkish)
            {
                builder.AppendLine("- Kullanıcıya mevcut dokümanlarda bu bilginin olmadığını kibarca açıkla");
                builder.AppendLine("- Sorusunu farklı kelimelerle tekrar denemesini öner");
            }
            else
            {
                builder.AppendLine("- Politely explain that this information is not in the current documents");
                builder.AppendLine("- Suggest trying a different phrasing of the question");
            }
        }

        return builder.ToString();
    }
}

