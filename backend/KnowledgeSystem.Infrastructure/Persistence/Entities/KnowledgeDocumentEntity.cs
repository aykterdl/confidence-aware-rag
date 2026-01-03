using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace KnowledgeSystem.Infrastructure.Persistence.Entities;

/// <summary>
/// EF Core persistence entity for KnowledgeDocument
/// This is NOT the Domain entity - it's a separate persistence model
/// </summary>
[Table("documents")]
public sealed class KnowledgeDocumentEntity
{
    [Key]
    [Column("id")]
    public Guid Id { get; set; }

    [Required]
    [Column("title")]
    [MaxLength(500)]
    public string Title { get; set; } = string.Empty;

    [Column("uploaded_at")]
    public DateTime UploadedAt { get; set; }

    /// <summary>
    /// Navigation property to sections
    /// EF Core manages this relationship
    /// </summary>
    public ICollection<ContentSectionEntity> Sections { get; set; } = new List<ContentSectionEntity>();
}

