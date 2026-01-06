using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeSystem.Api.Models;

[Table("documents")]
public class Document
{
    [Column("id")]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Column("filename")]
    public string Filename { get; set; } = string.Empty;
    
    [Column("file_path")]
    public string? FilePath { get; set; }
    
    [Column("upload_date")]
    public DateTime UploadDate { get; set; } = DateTime.UtcNow;
    
    [Column("total_chunks")]
    public int TotalChunks { get; set; }
    
    [Column("metadata")]
    public string? Metadata { get; set; } // JSON string
    
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public List<Chunk> Chunks { get; set; } = new();
}

