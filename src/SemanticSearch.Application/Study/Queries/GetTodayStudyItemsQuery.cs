using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetTodayStudyItemsQuery() : IRequest<TodayStudyItemsModel>;

public sealed class GetTodayStudyItemsQueryHandler : IRequestHandler<GetTodayStudyItemsQuery, TodayStudyItemsModel>
{
    private readonly IStudyRepository _studyRepository;
    private readonly IFlashCardRepository _flashCardRepository;

    public GetTodayStudyItemsQueryHandler(IStudyRepository studyRepository, IFlashCardRepository flashCardRepository)
    {
        _studyRepository = studyRepository;
        _flashCardRepository = flashCardRepository;
    }

    public async Task<TodayStudyItemsModel> Handle(GetTodayStudyItemsQuery request, CancellationToken cancellationToken)
    {
        var items = await _studyRepository.GetPlanItemsByDateAsync(DateTime.UtcNow.Date, cancellationToken);
        var dueItems = items
            .Where(item => item.Status is PlanItemStatus.Pending or PlanItemStatus.InProgress)
            .Select(item => item.ToModel())
            .ToList();

        var dueCardCount = await _flashCardRepository.GetDueCardCountAsync(DateTime.UtcNow.Date, cancellationToken);
        return new TodayStudyItemsModel(dueItems, dueCardCount, []);
    }
}
