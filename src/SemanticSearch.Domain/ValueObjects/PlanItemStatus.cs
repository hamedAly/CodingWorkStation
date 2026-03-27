namespace SemanticSearch.Domain.ValueObjects;

public static class PlanItemStatus
{
    public const string Pending = "Pending";
    public const string InProgress = "InProgress";
    public const string Done = "Done";
    public const string Skipped = "Skipped";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Pending,
        InProgress,
        Done,
        Skipped
    };
}
