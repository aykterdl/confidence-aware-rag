using Microsoft.Extensions.Logging;
using KnowledgeSystem.Application.Services;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Application.Abstractions.TextExtraction;
using KnowledgeSystem.Application.Abstractions.Chunking;
using KnowledgeSystem.Domain.Entities;
using KnowledgeSystem.Domain.ValueObjects;

namespace KnowledgeSystem.Infrastructure.Services;

/// <summary>
/// Service orchestrating the full document ingestion pipeline
/// 
/// Pipeline:
/// 1. Extract text from document (ITextExtractor)
/// 2. Chunk text semantically (IChunkingStrategy)
/// 3. Generate embeddings for each chunk (IEmbeddingGenerator)
/// 4. Persist document with chunks/vectors (IDocumentRepository)
/// 
/// All operations are transactional - if any step fails, nothing is persisted
/// </summary>
public sealed class DocumentIngestionService : IDocumentIngestionService
{
    private readonly ITextExtractor _textExtractor;
    private readonly IChunkingStrategy _chunkingStrategy;
    private readonly IEmbeddingGenerator _embeddingGenerator;
    private readonly IDocumentRepository _documentRepository;
    private readonly ILogger<DocumentIngestionService> _logger;

    public DocumentIngestionService(
        ITextExtractor textExtractor,
        IChunkingStrategy chunkingStrategy,
        IEmbeddingGenerator embeddingGenerator,
        IDocumentRepository documentRepository,
        ILogger<DocumentIngestionService> logger)
    {
        _textExtractor = textExtractor ?? throw new ArgumentNullException(nameof(textExtractor));
        _chunkingStrategy = chunkingStrategy ?? throw new ArgumentNullException(nameof(chunkingStrategy));
        _embeddingGenerator = embeddingGenerator ?? throw new ArgumentNullException(nameof(embeddingGenerator));
        _documentRepository = documentRepository ?? throw new ArgumentNullException(nameof(documentRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<DocumentIngestionResult> IngestAsync(
        Stream documentStream,
        string fileName,
        string contentType,
        string? title = null,
        CancellationToken cancellationToken = default)
    {
        if (documentStream == null)
            throw new ArgumentNullException(nameof(documentStream));

        if (string.IsNullOrWhiteSpace(fileName))
            throw new ArgumentException("File name is required", nameof(fileName));

        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentException("Content type is required", nameof(contentType));

        var documentTitle = string.IsNullOrWhiteSpace(title)
            ? Path.GetFileNameWithoutExtension(fileName)
            : title;

        _logger.LogInformation(
            "Starting document ingestion: {Title} ({FileName}, {ContentType})",
            documentTitle, fileName, contentType);

        try
        {
            // STEP 1: Extract text from document
            _logger.LogInformation("STEP 1/4: Extracting text...");
            var extractionResult = await _textExtractor.ExtractTextAsync(
                documentStream,
                contentType,
                cancellationToken);

            _logger.LogInformation(
                "Text extraction complete. Pages: {PageCount}, Characters: {CharCount}",
                extractionResult.PageCount, extractionResult.CharacterCount);

            // STEP 2: Chunk text semantically
            _logger.LogInformation("STEP 2/4: Chunking text semantically...");
            var chunks = await _chunkingStrategy.ChunkAsync(
                extractionResult.Text,
                maxChunkSize: 1000,
                overlapSize: 200,
                cancellationToken);

            _logger.LogInformation(
                "Chunking complete. Created {ChunkCount} chunks",
                chunks.Count);

            if (chunks.Count == 0)
            {
                throw new InvalidOperationException(
                    "No chunks were created from the document. Text extraction may have failed.");
            }

            // STEP 3: Create Domain aggregate
            _logger.LogInformation("STEP 3/4: Creating domain aggregate and generating embeddings...");
            var document = KnowledgeDocument.Create(documentTitle);

            var contentSections = new List<ContentSection>();

            for (int i = 0; i < chunks.Count; i++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunk = chunks[i];
                
                _logger.LogDebug(
                    "Processing chunk {Index}/{Total} ({Length} chars)",
                    i + 1, chunks.Count, chunk.Content.Length);

                // Generate embedding for this chunk
                var embedding = await _embeddingGenerator.GenerateEmbeddingAsync(
                    chunk.Content,
                    cancellationToken);

                // Create ContentSection (Domain entity)
                var section = ContentSection.CreateGeneric(
                    document.Id,
                    i,
                    chunk.Content);

                section.SetEmbedding(embedding);

                contentSections.Add(section);
            }

            document.AddSections(contentSections);

            _logger.LogInformation(
                "Embeddings generated for all {ChunkCount} chunks. Total embedding dimensions: {DimCount}",
                chunks.Count, contentSections.First().EmbeddingVector?.Length ?? 0);

            // STEP 4: Persist to database (transactional)
            _logger.LogInformation("STEP 4/4: Persisting document and chunks to database...");
            var documentId = await _documentRepository.SaveAsync(document, cancellationToken);

            _logger.LogInformation(
                "✅ Document ingestion completed successfully. DocumentId: {DocumentId}, Chunks: {ChunkCount}",
                documentId.Value, chunks.Count);

            return new DocumentIngestionResult
            {
                DocumentId = documentId,
                Title = documentTitle,
                ChunkCount = chunks.Count,
                CharacterCount = extractionResult.CharacterCount,
                PageCount = extractionResult.PageCount
            };
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex,
                "❌ Document ingestion failed for {Title}. Error: {ErrorMessage}",
                documentTitle, ex.Message);

            throw new InvalidOperationException(
                $"Document ingestion failed: {ex.Message}", ex);
        }
    }
}

