using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.Architecture;

/// <summary>
/// Builds per-file duplication heatmap entries using existing quality analysis data.
/// Line counts are approximated from search segment EndLine values.
/// </summary>
public sealed class HeatmapDataBuilder : IHeatmapDataBuilder
{
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly IQualityRepository _qualityRepository;

    public HeatmapDataBuilder(IProjectFileRepository projectFileRepository, IQualityRepository qualityRepository)
    {
        _projectFileRepository = projectFileRepository;
        _qualityRepository = qualityRepository;
    }

    public async Task<IReadOnlyList<FileHeatmapEntry>> BuildAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        // Approximate line counts from segment EndLine values per file
        var segments = await _projectFileRepository.ListSegmentsAsync(projectKey, cancellationToken);
        var lineCounts = segments
            .GroupBy(s => s.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Max(s => s.EndLine), StringComparer.OrdinalIgnoreCase);

        if (lineCounts.Count == 0)
            return [];

        // Build regionId → (RelativeFilePath) map
        var regions = await _qualityRepository.ListRegionsAsync(projectKey, cancellationToken);
        var regionFilePaths = regions
            .ToDictionary(r => r.RegionId, r => r.RelativeFilePath, StringComparer.Ordinal);

        // Count structural/semantic findings per file (a finding touches two files)
        var structuralCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var semanticCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var findings = await _qualityRepository.ListFindingsAsync(projectKey, cancellationToken);
        foreach (var finding in findings)
        {
            var leftPath = regionFilePaths.GetValueOrDefault(finding.LeftRegionId);
            var rightPath = regionFilePaths.GetValueOrDefault(finding.RightRegionId);

            if (finding.Type == DuplicationType.Structural)
            {
                if (leftPath is not null) structuralCounts[leftPath] = structuralCounts.GetValueOrDefault(leftPath) + 1;
                if (rightPath is not null && !string.Equals(rightPath, leftPath, StringComparison.OrdinalIgnoreCase))
                    structuralCounts[rightPath] = structuralCounts.GetValueOrDefault(rightPath) + 1;
            }
            else
            {
                if (leftPath is not null) semanticCounts[leftPath] = semanticCounts.GetValueOrDefault(leftPath) + 1;
                if (rightPath is not null && !string.Equals(rightPath, leftPath, StringComparison.OrdinalIgnoreCase))
                    semanticCounts[rightPath] = semanticCounts.GetValueOrDefault(rightPath) + 1;
            }
        }

        var result = new List<FileHeatmapEntry>();
        foreach (var kv in lineCounts)
        {
            var relativeFilePath = kv.Key;
            var totalLines = Math.Max(1, kv.Value);
            var structural = structuralCounts.GetValueOrDefault(relativeFilePath);
            var semantic = semanticCounts.GetValueOrDefault(relativeFilePath);
            var density = (structural + semantic) / (double)totalLines;

            result.Add(new FileHeatmapEntry(
                RelativeFilePath: relativeFilePath,
                FileName: Path.GetFileName(relativeFilePath),
                TotalLines: totalLines,
                StructuralDuplicateCount: structural,
                SemanticDuplicateCount: semantic,
                DuplicationDensity: density));
        }

        return result.OrderByDescending(e => e.DuplicationDensity).ToList();
    }
}
