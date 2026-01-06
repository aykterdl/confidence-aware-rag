namespace KnowledgeSystem.Api.Configuration;

/// <summary>
/// RAG confidence ve relevance gating ayarları
/// </summary>
public class RagConfidenceSettings
{
    public const string SectionName = "RagConfidence";
    
    /// <summary>
    /// Bu değerin altında LLM çağrısı yapılmaz (fallback döner)
    /// Default: 0.04
    /// </summary>
    public double MinAnswerSimilarity { get; set; } = 0.04;
    
    /// <summary>
    /// Bu değerin altında cevap "low confidence" olarak işaretlenir
    /// Default: 0.06
    /// </summary>
    public double LowConfidenceThreshold { get; set; } = 0.06;
}


