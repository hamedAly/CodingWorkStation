using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface IStudyReminderSettingsRepository
{
    Task<StudyReminderSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(StudyReminderSettings settings, CancellationToken cancellationToken = default);
}