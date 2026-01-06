namespace KnowledgeSystem.Api.Models;

/// <summary>
/// Vector search sonucu
/// </summary>
public class SearchResult
{
    public Guid ChunkId { get; set; }
    public Guid DocumentId { get; set; }
    public string DocumentTitle { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public double SimilarityScore { get; set; } // 0-1 arası (1 = tam benzer)
}




