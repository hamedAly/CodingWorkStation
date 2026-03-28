namespace SemanticSearch.Domain.ValueObjects;

public static class StudySessionType
{
    public const string Reading = "Reading";
    public const string Review = "Review";
    public const string Listening = "Listening";

    public static IReadOnlySet<string> All { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        Reading,
        Review,
        Listening
    };
}
