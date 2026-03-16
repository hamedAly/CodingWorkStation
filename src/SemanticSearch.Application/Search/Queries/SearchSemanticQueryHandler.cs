using MediatR;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Search.Queries;

public sealed class SearchSemanticQueryHandler : IRequestHandler<SearchSemanticQuery, SearchResponse>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IProjectFileRepository _projectFileRepository;

    public SearchSemanticQueryHandler(
        IEmbeddingService embeddingService,
        IProjectFileRepository projectFileRepository)
    {
        _embeddingService = embeddingService;
        _projectFileRepository = projectFileRepository;
    }

    public async Task<SearchResponse> Handle(SearchSemanticQuery request, CancellationToken cancellationToken)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var segments = await _projectFileRepository.ListSegmentsAsync(request.ProjectKey, cancellationToken);

        var results = segments
            .Select(segment => new
            {
                Segment = segment,
                Score = DotProduct(queryEmbedding, segment.EmbeddingVector)
            })
            .OrderByDescending(item => item.Score)
            .Take(request.TopK)
            .Select(item => new SearchResult(
                item.Segment.RelativeFilePath,
                item.Score,
                item.Segment.SnippetPreview,
                item.Segment.StartLine,
                item.Segment.EndLine,
                SearchMode.Semantic))
            .ToList();

        return new SearchResponse(request.ProjectKey, "Semantic", results);
    }

    private static float DotProduct(float[] left, float[] right)
    {
        var sum = 0f;
        var length = Math.Min(left.Length, right.Length);

        for (var i = 0; i < length; i++)
            sum += left[i] * right[i];

        return sum;
    }
}
