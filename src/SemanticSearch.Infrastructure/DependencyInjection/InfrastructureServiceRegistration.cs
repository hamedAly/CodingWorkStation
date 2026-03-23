using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.Architecture;
using SemanticSearch.Infrastructure.Embedding;
using SemanticSearch.Infrastructure.FileSystem;
using SemanticSearch.Infrastructure.Indexing;
using SemanticSearch.Infrastructure.ProjectTree;
using SemanticSearch.Infrastructure.Quality;
using SemanticSearch.Infrastructure.Quality.Assistant;
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
        services.AddSingleton<IQualityRepository>(sp => sp.GetRequiredService<SqliteVectorStore>());
        services.AddSingleton<IFileChunker, FileChunker>();
        services.AddSingleton<IProjectScanner, ProjectScanner>();
        services.AddSingleton<IProjectTreeService, ProjectTreeBuilder>();
        services.AddSingleton<IProjectFileReader, ProjectFileReader>();
        services.AddSingleton<IExactSearchService, ExactSearchService>();
        services.AddSingleton<QualityFileFilter>();
        services.AddSingleton<StructuralCloneNormalizer>();
        services.AddSingleton<StructuralCloneGrouper>();
        services.AddSingleton<IStructuralCloneAnalyzer, RoslynStructuralCloneAnalyzer>();
        services.AddSingleton<SemanticSegmentNormalizer>();
        services.AddSingleton<SemanticPairSelector>();
        services.AddSingleton<ISemanticDuplicationService, EmbeddingSemanticDuplicationService>();
        services.AddSingleton<QualitySummaryBuilder>();
        services.AddSingleton<IComparisonHighlightService, ComparisonHighlightService>();
        services.AddSingleton<IQualityRunCoordinator, QualityRunCoordinator>();
        services.AddSingleton<QualityRefreshPolicy>();
        services.AddSingleton<AssistantReadinessService>();
        services.AddSingleton<IAiAssistantModelProvider, LlamaModelProvider>();
        services.AddSingleton<DuplicateSnippetLimiter>();
        services.AddSingleton<IQualityAssistantPromptBuilder, QualityAssistantPromptBuilder>();
        services.AddSingleton<IQualityAssistantService, LlamaStreamingAssistantService>();

        var indexingChannel = new IndexingChannel();
        services.AddSingleton(indexingChannel);
        services.AddSingleton<IIndexingQueue>(indexingChannel);
        services.AddHostedService<IndexingWorker>();

        // Architecture / Visual Analysis
        services.AddSingleton<IDependencyRepository>(sp => sp.GetRequiredService<SqliteVectorStore>());
        services.AddSingleton<IDependencyExtractor, RoslynDependencyExtractor>();
        services.AddSingleton<IHeatmapDataBuilder, HeatmapDataBuilder>();
        services.AddSingleton<IErDiagramGenerator>(_ => new SqliteErDiagramGenerator(databasePath));

        return services;
    }
}
