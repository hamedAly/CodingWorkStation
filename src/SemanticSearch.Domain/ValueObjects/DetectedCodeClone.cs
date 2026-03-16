namespace SemanticSearch.Domain.ValueObjects;

public sealed record DetectedCodeClone(
    DuplicationType Type,
    double SimilarityScore,
    int MatchingLineCount,
    DetectedCodeRegion Left,
    DetectedCodeRegion Right,
    string? NormalizedFingerprint = null);
