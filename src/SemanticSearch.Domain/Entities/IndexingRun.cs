using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class IndexingRun
{
    public string RunId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public IndexingRunType RunType { get; init; } = IndexingRunType.Full;
    public IndexingRunState Status { get; init; } = IndexingRunState.Queued;
    public DateTime RequestedUtc { get; init; }
    public DateTime? StartedUtc { get; init; }
    public DateTime? CompletedUtc { get; init; }
    public string? RequestedFilePath { get; init; }
    public int TotalFilesPlanned { get; init; }
    public int FilesScanned { get; init; }
    public int FilesIndexed { get; init; }
    public int FilesSkipped { get; init; }
    public int SegmentsWritten { get; init; }
    public int WarningCount { get; init; }
    public string? CurrentFilePath { get; init; }
    public string? FailureReason { get; init; }
}
