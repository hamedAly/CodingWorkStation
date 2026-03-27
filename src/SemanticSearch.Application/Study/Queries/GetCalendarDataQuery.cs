using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetCalendarDataQuery(int Year, int Month) : IRequest<IReadOnlyList<CalendarDayModel>>;

public sealed class GetCalendarDataQueryHandler : IRequestHandler<GetCalendarDataQuery, IReadOnlyList<CalendarDayModel>>
{
    private readonly IStudyRepository _studyRepository;

    public GetCalendarDataQueryHandler(IStudyRepository studyRepository)
    {
        _studyRepository = studyRepository;
    }

    public async Task<IReadOnlyList<CalendarDayModel>> Handle(GetCalendarDataQuery request, CancellationToken cancellationToken)
    {
        var plans = await _studyRepository.GetAllPlansAsync(cancellationToken);
        var planLookup = plans.ToDictionary(plan => plan.Id, plan => plan.Title, StringComparer.Ordinal);
        var items = await _studyRepository.GetCalendarItemsAsync(request.Year, request.Month, cancellationToken);

        return items
            .GroupBy(item => item.ScheduledDate.Date)
            .OrderBy(group => group.Key)
            .Select(group => new CalendarDayModel(
                group.Key,
                group.Select(item => new CalendarItemModel(item.Id, item.Title, item.Status, planLookup.GetValueOrDefault(item.PlanId))).ToList()))
            .ToList();
    }
}
