using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Infrastructure.Persistence.Entities;
using Pgvector.EntityFrameworkCore;

namespace KnowledgeSystem.Infrastructure.Persistence;

/// <summary>
/// EF Core DbContext for Knowledge Management system
/// This context knows ONLY about persistence entities, NOT domain entities
/// </summary>
public sealed class KnowledgeDbContext : DbContext
{
    public KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options)
        : base(options)
    {
    }

    public DbSet<KnowledgeDocumentEntity> Documents => Set<KnowledgeDocumentEntity>();
    public DbSet<ContentSectionEntity> Sections => Set<ContentSectionEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Enable pgvector extension
        modelBuilder.HasPostgresExtension("vector");

        ConfigureKnowledgeDocument(modelBuilder);
        ConfigureContentSection(modelBuilder);
    }

    private static void ConfigureKnowledgeDocument(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeDocumentEntity>(entity =>
        {
            entity.ToTable("documents");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            entity.Property(e => e.UploadedAt)
                .HasColumnName("uploaded_at")
                .IsRequired();

            // One-to-many relationship: Document -> Sections
            entity.HasMany(e => e.Sections)
                .WithOne(s => s.Document)
                .HasForeignKey(s => s.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index for performance
            entity.HasIndex(e => e.UploadedAt);
        });
    }

    private static void ConfigureContentSection(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ContentSectionEntity>(entity =>
        {
            entity.ToTable("sections");

            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id)
                .HasColumnName("id")
                .IsRequired();

            entity.Property(e => e.DocumentId)
                .HasColumnName("document_id")
                .IsRequired();

            entity.Property(e => e.Index)
                .HasColumnName("index")
                .IsRequired();

            entity.Property(e => e.Content)
                .HasColumnName("content")
                .IsRequired();

            // Pgvector configuration for embeddings
            entity.Property(e => e.Embedding)
                .HasColumnName("embedding")
                .HasColumnType("vector(768)"); // 768 dimensions for Ollama embeddings

            entity.Property(e => e.ArticleNumber)
                .HasColumnName("article_number")
                .HasMaxLength(50);

            entity.Property(e => e.ArticleTitle)
                .HasColumnName("article_title");

            entity.Property(e => e.SectionType)
                .HasColumnName("section_type")
                .HasMaxLength(20)
                .IsRequired();

            // Indexes for performance
            entity.HasIndex(e => e.DocumentId);
            entity.HasIndex(e => e.Index);
            entity.HasIndex(e => e.ArticleNumber);
            entity.HasIndex(e => e.SectionType);
        });
    }
}

