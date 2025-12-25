using Microsoft.EntityFrameworkCore;
using RAG.Api.Data;
using RAG.Api.Models;
using Npgsql;
using Dapper;
using Pgvector;

namespace RAG.Api.Services;

public class VectorSearchService : IVectorSearchService
{
    private readonly RagDbContext _dbContext;
    private readonly IOllamaEmbeddingService _embeddingService;
    private readonly ILogger<VectorSearchService> _logger;
    private readonly string _connectionString;

    public VectorSearchService(
        RagDbContext dbContext,
        IOllamaEmbeddingService embeddingService,
        ILogger<VectorSearchService> logger,
        IConfiguration configuration)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _logger = logger;
        _connectionString = configuration.GetConnectionString("DefaultConnection") 
            ?? throw new InvalidOperationException("Database connection string not found");
    }

    public async Task<List<SearchResult>> SearchAsync(
        string queryText,
        int topK = 5,
        double similarityThreshold = 0.0,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(queryText))
        {
            throw new ArgumentException("Query text cannot be empty", nameof(queryText));
        }

        if (topK <= 0)
        {
            throw new ArgumentException("topK must be greater than 0", nameof(topK));
        }

        _logger.LogInformation(
            "Starting vector search: query='{Query}', topK={TopK}, threshold={Threshold}",
            queryText.Substring(0, Math.Min(100, queryText.Length)), topK, similarityThreshold
        );

        try
        {
            // 1. Query için embedding üret
            _logger.LogInformation("Step 1: Generating embedding for query...");
            var queryEmbeddingArray = await _embeddingService.GenerateEmbeddingAsync(
                queryText,
                cancellationToken
            );
            _logger.LogInformation("Step 1 COMPLETED: Query embedding generated ({Dimensions} dimensions)",
                queryEmbeddingArray.Length);

            // 2. NpgsqlCommand ile pgvector cosine similarity search
            _logger.LogInformation("Step 2: Executing vector similarity search...");
            
            // float[] array -> pgvector format: '[0.1,0.2,...]'
            var vectorArray = string.Join(",", queryEmbeddingArray.Select(f => 
                f.ToString("G", System.Globalization.CultureInfo.InvariantCulture)));
            var vectorLiteral = "[" + vectorArray + "]";
            
            _logger.LogDebug("Query vector prepared with {Dims} dimensions", queryEmbeddingArray.Length);

            // L2 distance works (<->), cosine distance (<=>)  has issues with Npgsql
            var sql = "SELECT c.id, c.document_id, c.chunk_index, c.content, d.filename as document_title, (c.embedding <-> CAST(@embedding AS vector(768))) as distance FROM chunks c INNER JOIN documents d ON c.document_id = d.id WHERE c.embedding IS NOT NULL ORDER BY c.embedding <-> CAST(@embedding AS vector(768)) LIMIT @limit";

            List<ChunkSearchResult> results = new();
            await using (var connection = new NpgsqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken);
                _logger.LogInformation("Database connection opened, executing query...");
                
                try
                {
                    // NpgsqlCommand ile manuel parameter binding
                    await using var command = new NpgsqlCommand(sql, connection);
                    
                    // Parametreleri explicit type ile ekle
                    command.Parameters.Add(new NpgsqlParameter("@embedding", NpgsqlTypes.NpgsqlDbType.Text) { Value = vectorLiteral });
                    command.Parameters.Add(new NpgsqlParameter("@limit", NpgsqlTypes.NpgsqlDbType.Integer) { Value = topK * 2 });
                    command.CommandTimeout = 30;
                    
                    _logger.LogInformation("🔍 Vector literal (first 150 chars): {Preview}...", 
                        vectorLiteral.Substring(0, Math.Min(150, vectorLiteral.Length)));
                    _logger.LogInformation("🔍 Executing SQL with {ParamCount} parameters", command.Parameters.Count);
                    
                    await using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        results.Add(new ChunkSearchResult
                        {
                            Id = reader.GetGuid(0),
                            DocumentId = reader.GetGuid(1),
                            ChunkIndex = reader.GetInt32(2),
                            Content = reader.GetString(3),
                            DocumentTitle = reader.GetString(4),
                            Distance = reader.GetDouble(5)
                        });
                    }
                    
                    _logger.LogInformation("✅ Query executed successfully, returned {Count} rows", results.Count);
                    
                    // Debug: İlk sonucun distance'ını logla
                    if (results.Any())
                    {
                        _logger.LogInformation("🎯 Top result distance: {Distance:F4}", results.First().Distance);
                    }
                }
                catch (Npgsql.PostgresException pgEx)
                {
                    _logger.LogError(pgEx, "❌ PostgreSQL error during vector search: {Message}", pgEx.MessageText);
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error executing query with NpgsqlCommand");
                    throw;
                }
            }

            _logger.LogInformation("Step 2 COMPLETED: Found {Count} candidate chunks from database",
                results.Count);

            if (results.Count == 0)
            {
                var totalChunks = await _dbContext.Chunks.CountAsync(cancellationToken);
                _logger.LogWarning("No results from vector search, but {Total} total chunks exist in database", 
                    totalChunks);
            }

            // 3. Similarity score hesapla ve filtrele
            _logger.LogInformation("Step 3: Computing similarity scores and filtering...");
            var searchResults = results
                .Select(r => new SearchResult
                {
                    ChunkId = r.Id,
                    DocumentId = r.DocumentId,
                    DocumentTitle = r.DocumentTitle,
                    ChunkIndex = r.ChunkIndex,
                    Content = r.Content,
                    // L2 distance: smaller is better
                    // Convert to similarity score: similarity = 1 / (1 + distance)
                    SimilarityScore = 1.0 / (1.0 + r.Distance)
                })
                .Where(r => r.SimilarityScore >= similarityThreshold)
                .OrderByDescending(r => r.SimilarityScore)
                .Take(topK)
                .ToList();

            _logger.LogInformation(
                "Step 3 COMPLETED: Returning {Count} results (threshold: {Threshold})",
                searchResults.Count, similarityThreshold
            );

            // 4. Log results summary
            if (searchResults.Any())
            {
                _logger.LogInformation(
                    "✅ SEARCH COMPLETED - Top result: score={TopScore:F4}, doc='{DocTitle}'",
                    searchResults.First().SimilarityScore,
                    searchResults.First().DocumentTitle
                );

                foreach (var result in searchResults)
                {
                    _logger.LogDebug(
                        "Result #{Index}: score={Score:F4}, chunk={ChunkId}, doc='{Doc}'",
                        searchResults.IndexOf(result) + 1,
                        result.SimilarityScore,
                        result.ChunkId,
                        result.DocumentTitle
                    );
                }
            }
            else
            {
                _logger.LogWarning(
                    "⚠️ SEARCH COMPLETED - No results found above threshold {Threshold}",
                    similarityThreshold
                );
            }

            return searchResults;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ SEARCH FAILED for query: '{Query}'",
                queryText.Substring(0, Math.Min(100, queryText.Length)));
            throw new InvalidOperationException($"Vector search failed: {ex.Message}", ex);
        }
    }
}

/// <summary>
/// Dapper result mapping
/// </summary>
internal class ChunkSearchResult
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public string Content { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public double Distance { get; set; }
}
