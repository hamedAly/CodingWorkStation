namespace SemanticSearch.Domain.ValueObjects;

public sealed record IndexingStatus(
    string ProjectKey,
    ProjectStatus Status,
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
