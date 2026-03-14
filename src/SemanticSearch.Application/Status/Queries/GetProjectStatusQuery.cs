using MediatR;

namespace SemanticSearch.Application.Status.Queries;

public sealed record GetProjectStatusQuery(string ProjectKey) : IRequest<GetProjectStatusResponse>;

public sealed record GetProjectStatusResponse(
    bool IsIndexed,
    int TotalFiles,
    int TotalChunks,
    DateTime? LastUpdated);
