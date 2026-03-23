using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

/// <summary>Builds per-file duplication heatmap data from existing quality analysis results.</summary>
public interface IHeatmapDataBuilder
{
    Task<IReadOnlyList<FileHeatmapEntry>> BuildAsync(string projectKey, CancellationToken cancellationToken = default);
}
