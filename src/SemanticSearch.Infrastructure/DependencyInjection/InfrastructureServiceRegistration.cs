using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.Embedding;
using SemanticSearch.Infrastructure.FileSystem;
using SemanticSearch.Infrastructure.Indexing;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.DependencyInjection;

public static class InfrastructureServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IWebHostEnvironment env,
        SemanticSearchOptions options)
    {
        var modelDirectory = Path.Combine(env.ContentRootPath, options.ModelPath);
        var databasePath = Path.Combine(env.ContentRootPath, options.DatabasePath);

        services.AddSingleton<IEmbeddingService>(
            new OnnxEmbeddingService(modelDirectory));

        var vectorStore = new SqliteVectorStore(databasePath);
        services.AddSingleton<IVectorStore>(vectorStore);

        services.AddSingleton<IFileChunker, FileChunker>();
        services.AddSingleton<IProjectScanner, ProjectScanner>();

        // Indexing background infrastructure
        var indexingChannel = new IndexingChannel();
        services.AddSingleton<IndexingChannel>(indexingChannel);
        services.AddSingleton<IIndexingQueue>(indexingChannel);
        services.AddHostedService<IndexingWorker>();

        return services;
    }
}
