using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.FileSystem;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Indexing;

public sealed class IndexingWorker : BackgroundService
{
    private readonly IndexingChannel _channel;
    private readonly IEmbeddingService _embeddingService;
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly IFileChunker _fileChunker;
    private readonly IProjectScanner _projectScanner;
    private readonly ProjectCatalogService _projectCatalogService;
    private readonly SemanticSearchOptions _options;
    private readonly ILogger<IndexingWorker> _logger;

    public IndexingWorker(
        IndexingChannel channel,
        IEmbeddingService embeddingService,
        IProjectWorkspaceRepository workspaceRepository,
        IProjectFileRepository projectFileRepository,
        IFileChunker fileChunker,
        IProjectScanner projectScanner,
        ProjectCatalogService projectCatalogService,
        IOptions<SemanticSearchOptions> options,
        ILogger<IndexingWorker> logger)
    {
        _channel = channel;
        _embeddingService = embeddingService;
        _workspaceRepository = workspaceRepository;
        _projectFileRepository = projectFileRepository;
        _fileChunker = fileChunker;
        _projectScanner = projectScanner;
        _projectCatalogService = projectCatalogService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("IndexingWorker started");

        await foreach (var workItem in _channel.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await ProcessWorkItemAsync(workItem, stoppingToken);
            }
            catch (IndexingPausedException)
            {
                _logger.LogInformation("Indexing paused for project '{ProjectKey}'", workItem.ProjectKey);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("Indexing worker cancellation requested.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Indexing failed for project '{ProjectKey}'", workItem.ProjectKey);
                await MarkFailedAsync(workItem, ex.Message, stoppingToken);
            }
        }

        _logger.LogInformation("IndexingWorker stopped");
    }

    private async Task ProcessWorkItemAsync(IndexingWorkItem workItem, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetAsync(workItem.ProjectKey, cancellationToken);
        var run = await _workspaceRepository.GetRunAsync(workItem.RunId, cancellationToken);

        if (workspace is null || run is null)
            return;

        if (!IsRunnable(run.Status))
            return;

        run = await EnsureRunCanContinueAsync(workspace, run.RunId, cancellationToken);
        run = await MarkRunningAsync(workspace, run, cancellationToken);

        var outcome = workItem.RunType switch
        {
            IndexingRunType.Full => await IndexProjectAsync(workspace, run, cancellationToken),
            IndexingRunType.SingleFile => await RefreshFileAsync(workspace, run, workItem.RelativeFilePath!, cancellationToken),
            _ => throw new InvalidOperationException($"Unsupported run type '{workItem.RunType}'.")
        };

        var files = await _projectFileRepository.ListFilesAsync(workspace.ProjectKey, cancellationToken);
        var totalSegments = files.Sum(file => file.SegmentCount);
        var completedAt = DateTime.UtcNow;

        await _workspaceRepository.UpsertAsync(new ProjectWorkspace
        {
            ProjectKey = workspace.ProjectKey,
            SourceRootPath = workspace.SourceRootPath,
            Status = ProjectStatus.Indexed,
            TotalFiles = files.Count,
            TotalSegments = totalSegments,
            LastIndexedUtc = completedAt,
            LastRunId = run.RunId,
            LastError = null
        }, cancellationToken);

        await _workspaceRepository.UpsertRunAsync(new IndexingRun
        {
            RunId = run.RunId,
            ProjectKey = run.ProjectKey,
            RunType = run.RunType,
            Status = IndexingRunState.Completed,
            RequestedUtc = run.RequestedUtc,
            StartedUtc = run.StartedUtc ?? DateTime.UtcNow,
            CompletedUtc = completedAt,
            RequestedFilePath = run.RequestedFilePath,
            TotalFilesPlanned = run.TotalFilesPlanned,
            FilesScanned = outcome.FilesScanned,
            FilesIndexed = outcome.FilesIndexed,
            FilesSkipped = outcome.FilesSkipped,
            SegmentsWritten = outcome.SegmentsWritten,
            WarningCount = outcome.WarningCount,
            CurrentFilePath = null
        }, cancellationToken);
    }

    private async Task<IndexingOutcome> IndexProjectAsync(
        ProjectWorkspace workspace,
        IndexingRun run,
        CancellationToken cancellationToken)
    {
        run = await UpdateRunProgressAsync(workspace, run, new IndexingOutcome(), 0, "Scanning project files...", cancellationToken);
        var excludedDirs = new HashSet<string>(_options.Indexing.ExcludedDirectories, StringComparer.OrdinalIgnoreCase);
        var allowedExtensions = new HashSet<string>(_options.Indexing.AllowedExtensions, StringComparer.OrdinalIgnoreCase);
        var files = _projectScanner.ScanProject(workspace.SourceRootPath, excludedDirs, allowedExtensions);
        var keepRelativePaths = files
            .Select(filePath => _projectCatalogService.ToRelativePath(workspace.SourceRootPath, filePath))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var outcome = new IndexingOutcome
        {
            FilesScanned = run.FilesScanned,
            FilesIndexed = run.FilesIndexed,
            FilesSkipped = run.FilesSkipped,
            SegmentsWritten = run.SegmentsWritten,
            WarningCount = run.WarningCount
        };

        run = await UpdateRunProgressAsync(workspace, run, outcome, files.Count, null, cancellationToken);

        foreach (var filePath in files.Skip(Math.Min(run.FilesScanned, files.Count)))
        {
            cancellationToken.ThrowIfCancellationRequested();
            run = await EnsureRunCanContinueAsync(workspace, run.RunId, cancellationToken);
            run = await MarkRunningAsync(workspace, run, cancellationToken);

            var relativeFilePath = _projectCatalogService.ToRelativePath(workspace.SourceRootPath, filePath);
            run = await UpdateRunProgressAsync(workspace, run, outcome, files.Count, relativeFilePath, cancellationToken);

            var fileOutcome = await IndexFileAsync(workspace, run.RunId, filePath, relativeFilePath, cancellationToken);
            outcome = outcome.Add(fileOutcome).WithProcessedFile();
            run = await UpdateRunProgressAsync(workspace, run, outcome, files.Count, relativeFilePath, cancellationToken);
        }

        await _projectFileRepository.DeleteFilesMissingFromSetAsync(workspace.ProjectKey, keepRelativePaths, cancellationToken);
        return outcome;
    }

    private async Task<IndexingOutcome> RefreshFileAsync(
        ProjectWorkspace workspace,
        IndexingRun run,
        string relativeFilePath,
        CancellationToken cancellationToken)
    {
        var absoluteFilePath = _projectCatalogService.ToAbsolutePath(workspace.SourceRootPath, relativeFilePath);
        var outcome = new IndexingOutcome
        {
            FilesScanned = run.FilesScanned,
            FilesIndexed = run.FilesIndexed,
            FilesSkipped = run.FilesSkipped,
            SegmentsWritten = run.SegmentsWritten,
            WarningCount = run.WarningCount
        };

        run = await EnsureRunCanContinueAsync(workspace, run.RunId, cancellationToken);
        run = await UpdateRunProgressAsync(workspace, run, outcome, 1, relativeFilePath, cancellationToken);

        if (!File.Exists(absoluteFilePath))
        {
            await _projectFileRepository.DeleteFileAsync(workspace.ProjectKey, relativeFilePath, cancellationToken);
            outcome = outcome.WithProcessedFile() with { FilesSkipped = 1, WarningCount = 1 };
            await UpdateRunProgressAsync(workspace, run, outcome, 1, relativeFilePath, cancellationToken);
            return outcome;
        }

        var fileOutcome = await IndexFileAsync(workspace, run.RunId, absoluteFilePath, relativeFilePath, cancellationToken);
        outcome = outcome.Add(fileOutcome).WithProcessedFile();
        await UpdateRunProgressAsync(workspace, run, outcome, 1, relativeFilePath, cancellationToken);
        return outcome;
    }

    private async Task<IndexingOutcome> IndexFileAsync(
        ProjectWorkspace workspace,
        string runId,
        string absoluteFilePath,
        string relativeFilePath,
        CancellationToken cancellationToken)
    {
        var chunkingResult = _fileChunker.ChunkFile(absoluteFilePath, _options.Chunking.ChunkSize, _options.Chunking.Overlap);
        if (chunkingResult.IsSkipped)
        {
            await _projectFileRepository.DeleteFileAsync(workspace.ProjectKey, relativeFilePath, cancellationToken);

            return new IndexingOutcome
            {
                FilesSkipped = 1,
                WarningCount = chunkingResult.ShouldWarn ? 1 : 0
            };
        }

        var segments = new List<SearchSegment>(chunkingResult.Chunks.Count);
        var segmentOrder = 0;

        foreach (var chunkInfo in chunkingResult.Chunks)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await EnsureRunCanContinueAsync(workspace, runId, cancellationToken);
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunkInfo.Content, cancellationToken);
            await EnsureRunCanContinueAsync(workspace, runId, cancellationToken);

            segments.Add(new SearchSegment
            {
                SegmentId = SqliteVectorStore.ComputeSegmentId(workspace.ProjectKey, relativeFilePath, chunkInfo.StartLine),
                ProjectKey = workspace.ProjectKey,
                RelativeFilePath = relativeFilePath,
                SegmentOrder = segmentOrder++,
                StartLine = chunkInfo.StartLine,
                EndLine = chunkInfo.EndLine,
                Content = chunkInfo.Content,
                SnippetPreview = chunkInfo.Content.Length > 400 ? chunkInfo.Content[..400] : chunkInfo.Content,
                ContentHash = SqliteVectorStore.ComputeContentHash(chunkInfo.Content),
                EmbeddingVector = embedding,
                TokenCount = chunkInfo.Content.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length,
                CreatedUtc = DateTime.UtcNow
            });
        }

        var indexedFile = await _projectCatalogService.CreateIndexedFileAsync(
            workspace.ProjectKey,
            workspace.SourceRootPath,
            absoluteFilePath,
            segments.Count,
            cancellationToken);

        await _projectFileRepository.UpsertFileAsync(indexedFile, cancellationToken);
        await _projectFileRepository.ReplaceSegmentsAsync(workspace.ProjectKey, relativeFilePath, segments, cancellationToken);

        return new IndexingOutcome
        {
            FilesIndexed = 1,
            SegmentsWritten = segments.Count
        };
    }

    private async Task MarkFailedAsync(IndexingWorkItem workItem, string errorMessage, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetAsync(workItem.ProjectKey, cancellationToken);
        var run = await _workspaceRepository.GetRunAsync(workItem.RunId, cancellationToken);

        if (workspace is not null)
        {
            await _workspaceRepository.UpsertAsync(new ProjectWorkspace
            {
                ProjectKey = workspace.ProjectKey,
                SourceRootPath = workspace.SourceRootPath,
                Status = ProjectStatus.Failed,
                TotalFiles = workspace.TotalFiles,
                TotalSegments = workspace.TotalSegments,
                LastIndexedUtc = workspace.LastIndexedUtc,
                LastRunId = workItem.RunId,
                LastError = errorMessage
            }, cancellationToken);
        }

        if (run is not null)
        {
            await _workspaceRepository.UpsertRunAsync(new IndexingRun
            {
                RunId = run.RunId,
                ProjectKey = run.ProjectKey,
                RunType = run.RunType,
                Status = IndexingRunState.Failed,
                RequestedUtc = run.RequestedUtc,
                StartedUtc = run.StartedUtc,
                CompletedUtc = DateTime.UtcNow,
                RequestedFilePath = run.RequestedFilePath,
                TotalFilesPlanned = run.TotalFilesPlanned,
                FilesScanned = run.FilesScanned,
                FilesIndexed = run.FilesIndexed,
                FilesSkipped = run.FilesSkipped,
                SegmentsWritten = run.SegmentsWritten,
                WarningCount = run.WarningCount,
                CurrentFilePath = run.CurrentFilePath,
                FailureReason = errorMessage
            }, cancellationToken);
        }
    }

    private async Task<IndexingRun> EnsureRunCanContinueAsync(
        ProjectWorkspace workspace,
        string runId,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var run = await _workspaceRepository.GetRunAsync(runId, cancellationToken);
        if (run is null)
            throw new InvalidOperationException($"The indexing run '{runId}' no longer exists.");

        if (run.Status is IndexingRunState.Paused)
        {
            await UpdateWorkspaceStatusAsync(workspace, ProjectStatus.Paused, run.RunId, null, cancellationToken);
            throw new IndexingPausedException(run.RunId);
        }

        if (!IsRunnable(run.Status))
            throw new InvalidOperationException($"The indexing run '{runId}' is no longer active.");

        return run;
    }

    private async Task<IndexingRun> MarkRunningAsync(
        ProjectWorkspace workspace,
        IndexingRun run,
        CancellationToken cancellationToken)
    {
        if (run.Status is IndexingRunState.Running && run.StartedUtc.HasValue)
        {
            await UpdateWorkspaceStatusAsync(workspace, ProjectStatus.Indexing, run.RunId, null, cancellationToken);
            return run;
        }

        var startedUtc = run.StartedUtc ?? DateTime.UtcNow;
        var runningRun = new IndexingRun
        {
            RunId = run.RunId,
            ProjectKey = run.ProjectKey,
            RunType = run.RunType,
            Status = IndexingRunState.Running,
            RequestedUtc = run.RequestedUtc,
            StartedUtc = startedUtc,
            CompletedUtc = null,
            RequestedFilePath = run.RequestedFilePath,
            TotalFilesPlanned = run.TotalFilesPlanned,
            FilesScanned = run.FilesScanned,
            FilesIndexed = run.FilesIndexed,
            FilesSkipped = run.FilesSkipped,
            SegmentsWritten = run.SegmentsWritten,
            WarningCount = run.WarningCount,
            CurrentFilePath = run.CurrentFilePath,
            FailureReason = null
        };

        await _workspaceRepository.UpsertRunAsync(runningRun, cancellationToken);
        await UpdateWorkspaceStatusAsync(workspace, ProjectStatus.Indexing, run.RunId, null, cancellationToken);
        return runningRun;
    }

    private async Task<IndexingRun> UpdateRunProgressAsync(
        ProjectWorkspace workspace,
        IndexingRun run,
        IndexingOutcome outcome,
        int totalFilesPlanned,
        string? currentFilePath,
        CancellationToken cancellationToken)
    {
        var latestRun = await _workspaceRepository.GetRunAsync(run.RunId, cancellationToken) ?? run;
        var runStatus = latestRun.Status;
        var startedUtc = latestRun.StartedUtc ?? run.StartedUtc;
        var updatedRun = new IndexingRun
        {
            RunId = run.RunId,
            ProjectKey = run.ProjectKey,
            RunType = run.RunType,
            Status = runStatus,
            RequestedUtc = run.RequestedUtc,
            StartedUtc = startedUtc,
            CompletedUtc = null,
            RequestedFilePath = run.RequestedFilePath,
            TotalFilesPlanned = totalFilesPlanned,
            FilesScanned = outcome.FilesScanned,
            FilesIndexed = outcome.FilesIndexed,
            FilesSkipped = outcome.FilesSkipped,
            SegmentsWritten = outcome.SegmentsWritten,
            WarningCount = outcome.WarningCount,
            CurrentFilePath = currentFilePath,
            FailureReason = null
        };

        await _workspaceRepository.UpsertRunAsync(updatedRun, cancellationToken);
        await UpdateWorkspaceStatusAsync(
            workspace,
            updatedRun.Status is IndexingRunState.Paused ? ProjectStatus.Paused : ProjectStatus.Indexing,
            run.RunId,
            null,
            cancellationToken);

        return updatedRun;
    }

    private Task UpdateWorkspaceStatusAsync(
        ProjectWorkspace workspace,
        ProjectStatus status,
        string runId,
        string? errorMessage,
        CancellationToken cancellationToken)
        => _workspaceRepository.UpsertAsync(new ProjectWorkspace
        {
            ProjectKey = workspace.ProjectKey,
            SourceRootPath = workspace.SourceRootPath,
            Status = status,
            TotalFiles = workspace.TotalFiles,
            TotalSegments = workspace.TotalSegments,
            LastIndexedUtc = workspace.LastIndexedUtc,
            LastRunId = runId,
            LastError = errorMessage
        }, cancellationToken);

    private static bool IsRunnable(IndexingRunState status)
        => status is IndexingRunState.Queued or IndexingRunState.Running or IndexingRunState.Paused;

    private sealed record IndexingOutcome
    {
        public int FilesScanned { get; init; }
        public int FilesIndexed { get; init; }
        public int FilesSkipped { get; init; }
        public int SegmentsWritten { get; init; }
        public int WarningCount { get; init; }

        public IndexingOutcome Add(IndexingOutcome other) => new()
        {
            FilesScanned = FilesScanned + other.FilesScanned,
            FilesIndexed = FilesIndexed + other.FilesIndexed,
            FilesSkipped = FilesSkipped + other.FilesSkipped,
            SegmentsWritten = SegmentsWritten + other.SegmentsWritten,
            WarningCount = WarningCount + other.WarningCount
        };

        public IndexingOutcome WithProcessedFile() => this with
        {
            FilesScanned = FilesScanned + 1
        };
    }

    private sealed class IndexingPausedException : Exception
    {
        public IndexingPausedException(string runId)
            : base($"Indexing paused for run '{runId}'.")
        {
        }
    }
}
