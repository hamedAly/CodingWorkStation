namespace SemanticSearch.Domain.Entities;

public sealed class StudySession
{
    public string Id { get; init; } = string.Empty;
    public string? BookId { get; init; }
    public string? ChapterId { get; init; }
    public string SessionType { get; init; } = string.Empty;
    public DateTime StartedAt { get; init; }
    public DateTime? EndedAt { get; init; }
    public int? DurationMinutes { get; init; }
    public bool IsPomodoroSession { get; init; }
    public int? FocusDurationMinutes { get; init; }
    public DateTime CreatedAt { get; init; }
}
