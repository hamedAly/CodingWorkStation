namespace SemanticSearch.Domain.Entities;

public sealed class StudyPlanItem
{
    public string Id { get; init; } = string.Empty;
    public string PlanId { get; init; } = string.Empty;
    public string? ChapterId { get; init; }
    public string Title { get; init; } = string.Empty;
    public DateTime ScheduledDate { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime? CompletedDate { get; init; }
    public int SortOrder { get; init; }
    public DateTime CreatedAt { get; init; }
}
