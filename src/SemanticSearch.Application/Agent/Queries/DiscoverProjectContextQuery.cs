using MediatR;

namespace SemanticSearch.Application.Agent.Queries;

public sealed record DiscoverProjectContextQuery(string ProjectKey, string Query) : IRequest<DiscoverProjectContextResponse>;

public sealed record DiscoverProjectContextResponse(string ProjectKey, string Query, string Markdown);
