using MediatR;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed record RetryProjectIndexingCommand(string ProjectKey) : IRequest<IndexProjectResponse>;
