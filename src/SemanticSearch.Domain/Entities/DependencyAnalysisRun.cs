using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class DependencyAnalysisRun
{
    public string RunId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public DependencyAnalysisStatus Status { get; init; } = DependencyAnalysisStatus.Queued;
    public DateTime RequestedUtc { get; init; }
    public DateTime? StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
    public int TotalFilesScanned { get; init; }
    public int TotalNodesFound { get; init; }
    public int TotalEdgesFound { get; init; }
    public string? FailureReason { get; init; }
}
