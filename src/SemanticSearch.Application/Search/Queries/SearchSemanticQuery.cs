using MediatR;

namespace SemanticSearch.Application.Search.Queries;

public sealed record SearchSemanticQuery(string Query, string ProjectKey, int TopK = 5)
    : IRequest<SearchResponse>;
