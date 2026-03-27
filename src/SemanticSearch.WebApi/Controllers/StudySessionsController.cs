using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Application.Study.Queries;
using SemanticSearch.WebApi.Contracts.Study;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/study")]
public sealed class StudySessionsController : ControllerBase
{
    private readonly IMediator _mediator;

    public StudySessionsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("sessions")]
    public async Task<IActionResult> StartSession([FromBody] StartStudySessionRequest request, CancellationToken cancellationToken)
    {
        var session = await _mediator.Send(new StartStudySessionCommand(request.SessionType, request.BookId, request.ChapterId, request.IsPomodoro, request.FocusDurationMinutes), cancellationToken);
        return CreatedAtAction(nameof(Dashboard), new { sessionId = session.Id }, MapSession(session));
    }

    [HttpPatch("sessions/{sessionId}/end")]
    public async Task<IActionResult> EndSession([FromRoute] string sessionId, CancellationToken cancellationToken)
        => Ok(MapSession(await _mediator.Send(new EndStudySessionCommand(sessionId), cancellationToken)));

    [HttpGet("dashboard")]
    public async Task<IActionResult> Dashboard(CancellationToken cancellationToken)
        => Ok(MapDashboard(await _mediator.Send(new GetStudyDashboardQuery(), cancellationToken)));

    private static StudySessionResponse MapSession(StudySessionModel model)
        => new(model.Id, model.BookId, model.ChapterId, model.SessionType, model.StartedAt, model.EndedAt, model.DurationMinutes, model.IsPomodoroSession, model.FocusDurationMinutes);

    private static StudyDashboardResponse MapDashboard(StudyDashboardModel model)
        => new(model.StudyStreakDays, model.DuePlanItemsCount, model.DueFlashCardCount, model.RetentionRate, model.WeeklyHours.Select(item => new DailyStudyHours(item.Date, item.Hours)).ToList(), model.BookProgress.Select(item => new BookProgressResponse(item.BookId, item.BookTitle, item.CompletedChapters, item.TotalChapters, item.ProgressPercent)).ToList());
}