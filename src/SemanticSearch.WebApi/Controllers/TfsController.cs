using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Tfs.Commands;
using SemanticSearch.Application.Tfs.Queries;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.WebApi.Contracts.Tfs;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/tfs")]
public sealed class TfsController : ControllerBase
{
    private readonly IMediator _mediator;

    public TfsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("credentials")]
    [ProducesResponseType(typeof(TfsCredentialStatusResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCredentialStatus(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetTfsCredentialStatusQuery(), cancellationToken);
        return Ok(new TfsCredentialStatusResponse(result.IsConfigured, result.ServerUrl, result.Username, result.UpdatedUtc));
    }

    [HttpPost("credentials")]
    [ProducesResponseType(typeof(SaveCredentialResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SaveCredential([FromBody] SaveTfsCredentialRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new SaveTfsCredentialCommand(request.ServerUrl, request.Pat, request.Username), cancellationToken);
        return Ok(new SaveCredentialResponse(result.Success, result.Error));
    }

    [HttpDelete("credentials")]
    [ProducesResponseType(typeof(DeleteCredentialResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteCredential(CancellationToken cancellationToken)
    {
        await _mediator.Send(new DeleteTfsCredentialCommand(), cancellationToken);
        return Ok(new DeleteCredentialResponse(true));
    }

    [HttpPost("credentials/test")]
    [ProducesResponseType(typeof(TestConnectionResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TestConnection(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new TestTfsConnectionQuery(), cancellationToken);
        return Ok(new TestConnectionResponse(result.Success, result.Error));
    }

    [HttpGet("workitems")]
    [ProducesResponseType(typeof(WorkItemsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetWorkItems(CancellationToken cancellationToken)
    {
        try
        {
            var items = await _mediator.Send(new GetMyWorkItemsQuery(), cancellationToken);
            var response = new WorkItemsResponse(items.Select(i => new WorkItemResponse(
                i.Id, i.Title, i.WorkItemType, i.State, i.AssignedTo,
                i.TeamProject, i.AreaPath, i.IterationPath, i.Priority, i.CreatedDate, i.ChangedDate, i.Url)).ToList());
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "Failed to load work items", statusCode: 502);
        }
    }

    [HttpGet("pullrequests")]
    [ProducesResponseType(typeof(PullRequestsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPullRequests(CancellationToken cancellationToken)
    {
        var prs = await _mediator.Send(new GetMyPullRequestsQuery(), cancellationToken);
        var response = new PullRequestsResponse(prs.Select(pr => new PullRequestResponse(
            pr.Id, pr.Title, pr.SourceBranch, pr.TargetBranch, pr.Status, pr.CreatedBy, pr.CreationDate,
            pr.Reviewers.Select(r => new ReviewerResponse(r.DisplayName, r.Vote, r.VoteLabel)).ToList(),
            pr.Url)).ToList());
        return Ok(response);
    }

    [HttpGet("contributions")]
    [ProducesResponseType(typeof(ContributionHeatmapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetContributions([FromQuery] int months = 12, CancellationToken cancellationToken = default)
    {
        try
        {
            var (days, resolvedUsername) = await _mediator.Send(new GetContributionHeatmapQuery(months), cancellationToken);
            var response = new ContributionHeatmapResponse(
                days.Select(d => new ContributionDayResponse(d.Date.ToString("yyyy-MM-dd"), d.Count, d.Level)).ToList(),
                days.Sum(d => d.Count),
                resolvedUsername);
            return Ok(response);
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, title: "Failed to load contribution data", statusCode: 502);
        }
    }

    [HttpPatch("workitems/{id:int}/state")]
    [ProducesResponseType(typeof(UpdateWorkItemStateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> UpdateWorkItemState(int id, [FromBody] UpdateWorkItemStateRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new UpdateWorkItemStateCommand(id, request.State), cancellationToken);
        if (!result.Success)
            return Problem(detail: result.Error, title: "Failed to update work item state", statusCode: 502);
        return Ok(new UpdateWorkItemStateResponse(result.Success, result.Error, result.NewState));
    }

    [HttpGet("workitems/{id:int}/comments")]
    [ProducesResponseType(typeof(WorkItemCommentsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> GetWorkItemComments(int id, CancellationToken cancellationToken)
    {
        var comments = await _mediator.Send(new GetWorkItemCommentsQuery(id), cancellationToken);
        var response = new WorkItemCommentsResponse(
            comments.Select(c => new WorkItemCommentResponse(c.Id, c.Text, c.CreatedBy, c.CreatedDate)).ToList(),
            comments.Count);
        return Ok(response);
    }

    [HttpPost("workitems/{id:int}/comments")]
    [ProducesResponseType(typeof(AddWorkItemCommentResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status502BadGateway)]
    public async Task<IActionResult> AddWorkItemComment(int id, [FromBody] AddWorkItemCommentRequest request, CancellationToken cancellationToken)
    {
        var comment = await _mediator.Send(new AddWorkItemCommentCommand(id, request.Text), cancellationToken);
        if (comment is null)
            return Problem(detail: "Failed to add comment to TFS.", title: "Comment submission failed", statusCode: 502);
        return Ok(new AddWorkItemCommentResponse(true,
            new WorkItemCommentResponse(comment.Id, comment.Text, comment.CreatedBy, comment.CreatedDate),
            null));
    }
}
