using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class PauseProjectIndexingCommandHandler
    : IRequestHandler<PauseProjectIndexingCommand, ProjectIndexingControlResponse>
{
    private readonly IProjectWorkspaceRepository _workspaceRepository;

    public PauseProjectIndexingCommandHandler(IProjectWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
    }

    public async Task<ProjectIndexingControlResponse> Handle(
        PauseProjectIndexingCommand request,
        CancellationToken cancellationToken)
    {
        var projectKey = request.ProjectKey.Trim();
        var workspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);

        if (workspace is null)
            throw new NotFoundException($"Project '{projectKey}' has not been indexed.");

        var activeRun = await _workspaceRepository.GetActiveRunAsync(projectKey, cancellationToken);
        if (activeRun is null)
            throw new ConflictException($"No active indexing run was found for '{projectKey}'.");

        if (activeRun.Status is IndexingRunState.Paused)
        {
            return new ProjectIndexingControlResponse(
                projectKey,
                IndexingRunState.Paused.ToString(),
                $"Indexing is already paused for '{projectKey}'.");
        }

        var pausedRun = new IndexingRun
        {
            RunId = activeRun.RunId,
            ProjectKey = activeRun.ProjectKey,
            RunType = activeRun.RunType,
            Status = IndexingRunState.Paused,
            RequestedUtc = activeRun.RequestedUtc,
            StartedUtc = activeRun.StartedUtc,
            CompletedUtc = activeRun.CompletedUtc,
            RequestedFilePath = activeRun.RequestedFilePath,
            TotalFilesPlanned = activeRun.TotalFilesPlanned,
            FilesScanned = activeRun.FilesScanned,
            FilesIndexed = activeRun.FilesIndexed,
            FilesSkipped = activeRun.FilesSkipped,
            SegmentsWritten = activeRun.SegmentsWritten,
            WarningCount = activeRun.WarningCount,
            CurrentFilePath = activeRun.CurrentFilePath,
            FailureReason = activeRun.FailureReason
        };

        var pausedWorkspace = new ProjectWorkspace
        {
            ProjectKey = workspace.ProjectKey,
            SourceRootPath = workspace.SourceRootPath,
            Status = ProjectStatus.Paused,
            TotalFiles = workspace.TotalFiles,
            TotalSegments = workspace.TotalSegments,
            LastIndexedUtc = workspace.LastIndexedUtc,
            LastRunId = activeRun.RunId,
            LastError = null
        };

        await _workspaceRepository.UpsertRunAsync(pausedRun, cancellationToken);
        await _workspaceRepository.UpsertAsync(pausedWorkspace, cancellationToken);

        return new ProjectIndexingControlResponse(
            projectKey,
            IndexingRunState.Paused.ToString(),
            $"Pause requested for '{projectKey}'. The worker will stop after the current file finishes.");
    }
}
