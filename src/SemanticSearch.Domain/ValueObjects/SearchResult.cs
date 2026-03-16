namespace SemanticSearch.Domain.ValueObjects;

public sealed record SearchResult(
    string RelativeFilePath,
    float Score,
    string Snippet,
    int StartLine,
    int EndLine,
    SearchMode MatchType);
