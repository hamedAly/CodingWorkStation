using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Search.Queries;
using SemanticSearch.WebApi.Contracts.Search;
using ApiSearchResponse = SemanticSearch.WebApi.Contracts.Search.SearchResponse;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/search")]
public sealed class SearchController : ControllerBase
{
    private readonly IMediator _mediator;

    public SearchController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("semantic")]
    [ProducesResponseType(typeof(ApiSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Semantic([FromBody] SemanticSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new SearchSemanticQuery(request.Query, request.ProjectKey, request.TopK), cancellationToken);
        return Ok(MapResponse(response));
    }

    [HttpPost("exact")]
    [ProducesResponseType(typeof(ApiSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Exact([FromBody] ExactSearchRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(
            new SearchExactQuery(request.Keyword, request.ProjectKey, request.MatchCase, request.TopK),
            cancellationToken);

        return Ok(MapResponse(response));
    }

    private static ApiSearchResponse MapResponse(SemanticSearch.Application.Search.Queries.SearchResponse response) => new(
        response.ProjectKey,
        response.Mode,
        response.Results
            .Select(result => new SearchResultResponse(
                result.RelativeFilePath,
                result.Score,
                result.Snippet,
                result.StartLine,
                result.EndLine,
                result.MatchType.ToString()))
            .ToList());
}
