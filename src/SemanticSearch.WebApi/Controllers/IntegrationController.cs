using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Slack.Commands;
using SemanticSearch.Application.Slack.Queries;
using SemanticSearch.WebApi.Contracts.Slack;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/integration")]
public sealed class IntegrationController : ControllerBase
{
    private readonly IMediator _mediator;

    public IntegrationController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("settings")]
    [ProducesResponseType(typeof(IntegrationSettingsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetIntegrationSettingsQuery(), cancellationToken);
        return Ok(new IntegrationSettingsResponse(
            result.StandupMessage,
            result.StandupEnabled,
            result.PrayerCity,
            result.PrayerCountry,
            result.PrayerMethod,
            result.PrayerEnabled,
            result.UpdatedUtc));
    }

    [HttpPut("settings")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateSettings([FromBody] UpdateIntegrationSettingsRequest request, CancellationToken cancellationToken)
    {
        await _mediator.Send(new UpdateIntegrationSettingsCommand(
            request.StandupMessage,
            request.StandupEnabled,
            request.PrayerCity,
            request.PrayerCountry,
            request.PrayerMethod,
            request.PrayerEnabled), cancellationToken);
        return NoContent();
    }

    [HttpPost("jobs/{jobName}/trigger")]
    [ProducesResponseType(typeof(TriggerJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> TriggerJob([FromRoute] string jobName, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TriggerJobCommand(jobName), cancellationToken);
        if (!result.Queued)
            return BadRequest(new ProblemDetails { Title = "Invalid job name", Detail = result.Error });
        return Ok(new TriggerJobResponse(result.Queued, result.Error));
    }
}
