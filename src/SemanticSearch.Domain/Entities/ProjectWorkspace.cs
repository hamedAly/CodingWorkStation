using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class ProjectWorkspace
{
    public string ProjectKey { get; init; } = string.Empty;
    public string SourceRootPath { get; init; } = string.Empty;
    public ProjectStatus Status { get; init; } = ProjectStatus.NotIndexed;
    public int TotalFiles { get; init; }
    public int TotalSegments { get; init; }
    public DateTime? LastIndexedUtc { get; init; }
    public string? LastRunId { get; init; }
    public string? LastError { get; init; }
}
