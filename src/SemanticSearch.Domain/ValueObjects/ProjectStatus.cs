namespace SemanticSearch.Domain.ValueObjects;

public enum ProjectStatus
{
    NotIndexed,
    Queued,
    Indexing,
    Paused,
    Indexed,
    Failed,
    Degraded
}
