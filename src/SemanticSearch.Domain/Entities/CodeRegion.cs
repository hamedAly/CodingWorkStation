using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class CodeRegion
{
    public string RegionId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RelativeFilePath { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public string Snippet { get; init; } = string.Empty;
    public string ContentHash { get; init; } = string.Empty;
    public string? SourceSegmentId { get; init; }
    public CodeRegionAvailability Availability { get; init; } = CodeRegionAvailability.Available;
}
