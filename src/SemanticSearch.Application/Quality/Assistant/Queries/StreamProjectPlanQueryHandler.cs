using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed class StreamProjectPlanQueryHandler : IRequestHandler<StreamProjectPlanQuery, ProjectPlanRequestModel>
{
    private readonly IQualityRepository _qualityRepository;

    public StreamProjectPlanQueryHandler(IQualityRepository qualityRepository)
    {
        _qualityRepository = qualityRepository;
    }

    public async Task<ProjectPlanRequestModel> Handle(StreamProjectPlanQuery request, CancellationToken cancellationToken)
    {
        var summary = await _qualityRepository.GetSummaryAsync(request.ProjectKey, cancellationToken)
            ?? throw new NotFoundException($"No quality snapshot is available for project '{request.ProjectKey}'.");

        if (!string.Equals(summary.RunId, request.RunId, StringComparison.Ordinal))
        {
            throw new ConflictException(
                $"The requested quality snapshot '{request.RunId}' is no longer the latest snapshot for project '{request.ProjectKey}'. Refresh the dashboard and try again.");
        }

        var allFindings = await BuildAllHotspotsAsync(request.ProjectKey, cancellationToken);
        var impactedControllers = BuildImpactedControllers(allFindings);
        var topHotspots = allFindings
            .OrderByDescending(item => item.MatchingLineCount)
            .ThenByDescending(item => item.SimilarityScore)
            .Take(20)
            .ToArray();

        return new ProjectPlanRequestModel(
            summary.ProjectKey,
            summary.RunId,
            summary.QualityGrade.ToString(),
            summary.TotalLinesOfCode,
            summary.DuplicationPercent,
            summary.StructuralFindingCount,
            summary.SemanticFindingCount,
            summary.LastAnalyzedUtc,
            allFindings.Count,
            impactedControllers,
            topHotspots);
    }

    private async Task<IReadOnlyList<ProjectPlanHotspotModel>> BuildAllHotspotsAsync(string projectKey, CancellationToken cancellationToken)
    {
        var findings = await _qualityRepository.ListFindingsAsync(projectKey, cancellationToken);
        var hotspots = new List<ProjectPlanHotspotModel>();

        foreach (var finding in findings)
        {
            var left = await _qualityRepository.GetRegionAsync(projectKey, finding.LeftRegionId, cancellationToken);
            var right = await _qualityRepository.GetRegionAsync(projectKey, finding.RightRegionId, cancellationToken);
            if (left is null || right is null)
            {
                continue;
            }

            hotspots.Add(ToHotspot(QualityReadModelMapper.MapFinding(finding, left, right)));
        }

        return hotspots;
    }

    private static IReadOnlyList<string> BuildImpactedControllers(IReadOnlyList<ProjectPlanHotspotModel> findings)
        => findings
            .SelectMany(finding => new[] { finding.LeftFilePath, finding.RightFilePath })
            .Where(IsControllerPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    private static bool IsControllerPath(string path)
    {
        var normalized = path.Replace('\\', '/');
        return normalized.EndsWith("Controller.cs", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/Controllers/", StringComparison.OrdinalIgnoreCase);
    }

    private static ProjectPlanHotspotModel ToHotspot(QualityFindingModel finding)
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
}
