namespace SemanticSearch.Domain.Entities;

public sealed class ProjectMetadata
{
    public string ProjectKey { get; init; } = string.Empty;
    public int TotalFiles { get; init; }
    public int TotalChunks { get; init; }
    public DateTime LastUpdated { get; init; }
}
