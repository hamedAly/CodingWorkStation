using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Slack.Commands;
using SemanticSearch.Application.Slack.Queries;
using SemanticSearch.WebApi.Contracts.Tfs;
using SemanticSearch.WebApi.Contracts.Slack;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/slack")]
public sealed class SlackController : ControllerBase
{
    private readonly IMediator _mediator;

    public SlackController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("credentials")]
    [ProducesResponseType(typeof(SlackCredentialStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCredentialStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetSlackCredentialStatusQuery(), cancellationToken);
        return Ok(new SlackCredentialStatusResponse(result.IsConfigured, result.HasUserToken, result.DefaultChannel, result.UpdatedUtc));
    }

    [HttpPost("credentials")]
    [ProducesResponseType(typeof(SaveCredentialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveCredential([FromBody] SaveSlackCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SaveSlackCredentialCommand(request.BotToken, request.UserToken, request.DefaultChannel), cancellationToken);
        return Ok(new SaveCredentialResponse(result.Success, result.Error));
    }

    [HttpDelete("credentials")]
    [ProducesResponseType(typeof(DeleteCredentialResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCredential(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteSlackCredentialCommand(), cancellationToken);
        return Ok(new DeleteCredentialResponse(true));
    }

    [HttpPost("credentials/test")]
    [ProducesResponseType(typeof(TestConnectionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TestSlackConnectionQuery(), cancellationToken);
        return Ok(new TestConnectionResponse(result.Success, result.Error));
    }
}
