using SemanticSearch.Application.Quality;
using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class QualitySummaryBuilder
{
    public QualitySummarySnapshot BuildSummary(
        string projectKey,
        string runId,
        DateTime analyzedAtUtc,
        int totalLinesOfCode,
        IReadOnlyList<DuplicationFinding> findings,
        IReadOnlyDictionary<string, CodeRegion> regions)
    {
        var structuralDuplicateLineCount = SumDistinctLines(findings, regions, DuplicationType.Structural);
        var semanticDuplicateLineCount = SumDistinctLines(findings, regions, DuplicationType.Semantic);
        var duplicateLineCount = Math.Min(totalLinesOfCode, structuralDuplicateLineCount + semanticDuplicateLineCount);
        var uniqueLineCount = Math.Max(0, totalLinesOfCode - duplicateLineCount);
        var duplicationPercent = totalLinesOfCode == 0
            ? 0d
            : Math.Round(duplicateLineCount * 100d / totalLinesOfCode, 2, MidpointRounding.AwayFromZero);

        return new QualitySummarySnapshot
        {
            ProjectKey = projectKey,
            RunId = runId,
            QualityGrade = QualityScoringRules.CalculateGrade(duplicationPercent),
            TotalLinesOfCode = totalLinesOfCode,
            UniqueLineCount = uniqueLineCount,
            StructuralDuplicateLineCount = structuralDuplicateLineCount,
            SemanticDuplicateLineCount = semanticDuplicateLineCount,
            DuplicationPercent = duplicationPercent,
            StructuralFindingCount = findings.Count(finding => finding.Type == DuplicationType.Structural),
            SemanticFindingCount = findings.Count(finding => finding.Type == DuplicationType.Semantic),
            LastAnalyzedUtc = analyzedAtUtc
        };
    }

    public IReadOnlyList<QualityBreakdownSlice> BuildBreakdown(QualitySummarySnapshot summary)
        =>
        [
            new("Unique", summary.UniqueLineCount, CalculatePercent(summary.UniqueLineCount, summary.TotalLinesOfCode)),
            new("Structural", summary.StructuralDuplicateLineCount, CalculatePercent(summary.StructuralDuplicateLineCount, summary.TotalLinesOfCode)),
            new("Semantic", summary.SemanticDuplicateLineCount, CalculatePercent(summary.SemanticDuplicateLineCount, summary.TotalLinesOfCode))
        ];

    private static int SumDistinctLines(
        IReadOnlyList<DuplicationFinding> findings,
        IReadOnlyDictionary<string, CodeRegion> regions,
        DuplicationType type)
    {
        var lineKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var finding in findings.Where(finding => finding.Type == type))
        {
            AddRegionLines(finding.LeftRegionId);
            AddRegionLines(finding.RightRegionId);
        }

        return lineKeys.Count;

        void AddRegionLines(string regionId)
        {
            if (!regions.TryGetValue(regionId, out var region))
            {
                return;
            }

            for (var line = region.StartLine; line <= region.EndLine; line++)
            {
                lineKeys.Add($"{region.RelativeFilePath}:{line}");
            }
        }
    }

    private static double CalculatePercent(int lineCount, int totalLinesOfCode)
        => totalLinesOfCode == 0
            ? 0d
            : Math.Round(lineCount * 100d / totalLinesOfCode, 2, MidpointRounding.AwayFromZero);
}
