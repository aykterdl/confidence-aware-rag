namespace KnowledgeSystem.Application.UseCases.IngestDocument;

/// <summary>
/// Result of document ingestion
/// Returns document ID and ingestion statistics
/// </summary>
public sealed class IngestDocumentResult
{
    public required Guid DocumentId { get; init; }
    public required int SectionsCreated { get; init; }
    public required string Title { get; init; }
}

