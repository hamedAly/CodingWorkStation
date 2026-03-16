using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality;
using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.Common;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class QualityRunCoordinator : IQualityRunCoordinator
{
    private readonly IProjectWorkspaceRepository _workspaceRepository;
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly IQualityRepository _qualityRepository;
    private readonly QualityFileFilter _fileFilter;
    private readonly IStructuralCloneAnalyzer _structuralCloneAnalyzer;
    private readonly ISemanticDuplicationService _semanticDuplicationService;
    private readonly QualitySummaryBuilder _summaryBuilder;
    private readonly SemanticSearchOptions _options;
    private readonly ILogger<QualityRunCoordinator> _logger;

    public QualityRunCoordinator(
        IProjectWorkspaceRepository workspaceRepository,
        IProjectFileRepository projectFileRepository,
        IQualityRepository qualityRepository,
        QualityFileFilter fileFilter,
        IStructuralCloneAnalyzer structuralCloneAnalyzer,
        ISemanticDuplicationService semanticDuplicationService,
        QualitySummaryBuilder summaryBuilder,
        IOptions<SemanticSearchOptions> options,
        ILogger<QualityRunCoordinator> logger)
    {
        _workspaceRepository = workspaceRepository;
        _projectFileRepository = projectFileRepository;
        _qualityRepository = qualityRepository;
        _fileFilter = fileFilter;
        _structuralCloneAnalyzer = structuralCloneAnalyzer;
        _semanticDuplicationService = semanticDuplicationService;
        _summaryBuilder = summaryBuilder;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<QualitySnapshotResult> GenerateSnapshotAsync(
        string projectKey,
        bool includeStructural,
        bool includeSemantic,
        string? scopePath = null,
        int? minimumStructuralLines = null,
        double? semanticThreshold = null,
        int? maxPairs = null,
        CancellationToken cancellationToken = default)
    {
        if (!includeStructural && !includeSemantic)
        {
            throw new ConflictException("At least one quality analysis mode must be requested.");
        }

        _ = await _workspaceRepository.GetAsync(projectKey, cancellationToken)
            ?? throw new NotFoundException($"Project '{projectKey}' was not found.");

        var files = await _projectFileRepository.ListFilesAsync(projectKey, cancellationToken);
        var analyzableFiles = files
            .Where(file => _fileFilter.ShouldAnalyze(file.RelativeFilePath, scopePath))
            .ToList();
        var totalLinesOfCode = await CountTotalLinesAsync(analyzableFiles, cancellationToken);
        var structuralFindings = includeStructural
            ? await _structuralCloneAnalyzer.AnalyzeAsync(
                projectKey,
                scopePath,
                minimumStructuralLines ?? _options.Quality.MinimumStructuralLines,
                _options.Quality.MaxFindingsPerType,
                cancellationToken)
            : [];
        var semanticFindings = includeSemantic
            ? await _semanticDuplicationService.AnalyzeAsync(
                projectKey,
                scopePath,
                semanticThreshold ?? _options.Quality.SemanticSimilarityThreshold,
                maxPairs ?? _options.Quality.MaxSemanticPairs,
                _options.Quality.MaxFindingsPerType,
                cancellationToken)
            : [];

        var runId = Guid.NewGuid().ToString("N");
        var now = DateTime.UtcNow;
        var regions = new Dictionary<string, CodeRegion>(StringComparer.OrdinalIgnoreCase);
        var findings = new List<DuplicationFinding>();
        var pairKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var clone in structuralFindings.Concat(semanticFindings))
        {
            var leftRegion = CreateRegion(projectKey, clone.Left);
            var rightRegion = CreateRegion(projectKey, clone.Right);
            regions[leftRegion.RegionId] = leftRegion;
            regions[rightRegion.RegionId] = rightRegion;

            var orderedPair = string.Compare(leftRegion.RegionId, rightRegion.RegionId, StringComparison.Ordinal) <= 0
                ? $"{clone.Type}:{leftRegion.RegionId}:{rightRegion.RegionId}"
                : $"{clone.Type}:{rightRegion.RegionId}:{leftRegion.RegionId}";
            if (!pairKeys.Add(orderedPair))
            {
                continue;
            }

            findings.Add(new DuplicationFinding
            {
                FindingId = Guid.NewGuid().ToString("N"),
                ProjectKey = projectKey,
                RunId = runId,
                Type = clone.Type,
                Severity = QualityScoringRules.CalculateSeverity(clone.MatchingLineCount),
                SimilarityScore = Math.Round(clone.SimilarityScore, 4, MidpointRounding.AwayFromZero),
                MatchingLineCount = clone.MatchingLineCount,
                NormalizedFingerprint = clone.NormalizedFingerprint,
                LeftRegionId = leftRegion.RegionId,
                RightRegionId = rightRegion.RegionId,
                CreatedUtc = now
            });
        }

        var summary = _summaryBuilder.BuildSummary(projectKey, runId, now, totalLinesOfCode, findings, regions);
        var run = new QualityAnalysisRun
        {
            RunId = runId,
            ProjectKey = projectKey,
            RequestedModes = BuildModes(includeStructural, includeSemantic),
            Status = QualityAnalysisStatus.Completed,
            RequestedUtc = now,
            StartedUtc = now,
            CompletedUtc = now,
            TotalFilesScanned = analyzableFiles.Count,
            TotalLinesAnalyzed = totalLinesOfCode,
            StructuralFindingCount = findings.Count(finding => finding.Type == DuplicationType.Structural),
            SemanticFindingCount = findings.Count(finding => finding.Type == DuplicationType.Semantic)
        };

        await _qualityRepository.ReplaceSnapshotAsync(run, summary, findings, regions.Values.ToList(), cancellationToken);

        _logger.LogInformation(
            "Completed quality analysis for {ProjectKey} with {StructuralCount} structural and {SemanticCount} semantic findings.",
            projectKey,
            run.StructuralFindingCount,
            run.SemanticFindingCount);

        return new QualitySnapshotResult(
            projectKey,
            runId,
            now,
            totalLinesOfCode,
            findings.Select(finding => MapFinding(finding, regions)).ToList(),
            new QualitySummaryModel(
                projectKey,
                runId,
                summary.QualityGrade.ToString(),
                summary.TotalLinesOfCode,
                summary.DuplicationPercent,
                summary.StructuralFindingCount,
                summary.SemanticFindingCount,
                _summaryBuilder.BuildBreakdown(summary),
                summary.LastAnalyzedUtc));
    }

    private static string BuildModes(bool includeStructural, bool includeSemantic)
        => includeStructural && includeSemantic
            ? "Structural,Semantic"
            : includeStructural
                ? "Structural"
                : "Semantic";

    private static CodeRegion CreateRegion(string projectKey, DetectedCodeRegion region)
    {
        var regionId = SqliteVectorStore.ComputeContentHash(
            $"{projectKey}|{region.RelativeFilePath}|{region.StartLine}|{region.EndLine}|{region.ContentHash}");

        return new CodeRegion
        {
            RegionId = regionId,
            ProjectKey = projectKey,
            RelativeFilePath = region.RelativeFilePath,
            StartLine = region.StartLine,
            EndLine = region.EndLine,
            Snippet = region.Snippet,
            ContentHash = region.ContentHash,
            SourceSegmentId = region.SourceSegmentId,
            Availability = CodeRegionAvailability.Available
        };
    }

    private static QualityFindingModel MapFinding(
        DuplicationFinding finding,
        IReadOnlyDictionary<string, CodeRegion> regions)
    {
        var left = regions[finding.LeftRegionId];
        var right = regions[finding.RightRegionId];
        return new QualityFindingModel(
            finding.FindingId,
            finding.Severity.ToString(),
            finding.Type.ToString(),
            finding.SimilarityScore,
            finding.MatchingLineCount,
            left.RelativeFilePath,
            left.StartLine,
            left.EndLine,
            right.RelativeFilePath,
            right.StartLine,
            right.EndLine);
    }

    private static async Task<int> CountTotalLinesAsync(
        IReadOnlyList<IndexedFile> files,
        CancellationToken cancellationToken)
    {
        var total = 0;
        foreach (var file in files)
        {
            if (!File.Exists(file.AbsoluteFilePath))
            {
                continue;
            }

            var result = await TextFileLoader.TryReadSanitizedTextAsync(file.AbsoluteFilePath, cancellationToken);
            if (!result.Success || result.IsBinary)
            {
                continue;
            }

            total += Math.Max(1, result.Content.Replace("\r", string.Empty).Split('\n').Length);
        }

        return total;
    }
}
