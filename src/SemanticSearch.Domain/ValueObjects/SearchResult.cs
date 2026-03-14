namespace SemanticSearch.Domain.ValueObjects;

public sealed record SearchResult(
    string FilePath,
    float RelevanceScore,
    string Snippet,
    int StartLine,
    int EndLine);
