using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Queries;

public sealed record ListStudyPlansQuery() : IRequest<IReadOnlyList<StudyPlanSummaryModel>>;

public sealed class ListStudyPlansQueryHandler : IRequestHandler<ListStudyPlansQuery, IReadOnlyList<StudyPlanSummaryModel>>
{
    private readonly IStudyRepository _studyRepository;

    public ListStudyPlansQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<IReadOnlyList<StudyPlanSummaryModel>> Handle(ListStudyPlansQuery request, CancellationToken cancellationToken)
    {
        var plans = await _studyRepository.GetAllPlansAsync(cancellationToken);
        var results = new List<StudyPlanSummaryModel>(plans.Count);

        foreach (var plan in plans)
        {
            var items = await _studyRepository.GetPlanItemsByPlanIdAsync(plan.Id, cancellationToken);
            var total = items.Count(item => item.Status != PlanItemStatus.Skipped);
            var completed = items.Count(item => item.Status == PlanItemStatus.Done);
            var progress = total == 0 ? 0d : Math.Round(completed * 100d / total, 1, MidpointRounding.AwayFromZero);
            var bookTitle = string.IsNullOrWhiteSpace(plan.BookId)
                ? null
                : (await _studyRepository.GetBookByIdAsync(plan.BookId, cancellationToken))?.Title;

            results.Add(new StudyPlanSummaryModel(plan.Id, plan.Title, plan.BookId, bookTitle, plan.StartDate, plan.EndDate, plan.Status, total, completed, progress));
        }

        return results;
    }
}
