namespace RAG.Api.Services;

/// <summary>
/// Chunk metadata - semantic chunking için
/// </summary>
public class ChunkMetadata
{
    public string? ArticleNumber { get; set; }
    public string? ArticleTitle { get; set; }
    public string ChunkType { get; set; } = "generic"; // "article" | "paragraph" | "generic"
}

/// <summary>
/// Chunk result with content and metadata
/// </summary>
public class ChunkResult
{
    public string Content { get; set; } = string.Empty;
    public ChunkMetadata Metadata { get; set; } = new();
}

public interface ITextChunkingService
{
    /// <summary>
    /// LEGACY: Simple string-based chunking (backward compatibility)
    /// </summary>
    List<string> ChunkText(string text, int maxChunkSize = 500, int overlap = 50);
    
    /// <summary>
    /// NEW: Semantic-aware chunking with metadata
    /// </summary>
    List<ChunkResult> ChunkTextWithMetadata(string text, int maxChunkSize = 500, int overlap = 50);
}
