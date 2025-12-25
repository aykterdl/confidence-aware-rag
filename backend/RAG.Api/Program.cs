using Microsoft.EntityFrameworkCore;
using RAG.Api.Data;
using RAG.Api.Configuration;
using RAG.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// PostgreSQL + pgvector bağlantısı
builder.Services.AddDbContext<RagDbContext>(options =>
    options.UseNpgsql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
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

// Ollama HttpClient - Embedding
builder.Services.AddHttpClient<IOllamaEmbeddingService, OllamaEmbeddingService>((serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromSeconds(settings.TimeoutSeconds);
});

// Ollama HttpClient - LLM
builder.Services.AddHttpClient(nameof(IOllamaLlmService), (serviceProvider, client) =>
{
    var settings = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<OllamaSettings>>().Value;
    client.BaseAddress = new Uri(settings.BaseUrl);
    client.Timeout = TimeSpan.FromMinutes(5); // LLM generation uzun sürebilir
});

// Ollama LLM Service
builder.Services.AddScoped<IOllamaLlmService, OllamaLlmService>();

// Text Chunking Service
builder.Services.AddScoped<ITextChunkingService, TextChunkingService>();

// Chunk Ingestion Service
builder.Services.AddScoped<IChunkIngestionService, ChunkIngestionService>();

// Vector Search Service
// Vector search with NpgsqlCommand + L2 distance
builder.Services.AddScoped<IVectorSearchService, VectorSearchService>();

// Conversation Store (Singleton - in-memory shared across all requests)
builder.Services.AddSingleton<IConversationStore, InMemoryConversationStore>();

// RAG Answer Service
builder.Services.AddScoped<IRagAnswerService, RagAnswerService>();

// PDF Reader Service
builder.Services.AddScoped<IPdfReaderService, PdfReaderService>();

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

// CORS middleware
app.UseCors();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

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
    RAG.Api.Models.RagRequest request,
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

app.Run();

record TestEmbeddingRequest(string Text);
record TestChunkingRequest(string Text, int? MaxChunkSize, int? Overlap);
record IngestTextRequest(string Text, string Title, string? Metadata);
record SearchRequest(string Query, int? TopK, double? SimilarityThreshold);
