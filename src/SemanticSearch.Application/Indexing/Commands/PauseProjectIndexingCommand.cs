using MediatR;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed record PauseProjectIndexingCommand(string ProjectKey) : IRequest<ProjectIndexingControlResponse>;

public sealed record ProjectIndexingControlResponse(string ProjectKey, string Status, string Message);
