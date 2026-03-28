using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetStudyDashboardQuery() : IRequest<StudyDashboardModel>;

public sealed class GetStudyDashboardQueryHandler : IRequestHandler<GetStudyDashboardQuery, StudyDashboardModel>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IFlashCardRepository _flashCardRepository;

    public GetStudyDashboardQueryHandler(IStudyRepository studyRepository, IFlashCardRepository flashCardRepository)
    {
        _studyRepository = studyRepository;
        _flashCardRepository = flashCardRepository;
    }

    public async Task<StudyDashboardModel> Handle(GetStudyDashboardQuery request, CancellationToken cancellationToken)
    {
        var today = DateTime.UtcNow.Date;
        var streakDays = await _studyRepository.GetStudyStreakDaysAsync(cancellationToken);
        var duePlanItems = await _studyRepository.GetPlanItemsByDateAsync(today, cancellationToken);
        var duePlanItemCount = duePlanItems.Count(item => item.Status is PlanItemStatus.Pending or PlanItemStatus.InProgress);
        var dueFlashCardCount = await _flashCardRepository.GetDueCardCountAsync(today, cancellationToken);
        var retentionRate = await _flashCardRepository.GetRetentionRateAsync(30, cancellationToken);
        var weeklyHours = await _studyRepository.GetWeeklyStudyHoursAsync(cancellationToken);
        var books = await _studyRepository.GetAllBooksAsync(cancellationToken);
        var plans = await _studyRepository.GetAllPlansAsync(cancellationToken);
        var bookProgress = new List<BookProgressModel>(books.Count);

        foreach (var book in books)
        {
            var chapters = await _studyRepository.GetChaptersByBookIdAsync(book.Id, cancellationToken);
            var planIds = plans.Where(plan => string.Equals(plan.BookId, book.Id, StringComparison.Ordinal)).Select(plan => plan.Id).ToList();
            var completedChapterIds = new HashSet<string>(StringComparer.Ordinal);

            foreach (var planId in planIds)
            {
                var items = await _studyRepository.GetPlanItemsByPlanIdAsync(planId, cancellationToken);
                foreach (var item in items.Where(item => item.Status == PlanItemStatus.Done && !string.IsNullOrWhiteSpace(item.ChapterId)))
                    completedChapterIds.Add(item.ChapterId!);
            }

            var totalChapters = chapters.Count;
            var completedChapters = chapters.Count(chapter => completedChapterIds.Contains(chapter.Id));
            var progressPercent = totalChapters == 0 ? 0d : Math.Round(completedChapters * 100d / totalChapters, 1, MidpointRounding.AwayFromZero);
            bookProgress.Add(new BookProgressModel(book.Id, book.Title, completedChapters, totalChapters, progressPercent));
        }

        return new StudyDashboardModel(streakDays, duePlanItemCount, dueFlashCardCount, retentionRate, weeklyHours.Select(item => new DailyStudyHoursModel(item.Date, item.Hours)).ToList(), bookProgress);
    }
}