using MediatR;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Queries;

public sealed record IntegrationSettingsModel(
    string StandupMessage,
    bool StandupEnabled,
    string PrayerCity,
    string PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled,
    bool StudyReminderEnabled,
    string StudyReminderTime,
    DateTime? UpdatedUtc);

public sealed record GetIntegrationSettingsQuery : IRequest<IntegrationSettingsModel>;

public sealed class GetIntegrationSettingsQueryHandler : IRequestHandler<GetIntegrationSettingsQuery, IntegrationSettingsModel>
{
    private readonly IIntegrationSettingsRepository _repo;
    private readonly IStudyReminderSettingsRepository _studyReminderSettingsRepository;

    public GetIntegrationSettingsQueryHandler(IIntegrationSettingsRepository repo, IStudyReminderSettingsRepository studyReminderSettingsRepository)
    {
        _repo = repo;
        _studyReminderSettingsRepository = studyReminderSettingsRepository;
    }

    public async Task<IntegrationSettingsModel> Handle(GetIntegrationSettingsQuery request, CancellationToken cancellationToken)
    {
        var studyReminderSettings = await _studyReminderSettingsRepository.GetAsync(cancellationToken);
        var settings = await _repo.GetAsync(cancellationToken);
        if (settings is null)
            return new IntegrationSettingsModel(string.Empty, false, string.Empty, string.Empty, 4, false, studyReminderSettings?.Enabled ?? false, studyReminderSettings?.ReminderTime ?? "08:00", studyReminderSettings?.UpdatedUtc);

        return new IntegrationSettingsModel(
            settings.StandupMessage,
            settings.StandupEnabled,
            settings.PrayerCity,
            settings.PrayerCountry,
            settings.PrayerMethod,
            settings.PrayerEnabled,
            studyReminderSettings?.Enabled ?? false,
            studyReminderSettings?.ReminderTime ?? "08:00",
            Max(settings.UpdatedUtc, studyReminderSettings?.UpdatedUtc));
    }

    private static DateTime? Max(DateTime? left, DateTime? right)
        => left is null ? right : right is null ? left : left > right ? left : right;
}
