using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Application.Study.Queries;
using SemanticSearch.WebApi.Contracts.Study;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/study")]
public sealed class StudyPlansController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudyPlansController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("plans")]
    public async Task<IActionResult> ListPlans(CancellationToken cancellationToken)
        => Ok((await _mediator.Send(new ListStudyPlansQuery(), cancellationToken)).Select(MapSummary).ToList());

    [HttpPost("plans")]
    public async Task<IActionResult> CreatePlan([FromBody] CreateStudyPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _mediator.Send(new CreateStudyPlanCommand(request.Title, request.BookId, request.StartDate, request.EndDate, request.SkipWeekends), cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { planId = plan.Id }, MapDetail(plan));
    }

    [HttpGet("plans/{planId}")]
    public async Task<IActionResult> GetPlan([FromRoute] string planId, CancellationToken cancellationToken)
        => Ok(MapDetail(await _mediator.Send(new GetStudyPlanQuery(planId), cancellationToken)));

    [HttpPost("plans/{planId}/auto-generate")]
    public async Task<IActionResult> AutoGenerate([FromRoute] string planId, CancellationToken cancellationToken)
        => Ok(MapDetail(await _mediator.Send(new AutoGeneratePlanItemsCommand(planId), cancellationToken)));

    [HttpPatch("plans/{planId}/items/{itemId}/status")]
    public async Task<IActionResult> UpdateItemStatus([FromRoute] string planId, [FromRoute] string itemId, [FromBody] UpdatePlanItemStatusRequest request, CancellationToken cancellationToken)
        => Ok(MapItem(await _mediator.Send(new UpdatePlanItemStatusCommand(planId, itemId, request.Status), cancellationToken)));

    [HttpGet("today")]
    public async Task<IActionResult> Today(CancellationToken cancellationToken)
        => Ok(MapToday(await _mediator.Send(new GetTodayStudyItemsQuery(), cancellationToken)));

    [HttpGet("calendar")]
    public async Task<IActionResult> Calendar([FromQuery] int year, [FromQuery] int month, CancellationToken cancellationToken)
        => Ok((await _mediator.Send(new GetCalendarDataQuery(year, month), cancellationToken)).Select(MapCalendarDay).ToList());

    private static StudyPlanSummaryResponse MapSummary(StudyPlanSummaryModel model)
        => new(model.Id, model.Title, model.BookId, model.BookTitle, model.StartDate, model.EndDate, model.Status, model.TotalItems, model.CompletedItems, model.ProgressPercent);

    private static StudyPlanDetailResponse MapDetail(StudyPlanDetailModel model)
        => new(model.Id, model.Title, model.BookId, model.BookTitle, model.StartDate, model.EndDate, model.Status, model.SkipWeekends, model.Items.Select(MapItem).ToList(), model.ProgressPercent);

    private static PlanItemResponse MapItem(PlanItemModel model)
        => new(model.Id, model.ChapterId, model.Title, model.ScheduledDate, model.Status, model.CompletedDate, model.SortOrder);

    private static TodayStudyItemsResponse MapToday(TodayStudyItemsModel model)
        => new(model.PlanItems.Select(MapItem).ToList(), model.DueFlashCardCount, model.DueDeckIds);

    private static CalendarDayResponse MapCalendarDay(CalendarDayModel model)
        => new(model.Date, model.Items.Select(item => new CalendarItemResponse(item.Id, item.Title, item.Status, item.PlanTitle)).ToList());
}
