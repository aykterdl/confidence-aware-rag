namespace KnowledgeSystem.Api.Models;

/// <summary>
/// RAG cevabının güvenilirlik bilgisi
/// </summary>
public class ConfidenceInfo
{
    /// <summary>
    /// Confidence seviyesi: high, low, none
    /// </summary>
    public string Level { get; set; } = "none";
    
    /// <summary>
    /// En yüksek similarity skoru
    /// </summary>
    public double MaxSimilarity { get; set; }
    
    /// <summary>
    /// Ortalama similarity skoru
    /// </summary>
    public double AverageSimilarity { get; set; }
    
    /// <summary>
    /// Confidence değerlendirme metni (optional, UI için)
    /// </summary>
    public string? Explanation { get; set; }
}


