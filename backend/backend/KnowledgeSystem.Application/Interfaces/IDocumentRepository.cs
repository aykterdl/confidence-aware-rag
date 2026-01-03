using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.Interfaces;

/// <summary>
/// Port: Repository for persisting KnowledgeDocument aggregates
/// Implementation: Infrastructure layer (EF Core, Dapper, etc.)
/// </summary>
public interface IDocumentRepository
{
    /// <summary>
    /// Save a new document with its sections
    /// </summary>
    Task<DocumentId> SaveAsync(KnowledgeDocument document, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieve a document by its ID
    /// </summary>
    Task<KnowledgeDocument?> GetByIdAsync(DocumentId id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a document exists
    /// </summary>
    Task<bool> ExistsAsync(DocumentId id, CancellationToken cancellationToken = default);
}

