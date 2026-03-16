using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class ResumeProjectIndexingCommandHandler
    : IRequestHandler<ResumeProjectIndexingCommand, ProjectIndexingControlResponse>
{
    private readonly IIndexingQueue _indexingQueue;
    private readonly IProjectWorkspaceRepository _workspaceRepository;

    public ResumeProjectIndexingCommandHandler(
        IIndexingQueue indexingQueue,
        IProjectWorkspaceRepository workspaceRepository)
    {
        _indexingQueue = indexingQueue;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<ProjectIndexingControlResponse> Handle(
        ResumeProjectIndexingCommand request,
        CancellationToken cancellationToken)
    {
        var projectKey = request.ProjectKey.Trim();
        var workspace = await _workspaceRepository.GetAsync(projectKey, cancellationToken);

        if (workspace is null)
            throw new NotFoundException($"Project '{projectKey}' has not been indexed.");

        var activeRun = await _workspaceRepository.GetActiveRunAsync(projectKey, cancellationToken);
        if (activeRun is null)
            throw new ConflictException($"No paused indexing run was found for '{projectKey}'.");

        if (activeRun.Status is not IndexingRunState.Paused)
        {
            return new ProjectIndexingControlResponse(
                projectKey,
                activeRun.Status.ToString(),
                $"Indexing is already {activeRun.Status.ToString().ToLowerInvariant()} for '{projectKey}'.");
        }

        var resumedStatus = activeRun.StartedUtc.HasValue ? IndexingRunState.Running : IndexingRunState.Queued;
        var resumedWorkspaceStatus = activeRun.StartedUtc.HasValue ? ProjectStatus.Indexing : ProjectStatus.Queued;

        var resumedRun = new IndexingRun
        {
            RunId = activeRun.RunId,
            ProjectKey = activeRun.ProjectKey,
            RunType = activeRun.RunType,
            Status = resumedStatus,
            RequestedUtc = activeRun.RequestedUtc,
            StartedUtc = activeRun.StartedUtc,
            CompletedUtc = null,
            RequestedFilePath = activeRun.RequestedFilePath,
            TotalFilesPlanned = activeRun.TotalFilesPlanned,
            FilesScanned = activeRun.FilesScanned,
            FilesIndexed = activeRun.FilesIndexed,
            FilesSkipped = activeRun.FilesSkipped,
            SegmentsWritten = activeRun.SegmentsWritten,
            WarningCount = activeRun.WarningCount,
            CurrentFilePath = activeRun.CurrentFilePath,
            FailureReason = null
        };

        var resumedWorkspace = new ProjectWorkspace
        {
            ProjectKey = workspace.ProjectKey,
            SourceRootPath = workspace.SourceRootPath,
            Status = resumedWorkspaceStatus,
            TotalFiles = workspace.TotalFiles,
            TotalSegments = workspace.TotalSegments,
            LastIndexedUtc = workspace.LastIndexedUtc,
            LastRunId = activeRun.RunId,
            LastError = null
        };

        await _workspaceRepository.UpsertRunAsync(resumedRun, cancellationToken);
        await _workspaceRepository.UpsertAsync(resumedWorkspace, cancellationToken);
        await _indexingQueue.EnqueueAsync(
            new IndexingWorkItem(
                activeRun.RunId,
                activeRun.ProjectKey,
                workspace.SourceRootPath,
                activeRun.RunType,
                activeRun.RequestedFilePath),
            cancellationToken);

        return new ProjectIndexingControlResponse(
            projectKey,
            resumedStatus.ToString(),
            $"Indexing resumed for '{projectKey}'.");
    }
}
