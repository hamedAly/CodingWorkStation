using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Application.Quality;

internal static class QualityReadModelMapper
{
    public static QualitySummaryModel MapSummary(QualitySummarySnapshot summary)
        => new(
            summary.ProjectKey,
            summary.RunId,
            summary.QualityGrade.ToString(),
            summary.TotalLinesOfCode,
            summary.DuplicationPercent,
            summary.StructuralFindingCount,
            summary.SemanticFindingCount,
            CreateBreakdown(summary),
            summary.LastAnalyzedUtc);

    public static QualityFindingModel MapFinding(
        DuplicationFinding finding,
        CodeRegion leftRegion,
        CodeRegion rightRegion)
        => new(
            finding.FindingId,
            finding.Severity.ToString(),
            finding.Type.ToString(),
            finding.SimilarityScore,
            finding.MatchingLineCount,
            leftRegion.RelativeFilePath,
            leftRegion.StartLine,
            leftRegion.EndLine,
            rightRegion.RelativeFilePath,
            rightRegion.StartLine,
            rightRegion.EndLine);

    public static QualityRunResult ToRunResult(QualitySnapshotResult snapshot, string mode)
        => new(
            snapshot.ProjectKey,
            snapshot.RunId,
            mode,
            snapshot.AnalyzedAtUtc,
            snapshot.TotalLinesOfCode,
            snapshot.Findings.Count,
            snapshot.Findings);

    private static IReadOnlyList<QualityBreakdownSlice> CreateBreakdown(QualitySummarySnapshot summary)
        =>
        [
            new("Unique", summary.UniqueLineCount, Percent(summary.UniqueLineCount, summary.TotalLinesOfCode)),
            new("Structural", summary.StructuralDuplicateLineCount, Percent(summary.StructuralDuplicateLineCount, summary.TotalLinesOfCode)),
            new("Semantic", summary.SemanticDuplicateLineCount, Percent(summary.SemanticDuplicateLineCount, summary.TotalLinesOfCode))
        ];

    private static double Percent(int value, int total)
        => total == 0 ? 0d : Math.Round(value * 100d / total, 2, MidpointRounding.AwayFromZero);
}
