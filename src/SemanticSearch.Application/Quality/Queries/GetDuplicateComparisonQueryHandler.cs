using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Models;

namespace SemanticSearch.Application.Quality.Queries;

public sealed class GetDuplicateComparisonQueryHandler : IRequestHandler<GetDuplicateComparisonQuery, DuplicateComparisonModel>
{
    private readonly IComparisonHighlightService _comparisonHighlightService;

    public GetDuplicateComparisonQueryHandler(IComparisonHighlightService comparisonHighlightService)
    {
        _comparisonHighlightService = comparisonHighlightService;
    }

    public Task<DuplicateComparisonModel> Handle(GetDuplicateComparisonQuery request, CancellationToken cancellationToken)
        => _comparisonHighlightService.BuildAsync(request.ProjectKey, request.FindingId, cancellationToken);
}
