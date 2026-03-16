using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IProjectWorkspaceRepository
{
    Task<IReadOnlyList<ProjectWorkspace>> ListAsync(CancellationToken cancellationToken = default);
    Task<ProjectWorkspace?> GetAsync(string projectKey, CancellationToken cancellationToken = default);
    Task UpsertAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default);
    Task<IndexingRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default);
    Task<IndexingRun?> GetActiveRunAsync(string projectKey, CancellationToken cancellationToken = default);
    Task UpsertRunAsync(IndexingRun run, CancellationToken cancellationToken = default);
}
