using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Quality.Assistant.Queries;

public sealed class StreamFindingFixQueryHandler : IRequestHandler<StreamFindingFixQuery, FindingFixRequestModel>
{
    private readonly IComparisonHighlightService _comparisonHighlightService;

    public StreamFindingFixQueryHandler(IComparisonHighlightService comparisonHighlightService)
    {
        _comparisonHighlightService = comparisonHighlightService;
    }

    public async Task<FindingFixRequestModel> Handle(StreamFindingFixQuery request, CancellationToken cancellationToken)
    {
        var comparison = await _comparisonHighlightService.BuildAsync(request.ProjectKey, request.FindingId, cancellationToken);

        return new FindingFixRequestModel(
            request.ProjectKey,
            comparison.FindingId,
            comparison.Severity,
            comparison.Type,
            comparison.SimilarityScore,
            comparison.LeftRegion.RelativeFilePath,
            comparison.LeftRegion.Availability,
            comparison.LeftRegion.Snippet,
            comparison.RightRegion.RelativeFilePath,
            comparison.RightRegion.Availability,
            comparison.RightRegion.Snippet);
    }
}
