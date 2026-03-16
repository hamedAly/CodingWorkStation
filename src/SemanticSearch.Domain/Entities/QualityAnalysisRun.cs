using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class QualityAnalysisRun
{
    public string RunId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RequestedModes { get; init; } = string.Empty;
    public QualityAnalysisStatus Status { get; init; } = QualityAnalysisStatus.Queued;
    public DateTime RequestedUtc { get; init; }
    public DateTime? StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
    public int TotalFilesScanned { get; init; }
    public int TotalLinesAnalyzed { get; init; }
    public int StructuralFindingCount { get; init; }
    public int SemanticFindingCount { get; init; }
    public string? FailureReason { get; init; }
}
