using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Indexing.Commands;
using SemanticSearch.Application.Projects.Queries;
using SemanticSearch.Application.Status.Queries;
using SemanticSearch.WebApi.Contracts.Projects;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/project")]
public sealed class ProjectController : ControllerBase
{
    private readonly IMediator _mediator;

    public ProjectController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectWorkspaceSummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ListProjectsQuery(), cancellationToken);
        var results = response
            .Select(project => new ProjectWorkspaceSummaryResponse(
                project.ProjectKey,
                project.Status.ToString(),
                project.TotalFiles,
                project.TotalSegments,
                project.IndexingInProgress,
                project.CanPause,
                project.CanResume,
                project.RunId,
                project.FilesProcessed,
                project.TotalFilesPlanned,
                project.ProgressPercent,
                project.CurrentFilePath,
                project.LastIndexedUtc,
                project.LastError))
            .ToList();

        return Ok(results);
    }

    [HttpPost("index")]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Index([FromBody] IndexProjectRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new IndexProjectCommand(request.ProjectPath, request.ProjectKey), cancellationToken);
        var payload = new IndexAcceptedResponse(response.ProjectKey, response.RunId, response.Status, response.Message);

        return response.Status is "Queued" or "Running"
            ? Accepted(payload)
            : Conflict(payload);
    }

    [HttpPost("index/file")]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshFile([FromBody] RefreshProjectFileRequest request, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RefreshProjectFileCommand(request.ProjectKey, request.RelativeFilePath), cancellationToken);
        var payload = new IndexAcceptedResponse(response.ProjectKey, response.RunId, response.Status, response.Message);

        return response.Status is "Queued" or "Running"
            ? Accepted(payload)
            : Conflict(payload);
    }

    [HttpGet("status/{projectKey}")]
    [ProducesResponseType(typeof(ProjectStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Status([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetProjectStatusQuery(projectKey), cancellationToken);
        return Ok(new ProjectStatusResponse(
            response.ProjectKey,
            response.Status.ToString(),
            response.TotalFiles,
            response.TotalSegments,
            response.IndexingInProgress,
            response.CanPause,
            response.CanResume,
            response.RunId,
            response.FilesProcessed,
            response.TotalFilesPlanned,
            response.ProgressPercent,
            response.CurrentFilePath,
            response.LastIndexedUtc,
            response.LastError));
    }

    [HttpPost("pause/{projectKey}")]
    [ProducesResponseType(typeof(IndexingControlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Pause([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new PauseProjectIndexingCommand(projectKey), cancellationToken);
        return Ok(new IndexingControlResponse(response.ProjectKey, response.Status, response.Message));
    }

    [HttpPost("resume/{projectKey}")]
    [ProducesResponseType(typeof(IndexingControlResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Resume([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new ResumeProjectIndexingCommand(projectKey), cancellationToken);
        return Ok(new IndexingControlResponse(response.ProjectKey, response.Status, response.Message));
    }

    [HttpPost("retry/{projectKey}")]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status202Accepted)]
    [ProducesResponseType(typeof(IndexAcceptedResponse), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Retry([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new RetryProjectIndexingCommand(projectKey), cancellationToken);
        var payload = new IndexAcceptedResponse(response.ProjectKey, response.RunId, response.Status, response.Message);

        return response.Status is "Queued" or "Running"
            ? Accepted(payload)
            : Conflict(payload);
    }

    [HttpGet("tree/{projectKey}")]
    [ProducesResponseType(typeof(IReadOnlyList<ProjectTreeNodeResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Tree([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var response = await _mediator.Send(new GetProjectTreeQuery(projectKey), cancellationToken);
        return Ok(response.Select(MapNode).ToList());
    }

    private static ProjectTreeNodeResponse MapNode(SemanticSearch.Domain.ValueObjects.ProjectTreeNode node) => new(
        node.Path,
        node.Name,
        node.NodeType.ToString(),
        node.RelativeFilePath,
        node.Children.Select(MapNode).ToList());
}
