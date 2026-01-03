using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;
using KnowledgeSystem.Infrastructure.Persistence.Entities;
using Pgvector;

namespace KnowledgeSystem.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation (Adapter) for KnowledgeDocument aggregate
/// Implements the Port (IDocumentRepository) defined in Application layer
/// 
/// CRITICAL: This is the ONLY place where Domain ↔ Persistence mapping happens
/// Domain entities NEVER leak into Infrastructure
/// Persistence entities NEVER leak outside Infrastructure
/// </summary>
public sealed class DocumentRepository : IDocumentRepository
{
    private readonly KnowledgeDbContext _context;

    public DocumentRepository(KnowledgeDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Save a domain aggregate to persistence
    /// Mapping: Domain → Persistence
    /// </summary>
    public async Task<DocumentId> SaveAsync(
        KnowledgeDocument document,
        CancellationToken cancellationToken = default)
    {
        // MAPPING: Domain → Persistence
        var documentEntity = MapToEntity(document);

        // Check if document already exists (update vs insert)
        var existing = await _context.Documents
            .Include(d => d.Sections)
            .FirstOrDefaultAsync(d => d.Id == documentEntity.Id, cancellationToken);

        if (existing == null)
        {
            // Insert new document
            await _context.Documents.AddAsync(documentEntity, cancellationToken);
        }
        else
        {
            // Update existing document
            _context.Entry(existing).CurrentValues.SetValues(documentEntity);

            // Update sections (remove old, add new)
            _context.Sections.RemoveRange(existing.Sections);
            existing.Sections = documentEntity.Sections;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return DocumentId.From(documentEntity.Id);
    }

    /// <summary>
    /// Retrieve a domain aggregate from persistence
    /// Mapping: Persistence → Domain
    /// </summary>
    public async Task<KnowledgeDocument?> GetByIdAsync(
        DocumentId id,
        CancellationToken cancellationToken = default)
    {
        var documentEntity = await _context.Documents
            .Include(d => d.Sections.OrderBy(s => s.Index))
            .FirstOrDefaultAsync(d => d.Id == id.Value, cancellationToken);

        if (documentEntity == null)
            return null;

        // MAPPING: Persistence → Domain
        return MapToDomain(documentEntity);
    }

    /// <summary>
    /// Check if document exists
    /// </summary>
    public async Task<bool> ExistsAsync(
        DocumentId id,
        CancellationToken cancellationToken = default)
    {
        return await _context.Documents
            .AnyAsync(d => d.Id == id.Value, cancellationToken);
    }

    // ============================================================================
    // MAPPING LOGIC: Domain ↔ Persistence
    // ============================================================================

    /// <summary>
    /// Map Domain aggregate to Persistence entity
    /// Domain → Persistence
    /// </summary>
    private static KnowledgeDocumentEntity MapToEntity(KnowledgeDocument document)
    {
        var documentEntity = new KnowledgeDocumentEntity
        {
            Id = document.Id.Value,
            Title = document.Title,
            UploadedAt = document.UploadedAt,
            Sections = document.Sections.Select(MapSectionToEntity).ToList()
        };

        return documentEntity;
    }

    /// <summary>
    /// Map Domain ContentSection to Persistence entity
    /// Domain → Persistence
    /// </summary>
    private static ContentSectionEntity MapSectionToEntity(ContentSection section)
    {
        return new ContentSectionEntity
        {
            Id = section.Id.Value,
            DocumentId = section.DocumentId.Value,
            Index = section.Index,
            Content = section.Content,
            Embedding = section.HasEmbedding() 
                ? new Vector(section.EmbeddingVector) 
                : null,
            ArticleNumber = section.ArticleNumber,
            ArticleTitle = section.ArticleTitle,
            SectionType = MapSectionTypeToString(section.Type)
        };
    }

    /// <summary>
    /// Map Persistence entity to Domain aggregate
    /// Persistence → Domain
    /// Uses Domain factory method for aggregate reconstitution
    /// </summary>
    private static KnowledgeDocument MapToDomain(KnowledgeDocumentEntity entity)
    {
        var documentId = DocumentId.From(entity.Id);

        // Map sections first
        var sections = entity.Sections
            .OrderBy(s => s.Index)
            .Select(s => MapSectionToDomain(s, documentId))
            .ToList();

        // Use Domain factory method to reconstitute aggregate
        // This ensures all domain invariants are respected
        return KnowledgeDocument.Reconstitute(
            documentId,
            entity.Title,
            entity.UploadedAt,
            sections);
    }

    /// <summary>
    /// Map Persistence entity to Domain ContentSection
    /// Persistence → Domain
    /// Uses Domain factory methods to create sections
    /// </summary>
    private static ContentSection MapSectionToDomain(
        ContentSectionEntity entity,
        DocumentId documentId)
    {
        // Determine section type and use appropriate factory method
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

        // Set embedding if present
        if (entity.Embedding != null)
        {
            section.SetEmbedding(entity.Embedding.ToArray());
        }

        return section;
    }

    // ============================================================================
    // TYPE CONVERTERS
    // ============================================================================

    private static string MapSectionTypeToString(SectionType type) => type switch
    {
        SectionType.Article => "article",
        SectionType.Paragraph => "paragraph",
        SectionType.Generic => "generic",
        _ => "generic"
    };

    private static SectionType MapStringToSectionType(string type) => type?.ToLowerInvariant() switch
    {
        "article" => SectionType.Article,
        "paragraph" => SectionType.Paragraph,
        _ => SectionType.Generic
    };
}

