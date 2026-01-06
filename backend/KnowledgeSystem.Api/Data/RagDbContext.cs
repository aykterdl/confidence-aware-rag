using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Api.Models;
using Pgvector.EntityFrameworkCore;

namespace KnowledgeSystem.Api.Data;

public class RagDbContext : DbContext
{
    public RagDbContext(DbContextOptions<RagDbContext> options) 
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Chunk> Chunks => Set<Chunk>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // pgvector extension aktif et
        modelBuilder.HasPostgresExtension("vector");

        // Document entity
        modelBuilder.Entity<Document>(entity =>
        {
            entity.ToTable("documents");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).HasMaxLength(500).IsRequired();
            // Metadata as text (not jsonb) to support both JSON and plain string
            entity.Property(e => e.Metadata).HasColumnType("text");
        });

        // Chunk entity
        modelBuilder.Entity<Chunk>(entity =>
        {
            entity.ToTable("chunks");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired();
            
            // pgvector - 768 boyutlu embedding
            entity.Property(e => e.Embedding)
                .HasColumnType("vector(768)");

            // Foreign key
            entity.HasOne(c => c.Document)
                .WithMany(d => d.Chunks)
                .HasForeignKey(c => c.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            // Index
            entity.HasIndex(e => e.DocumentId);
            
            // Unique constraint
            entity.HasIndex(e => new { e.DocumentId, e.ChunkIndex })
                .IsUnique();
        });

        // Vector index (similarity search için)
        // EF Core migrations ile oluşturulmayabilir, SQL'den manuel ekledik
    }
}





