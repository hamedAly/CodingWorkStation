using Hangfire;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Infrastructure.BackgroundJobs;

namespace SemanticSearch.Infrastructure.BackgroundJobs;

public sealed class HangfireJobDispatcher : IBackgroundJobDispatcher
{
    public void EnqueueStandup()
        => BackgroundJob.Enqueue<StandupJob>(j => j.Execute());

    public void EnqueuePrayerFetch()
        => BackgroundJob.Enqueue<PrayerTimeFetcherJob>(j => j.Execute());
}
