namespace SemanticSearch.WebApi.Contracts.Quality;

public sealed record QualitySnapshotRequest(string ProjectKey, string? ScopePath);

public sealed record StructuralDuplicationRequest(string ProjectKey, string? ScopePath, int? MinimumLines);

public sealed record SemanticDuplicationRequest(string ProjectKey, string? ScopePath, double? SimilarityThreshold, int? MaxPairs);

public sealed record QualityBreakdownSliceResponse(string Category, int LineCount, double Percent);

public sealed record DuplicationFindingResponse(
    string FindingId,
    string Severity,
    string Type,
    double SimilarityScore,
    int MatchingLineCount,
    string LeftFilePath,
    int LeftStartLine,
    int LeftEndLine,
    string RightFilePath,
    int RightStartLine,
    int RightEndLine);

public sealed record QualityRunResponse(
    string ProjectKey,
    string RunId,
    string Mode,
    DateTime AnalyzedAtUtc,
    int TotalLinesOfCode,
    int FindingCount,
    IReadOnlyList<DuplicationFindingResponse> Findings);

public sealed record QualitySummaryResponse(
    string ProjectKey,
    string RunId,
    string QualityGrade,
    int TotalLinesOfCode,
    double DuplicationPercent,
    int StructuralFindingCount,
    int SemanticFindingCount,
    IReadOnlyList<QualityBreakdownSliceResponse> Breakdown,
    DateTime? LastAnalyzedUtc);

public sealed record QualitySnapshotResponse(
    QualitySummaryResponse Summary,
    QualityFindingsResponse Findings);

public sealed record QualityFindingsResponse(
    string ProjectKey,
    string RunId,
    IReadOnlyList<DuplicationFindingResponse> Findings);

public sealed record CodeRegionViewResponse(
    string RelativeFilePath,
    int StartLine,
    int EndLine,
    string Snippet,
    IReadOnlyList<int> HighlightedLineNumbers,
    string Availability);

public sealed record DuplicateComparisonViewResponse(
    string FindingId,
    string Severity,
    string Type,
    double SimilarityScore,
    CodeRegionViewResponse LeftRegion,
    CodeRegionViewResponse RightRegion);

public sealed record DuplicateComparisonResponse(string ProjectKey, DuplicateComparisonViewResponse Finding);
