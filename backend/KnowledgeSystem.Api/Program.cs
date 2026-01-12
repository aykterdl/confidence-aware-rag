using Microsoft.EntityFrameworkCore;
using KnowledgeSystem.Infrastructure.Configuration;
using KnowledgeSystem.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// ============================================================================
// CLEAN ARCHITECTURE INFRASTRUCTURE REGISTRATION
// ============================================================================
// This registers all Clean Architecture components:
// - KnowledgeDbContext (EF Core with PostgreSQL + pgvector)
// - IDocumentRepository → DocumentRepository
// - IVectorSearchEngine → PgVectorSearchEngine
// - IEmbeddingGenerator → OllamaEmbeddingGenerator
// - ILanguageModel → OllamaLanguageModel
// - IDocumentIngestionService → DocumentIngestionService
// - Use case handlers (SemanticSearch, ComposePrompt, GenerateAnswer)
// - ConfidencePolicy (Domain)
builder.Services.AddInfrastructure(builder.Configuration);

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
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        var knowledgeDb = scope.ServiceProvider.GetRequiredService<KnowledgeDbContext>();
        await knowledgeDb.Database.MigrateAsync();
        
        logger.LogInformation("Database migrations applied successfully");
    }
    catch (Exception ex)
    {
        logger.LogCritical(ex, "Failed to apply database migrations");
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
// MONITORING & DEBUG ENDPOINTS
// ============================================================================

// Health check endpoint
app.MapGet("/health", async (KnowledgeDbContext db) =>
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

// Document count endpoint (debug/monitoring)
app.MapGet("/api/documents/count", async (KnowledgeDbContext db) =>
{
    var count = await db.Documents.CountAsync();
    return Results.Ok(new { totalDocuments = count });
});

// ============================================================================
// CLEAN ARCHITECTURE ENDPOINTS
// ============================================================================

// Document Ingestion (Clean Architecture) - POST /api/documents/ingest
app.MapPost("/api/documents/ingest", async (
    HttpRequest request,
    KnowledgeSystem.Application.Services.IDocumentIngestionService ingestionService,
    ILogger<Program> logger,
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

        logger.LogInformation(
            "Starting document ingestion: Title={Title}, Size={Size} bytes",
            title, file.Length);

        // Clean Architecture ingestion pipeline
        await using var stream = file.OpenReadStream();
        var result = await ingestionService.IngestAsync(
            stream,
            file.FileName,
            file.ContentType ?? "application/pdf",
            title,
            cancellationToken
        );

        logger.LogInformation(
            "Document ingestion completed successfully: DocumentId={DocumentId}, ChunkCount={ChunkCount}",
            result.DocumentId.Value, result.ChunkCount);

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
        logger.LogWarning(ex, "Document ingestion validation error");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during document ingestion");
        return Results.Problem($"Document ingestion failed: {ex.Message}");
    }
}).DisableAntiforgery(); // File upload için CSRF kontrolünü devre dışı bırak

// Semantic Search (Clean Architecture - Phase 4 Step 1) - POST /api/query/semantic-search
app.MapPost("/api/query/semantic-search", async (
    SemanticSearchRequest request,
    KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchHandler handler,
    ILogger<Program> logger,
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

        logger.LogInformation(
            "Semantic search request: Query=\"{Query}\", TopK={TopK}",
            query.Query, query.TopK);

        var result = await handler.HandleAsync(query, cancellationToken);

        logger.LogInformation(
            "Semantic search completed: Matches={TotalMatches}",
            result.TotalMatches);

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
        logger.LogWarning(ex, "Semantic search validation error");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during semantic search");
        return Results.Problem($"Semantic search failed: {ex.Message}");
    }
});

// Unified Answer Endpoint (Clean Architecture - Phase 4 Step 3) - POST /api/query/answer
app.MapPost("/api/query/answer", async (
    AnswerQueryRequest request,
    KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchHandler searchHandler,
    KnowledgeSystem.Application.UseCases.Prompting.ComposePromptHandler promptHandler,
    KnowledgeSystem.Application.UseCases.GenerateAnswer.GenerateAnswerHandler answerHandler,
    ILogger<Program> logger,
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

        logger.LogInformation(
            "RAG answer request started: Query=\"{Query}\", TopK={TopK}, Language={Language}",
            request.Query, request.TopK ?? 5, request.Language ?? "auto");

        // STEP 1: Semantic Search (retrieve relevant chunks)
        var searchQuery = new KnowledgeSystem.Application.UseCases.SemanticSearch.SemanticSearchQuery
        {
            Query = request.Query,
            TopK = request.TopK ?? 5,
            DocumentId = request.DocumentId
        };

        var searchResult = await searchHandler.HandleAsync(searchQuery, cancellationToken);
        logger.LogDebug("Step 1 complete: Retrieved {MatchCount} relevant chunks", searchResult.TotalMatches);

        // STEP 2: Compose Prompt (build structured LLM prompt)
        var composeCommand = new KnowledgeSystem.Application.UseCases.Prompting.ComposePromptCommand
        {
            Query = request.Query,
            RetrievedChunks = searchResult.Results,
            Language = request.Language
        };

        var composedPrompt = promptHandler.Handle(composeCommand);
        logger.LogDebug(
            "Step 2 complete: Prompt composed with {SourceCount} sources, {TokenCount} estimated tokens",
            composedPrompt.SourceCount, composedPrompt.EstimatedTokenCount);

        // STEP 3: Generate Answer (invoke LLM with composed prompt)
        var generateCommand = new KnowledgeSystem.Application.UseCases.GenerateAnswer.GenerateAnswerCommand
        {
            Prompt = composedPrompt
        };

        var answerResult = await answerHandler.HandleAsync(generateCommand, cancellationToken);
        logger.LogInformation(
            "RAG answer request completed: Confidence={Confidence}, LLMInvoked={LLMInvoked}, SourceCount={SourceCount}",
            answerResult.ConfidenceLevel, answerResult.LlmInvoked, answerResult.SourceCount);

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
        logger.LogWarning(ex, "RAG answer request validation error");
        return Results.BadRequest(new { error = ex.Message });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Unexpected error during RAG answer generation");
        return Results.Problem($"Answer generation failed: {ex.Message}");
    }
});

app.Run();

// ============================================================================
// REQUEST/RESPONSE MODELS (Clean Architecture)
// ============================================================================
record SemanticSearchRequest(string Query, int? TopK, string? DocumentId);
record AnswerQueryRequest(string Query, int? TopK, string? DocumentId, string? Language);
