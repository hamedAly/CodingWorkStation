using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static void RegisterAll(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<IntegrationOptions>>().Value;

        RecurringJob.AddOrUpdate<StandupJob>(
            "standup-daily",
            job => job.Execute(),
            options.StandupCron);

        RecurringJob.AddOrUpdate<PrayerTimeFetcherJob>(
            "prayer-time-fetcher",
            job => job.Execute(),
            options.PrayerFetchCron);
    }
}
