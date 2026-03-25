using Hangfire;
using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public sealed class PrayerTimeFetcherJob
{
    private readonly ICredentialRepository _credentialRepository;
    private readonly IIntegrationSettingsRepository _settingsRepository;
    private readonly IAladhanApiClient _aladhanClient;
    private readonly ILogger<PrayerTimeFetcherJob> _logger;

    public PrayerTimeFetcherJob(
        ICredentialRepository credentialRepository,
        IIntegrationSettingsRepository settingsRepository,
        IAladhanApiClient aladhanClient,
        ILogger<PrayerTimeFetcherJob> logger)
    {
        _credentialRepository = credentialRepository;
        _settingsRepository = settingsRepository;
        _aladhanClient = aladhanClient;
        _logger = logger;
    }

    public async Task Execute()
    {
        var settings = await _settingsRepository.GetAsync();
        if (settings is null || !settings.PrayerEnabled)
        {
            _logger.LogInformation("Prayer time fetcher skipped: not enabled or no settings configured.");
            return;
        }

        var prayerTimes = await _aladhanClient.GetPrayerTimesAsync(
            settings.PrayerCity,
            settings.PrayerCountry,
            settings.PrayerMethod);

        if (prayerTimes is null)
        {
            _logger.LogWarning("Prayer time fetcher: failed to retrieve prayer times for {City}, {Country}.",
                settings.PrayerCity, settings.PrayerCountry);
            return;
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var prayers = new[]
        {
            ("Fajr", prayerTimes.Fajr),
            ("Dhuhr", prayerTimes.Dhuhr),
            ("Asr", prayerTimes.Asr),
            ("Maghrib", prayerTimes.Maghrib),
            ("Isha", prayerTimes.Isha)
        };

        foreach (var (name, time) in prayers)
        {
            var scheduleAt = today.ToDateTime(time, DateTimeKind.Local);
            if (scheduleAt > DateTime.Now)
            {
                BackgroundJob.Schedule<PrayerStatusUpdaterJob>(
                    job => job.Execute(name),
                    scheduleAt - DateTime.Now);
                _logger.LogInformation("Scheduled prayer status job for {Prayer} at {Time}.", name, scheduleAt);
            }
        }
    }
}
