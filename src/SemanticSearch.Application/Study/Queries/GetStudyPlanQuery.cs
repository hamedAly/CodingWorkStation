using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetStudyPlanQuery(string PlanId) : IRequest<StudyPlanDetailModel>;

public sealed class GetStudyPlanQueryHandler : IRequestHandler<GetStudyPlanQuery, StudyPlanDetailModel>
{
    private readonly IStudyRepository _studyRepository;

    public GetStudyPlanQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<StudyPlanDetailModel> Handle(GetStudyPlanQuery request, CancellationToken cancellationToken)
    {
        var plan = await _studyRepository.GetPlanByIdAsync(request.PlanId, cancellationToken)
            ?? throw new NotFoundException($"Study plan '{request.PlanId}' was not found.");
        var items = await _studyRepository.GetPlanItemsByPlanIdAsync(request.PlanId, cancellationToken);
        var bookTitle = string.IsNullOrWhiteSpace(plan.BookId)
            ? null
            : (await _studyRepository.GetBookByIdAsync(plan.BookId, cancellationToken))?.Title;

        var total = items.Count(item => item.Status != PlanItemStatus.Skipped);
        var completed = items.Count(item => item.Status == PlanItemStatus.Done);
        var progress = total == 0 ? 0d : Math.Round(completed * 100d / total, 1, MidpointRounding.AwayFromZero);

        return new StudyPlanDetailModel(plan.Id, plan.Title, plan.BookId, bookTitle, plan.StartDate, plan.EndDate, plan.Status, plan.SkipWeekends, items.Select(item => item.ToModel()).ToList(), progress);
    }
}
