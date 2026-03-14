namespace SemanticSearch.Domain.ValueObjects;

public sealed record IndexingStatus(
    bool IsIndexed,
    int TotalFiles,
    int TotalChunks,
    DateTime? LastUpdated);
