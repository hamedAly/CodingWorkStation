using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface IStudyRepository
{
    Task<IReadOnlyList<StudyBook>> GetAllBooksAsync(CancellationToken cancellationToken = default);
    Task<StudyBook?> GetBookByIdAsync(string bookId, CancellationToken cancellationToken = default);
    Task InsertBookAsync(StudyBook book, CancellationToken cancellationToken = default);
    Task UpdateBookAsync(StudyBook book, CancellationToken cancellationToken = default);
    Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default);
    Task UpdateLastReadPageAsync(string bookId, int page, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyChapter>> GetChaptersByBookIdAsync(string bookId, CancellationToken cancellationToken = default);
    Task<StudyChapter?> GetChapterByIdAsync(string chapterId, CancellationToken cancellationToken = default);
    Task InsertChapterAsync(StudyChapter chapter, CancellationToken cancellationToken = default);
    Task UpdateChapterAsync(StudyChapter chapter, CancellationToken cancellationToken = default);
    Task DeleteChapterAsync(string chapterId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyPlan>> GetAllPlansAsync(CancellationToken cancellationToken = default);
    Task<StudyPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken = default);
    Task InsertPlanAsync(StudyPlan plan, CancellationToken cancellationToken = default);
    Task UpdatePlanAsync(StudyPlan plan, CancellationToken cancellationToken = default);
    Task DeletePlanAsync(string planId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByPlanIdAsync(string planId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByDateAsync(DateTime date, CancellationToken cancellationToken = default);
    Task InsertPlanItemsAsync(IReadOnlyList<StudyPlanItem> items, CancellationToken cancellationToken = default);
    Task UpdatePlanItemStatusAsync(string itemId, string status, DateTime? completedDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudyPlanItem>> GetCalendarItemsAsync(int year, int month, CancellationToken cancellationToken = default);

    Task InsertSessionAsync(StudySession session, CancellationToken cancellationToken = default);
    Task UpdateSessionEndAsync(string sessionId, DateTime endedAt, int durationMinutes, CancellationToken cancellationToken = default);
    Task<StudySession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default);
    Task<int> GetStudyStreakDaysAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<(DateTime Date, double Hours)>> GetWeeklyStudyHoursAsync(CancellationToken cancellationToken = default);
}
