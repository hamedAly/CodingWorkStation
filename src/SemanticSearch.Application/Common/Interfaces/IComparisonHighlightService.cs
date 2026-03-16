using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IComparisonHighlightService
{
    Task<DuplicateComparisonModel> BuildAsync(
        string projectKey,
        string findingId,
        CancellationToken cancellationToken = default);
}
