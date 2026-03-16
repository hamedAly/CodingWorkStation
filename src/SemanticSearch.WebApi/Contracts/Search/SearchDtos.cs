namespace SemanticSearch.WebApi.Contracts.Search;

public sealed record SemanticSearchRequest(string Query, string ProjectKey, int TopK = 5);

public sealed record ExactSearchRequest(string Keyword, string ProjectKey, bool MatchCase = false, int TopK = 50);

public sealed record SearchResponse(
    string ProjectKey,
    string Mode,
    IReadOnlyList<SearchResultResponse> Results);

public sealed record SearchResultResponse(
    string RelativeFilePath,
    float Score,
    string Snippet,
    int StartLine,
    int EndLine,
    string MatchType);
