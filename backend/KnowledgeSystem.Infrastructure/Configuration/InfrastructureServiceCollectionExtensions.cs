using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Infrastructure.Persistence;
using KnowledgeSystem.Infrastructure.Persistence.Repositories;
using KnowledgeSystem.Infrastructure.VectorSearch;
using KnowledgeSystem.Infrastructure.Embedding;
using KnowledgeSystem.Infrastructure.LanguageModel;

namespace KnowledgeSystem.Infrastructure.Configuration;

/// <summary>
/// Extension methods for registering Infrastructure services
/// This is the composition root for the Infrastructure layer
/// </summary>
public static class InfrastructureServiceCollectionExtensions
{
    /// <summary>
    /// Register all Infrastructure services (Persistence, External Services, etc.)
    /// </summary>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register Persistence (Repository + DbContext)
        services.AddPersistence(configuration);

        // Register Vector Search
        services.AddVectorSearch();

        // Register Ollama Services (Embeddings + LLM)
        services.AddOllamaServices(configuration);

        return services;
    }

    /// <summary>
    /// Register Persistence layer (DbContext + Repositories)
    /// </summary>
    private static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Get connection string from configuration
        var connectionString = configuration.GetConnectionString("KnowledgeDb")
            ?? throw new InvalidOperationException(
                "Connection string 'KnowledgeDb' not found in configuration");

        // Register DbContext
        services.AddDbContext<KnowledgeDbContext>(options =>
        {
            options.UseNpgsql(
                connectionString,
                npgsqlOptions =>
                {
                    // Enable pgvector extension
                    npgsqlOptions.UseVector();
                    
                    // Set command timeout (important for large embeddings)
                    npgsqlOptions.CommandTimeout(180);
                    
                    // Set migrations assembly
                    npgsqlOptions.MigrationsAssembly(
                        typeof(KnowledgeDbContext).Assembly.FullName);
                });

            // Development settings (can be made conditional based on environment)
            options.EnableSensitiveDataLogging();
            options.EnableDetailedErrors();
        });

        // Register Repositories (Adapters implementing Application Ports)
        services.AddScoped<IDocumentRepository, DocumentRepository>();

        return services;
    }

    /// <summary>
    /// Register Vector Search services
    /// </summary>
    private static IServiceCollection AddVectorSearch(this IServiceCollection services)
    {
        // Register Vector Search Engine (Adapter implementing Application Port)
        services.AddScoped<IVectorSearchEngine, PgVectorSearchEngine>();

        return services;
    }

    /// <summary>
    /// Register Ollama services (Embeddings + LLM)
    /// </summary>
    private static IServiceCollection AddOllamaServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Read Ollama base URL from configuration
        var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";

        // Read timeout from configuration
        // Default: 60 seconds for embeddings, 120 seconds for LLM (generation takes longer)
        var embeddingTimeoutSeconds = ParseTimeout(configuration, "Ollama:Embeddings:TimeoutSeconds", 60);
        var llmTimeoutSeconds = ParseTimeout(configuration, "Ollama:LanguageModel:TimeoutSeconds", 120);

        // Register typed HttpClient for OllamaEmbeddingGenerator
        services.AddHttpClient<IEmbeddingGenerator, OllamaEmbeddingGenerator>(client =>
        {
            client.BaseAddress = new Uri(ollamaBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(embeddingTimeoutSeconds);
        });

        // Register typed HttpClient for OllamaLanguageModel
        services.AddHttpClient<ILanguageModel, OllamaLanguageModel>(client =>
        {
            client.BaseAddress = new Uri(ollamaBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(llmTimeoutSeconds);
        });

        return services;
    }

    /// <summary>
    /// Parse timeout configuration with fallback to default
    /// </summary>
    private static int ParseTimeout(IConfiguration configuration, string key, int defaultValue)
    {
        var configValue = configuration[key];
        if (!string.IsNullOrWhiteSpace(configValue) && int.TryParse(configValue, out var parsed))
        {
            return parsed;
        }
        return defaultValue;
    }
}

