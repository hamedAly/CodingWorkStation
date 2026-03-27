namespace SemanticSearch.Domain.ValueObjects;

public static class StudyPlanStatus
{
    public const string Draft = "Draft";
    public const string Active = "Active";
    public const string Paused = "Paused";
    public const string Completed = "Completed";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Draft,
        Active,
        Paused,
        Completed
    };
}
