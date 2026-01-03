namespace KnowledgeSystem.Application.UseCases.IngestDocument;

/// <summary>
/// Command: Ingest a new document into the knowledge system
/// Contains raw document content and metadata
/// </summary>
public sealed class IngestDocumentCommand
{
    public required string Title { get; init; }
    public required string Content { get; init; }
}

