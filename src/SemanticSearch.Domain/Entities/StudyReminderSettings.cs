namespace SemanticSearch.Domain.Entities;

public sealed class StudyReminderSettings
{
    public string SettingsId { get; init; } = "default";
    public bool Enabled { get; init; }
    public string ReminderTime { get; init; } = "08:00";
    public DateTime UpdatedUtc { get; init; }
}