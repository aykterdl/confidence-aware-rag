using Microsoft.EntityFrameworkCore;
using Pgvector;
using Pgvector.EntityFrameworkCore;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;
using KnowledgeSystem.Infrastructure.Persistence;

namespace KnowledgeSystem.Infrastructure.VectorSearch;

/// <summary>
/// Adapter implementing vector similarity search using PostgreSQL + pgvector
/// This is the ONLY place where pgvector implementation details exist
/// 
/// ARCHITECTURE NOTES:
/// - Implements IVectorSearchEngine port from Application layer
/// - Uses KnowledgeDbContext to access persistence entities
/// - Performs cosine similarity search server-side (PostgreSQL)
/// - Maps persistence entities to Domain entities explicitly
/// - Does NOT expose similarity scores (returns ordered list only)
/// </summary>
public sealed class PgVectorSearchEngine : IVectorSearchEngine
{
    private readonly KnowledgeDbContext _context;

    public PgVectorSearchEngine(KnowledgeDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    /// <summary>
    /// Search for content sections similar to the query embedding
    /// Uses pgvector cosine similarity for relevance calculation
    /// </summary>
    public async Task<IReadOnlyList<SectionSearchResult>> SearchAsync(
        float[] queryEmbedding,
        int topK = 5,
        CancellationToken cancellationToken = default)
    {
        // Validation
        if (queryEmbedding == null || queryEmbedding.Length == 0)
            throw new ArgumentException("Query embedding cannot be null or empty", nameof(queryEmbedding));

        if (topK <= 0)
            throw new ArgumentException("TopK must be greater than 0", nameof(topK));

        // Convert float[] to pgvector Vector type
        var queryVector = new Vector(queryEmbedding);

        // Perform similarity search using pgvector
        // CosineDistance: 0 = identical, 2 = opposite
        // We convert distance to similarity: similarity = 1 - (distance / 2)
        // This gives us a 0-1 range where 1 = identical, 0 = opposite
        var results = await _context.Sections
            .Where(s => s.Embedding != null) // Only search sections with embeddings
            .Select(s => new
            {
                Section = s,
                Distance = s.Embedding!.CosineDistance(queryVector) // Server-side calculation
            })
            .OrderBy(x => x.Distance) // Order by distance (lowest first = most similar)
            .Take(topK)
            .AsNoTracking() // Read-only optimization
            .ToListAsync(cancellationToken);

        // Map persistence entities to Domain entities and wrap in SectionSearchResult
        // Convert distance to similarity score (0-1 range)
        return results
            .Select(x => new SectionSearchResult
            {
                Section = MapToDomain(x.Section),
                SimilarityScore = ConvertDistanceToSimilarity(x.Distance),
                DocumentId = DocumentId.From(x.Section.DocumentId)
            })
            .ToList()
            .AsReadOnly();
    }

    // ============================================================================
    // HELPER METHODS
    // ============================================================================

    /// <summary>
    /// Convert pgvector cosine distance to similarity score
    /// Distance: 0 = identical, 2 = opposite
    /// Similarity: 1 = identical, 0 = opposite
    /// </summary>
    private static double ConvertDistanceToSimilarity(double distance)
    {
        return 1.0 - (distance / 2.0);
    }

    // ============================================================================
    // MAPPING LOGIC: Persistence → Domain
    // ============================================================================

    /// <summary>
    /// Map persistence entity to Domain ContentSection
    /// This mapping ensures Infrastructure concerns don't leak into Domain
    /// </summary>
    private static ContentSection MapToDomain(Persistence.Entities.ContentSectionEntity entity)
    {
        var documentId = DocumentId.From(entity.DocumentId);

        // Determine section type and use appropriate Domain factory method
        var sectionType = MapStringToSectionType(entity.SectionType);

        ContentSection section = sectionType switch
        {
            SectionType.Article => ContentSection.CreateArticle(
                documentId,
                entity.Index,
                entity.Content,
                entity.ArticleNumber ?? "Unknown",
                entity.ArticleTitle),

            SectionType.Paragraph => ContentSection.CreateParagraph(
                documentId,
                entity.Index,
                entity.Content),

            _ => ContentSection.CreateGeneric(
                documentId,
                entity.Index,
                entity.Content)
        };

        // Set embedding if present (should always be present due to WHERE clause)
        if (entity.Embedding != null)
        {
            section.SetEmbedding(entity.Embedding.ToArray());
        }

        return section;
    }

    /// <summary>
    /// Convert string section type to Domain enum
    /// </summary>
    private static SectionType MapStringToSectionType(string type) => type?.ToLowerInvariant() switch
    {
        "article" => SectionType.Article,
        "paragraph" => SectionType.Paragraph,
        _ => SectionType.Generic
    };
}

