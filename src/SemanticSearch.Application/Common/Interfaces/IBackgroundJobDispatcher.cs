namespace SemanticSearch.Application.Common.Interfaces;

public interface IBackgroundJobDispatcher
{
    void EnqueueStandup();
    void EnqueuePrayerFetch();
    void EnqueueStudyReminder();
}
