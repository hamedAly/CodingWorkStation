using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Files.Queries;
using SemanticSearch.WebApi.Contracts.Files;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/file")]
public sealed class FileController : ControllerBase
{
    private readonly IMediator _mediator;

    public FileController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("read")]
    [ProducesResponseType(typeof(ReadFileResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Read([FromBody] ReadFileRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new ReadProjectFileQuery(request.ProjectKey, request.RelativeFilePath),
            cancellationToken);

        return Ok(new ReadFileResponse(
            response.ProjectKey,
            response.RelativeFilePath,
            response.Content,
            response.LastModifiedUtc));
    }
}
