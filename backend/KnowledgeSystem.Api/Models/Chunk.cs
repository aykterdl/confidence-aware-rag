using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeSystem.Api.Models;

[Table("chunks")]
public class Chunk
{
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("document_id")]
    public Guid DocumentId { get; set; }
    
    [Column("chunk_index")]
    public int ChunkIndex { get; set; }
    
    [Column("content")]
    public string Content { get; set; } = string.Empty;
    
    [Column("embedding")]
    public Vector? Embedding { get; set; } // pgvector type
    
    [Column("token_count")]
    public int? TokenCount { get; set; }
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Semantic chunking metadata
    [Column("article_number")]
    public string? ArticleNumber { get; set; } // e.g. "1", "2", "3a"
    
    [Column("article_title")]
    public string? ArticleTitle { get; set; } // e.g. "Devletin Şekli", "Introduction"
    
    [Column("chunk_type")]
    public string ChunkType { get; set; } = "generic"; // "article" | "paragraph" | "generic"

    // Navigation property
    public Document Document { get; set; } = null!;
}

