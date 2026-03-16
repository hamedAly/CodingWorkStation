using MediatR;
using SemanticSearch.Application.Common.Interfaces;

namespace SemanticSearch.Application.Search.Queries;

public sealed class SearchExactQueryHandler : IRequestHandler<SearchExactQuery, SearchResponse>
{
    private readonly IExactSearchService _exactSearchService;

    public SearchExactQueryHandler(IExactSearchService exactSearchService)
    {
        _exactSearchService = exactSearchService;
    }

    public async Task<SearchResponse> Handle(SearchExactQuery request, CancellationToken cancellationToken)
    {
        var results = await _exactSearchService.SearchAsync(
            request.ProjectKey,
            request.Keyword,
            request.MatchCase,
            request.TopK,
            cancellationToken);

        return new SearchResponse(request.ProjectKey, "Exact", results);
    }
}
