using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Search.Queries;

public sealed record SearchResponse(string ProjectKey, string Mode, IReadOnlyList<SearchResult> Results);
