using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Indexing.Commands;
using SemanticSearch.Application.Search.Queries;
using SemanticSearch.Application.Status.Queries;

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

    /// <summary>POST /api/search/index — Queue a background indexing job for a project.</summary>
    [HttpPost("index")]
    [ProducesResponseType(typeof(IndexResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Index([FromBody] IndexRequest request, CancellationToken cancellationToken)
    {
        var command = new IndexProjectCommand(request.ProjectPath, request.ProjectKey);
        var response = await _mediator.Send(command, cancellationToken);

        return Accepted(new IndexResponse(response.ProjectKey, response.Status, response.Message));
    }

    /// <summary>POST /api/search/query — Search an indexed project for semantically relevant code.</summary>
    [HttpPost("query")]
    [ProducesResponseType(typeof(SearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Query([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        var query = new SearchProjectQuery(request.Query, request.ProjectKey, request.TopK);
        var response = await _mediator.Send(query, cancellationToken);

        var results = response.Results.Select(r => new SearchResultItem(
            r.FilePath, r.RelevanceScore, r.Snippet, r.StartLine, r.EndLine)).ToList();

        return Ok(new SearchResponse(results));
    }

    /// <summary>GET /api/search/status/{projectKey} — Get indexing statistics for a project.</summary>
    [HttpGet("status/{projectKey}")]
    [ProducesResponseType(typeof(StatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Status([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var query = new GetProjectStatusQuery(projectKey);
        var response = await _mediator.Send(query, cancellationToken);

        return Ok(new StatusResponse(
            response.IsIndexed,
            response.TotalFiles,
            response.TotalChunks,
            response.LastUpdated));
    }
}

// DTOs
public sealed record IndexRequest(string ProjectPath, string ProjectKey);
public sealed record IndexResponse(string ProjectKey, string Status, string Message);

public sealed record SearchRequest(string Query, string ProjectKey, int TopK = 10);
public sealed record SearchResponse(IReadOnlyList<SearchResultItem> Results);
public sealed record SearchResultItem(string FilePath, float RelevanceScore, string Snippet, int StartLine, int EndLine);

public sealed record StatusResponse(bool IsIndexed, int TotalFiles, int TotalChunks, DateTime? LastUpdated);

