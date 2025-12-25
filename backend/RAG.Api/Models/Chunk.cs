using Pgvector;
using System.ComponentModel.DataAnnotations.Schema;

namespace RAG.Api.Models;

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

    // Navigation property
    public Document Document { get; set; } = null!;
}

