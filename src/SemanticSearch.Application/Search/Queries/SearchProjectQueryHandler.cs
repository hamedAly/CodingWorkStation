using MediatR;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Search.Queries;

public sealed class SearchProjectQueryHandler : IRequestHandler<SearchProjectQuery, SearchProjectResponse>
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IVectorStore _vectorStore;

    public SearchProjectQueryHandler(IEmbeddingService embeddingService, IVectorStore vectorStore)
    {
        _embeddingService = embeddingService;
        _vectorStore = vectorStore;
    }

    public async Task<SearchProjectResponse> Handle(SearchProjectQuery request, CancellationToken cancellationToken)
    {
        var queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(request.Query, cancellationToken);
        var chunks = await _vectorStore.GetChunksByProjectAsync(request.ProjectKey, cancellationToken);

        if (chunks.Count == 0)
            return new SearchProjectResponse(Array.Empty<SearchResult>());

        // Cosine similarity — embeddings are pre-normalized, so this is a dot product
        var scored = chunks
            .Select(chunk => (
                Chunk: chunk,
                Score: DotProduct(queryEmbedding, chunk.Embedding)
            ))
            .OrderByDescending(x => x.Score)
            .Take(request.TopK)
            .Select(x => new SearchResult(
                FilePath: x.Chunk.FilePath,
                RelevanceScore: x.Score,
                Snippet: x.Chunk.Content,
                StartLine: x.Chunk.StartLine,
                EndLine: x.Chunk.EndLine))
            .ToList();

        return new SearchProjectResponse(scored);
    }

    private static float DotProduct(float[] a, float[] b)
    {
        var sum = 0f;
        var len = Math.Min(a.Length, b.Length);
        for (int i = 0; i < len; i++)
            sum += a[i] * b[i];
        return sum;
    }
}
