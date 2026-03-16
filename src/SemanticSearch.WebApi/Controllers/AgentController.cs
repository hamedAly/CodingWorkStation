using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Agent.Queries;
using SemanticSearch.WebApi.Contracts.Agent;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/agent")]
public sealed class AgentController : ControllerBase
{
    private readonly IMediator _mediator;

    public AgentController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("discover")]
    [Produces("text/markdown")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Discover([FromBody] AgentDiscoverRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new DiscoverProjectContextQuery(request.ProjectKey, request.Query),
            cancellationToken);

        return Content(response.Markdown, "text/markdown; charset=utf-8");
    }
}
