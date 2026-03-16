namespace SemanticSearch.Domain.ValueObjects;

public sealed record DetectedCodeRegion(
    string RelativeFilePath,
    int StartLine,
    int EndLine,
    string Snippet,
    string ContentHash,
    string? SourceSegmentId = null);
