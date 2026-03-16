using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IProjectTreeService
{
    Task<IReadOnlyList<ProjectTreeNode>> GetTreeAsync(
        string projectKey,
        CancellationToken cancellationToken = default);
}
