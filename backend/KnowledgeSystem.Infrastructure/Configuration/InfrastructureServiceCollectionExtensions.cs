using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using KnowledgeSystem.Application.Interfaces;
using KnowledgeSystem.Infrastructure.Persistence;
using KnowledgeSystem.Infrastructure.Persistence.Repositories;

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

        // TODO: In future, add other infrastructure services here:
        // - AddOllamaServices(configuration)
        // - AddVectorSearch(configuration)
        // - etc.

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
}

