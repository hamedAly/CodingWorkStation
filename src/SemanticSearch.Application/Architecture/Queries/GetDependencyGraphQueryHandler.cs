using MediatR;
using SemanticSearch.Application.Architecture.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Architecture.Queries;

public sealed class GetDependencyGraphQueryHandler : IRequestHandler<GetDependencyGraphQuery, DependencyGraphModel?>
{
    private readonly IDependencyRepository _repository;

    public GetDependencyGraphQueryHandler(IDependencyRepository repository)
    {
        _repository = repository;
    }

    public async Task<DependencyGraphModel?> Handle(GetDependencyGraphQuery request, CancellationToken cancellationToken)
    {
        var run = await _repository.GetLatestRunAsync(request.ProjectKey, cancellationToken);
        if (run is null) return null;

        var allNodes = await _repository.ListNodesAsync(request.ProjectKey, cancellationToken);
        var allEdges = await _repository.ListEdgesAsync(request.ProjectKey, cancellationToken);

        var filteredNodes = allNodes.Where(n =>
            (string.IsNullOrWhiteSpace(request.Namespace) ||
             n.Namespace.StartsWith(request.Namespace, StringComparison.OrdinalIgnoreCase)) &&
            (string.IsNullOrWhiteSpace(request.FilePath) ||
             n.FilePath.Equals(request.FilePath, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        var nodeIds = filteredNodes.Select(n => n.NodeId).ToHashSet(StringComparer.Ordinal);
        var filteredEdges = allEdges
            .Where(e => nodeIds.Contains(e.SourceNodeId) && nodeIds.Contains(e.TargetNodeId))
            .ToList();

        return new DependencyGraphModel(
            request.ProjectKey,
            run.RunId,
            run.CompletedUtc ?? run.RequestedUtc,
            filteredNodes.Count,
            filteredEdges.Count,
            filteredNodes,
            filteredEdges);
    }
}
