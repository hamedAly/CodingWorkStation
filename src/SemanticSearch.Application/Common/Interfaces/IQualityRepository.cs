using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IQualityRepository
{
    Task<QualitySummarySnapshot?> GetSummaryAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DuplicationFinding>> ListFindingsAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CodeRegion>> ListRegionsAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<DuplicationFinding?> GetFindingAsync(string projectKey, string findingId, CancellationToken cancellationToken = default);
    Task<CodeRegion?> GetRegionAsync(string projectKey, string regionId, CancellationToken cancellationToken = default);
    Task<QualityAnalysisRun?> GetLatestAnalysisRunAsync(string projectKey, CancellationToken cancellationToken = default);
    Task ReplaceSnapshotAsync(
        QualityAnalysisRun run,
        QualitySummarySnapshot summary,
        IReadOnlyList<DuplicationFinding> findings,
        IReadOnlyList<CodeRegion> regions,
        CancellationToken cancellationToken = default);
}
