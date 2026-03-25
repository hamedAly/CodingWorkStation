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
    DateTime? UpdatedUtc);

public sealed record GetIntegrationSettingsQuery : IRequest<IntegrationSettingsModel>;

public sealed class GetIntegrationSettingsQueryHandler : IRequestHandler<GetIntegrationSettingsQuery, IntegrationSettingsModel>
{
    private readonly IIntegrationSettingsRepository _repo;

    public GetIntegrationSettingsQueryHandler(IIntegrationSettingsRepository repo)
    {
        _repo = repo;
    }

    public async Task<IntegrationSettingsModel> Handle(GetIntegrationSettingsQuery request, CancellationToken cancellationToken)
    {
        var settings = await _repo.GetAsync(cancellationToken);
        if (settings is null)
            return new IntegrationSettingsModel(string.Empty, false, string.Empty, string.Empty, 4, false, null);

        return new IntegrationSettingsModel(
            settings.StandupMessage,
            settings.StandupEnabled,
            settings.PrayerCity,
            settings.PrayerCountry,
            settings.PrayerMethod,
            settings.PrayerEnabled,
            settings.UpdatedUtc);
    }
}
