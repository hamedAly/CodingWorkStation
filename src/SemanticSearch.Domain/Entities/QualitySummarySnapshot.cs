using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class QualitySummarySnapshot
{
    public string ProjectKey { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public QualityGrade QualityGrade { get; init; } = QualityGrade.A;
    public int TotalLinesOfCode { get; init; }
    public int UniqueLineCount { get; init; }
    public int StructuralDuplicateLineCount { get; init; }
    public int SemanticDuplicateLineCount { get; init; }
    public double DuplicationPercent { get; init; }
    public int StructuralFindingCount { get; init; }
    public int SemanticFindingCount { get; init; }
    public DateTime LastAnalyzedUtc { get; init; }
}
