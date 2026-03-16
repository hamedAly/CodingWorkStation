namespace SemanticSearch.Domain.ValueObjects;

public enum IndexingRunState
{
    Queued,
    Running,
    Paused,
    Completed,
    Failed,
    Cancelled
}
