using MediatR;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed record ResumeProjectIndexingCommand(string ProjectKey) : IRequest<ProjectIndexingControlResponse>;
