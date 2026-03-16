using MediatR;

namespace SemanticSearch.Application.Search.Queries;

public sealed record SearchExactQuery(string Keyword, string ProjectKey, bool MatchCase, int TopK = 50)
    : IRequest<SearchResponse>;
