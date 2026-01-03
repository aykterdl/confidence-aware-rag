using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Pgvector;

namespace KnowledgeSystem.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core persistence entity for ContentSection
/// This is NOT the Domain entity - it's a separate persistence model
/// </summary>
[Table("sections")]
public sealed class ContentSectionEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("document_id")]
    public Guid DocumentId { get; set; }

    [Column("index")]
    public int Index { get; set; }

    [Required]
    [Column("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Embedding vector stored as pgvector type
    /// </summary>
    [Column("embedding", TypeName = "vector(768)")]
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Semantic metadata: Article number (optional)
    /// </summary>
    [Column("article_number")]
    [MaxLength(50)]
    public string? ArticleNumber { get; set; }

    /// <summary>
    /// Semantic metadata: Article title (optional)
    /// </summary>
    [Column("article_title")]
    public string? ArticleTitle { get; set; }

    /// <summary>
    /// Section type: "article", "paragraph", "generic"
    /// </summary>
    [Required]
    [Column("section_type")]
    [MaxLength(20)]
    public string SectionType { get; set; } = "generic";

    /// <summary>
    /// Navigation property to parent document
    /// </summary>
    [ForeignKey(nameof(DocumentId))]
    public KnowledgeDocumentEntity Document { get; set; } = null!;
}

