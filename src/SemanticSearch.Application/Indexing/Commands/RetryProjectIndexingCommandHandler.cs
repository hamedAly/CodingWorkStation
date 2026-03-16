using MediatR;
using Microsoft.Extensions.Logging;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class RetryProjectIndexingCommandHandler : IRequestHandler<RetryProjectIndexingCommand, IndexProjectResponse>
{
    private readonly IIndexingQueue _indexingQueue;
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly ILogger<RetryProjectIndexingCommandHandler> _logger;

    public RetryProjectIndexingCommandHandler(
        IIndexingQueue indexingQueue,
        IProjectWorkspaceRepository workspaceRepository,
        ILogger<RetryProjectIndexingCommandHandler> logger)
    {
        _indexingQueue = indexingQueue;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task<IndexProjectResponse> Handle(RetryProjectIndexingCommand request, CancellationToken cancellationToken)
    {
        var projectKey = request.ProjectKey.Trim();
        var workspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);

        if (workspace is null)
            throw new NotFoundException($"Project '{projectKey}' has not been indexed.");

        var activeRun = await _workspaceRepository.GetActiveRunAsync(projectKey, cancellationToken);
        if (activeRun is not null)
        {
            return new IndexProjectResponse(
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
            ProjectKey = workspace.ProjectKey,
            RunType = IndexingRunType.Full,
            Status = IndexingRunState.Queued,
            RequestedUtc = DateTime.UtcNow,
            TotalFilesPlanned = 0
        };

        await _workspaceRepository.UpsertAsync(queuedWorkspace, cancellationToken);
        await _workspaceRepository.UpsertRunAsync(run, cancellationToken);
        await _indexingQueue.EnqueueAsync(
            new IndexingWorkItem(runId, workspace.ProjectKey, workspace.SourceRootPath, IndexingRunType.Full, null),
            cancellationToken);

        _logger.LogInformation("Retried indexing job for project '{ProjectKey}' at '{ProjectPath}'", workspace.ProjectKey, workspace.SourceRootPath);

        return new IndexProjectResponse(
            workspace.ProjectKey,
            runId,
            IndexingRunState.Queued.ToString(),
            $"Indexing retried for project '{workspace.ProjectKey}'.");
    }
}
