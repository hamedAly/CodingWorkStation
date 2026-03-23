using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface IDependencyRepository
{
    Task<DependencyAnalysisRun?> GetLatestRunAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DependencyNode>> ListNodesAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DependencyEdge>> ListEdgesAsync(string projectKey, CancellationToken cancellationToken = default);
    Task ReplaceDependencyGraphAsync(
        DependencyAnalysisRun run,
        IReadOnlyList<DependencyNode> nodes,
        IReadOnlyList<DependencyEdge> edges,
        CancellationToken cancellationToken = default);
}
