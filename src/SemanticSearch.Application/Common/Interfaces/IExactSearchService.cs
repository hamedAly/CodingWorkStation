using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IExactSearchService
{
    Task<IReadOnlyList<SearchResult>> SearchAsync(
        string projectKey,
        string keyword,
        bool matchCase,
        int topK,
        CancellationToken cancellationToken = default);
}
