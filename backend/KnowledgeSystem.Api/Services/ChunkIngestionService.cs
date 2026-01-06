using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Api.Data;
using KnowledgeSystem.Api.Models;
using Pgvector;

namespace KnowledgeSystem.Api.Services;

public class ChunkIngestionService : IChunkIngestionService
{
    private readonly RagDbContext _dbContext;
    private readonly ITextChunkingService _chunkingService;
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly ILogger<ChunkIngestionService> _logger;

    public ChunkIngestionService(
        RagDbContext dbContext,
        ITextChunkingService chunkingService,
        IOllamaEmbeddingService embeddingService,
        ILogger<ChunkIngestionService> logger)
    {
        _dbContext = dbContext;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _logger = logger;
    }

    public async Task<ChunkIngestionResult> IngestTextAsync(
        string text,
        string documentTitle,
        string? metadata = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            throw new ArgumentException("Text cannot be empty", nameof(text));
        }

        if (string.IsNullOrWhiteSpace(documentTitle))
        {
            throw new ArgumentException("Document title cannot be empty", nameof(documentTitle));
        }

        _logger.LogInformation("Starting text ingestion for document: {Title}, text length: {Length}", 
            documentTitle, text.Length);

        // Transaction başlat
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // 1. Document oluştur
            var document = new Document
            {
                Id = Guid.NewGuid(),
                Filename = documentTitle,
                FilePath = null, // PDF değil, direkt text
                UploadDate = DateTime.UtcNow,
                Metadata = metadata,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Documents.Add(document);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Document created with ID: {DocumentId}", document.Id);

            // 2. Text'i SEMANTIC-AWARE chunk'lara böl
            _logger.LogInformation("Step 2: Starting SEMANTIC text chunking...");
            var chunkResults = _chunkingService.ChunkTextWithMetadata(text, maxChunkSize: 500, overlap: 50);
            _logger.LogInformation("Step 2 COMPLETED: Text split into {Count} chunks", chunkResults.Count);

            if (chunkResults.Count == 0)
            {
                _logger.LogWarning("No chunks created from text. Aborting ingestion.");
                throw new InvalidOperationException("Text chunking produced zero chunks");
            }

            // Log chunk type distribution
            var typeDistribution = chunkResults
                .GroupBy(c => c.Metadata.ChunkType)
                .Select(g => $"{g.Key}: {g.Count()}")
                .ToList();
            _logger.LogInformation("Chunk type distribution: {Distribution}", string.Join(", ", typeDistribution));

            // 3. Her chunk için embedding üret ve kaydet
            _logger.LogInformation("Step 3: Starting embedding generation for {Count} chunks...", chunkResults.Count);
            var chunkEntities = new List<Chunk>();

            for (int i = 0; i < chunkResults.Count; i++)
            {
                var chunkResult = chunkResults[i];
                var chunkText = chunkResult.Content;
                var chunkMeta = chunkResult.Metadata;
                var chunkId = Guid.NewGuid();
                
                var metaInfo = chunkMeta.ChunkType == "article" 
                    ? $"Article {chunkMeta.ArticleNumber}: {chunkMeta.ArticleTitle}" 
                    : chunkMeta.ChunkType;
                
                _logger.LogInformation("Step 3.{Index}: Processing chunk {Index}/{Total} [{Type}] (ID: {ChunkId}, Length: {Length})", 
                    i + 1, i + 1, chunkResults.Count, metaInfo, chunkId, chunkText.Length);

                try
                {
                    // Embedding üret
                    _logger.LogDebug("Calling embedding service for chunk {Index}...", i + 1);
                    var embeddingArray = await _embeddingService.GenerateEmbeddingAsync(
                        chunkText, 
                        cancellationToken
                    );
                    _logger.LogInformation("Embedding generated successfully for chunk {Index} (Dimensions: {Dim})", 
                        i + 1, embeddingArray.Length);

                    // Pgvector formatına çevir
                    var embedding = new Vector(embeddingArray);
                    _logger.LogDebug("Converted embedding to Vector type for chunk {Index}", i + 1);

                    // Chunk entity oluştur (METADATA İLE)
                    var chunk = new Chunk
                    {
                        Id = chunkId,
                        DocumentId = document.Id,
                        ChunkIndex = i,
                        Content = chunkText,
                        Embedding = embedding,
                        TokenCount = EstimateTokenCount(chunkText),
                        CreatedAt = DateTime.UtcNow,
                        // NEW: Semantic metadata
                        ArticleNumber = chunkMeta.ArticleNumber,
                        ArticleTitle = chunkMeta.ArticleTitle,
                        ChunkType = chunkMeta.ChunkType
                    };

                    chunkEntities.Add(chunk);
                    _logger.LogInformation("Chunk entity created and added to list: {Index}/{Total}", i + 1, chunkResults.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to process chunk {Index}/{Total}. Chunk text: {Text}", 
                        i + 1, chunkResults.Count, chunkText.Substring(0, Math.Min(100, chunkText.Length)));
                    throw;
                }
            }

            _logger.LogInformation("Step 3 COMPLETED: All {Count} embeddings generated successfully", chunkEntities.Count);

            // 4. Tüm chunk'ları kaydet
            _logger.LogInformation("Step 4: Saving {Count} chunks to database...", chunkEntities.Count);
            _dbContext.Chunks.AddRange(chunkEntities);
            _logger.LogDebug("Chunks added to DbContext. Calling SaveChangesAsync...");
            
            var savedChunks = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Step 4 COMPLETED: {SavedCount} chunk records saved to database", savedChunks);

            // 5. Document'ın chunk sayısını güncelle
            _logger.LogInformation("Step 5: Updating document total_chunks to {Count}...", chunkResults.Count);
            document.TotalChunks = chunkResults.Count;
            var updatedRows = await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Step 5 COMPLETED: Document updated ({UpdatedRows} rows affected)", updatedRows);

            // Transaction commit
            _logger.LogInformation("Step 6: Committing transaction...");
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation("Step 6 COMPLETED: Transaction committed successfully");

            _logger.LogInformation(
                "✅ TEXT INGESTION COMPLETED SUCCESSFULLY - Document ID: {DocumentId}, Chunks: {ChunkCount}",
                document.Id, chunkResults.Count
            );

            return new ChunkIngestionResult(
                document.Id,
                chunkResults.Count,
                documentTitle
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, 
                "❌ INGESTION FAILED for document: {Title}. Error: {Message}. Rolling back transaction...", 
                documentTitle, ex.Message);
            
            try
            {
                await transaction.RollbackAsync(cancellationToken);
                _logger.LogWarning("Transaction rolled back successfully");
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Failed to rollback transaction");
            }
            
            throw new InvalidOperationException(
                $"Text ingestion failed for document '{documentTitle}': {ex.Message}", 
                ex
            );
        }
    }

    /// <summary>
    /// Token sayısını tahmin eder (yaklaşık: kelime sayısı * 1.3)
    /// </summary>
    private int EstimateTokenCount(string text)
    {
        var wordCount = text.Split(new[] { ' ', '\n', '\r', '\t' }, 
            StringSplitOptions.RemoveEmptyEntries).Length;
        return (int)(wordCount * 1.3);
    }
}

