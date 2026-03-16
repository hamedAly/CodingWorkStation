using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class DuplicationFinding
{
    public string FindingId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public DuplicationType Type { get; init; } = DuplicationType.Structural;
    public DuplicationSeverity Severity { get; init; } = DuplicationSeverity.Low;
    public double SimilarityScore { get; init; }
    public int MatchingLineCount { get; init; }
    public string? NormalizedFingerprint { get; init; }
    public string LeftRegionId { get; init; } = string.Empty;
    public string RightRegionId { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
}
