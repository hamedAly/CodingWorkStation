using MediatR;
using Microsoft.Extensions.Logging;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class IndexProjectCommandHandler : IRequestHandler<IndexProjectCommand, IndexProjectResponse>
{
    private readonly IIndexingQueue _indexingQueue;
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly ILogger<IndexProjectCommandHandler> _logger;

    public IndexProjectCommandHandler(
        IIndexingQueue indexingQueue,
        IProjectWorkspaceRepository workspaceRepository,
        ILogger<IndexProjectCommandHandler> logger)
    {
        _indexingQueue = indexingQueue;
        _workspaceRepository = workspaceRepository;
        _logger = logger;
    }

    public async Task<IndexProjectResponse> Handle(IndexProjectCommand request, CancellationToken cancellationToken)
    {
        var projectKey = request.ProjectKey.Trim();
        var projectPath = Path.GetFullPath(request.ProjectPath.Trim());
        var activeRun = await _workspaceRepository.GetActiveRunAsync(projectKey, cancellationToken);

        if (activeRun is not null)
        {
            return new IndexProjectResponse(
                projectKey,
                activeRun.RunId,
                activeRun.Status.ToString(),
                $"An indexing run is already active for '{projectKey}'.");
        }

        var existingWorkspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);
        var runId = Guid.NewGuid().ToString("N");

        var workspace = new ProjectWorkspace
        {
            ProjectKey = projectKey,
            SourceRootPath = projectPath,
            Status = ProjectStatus.Queued,
            TotalFiles = existingWorkspace?.TotalFiles ?? 0,
            TotalSegments = existingWorkspace?.TotalSegments ?? 0,
            LastIndexedUtc = existingWorkspace?.LastIndexedUtc,
            LastRunId = runId,
            LastError = null
        };

        var run = new IndexingRun
        {
            RunId = runId,
            ProjectKey = projectKey,
            RunType = IndexingRunType.Full,
            Status = IndexingRunState.Queued,
            RequestedUtc = DateTime.UtcNow,
            TotalFilesPlanned = 0
        };

        await _workspaceRepository.UpsertAsync(workspace, cancellationToken);
        await _workspaceRepository.UpsertRunAsync(run, cancellationToken);
        await _indexingQueue.EnqueueAsync(
            new IndexingWorkItem(runId, projectKey, projectPath, IndexingRunType.Full, null),
            cancellationToken);

        _logger.LogInformation("Queued indexing job for project '{ProjectKey}' at '{ProjectPath}'", projectKey, projectPath);

        return new IndexProjectResponse(
            projectKey,
            runId,
            IndexingRunState.Queued.ToString(),
            $"Indexing queued for project '{projectKey}'.");
    }
}
