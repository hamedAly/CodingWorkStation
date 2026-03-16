using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Quality.Commands;
using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Application.Quality.Queries;
using SemanticSearch.WebApi.Contracts.Quality;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/quality")]
public sealed class QualityController : ControllerBase
{
    private readonly IMediator _mediator;

    public QualityController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("{projectKey}")]
    [ProducesResponseType(typeof(QualitySummaryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSummary([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var summary = await _mediator.Send(new GetQualitySummaryQuery(projectKey), cancellationToken);
        return Ok(ToResponse(summary));
    }

    [HttpGet("{projectKey}/findings")]
    [ProducesResponseType(typeof(QualityFindingsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ListFindings([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var findings = await _mediator.Send(new ListQualityFindingsQuery(projectKey), cancellationToken);
        return Ok(new QualityFindingsResponse(findings.ProjectKey, findings.RunId, findings.Findings.Select(ToResponse).ToList()));
    }

    [HttpGet("{projectKey}/findings/{findingId}")]
    [ProducesResponseType(typeof(DuplicateComparisonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFinding(
        [FromRoute] string projectKey,
        [FromRoute] string findingId,
        CancellationToken cancellationToken)
    {
        var finding = await _mediator.Send(new GetDuplicateComparisonQuery(projectKey, findingId), cancellationToken);
        return Ok(new DuplicateComparisonResponse(projectKey, ToResponse(finding)));
    }

    [HttpPost("snapshot")]
    [ProducesResponseType(typeof(QualitySnapshotResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GenerateSnapshot([FromBody] QualitySnapshotRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new GenerateQualitySnapshotCommand(request.ProjectKey, request.ScopePath),
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("structural")]
    [ProducesResponseType(typeof(QualityRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunStructural([FromBody] StructuralDuplicationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RunStructuralDuplicationAnalysisCommand(request.ProjectKey, request.ScopePath, request.MinimumLines),
            cancellationToken);
        return Ok(ToResponse(result));
    }

    [HttpPost("semantic")]
    [ProducesResponseType(typeof(QualityRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunSemantic([FromBody] SemanticDuplicationRequest request, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(
            new RunSemanticDuplicationAnalysisCommand(request.ProjectKey, request.ScopePath, request.SimilarityThreshold, request.MaxPairs),
            cancellationToken);
        return Ok(ToResponse(result));
    }

    private static QualitySnapshotResponse ToResponse(QualitySnapshotResult result)
        => new(
            ToResponse(result.Summary),
            new QualityFindingsResponse(
                result.ProjectKey,
                result.RunId,
                result.Findings.Select(ToResponse).ToList()));

    private static QualitySummaryResponse ToResponse(QualitySummaryModel summary)
        => new(
            summary.ProjectKey,
            summary.RunId,
            summary.QualityGrade,
            summary.TotalLinesOfCode,
            summary.DuplicationPercent,
            summary.StructuralFindingCount,
            summary.SemanticFindingCount,
            summary.Breakdown.Select(slice => new QualityBreakdownSliceResponse(slice.Category, slice.LineCount, slice.Percent)).ToList(),
            summary.LastAnalyzedUtc);

    private static QualityRunResponse ToResponse(QualityRunResult result)
        => new(
            result.ProjectKey,
            result.RunId,
            result.Mode,
            result.AnalyzedAtUtc,
            result.TotalLinesOfCode,
            result.FindingCount,
            result.Findings.Select(ToResponse).ToList());

    private static DuplicationFindingResponse ToResponse(QualityFindingModel finding)
        => new(
            finding.FindingId,
            finding.Severity,
            finding.Type,
            finding.SimilarityScore,
            finding.MatchingLineCount,
            finding.LeftFilePath,
            finding.LeftStartLine,
            finding.LeftEndLine,
            finding.RightFilePath,
            finding.RightStartLine,
            finding.RightEndLine);

    private static DuplicateComparisonViewResponse ToResponse(DuplicateComparisonModel finding)
        => new(
            finding.FindingId,
            finding.Severity,
            finding.Type,
            finding.SimilarityScore,
            new CodeRegionViewResponse(
                finding.LeftRegion.RelativeFilePath,
                finding.LeftRegion.StartLine,
                finding.LeftRegion.EndLine,
                finding.LeftRegion.Snippet,
                finding.LeftRegion.HighlightedLineNumbers,
                finding.LeftRegion.Availability),
            new CodeRegionViewResponse(
                finding.RightRegion.RelativeFilePath,
                finding.RightRegion.StartLine,
                finding.RightRegion.EndLine,
                finding.RightRegion.Snippet,
                finding.RightRegion.HighlightedLineNumbers,
                finding.RightRegion.Availability));
}
