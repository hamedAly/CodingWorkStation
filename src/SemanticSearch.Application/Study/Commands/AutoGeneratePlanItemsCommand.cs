using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record AutoGeneratePlanItemsCommand(string PlanId) : IRequest<StudyPlanDetailModel>;

public sealed class AutoGeneratePlanItemsCommandHandler : IRequestHandler<AutoGeneratePlanItemsCommand, StudyPlanDetailModel>
{
    private readonly IStudyRepository _studyRepository;

    public AutoGeneratePlanItemsCommandHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<StudyPlanDetailModel> Handle(AutoGeneratePlanItemsCommand request, CancellationToken cancellationToken)
    {
        var plan = await _studyRepository.GetPlanByIdAsync(request.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Study plan '{request.PlanId}' was not found.");

        if (string.IsNullOrWhiteSpace(plan.BookId))
            throw new InvalidOperationException("A book must be selected before auto-generating a study plan.");

        var book = await _studyRepository.GetBookByIdAsync(plan.BookId, cancellationToken)
            ?? throw new NotFoundException($"Study book '{plan.BookId}' was not found.");
        var chapters = await _studyRepository.GetChaptersByBookIdAsync(plan.BookId, cancellationToken);
        if (chapters.Count == 0)
            throw new InvalidOperationException("Add at least one chapter before auto-generating a study plan.");

        var dates = GetAvailableDates(plan.StartDate.Date, plan.EndDate.Date, plan.SkipWeekends);
        if (dates.Count == 0)
            throw new InvalidOperationException("No available schedule dates were found for this plan.");

        var existingItems = await _studyRepository.GetPlanItemsByPlanIdAsync(plan.Id, cancellationToken);
        if (existingItems.Count > 0)
        {
            return new StudyPlanDetailModel(
                plan.Id,
                plan.Title,
                plan.BookId,
                book.Title,
                plan.StartDate,
                plan.EndDate,
                plan.Status,
                plan.SkipWeekends,
                existingItems.Select(item => item.ToModel()).ToList(),
                CalculateProgress(existingItems));
        }

        var items = chapters
            .OrderBy(chapter => chapter.SortOrder)
            .Select((chapter, index) => new StudyPlanItem
            {
                Id = Guid.NewGuid().ToString("N"),
                PlanId = plan.Id,
                ChapterId = chapter.Id,
                Title = chapter.Title,
                ScheduledDate = dates[index % dates.Count],
                Status = SemanticSearch.Domain.ValueObjects.PlanItemStatus.Pending,
                CompletedDate = null,
                SortOrder = index,
                CreatedAt = DateTime.UtcNow
            })
            .OrderBy(item => item.ScheduledDate)
            .ThenBy(item => item.SortOrder)
            .ToList();

        await _studyRepository.InsertPlanItemsAsync(items, cancellationToken);
        return new StudyPlanDetailModel(plan.Id, plan.Title, plan.BookId, book.Title, plan.StartDate, plan.EndDate, plan.Status, plan.SkipWeekends, items.Select(item => item.ToModel()).ToList(), 0d);
    }

    private static List<DateTime> GetAvailableDates(DateTime startDate, DateTime endDate, bool skipWeekends)
    {
        var dates = new List<DateTime>();
        for (var day = startDate; day <= endDate; day = day.AddDays(1))
        {
            if (skipWeekends && (day.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday))
                continue;

            dates.Add(day);
        }

        return dates;
    }

    private static double CalculateProgress(IReadOnlyList<StudyPlanItem> items)
    {
        var total = items.Count(item => item.Status != SemanticSearch.Domain.ValueObjects.PlanItemStatus.Skipped);
        if (total == 0)
            return 0d;

        var completed = items.Count(item => item.Status == SemanticSearch.Domain.ValueObjects.PlanItemStatus.Done);
        return Math.Round(completed * 100d / total, 1, MidpointRounding.AwayFromZero);
    }
}
