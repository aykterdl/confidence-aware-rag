using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Api.Data;
using KnowledgeSystem.Api.Configuration;
using KnowledgeSystem.Api.Services;
using KnowledgeSystem.Infrastructure.Configuration;
using KnowledgeSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CLEAN ARCHITECTURE INFRASTRUCTURE REGISTRATION
// ============================================================================
// This registers:
// - KnowledgeDbContext (EF Core with PostgreSQL + pgvector)
// - IDocumentRepository → DocumentRepository
// - IVectorSearchEngine → PgVectorSearchEngine
// - IEmbeddingGenerator → OllamaEmbeddingGenerator
// - ILanguageModel → OllamaLanguageModel
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================================
// LEGACY SERVICES (KnowledgeSystem.Api services - will be phased out)
// ============================================================================
// PostgreSQL + pgvector bağlantısı (legacy DbContext)
// NOTE: Uses same connection string as Clean Architecture (KnowledgeDb)
// Legacy context does NOT own schema or migrations
builder.Services.AddDbContext<RagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("KnowledgeDb"),
        o => o.UseVector()
    )
);

// Ollama ayarları
builder.Services.Configure<OllamaSettings>(
    builder.Configuration.GetSection(OllamaSettings.SectionName)
);

// RAG Confidence ayarları
builder.Services.Configure<RagConfidenceSettings>(
    builder.Configuration.GetSection(RagConfidenceSettings.SectionName)
);

// Ollama HttpClient - Embedding (legacy)
builder.Services.AddHttpClient<IOllamaEmbeddingService, OllamaEmbeddingService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// Ollama HttpClient - LLM (legacy)
builder.Services.AddHttpClient(nameof(IOllamaLlmService), (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // LLM generation uzun sürebilir
});

// Ollama LLM Service (legacy)
builder.Services.AddScoped<IOllamaLlmService, OllamaLlmService>();

// Text Chunking Service (legacy)
builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();

// Chunk Ingestion Service (legacy)
builder.Services.AddScoped<IChunkIngestionService, ChunkIngestionService>();

// Vector Search Service (legacy)
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();

// Conversation Store (legacy - Singleton)
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();

// RAG Answer Service (legacy)
builder.Services.AddScoped<IRagAnswerService, RagAnswerService>();

// PDF Reader Service (legacy)
builder.Services.AddScoped<IPdfReaderService, PdfReaderService>();

// ============================================================================
// CORE SERVICES
// ============================================================================
builder.Services.AddOpenApi();

// CORS - Frontend'den direkt istek alabilmek için
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:3000", "http://localhost:3001")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

// ============================================================================
// DATABASE AUTO-MIGRATION ON STARTUP
// ============================================================================
// Apply EF Core migrations automatically on startup
// KnowledgeDbContext is the SINGLE source of truth for schema management
using (var scope = app.Services.CreateScope())
{
    try
    {
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        await knowledgeDb.Database.MigrateAsync();
        
        Console.WriteLine("✅ Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Failed to apply database migrations: {ex.Message}");
        throw; // Fail fast - database schema issues must be resolved before startup
    }
}

// ============================================================================
// MIDDLEWARE
// ============================================================================
// CORS middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// ============================================================================
// ENDPOINTS (LEGACY - will be migrated to Clean Architecture controllers)
// ============================================================================

// Health check endpoint
app.MapGet("/health", async (RagDbContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", database = "connected" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Database error: {ex.Message}");
    }
});

// Test endpoint - döküman sayısı
app.MapGet("/api/documents/count", async (RagDbContext db) =>
{
    var count = await db.Documents.CountAsync();
    return Results.Ok(new { totalDocuments = count });
});

// Test endpoint - embedding servisi
app.MapPost("/api/embedding/test", async (IOllamaEmbeddingService embeddingService, TestEmbeddingRequest request) =>
{
    try
    {
        var embedding = await embeddingService.GenerateEmbeddingAsync(request.Text);
        
        return Results.Ok(new
        {
            text = request.Text,
            embeddingLength = embedding.Length,
            firstFiveValues = embedding.Take(5).ToArray(),
            lastFiveValues = embedding.TakeLast(5).ToArray()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

// Test endpoint - text chunking servisi
app.MapPost("/api/chunking/test", (ITextChunkingService chunkingService, TestChunkingRequest request) =>
{
    try
    {
        var chunks = chunkingService.ChunkText(
            request.Text,
            request.MaxChunkSize ?? 500,
            request.Overlap ?? 50
        );

        return Results.Ok(new
        {
            originalLength = request.Text.Length,
            chunkCount = chunks.Count,
            maxChunkSize = request.MaxChunkSize ?? 500,
            overlap = request.Overlap ?? 50,
            chunks = chunks.Select((chunk, index) => new
            {
                index = index + 1,
                length = chunk.Length,
                preview = chunk.Length > 50 ? chunk.Substring(0, 50) + "..." : chunk,
                fullText = chunk
            }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Error: {ex.Message}");
    }
});

// Ingestion endpoint - metni chunk'lara böl, embedding üret ve kaydet
app.MapPost("/api/ingest/text", async (
    IChunkIngestionService ingestionService,
    IngestTextRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var result = await ingestionService.IngestTextAsync(
            request.Text,
            request.Title,
            request.Metadata,
            cancellationToken
        );

        return Results.Ok(new
        {
            success = true,
            documentId = result.DocumentId,
            documentTitle = result.DocumentTitle,
            chunkCount = result.ChunkCount,
            message = $"Successfully ingested document with {result.ChunkCount} chunks"
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ingestion error: {ex.Message}");
    }
});

// PDF Upload endpoint - PDF dosyasından metin çıkar ve sisteme yükle
app.MapPost("/api/ingest/pdf", async (
    HttpRequest request,
    IPdfReaderService pdfReader,
    IChunkIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Multipart form-data kontrolü
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Request must be multipart/form-data");
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest("No file uploaded. Use 'file' as the field name.");
        }

        // Sadece PDF kabul et
        if (!file.ContentType.Contains("pdf") && !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest("Only PDF files are supported");
        }

        // Dosya boyutu kontrolü (max 50MB)
        if (file.Length > 50 * 1024 * 1024)
        {
            return Results.BadRequest("File size must be less than 50MB");
        }

        // PDF'den metin çıkar
        string extractedText;
        await using (var stream = file.OpenReadStream())
        {
            extractedText = await pdfReader.ExtractTextAsync(stream, cancellationToken);
        }

        if (string.IsNullOrWhiteSpace(extractedText))
        {
            return Results.BadRequest("No text could be extracted from the PDF");
        }

        // Dosya adını başlık olarak kullan (uzantıyı kaldır)
        var title = form["title"].FirstOrDefault() 
            ?? System.IO.Path.GetFileNameWithoutExtension(file.FileName);

        // Metadata ekle (JSON string olarak)
        var metadata = System.Text.Json.JsonSerializer.Serialize(new
        {
            source = "pdf_upload",
            original_filename = file.FileName,
            file_size_bytes = file.Length,
            upload_date = DateTime.UtcNow
        });

        // Sisteme yükle (chunk + embed + store)
        var result = await ingestionService.IngestTextAsync(
            extractedText,
            title,
            metadata,
            cancellationToken
        );

        return Results.Ok(new
        {
            success = true,
            documentId = result.DocumentId,
            documentTitle = result.DocumentTitle,
            chunkCount = result.ChunkCount,
            extractedTextLength = extractedText.Length,
            message = $"PDF başarıyla yüklendi: {result.ChunkCount} chunk oluşturuldu"
        });
    }
    catch (InvalidOperationException ex)
    {
        return Results.BadRequest(ex.Message);
    }
    catch (Exception ex)
    {
        return Results.Problem($"PDF upload error: {ex.Message}");
    }
}).DisableAntiforgery(); // File upload için CSRF kontrolünü devre dışı bırak

// Search endpoint - vector similarity search
app.MapPost("/api/search", async (
    IVectorSearchService searchService,
    SearchRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var results = await searchService.SearchAsync(
            request.Query,
            request.TopK ?? 5,
            request.SimilarityThreshold ?? 0.0,
            cancellationToken
        );

        return Results.Ok(new
        {
            query = request.Query,
            resultCount = results.Count,
            topK = request.TopK ?? 5,
            similarityThreshold = request.SimilarityThreshold ?? 0.0,
            results = results.Select(r => new
            {
                chunkId = r.ChunkId,
                documentId = r.DocumentId,
                documentTitle = r.DocumentTitle,
                chunkIndex = r.ChunkIndex,
                content = r.Content,
                similarityScore = Math.Round(r.SimilarityScore, 4),
                preview = r.Content.Length > 200 ? r.Content.Substring(0, 200) + "..." : r.Content
            }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Search error: {ex.Message}");
    }
});

// RAG Ask endpoint - Vector search + LLM answer generation
app.MapPost("/api/rag/ask", async (
    IRagAnswerService ragService,
    KnowledgeSystem.Api.Models.RagRequest request,
    CancellationToken cancellationToken) =>
{
    try
    {
        var response = await ragService.AskAsync(request, cancellationToken);

        return Results.Ok(new
        {
            question = response.Question,
            answer = response.Answer,
            conversationId = response.ConversationId,
            language = response.Language,
            confidence = new
            {
                level = response.Confidence.Level,
                maxSimilarity = response.Confidence.MaxSimilarity,
                averageSimilarity = response.Confidence.AverageSimilarity,
                explanation = response.Confidence.Explanation
            },
            sourceCount = response.SourceCount,
            averageSimilarity = response.AverageSimilarity, // backward compatibility
            sources = response.Sources.Select(s => new
            {
                chunkId = s.ChunkId,
                documentId = s.DocumentId,
                documentTitle = s.DocumentTitle,
                chunkIndex = s.ChunkIndex,
                similarityScore = Math.Round(s.SimilarityScore, 4),
                contentPreview = s.ContentPreview
            }).ToList()
        });
    }
    catch (Exception ex)
    {
        return Results.Problem($"RAG error: {ex.Message}");
    }
});

// ============================================================================
// CLEAN ARCHITECTURE ENDPOINTS
// ============================================================================

// Document Ingestion (Clean Architecture) - POST /api/documents/ingest
app.MapPost("/api/documents/ingest", async (
    HttpRequest request,
    KnowledgeSystem.Application.Services.IDocumentIngestionService ingestionService,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Multipart form-data kontrolü
        if (!request.HasFormContentType)
        {
            return Results.BadRequest(new { error = "Request must be multipart/form-data" });
        }

        var form = await request.ReadFormAsync(cancellationToken);
        var file = form.Files.GetFile("file");

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new { error = "No file uploaded. Use 'file' as the field name." });
        }

        // Sadece PDF kabul et
        if (!file.ContentType.Contains("pdf", StringComparison.OrdinalIgnoreCase) && 
            !file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new { error = "Only PDF files are supported" });
        }

        // Dosya boyutu kontrolü (max 50MB)
        if (file.Length > 50 * 1024 * 1024)
        {
            return Results.BadRequest(new { error = "File size must be less than 50MB" });
        }

        // Dosya adını başlık olarak kullan (uzantıyı kaldır)
        var title = form["title"].FirstOrDefault() 
            ?? System.IO.Path.GetFileNameWithoutExtension(file.FileName);

        Console.WriteLine($"📄 Starting ingestion: {title} ({file.Length} bytes)");

        // Clean Architecture ingestion pipeline
        await using var stream = file.OpenReadStream();
        var result = await ingestionService.IngestAsync(
            stream,
            file.FileName,
            file.ContentType ?? "application/pdf",
            title,
            cancellationToken
        );

        Console.WriteLine($"✅ Ingestion completed: {result.ChunkCount} chunks created");

        return Results.Ok(new
        {
            success = true,
            documentId = result.DocumentId.Value.ToString(),
            title = result.Title,
            chunkCount = result.ChunkCount,
            characterCount = result.CharacterCount,
            pageCount = result.PageCount,
            message = $"Document successfully ingested with {result.ChunkCount} semantic chunks"
        });
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"❌ Ingestion error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Unexpected error: {ex.Message}");
        return Results.Problem($"Document ingestion failed: {ex.Message}");
    }
}).DisableAntiforgery(); // File upload için CSRF kontrolünü devre dışı bırak

// Semantic Search (Clean Architecture - Phase 4 Step 1) - POST /api/query/semantic-search
app.MapPost("/api/query/semantic-search", async (
    SemanticSearchRequest request,
    KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchHandler handler,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty" });
        }

        if (request.TopK is < 1 or > 50)
        {
            return Results.BadRequest(new { error = "TopK must be between 1 and 50" });
        }

        var query = new KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchQuery
        {
            Query = request.Query,
            TopK = request.TopK ?? 5,
            DocumentId = request.DocumentId
        };

        Console.WriteLine($"🔍 Semantic search: \"{query.Query}\" (topK={query.TopK})");

        var result = await handler.HandleAsync(query, cancellationToken);

        Console.WriteLine($"✅ Search completed: {result.TotalMatches} matches found");

        return Results.Ok(new
        {
            query = result.Query,
            totalMatches = result.TotalMatches,
            results = result.Results.Select(r => new
            {
                chunkId = r.ChunkId,
                documentId = r.DocumentId,
                documentTitle = r.DocumentTitle,
                content = r.Content,
                similarityScore = r.SimilarityScore,
                sourcePageNumbers = r.SourcePageNumbers,
                sectionType = r.SectionType,
                articleNumber = r.ArticleNumber,
                articleTitle = r.ArticleTitle
            })
        });
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"❌ Search error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Unexpected search error: {ex.Message}");
        return Results.Problem($"Semantic search failed: {ex.Message}");
    }
});

// Unified Answer Endpoint (Clean Architecture - Phase 4 Step 3) - POST /api/query/answer
app.MapPost("/api/query/answer", async (
    AnswerQueryRequest request,
    KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchHandler searchHandler,
    KnowledgeSystem.Application.UseCases.Prompting.ComposePromptHandler promptHandler,
    KnowledgeSystem.Application.UseCases.GenerateAnswer.GenerateAnswerHandler answerHandler,
    CancellationToken cancellationToken) =>
{
    try
    {
        // Validation
        if (string.IsNullOrWhiteSpace(request.Query))
        {
            return Results.BadRequest(new { error = "Query cannot be empty" });
        }

        if (request.TopK is < 1 or > 50)
        {
            return Results.BadRequest(new { error = "TopK must be between 1 and 50" });
        }

        Console.WriteLine($"💬 RAG Question: \"{request.Query}\" (topK={request.TopK ?? 5})");

        // STEP 1: Semantic Search (retrieve relevant chunks)
        var searchQuery = new KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchQuery
        {
            Query = request.Query,
            TopK = request.TopK ?? 5,
            DocumentId = request.DocumentId
        };

        var searchResult = await searchHandler.HandleAsync(searchQuery, cancellationToken);
        Console.WriteLine($"  ✓ Retrieved {searchResult.TotalMatches} relevant chunks");

        // STEP 2: Compose Prompt (build structured LLM prompt)
        var composeCommand = new KnowledgeSystem.Application.UseCases.Prompting.ComposePromptCommand
        {
            Query = request.Query,
            RetrievedChunks = searchResult.Results,
            Language = request.Language
        };

        var composedPrompt = promptHandler.Handle(composeCommand);
        Console.WriteLine($"  ✓ Prompt composed ({composedPrompt.SourceCount} sources, {composedPrompt.EstimatedTokenCount} tokens)");

        // STEP 3: Generate Answer (invoke LLM with composed prompt)
        var generateCommand = new KnowledgeSystem.Application.UseCases.GenerateAnswer.GenerateAnswerCommand
        {
            Prompt = composedPrompt
        };

        var answerResult = await answerHandler.HandleAsync(generateCommand, cancellationToken);
        Console.WriteLine($"  ✓ Answer generated (confidence: {answerResult.ConfidenceLevel}, LLM invoked: {answerResult.LlmInvoked})");

        // Return structured response
        return Results.Ok(new
        {
            answer = answerResult.Answer,
            sources = answerResult.Sources.Select(s => new
            {
                chunkId = s.ChunkId.ToString(),
                documentId = s.DocumentId.ToString(),
                documentTitle = s.DocumentTitle,
                content = s.Content,
                similarityScore = s.SimilarityScore,
                sectionType = s.SectionType,
                articleNumber = s.ArticleNumber,
                articleTitle = s.ArticleTitle
            }),
            confidence = answerResult.ConfidenceLevel.ToString().ToLower(),
            confidenceExplanation = answerResult.ConfidenceExplanation,
            sourceCount = answerResult.SourceCount,
            llmInvoked = answerResult.LlmInvoked
        });
    }
    catch (InvalidOperationException ex)
    {
        Console.WriteLine($"❌ Answer generation error: {ex.Message}");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ Unexpected error: {ex.Message}");
        return Results.Problem($"Answer generation failed: {ex.Message}");
    }
});

app.Run();

// ============================================================================
// REQUEST/RESPONSE MODELS
// ============================================================================
record TestEmbeddingRequest(string Text);
record TestChunkingRequest(string Text, int? MaxChunkSize, int? Overlap);
record IngestTextRequest(string Text, string Title, string? Metadata);
record SearchRequest(string Query, int? TopK, double? SimilarityThreshold);
record SemanticSearchRequest(string Query, int? TopK, string? DocumentId);
record AnswerQueryRequest(string Query, int? TopK, string? DocumentId, string? Language);
