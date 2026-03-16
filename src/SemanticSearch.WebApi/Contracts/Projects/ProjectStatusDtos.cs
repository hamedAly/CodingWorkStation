namespace SemanticSearch.WebApi.Contracts.Projects;

public sealed record ProjectStatusResponse(
    string ProjectKey,
    string Status,
    int TotalFiles,
    int TotalSegments,
    bool IndexingInProgress,
    bool CanPause,
    bool CanResume,
    string? RunId,
    int FilesProcessed,
    int TotalFilesPlanned,
    double ProgressPercent,
    string? CurrentFilePath,
    DateTime? LastIndexedUtc,
    string? LastError);

public sealed record ProjectWorkspaceSummaryResponse(
    string ProjectKey,
    string Status,
    int TotalFiles,
    int TotalSegments,
    bool IndexingInProgress,
    bool CanPause,
    bool CanResume,
    string? RunId,
    int FilesProcessed,
    int TotalFilesPlanned,
    double ProgressPercent,
    string? CurrentFilePath,
    DateTime? LastIndexedUtc,
    string? LastError);

public sealed record IndexingControlResponse(string ProjectKey, string Status, string Message);

public sealed record ProjectTreeNodeResponse(
    string Path,
    string Name,
    string NodeType,
    string? RelativeFilePath,
    IReadOnlyList<ProjectTreeNodeResponse> Children);
