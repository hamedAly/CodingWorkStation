using MediatR;
using Microsoft.AspNetCore.Mvc;
using SemanticSearch.Application.Architecture.Commands;
using SemanticSearch.Application.Architecture.Queries;
using SemanticSearch.WebApi.Contracts.Architecture;

namespace SemanticSearch.WebApi.Controllers;

[ApiController]
[Route("api/architecture")]
public sealed class ArchitectureController : ControllerBase
{
    private readonly IMediator _mediator;

    public ArchitectureController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("{projectKey}/dependency-graph")]
    [ProducesResponseType(typeof(DependencyAnalysisRunResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RunDependencyAnalysis([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new RunDependencyAnalysisCommand(projectKey), cancellationToken);

        return Ok(new DependencyAnalysisRunResponse(
            result.RunId,
            result.ProjectKey,
            result.Status.ToString(),
            DateTime.UtcNow,
            DateTime.UtcNow,
            result.Status == Domain.ValueObjects.DependencyAnalysisStatus.Completed ? DateTime.UtcNow : null,
            result.TotalFilesScanned,
            result.TotalNodesFound,
            result.TotalEdgesFound,
            result.FailureReason));
    }

    [HttpGet("{projectKey}/dependency-graph")]
    [ProducesResponseType(typeof(DependencyGraphResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetDependencyGraph(
        [FromRoute] string projectKey,
        [FromQuery] string? @namespace,
        [FromQuery] string? filePath,
        CancellationToken cancellationToken)
    {
        var graph = await _mediator.Send(new GetDependencyGraphQuery(projectKey, @namespace, filePath), cancellationToken);
        if (graph is null)
            return NotFound(new ProblemDetails { Detail = $"No dependency analysis found for project '{projectKey}'." });

        var nodeNames = graph.Nodes.ToDictionary(n => n.NodeId, n => n.Name);

        var nodes = graph.Nodes.Select(n => new DependencyNodeResponse(
            n.NodeId, n.Name, n.FullName, n.Kind.ToString(),
            n.Namespace, n.FilePath, n.StartLine, n.ParentNodeId)).ToList();

        var edges = graph.Edges.Select(e => new DependencyEdgeResponse(
            e.EdgeId, e.SourceNodeId, e.TargetNodeId, e.RelationshipType.ToString(),
            nodeNames.GetValueOrDefault(e.SourceNodeId, e.SourceNodeId),
            nodeNames.GetValueOrDefault(e.TargetNodeId, e.TargetNodeId))).ToList();

        return Ok(new DependencyGraphResponse(
            graph.ProjectKey, graph.RunId, graph.AnalyzedUtc,
            graph.TotalNodes, graph.TotalEdges, nodes, edges));
    }

    [HttpGet("{projectKey}/heatmap")]
    [ProducesResponseType(typeof(FileHeatmapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetFileHeatmap([FromRoute] string projectKey, CancellationToken cancellationToken)
    {
        var heatmap = await _mediator.Send(new GetFileHeatmapQuery(projectKey), cancellationToken);
        if (heatmap is null)
            return NotFound(new ProblemDetails { Detail = $"No quality analysis found for project '{projectKey}'. Run a quality analysis first." });

        var entries = heatmap.Entries.Select(e => new FileHeatmapEntryResponse(
            e.RelativeFilePath, e.FileName, e.TotalLines,
            e.StructuralDuplicateCount, e.SemanticDuplicateCount, e.DuplicationDensity)).ToList();

        return Ok(new FileHeatmapResponse(heatmap.ProjectKey, heatmap.TotalFiles, entries));
    }

    [HttpGet("er-diagram")]
    [ProducesResponseType(typeof(ErDiagramResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetErDiagram(CancellationToken cancellationToken)
    {
        var result = await _mediator.Send(new GetErDiagramQuery(), cancellationToken);
        return Ok(new ErDiagramResponse(result.MermaidMarkup, result.EntityCount, result.RelationshipCount));
    }
}
