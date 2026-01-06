namespace KnowledgeSystem.Api.Models;

/// <summary>
/// RAG soru-cevap için request modeli
/// </summary>
public class RagRequest
{
    public string Question { get; set; } = string.Empty;
    
    /// <summary>
    /// Conversation ID - multi-turn için (optional)
    /// Yoksa yeni conversation oluşturulur
    /// </summary>
    public Guid? ConversationId { get; set; }
    
    /// <summary>
    /// Vector search'ten kaç chunk alınacağı (default: 5)
    /// </summary>
    public int? TopK { get; set; }
    
    /// <summary>
    /// Minimum benzerlik skoru (default: 0.0)
    /// </summary>
    public double? MinSimilarity { get; set; }
}

