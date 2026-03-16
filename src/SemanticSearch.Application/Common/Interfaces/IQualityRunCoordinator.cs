using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IQualityRunCoordinator
{
    Task<QualitySnapshotResult> GenerateSnapshotAsync(
        string projectKey,
        bool includeStructural,
        bool includeSemantic,
        string? scopePath = null,
        int? minimumStructuralLines = null,
        double? semanticThreshold = null,
        int? maxPairs = null,
        CancellationToken cancellationToken = default);
}
