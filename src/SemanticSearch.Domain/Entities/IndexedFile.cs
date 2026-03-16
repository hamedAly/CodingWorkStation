using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class IndexedFile
{
    public string ProjectKey { get; init; } = string.Empty;
    public string RelativeFilePath { get; init; } = string.Empty;
    public string AbsoluteFilePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Checksum { get; init; } = string.Empty;
    public long SizeBytes { get; init; }
    public DateTime LastModifiedUtc { get; init; }
    public DateTime LastIndexedUtc { get; init; }
    public int SegmentCount { get; init; }
    public ProjectFileAvailability Availability { get; init; } = ProjectFileAvailability.Available;
}
