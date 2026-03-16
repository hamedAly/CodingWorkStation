using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Status.Queries;

public sealed class GetProjectStatusQueryHandler : IRequestHandler<GetProjectStatusQuery, GetProjectStatusResponse>
{
    private readonly IProjectWorkspaceRepository _workspaceRepository;

    public GetProjectStatusQueryHandler(IProjectWorkspaceRepository workspaceRepository)
    {
        _workspaceRepository = workspaceRepository;
    }

    public async Task<GetProjectStatusResponse> Handle(GetProjectStatusQuery request, CancellationToken cancellationToken)
    {
        var workspace = await _workspaceRepository.GetAsync(request.ProjectKey, cancellationToken);

        if (workspace is null)
        {
            return new GetProjectStatusResponse(
                request.ProjectKey,
                ProjectStatus.NotIndexed,
                0,
                0,
                false,
                false,
                false,
                null,
                0,
                0,
                0,
                null,
                null,
                null);
        }

        var run = workspace.LastRunId is { Length: > 0 }
            ? await _workspaceRepository.GetRunAsync(workspace.LastRunId, cancellationToken)
            : null;

        return new GetProjectStatusResponse(
            workspace.ProjectKey,
            workspace.Status,
            workspace.TotalFiles,
            workspace.TotalSegments,
            workspace.Status is ProjectStatus.Queued or ProjectStatus.Indexing or ProjectStatus.Paused,
            run?.Status is IndexingRunState.Queued or IndexingRunState.Running,
            run?.Status is IndexingRunState.Paused,
            run?.RunId,
            run?.FilesScanned ?? 0,
            run?.TotalFilesPlanned ?? 0,
            CalculateProgressPercent(run, workspace),
            run?.CurrentFilePath ?? run?.RequestedFilePath,
            workspace.LastIndexedUtc,
            workspace.LastError ?? run?.FailureReason);
    }

    private static double CalculateProgressPercent(SemanticSearch.Domain.Entities.IndexingRun? run, SemanticSearch.Domain.Entities.ProjectWorkspace workspace)
    {
        if (run is null)
            return workspace.Status is ProjectStatus.Indexed ? 100 : 0;

        if (run.Status is IndexingRunState.Completed)
            return 100;

        if (run.TotalFilesPlanned <= 0)
            return 0;

        var hasActiveFile = run.Status is IndexingRunState.Running or IndexingRunState.Paused &&
            !string.IsNullOrWhiteSpace(run.CurrentFilePath) &&
            run.FilesScanned < run.TotalFilesPlanned;

        var processedUnits = run.FilesScanned + (hasActiveFile ? 0.5 : 0);
        var progress = Math.Min(100, (processedUnits * 100d) / run.TotalFilesPlanned);
        return Math.Round(hasActiveFile ? Math.Max(0.1, progress) : progress, 1);
    }
}
