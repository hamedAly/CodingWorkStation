using MediatR;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Search.Queries;

public sealed record SearchProjectQuery(string Query, string ProjectKey, int TopK = 10)
    : IRequest<SearchProjectResponse>;

public sealed record SearchProjectResponse(IReadOnlyList<SearchResult> Results);
