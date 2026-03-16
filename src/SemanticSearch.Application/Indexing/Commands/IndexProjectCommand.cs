using MediatR;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed record IndexProjectCommand(string ProjectPath, string ProjectKey) : IRequest<IndexProjectResponse>;

public sealed record IndexProjectResponse(string ProjectKey, string RunId, string Status, string Message);
