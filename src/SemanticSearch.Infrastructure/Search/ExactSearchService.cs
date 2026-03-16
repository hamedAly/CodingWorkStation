using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.Search;

public sealed class ExactSearchService : IExactSearchService
{
    private readonly IProjectFileRepository _projectFileRepository;

    public ExactSearchService(IProjectFileRepository projectFileRepository)
    {
        _projectFileRepository = projectFileRepository;
    }

    public async Task<IReadOnlyList<SearchResult>> SearchAsync(
        string projectKey,
        string keyword,
        bool matchCase,
        int topK,
        CancellationToken cancellationToken = default)
    {
        var comparison = matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        var segments = await _projectFileRepository.ListSegmentsAsync(projectKey, cancellationToken);

        return segments
            .Select(segment => new
            {
                Segment = segment,
                Index = segment.Content.IndexOf(keyword, comparison)
            })
            .Where(item => item.Index >= 0)
            .OrderBy(item => item.Index)
            .ThenBy(item => item.Segment.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .Take(topK)
            .Select(item => new SearchResult(
                item.Segment.RelativeFilePath,
                1f / (item.Index + 1),
                item.Segment.SnippetPreview,
                item.Segment.StartLine,
                item.Segment.EndLine,
                SearchMode.Exact))
            .ToList();
    }
}
