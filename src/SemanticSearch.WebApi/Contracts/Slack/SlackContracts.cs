namespace SemanticSearch.WebApi.Contracts.Slack;

public sealed record SaveSlackCredentialRequest(
    string BotToken,
    string? UserToken,
    string DefaultChannel);

public sealed record SlackCredentialStatusResponse(
    bool IsConfigured,
    bool HasUserToken,
    string? DefaultChannel,
    DateTime? UpdatedUtc);

public sealed record IntegrationSettingsResponse(
    string StandupMessage,
    bool StandupEnabled,
    string PrayerCity,
    string PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled,
    bool StudyReminderEnabled,
    string StudyReminderTime,
    DateTime? UpdatedUtc);

public sealed record UpdateIntegrationSettingsRequest(
    string StandupMessage,
    bool StandupEnabled,
    string PrayerCity,
    string PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled,
    bool StudyReminderEnabled,
    string StudyReminderTime);

public sealed record TriggerJobResponse(bool Queued, string? Error);
