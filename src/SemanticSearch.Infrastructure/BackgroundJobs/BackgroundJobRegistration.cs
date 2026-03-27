using Hangfire;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public static class BackgroundJobRegistration
{
    public static void RegisterAll(IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<IntegrationOptions>>().Value;
        var studyReminderSettings = services.GetRequiredService<IStudyReminderSettingsRepository>().GetAsync().GetAwaiter().GetResult();

        RecurringJob.AddOrUpdate<StandupJob>(
            "standup-daily",
            job => job.Execute(),
            options.StandupCron);

        RecurringJob.AddOrUpdate<PrayerTimeFetcherJob>(
            "prayer-time-fetcher",
            job => job.Execute(),
            options.PrayerFetchCron);

        if (studyReminderSettings?.Enabled == true)
        {
            RecurringJob.AddOrUpdate<StudyReminderJob>(
                "study-daily-reminder",
                job => job.Execute(),
                BuildDailyCron(studyReminderSettings.ReminderTime));
        }
        else
        {
            RecurringJob.RemoveIfExists("study-daily-reminder");
        }
    }

    private static string BuildDailyCron(string reminderTime)
    {
        if (TimeOnly.TryParse(reminderTime, out var parsedTime))
            return $"{parsedTime.Minute} {parsedTime.Hour} * * *";

        return "0 8 * * *";
    }
}
