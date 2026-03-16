using MediatR;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Status.Queries;

public sealed record GetProjectStatusQuery(string ProjectKey) : IRequest<GetProjectStatusResponse>;

public sealed record GetProjectStatusResponse(
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
