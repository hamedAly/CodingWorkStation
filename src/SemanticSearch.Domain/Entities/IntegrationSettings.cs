namespace SemanticSearch.Domain.Entities;

public sealed class IntegrationSettings
{
    public string SettingsId { get; init; } = "default";
    public string StandupMessage { get; init; } = string.Empty;
    public bool StandupEnabled { get; init; }
    public string PrayerCity { get; init; } = string.Empty;
    public string PrayerCountry { get; init; } = string.Empty;
    public int PrayerMethod { get; init; } = 4;
    public bool PrayerEnabled { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
