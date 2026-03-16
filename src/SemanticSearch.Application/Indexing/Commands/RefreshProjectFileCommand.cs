using MediatR;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed record RefreshProjectFileCommand(string ProjectKey, string RelativeFilePath)
    : IRequest<RefreshProjectFileResponse>;

public sealed record RefreshProjectFileResponse(string ProjectKey, string RunId, string Status, string Message);
