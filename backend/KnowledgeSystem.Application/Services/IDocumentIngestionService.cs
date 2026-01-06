using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Application.Services;

/// <summary>
/// Port: Service orchestrating the full document ingestion pipeline
/// Implementation: Infrastructure layer
/// 
/// Pipeline:
/// 1. Extract text from document
/// 2. Chunk text semantically
/// 3. Generate embeddings for each chunk
/// 4. Persist chunks with vectors to database
/// </summary>
public interface IDocumentIngestionService
{
    /// <summary>
    /// Ingest a document into the knowledge system
    /// </summary>
    /// <param name="documentStream">Document content stream (PDF, etc.)</param>
    /// <param name="fileName">Original file name</param>
    /// <param name="contentType">MIME type (e.g., "application/pdf")</param>
    /// <param name="title">Document title (optional, defaults to fileName)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Document ID and ingestion statistics</returns>
    /// <exception cref="InvalidOperationException">If ingestion fails at any stage</exception>
    Task<DocumentIngestionResult> IngestAsync(
        Stream documentStream,
        string fileName,
        string contentType,
        string? title = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Result of document ingestion operation
/// </summary>
public sealed class DocumentIngestionResult
{
    /// <summary>
    /// ID of the ingested document
    /// </summary>
    public required DocumentId DocumentId { get; init; }

    /// <summary>
    /// Document title
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Number of chunks created
    /// </summary>
    public required int ChunkCount { get; init; }

    /// <summary>
    /// Total characters in extracted text
    /// </summary>
    public required int CharacterCount { get; init; }

    /// <summary>
    /// Total pages in document (if applicable)
    /// </summary>
    public int? PageCount { get; init; }
}

