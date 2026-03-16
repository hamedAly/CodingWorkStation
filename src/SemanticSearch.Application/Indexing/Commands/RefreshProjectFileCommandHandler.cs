using MediatR;
using Microsoft.Extensions.Logging;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class RefreshProjectFileCommandHandler
    : IRequestHandler<RefreshProjectFileCommand, RefreshProjectFileResponse>
{
    private readonly IIndexingQueue _indexingQueue;
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly ILogger<RefreshProjectFileCommandHandler> _logger;

    public RefreshProjectFileCommandHandler(
        IIndexingQueue indexingQueue,
        IProjectWorkspaceRepository workspaceRepository,
        ILogger<RefreshProjectFileCommandHandler> logger)
    {
        _indexingQueue = indexingQueue;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task<RefreshProjectFileResponse> Handle(
        RefreshProjectFileCommand request,
        CancellationToken cancellationToken)
    {
        var projectKey = request.ProjectKey.Trim();
        var relativeFilePath = request.RelativeFilePath.Trim();
        var workspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);

        if (workspace is null)
            throw new NotFoundException($"Project '{projectKey}' has not been indexed.");

        var activeRun = await _workspaceRepository.GetActiveRunAsync(projectKey, cancellationToken);
        if (activeRun is not null)
        {
            return new RefreshProjectFileResponse(
                projectKey,
                activeRun.RunId,
                activeRun.Status.ToString(),
                $"An indexing run is already active for '{projectKey}'.");
        }

        var runId = Guid.NewGuid().ToString("N");
        var queuedWorkspace = new ProjectWorkspace
        {
            ProjectKey = workspace.ProjectKey,
            SourceRootPath = workspace.SourceRootPath,
            Status = ProjectStatus.Queued,
            TotalFiles = workspace.TotalFiles,
            TotalSegments = workspace.TotalSegments,
            LastIndexedUtc = workspace.LastIndexedUtc,
            LastRunId = runId,
            LastError = null
        };

        var run = new IndexingRun
        {
            RunId = runId,
            ProjectKey = projectKey,
            RunType = IndexingRunType.SingleFile,
            Status = IndexingRunState.Queued,
            RequestedUtc = DateTime.UtcNow,
            RequestedFilePath = relativeFilePath,
            TotalFilesPlanned = 1
        };

        await _workspaceRepository.UpsertAsync(queuedWorkspace, cancellationToken);
        await _workspaceRepository.UpsertRunAsync(run, cancellationToken);
        await _indexingQueue.EnqueueAsync(
            new IndexingWorkItem(runId, projectKey, workspace.SourceRootPath, IndexingRunType.SingleFile, relativeFilePath),
            cancellationToken);

        _logger.LogInformation(
            "Queued single-file refresh for project '{ProjectKey}' and file '{RelativeFilePath}'",
            projectKey,
            relativeFilePath);

        return new RefreshProjectFileResponse(
            projectKey,
            runId,
            IndexingRunState.Queued.ToString(),
            $"Single-file refresh queued for '{relativeFilePath}'.");
    }
}
