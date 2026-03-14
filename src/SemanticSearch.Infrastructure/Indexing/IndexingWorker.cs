using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Indexing;

public sealed class IndexingWorker : BackgroundService
{
    private readonly IndexingChannel _channel;
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;
    private readonly IFileChunker _fileChunker;
    private readonly IProjectScanner _projectScanner;
    private readonly SemanticSearchOptions _options;
    private readonly ILogger<IndexingWorker> _logger;

    public IndexingWorker(
        IndexingChannel channel,
        IEmbeddingService embeddingService,
        IVectorStore vectorStore,
        IFileChunker fileChunker,
        IProjectScanner projectScanner,
        IOptions<SemanticSearchOptions> options,
        ILogger<IndexingWorker> logger)
    {
        _channel = channel;
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
        _fileChunker = fileChunker;
        _projectScanner = projectScanner;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IndexingWorker started");

        await foreach (var command in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Starting indexing for project '{ProjectKey}' at '{ProjectPath}'",
                    command.ProjectKey, command.ProjectPath);

                await IndexProjectAsync(command.ProjectKey, command.ProjectPath, stoppingToken);

                _logger.LogInformation("Completed indexing for project '{ProjectKey}'", command.ProjectKey);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Indexing cancelled for project '{ProjectKey}'", command.ProjectKey);
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indexing failed for project '{ProjectKey}'", command.ProjectKey);
            }
        }

        _logger.LogInformation("IndexingWorker stopped");
    }

    private async Task IndexProjectAsync(string projectKey, string projectPath, CancellationToken cancellationToken)
    {
        var excludedDirs = new HashSet<string>(_options.Indexing.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
        var allowedExtensions = new HashSet<string>(_options.Indexing.AllowedExtensions, StringComparer.OrdinalIgnoreCase);

        var files = _projectScanner.ScanProject(projectPath, excludedDirs, allowedExtensions);
        _logger.LogInformation("Found {FileCount} files to index in project '{ProjectKey}'", files.Count, projectKey);

        var totalChunks = 0;

        foreach (var filePath in files)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var chunkInfos = _fileChunker.ChunkFile(filePath, _options.Chunking.ChunkSize, _options.Chunking.Overlap);
            if (chunkInfos.Count == 0) continue;

            var chunks = new List<Chunk>(chunkInfos.Count);
            var chunkIds = new HashSet<string>(chunkInfos.Count);

            foreach (var info in chunkInfos)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var chunkId = SqliteVectorStore.ComputeChunkId(projectKey, filePath, info.StartLine);
                var embedding = await _embeddingService.GenerateEmbeddingAsync(info.Content, cancellationToken);

                var chunk = new Chunk
                {
                    Id = chunkId,
                    ProjectKey = projectKey,
                    FilePath = filePath,
                    StartLine = info.StartLine,
                    EndLine = info.EndLine,
                    Content = info.Content,
                    Embedding = embedding,
                    CreatedAt = DateTime.UtcNow
                };

                chunks.Add(chunk);
                chunkIds.Add(chunkId);
            }

            await _vectorStore.UpsertChunksAsync(chunks, cancellationToken);
            await _vectorStore.DeleteStaleChunksAsync(projectKey, filePath, chunkIds, cancellationToken);
            totalChunks += chunks.Count;

            _logger.LogDebug("Indexed {ChunkCount} chunks from '{FilePath}'", chunks.Count, filePath);
        }

        var metadata = new ProjectMetadata
        {
            ProjectKey = projectKey,
            TotalFiles = files.Count,
            TotalChunks = totalChunks,
            LastUpdated = DateTime.UtcNow
        };

        await _vectorStore.UpsertProjectMetadataAsync(metadata, cancellationToken);
        _logger.LogInformation("Indexed {TotalChunks} chunks from {TotalFiles} files for project '{ProjectKey}'",
            totalChunks, files.Count, projectKey);
    }
}
