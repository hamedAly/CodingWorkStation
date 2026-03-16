using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.Embedding;
using SemanticSearch.Infrastructure.FileSystem;
using SemanticSearch.Infrastructure.Indexing;
using SemanticSearch.Infrastructure.ProjectTree;
using SemanticSearch.Infrastructure.Search;
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

        services.AddSingleton<IEmbeddingService>(new OnnxEmbeddingService(modelDirectory));

        services.AddSingleton<ProjectCatalogService>();
        services.AddSingleton<SqliteVectorStore>(_ => new SqliteVectorStore(databasePath));
        services.AddSingleton<IProjectWorkspaceRepository>(sp => sp.GetRequiredService<SqliteVectorStore>());
        services.AddSingleton<IProjectFileRepository>(sp => sp.GetRequiredService<SqliteVectorStore>());
        services.AddSingleton<IFileChunker, FileChunker>();
        services.AddSingleton<IProjectScanner, ProjectScanner>();
        services.AddSingleton<IProjectTreeService, ProjectTreeBuilder>();
        services.AddSingleton<IProjectFileReader, ProjectFileReader>();
        services.AddSingleton<IExactSearchService, ExactSearchService>();

        var indexingChannel = new IndexingChannel();
        services.AddSingleton(indexingChannel);
        services.AddSingleton<IIndexingQueue>(indexingChannel);
        services.AddHostedService<IndexingWorker>();

        return services;
    }
}
