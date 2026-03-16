using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class ComparisonHighlightService : IComparisonHighlightService
{
    private readonly IProjectFileReader _projectFileReader;
    private readonly IQualityRepository _qualityRepository;

    public ComparisonHighlightService(
        IProjectFileReader projectFileReader,
        IQualityRepository qualityRepository)
    {
        _projectFileReader = projectFileReader;
        _qualityRepository = qualityRepository;
    }

    public async Task<DuplicateComparisonModel> BuildAsync(
        string projectKey,
        string findingId,
        CancellationToken cancellationToken = default)
    {
        var finding = await _qualityRepository.GetFindingAsync(projectKey, findingId, cancellationToken)
            ?? throw new SemanticSearch.Application.Common.Exceptions.NotFoundException($"Quality finding '{findingId}' was not found for project '{projectKey}'.");

        var leftRegion = await _qualityRepository.GetRegionAsync(projectKey, finding.LeftRegionId, cancellationToken)
            ?? throw new SemanticSearch.Application.Common.Exceptions.NotFoundException($"Left region '{finding.LeftRegionId}' was not found.");
        var rightRegion = await _qualityRepository.GetRegionAsync(projectKey, finding.RightRegionId, cancellationToken)
            ?? throw new SemanticSearch.Application.Common.Exceptions.NotFoundException($"Right region '{finding.RightRegionId}' was not found.");

        var leftView = await ResolveRegionAsync(projectKey, leftRegion, cancellationToken);
        var rightView = await ResolveRegionAsync(projectKey, rightRegion, cancellationToken);
        var highlights = BuildHighlightSets(leftView.Snippet, leftView.StartLine, rightView.Snippet, rightView.StartLine);

        return new DuplicateComparisonModel(
            finding.FindingId,
            finding.Severity.ToString(),
            finding.Type.ToString(),
            finding.SimilarityScore,
            leftView with { HighlightedLineNumbers = highlights.Left },
            rightView with { HighlightedLineNumbers = highlights.Right });
    }

    private async Task<CodeRegionModel> ResolveRegionAsync(
        string projectKey,
        SemanticSearch.Domain.Entities.CodeRegion region,
        CancellationToken cancellationToken)
    {
        try
        {
            var content = await _projectFileReader.ReadFileAsync(projectKey, region.RelativeFilePath, cancellationToken);
            var snippet = ExtractSnippet(content.Content, region.StartLine, region.EndLine);
            var availability = SqliteVectorStore.ComputeContentHash(snippet) == region.ContentHash
                ? CodeRegionAvailability.Available
                : CodeRegionAvailability.Stale;

            return new CodeRegionModel(
                region.RelativeFilePath,
                region.StartLine,
                region.EndLine,
                string.IsNullOrWhiteSpace(snippet) ? region.Snippet : snippet,
                [],
                availability.ToString());
        }
        catch (Exception)
        {
            return new CodeRegionModel(
                region.RelativeFilePath,
                region.StartLine,
                region.EndLine,
                region.Snippet,
                [],
                CodeRegionAvailability.Missing.ToString());
        }
    }

    private static string ExtractSnippet(string content, int startLine, int endLine)
    {
        var lines = content.Replace("\r", string.Empty).Split('\n');
        var startIndex = Math.Max(0, startLine - 1);
        var count = Math.Max(0, Math.Min(lines.Length, endLine) - startIndex);
        return count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines.Skip(startIndex).Take(count)).TrimEnd();
    }

    private static (IReadOnlyList<int> Left, IReadOnlyList<int> Right) BuildHighlightSets(
        string leftSnippet,
        int leftStartLine,
        string rightSnippet,
        int rightStartLine)
    {
        var leftLines = leftSnippet.Replace("\r", string.Empty).Split('\n');
        var rightLines = rightSnippet.Replace("\r", string.Empty).Split('\n');
        var normalizedRight = rightLines
            .Select((line, index) => new { Line = NormalizeLine(line), Index = index })
            .Where(item => !string.IsNullOrWhiteSpace(item.Line))
            .GroupBy(item => item.Line)
            .ToDictionary(group => group.Key, group => group.Select(item => item.Index).ToArray(), StringComparer.Ordinal);

        var leftHighlights = new List<int>();
        var rightHighlights = new HashSet<int>();
        for (var index = 0; index < leftLines.Length; index++)
        {
            var normalized = NormalizeLine(leftLines[index]);
            if (string.IsNullOrWhiteSpace(normalized) || !normalizedRight.TryGetValue(normalized, out var matches))
            {
                continue;
            }

            leftHighlights.Add(leftStartLine + index);
            foreach (var match in matches)
            {
                rightHighlights.Add(rightStartLine + match);
            }
        }

        if (leftHighlights.Count == 0)
        {
            var fallbackCount = Math.Min(leftLines.Length, rightLines.Length);
            leftHighlights = Enumerable.Range(leftStartLine, fallbackCount).ToList();
            rightHighlights = Enumerable.Range(rightStartLine, fallbackCount).ToHashSet();
        }

        return (leftHighlights, rightHighlights.OrderBy(line => line).ToArray());
    }

    private static string NormalizeLine(string line)
        => string.Concat(line.Where(ch => !char.IsWhiteSpace(ch))).Trim().ToLowerInvariant();
}
