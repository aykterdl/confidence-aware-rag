namespace KnowledgeSystem.Api.Models;

/// <summary>
/// RAG soru-cevap için response modeli
/// </summary>
public class RagResponse
{
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    
    /// <summary>
    /// Conversation ID - multi-turn tracking için
    /// </summary>
    public Guid ConversationId { get; set; }
    
    /// <summary>
    /// Cevabın dili (tr veya en)
    /// </summary>
    public string Language { get; set; } = string.Empty;
    
    /// <summary>
    /// Confidence bilgisi (similarity-based)
    /// </summary>
    public ConfidenceInfo Confidence { get; set; } = new();
    
    /// <summary>
    /// Kullanılan kaynak chunk'lar
    /// </summary>
    public List<SourceReference> Sources { get; set; } = new();
    
    /// <summary>
    /// Kaynak sayısı
    /// </summary>
    public int SourceCount { get; set; }
    
    /// <summary>
    /// Ortalama benzerlik skoru (backward compatibility için kalıyor)
    /// </summary>
    public double AverageSimilarity { get; set; }
}

/// <summary>
/// Kullanılan kaynak chunk bilgisi (traceability için)
/// </summary>
public class SourceReference
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public double SimilarityScore { get; set; }
    public string ContentPreview { get; set; } = string.Empty;
}

