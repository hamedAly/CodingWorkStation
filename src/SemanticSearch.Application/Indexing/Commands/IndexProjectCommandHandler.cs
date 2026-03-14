using MediatR;
using Microsoft.Extensions.Logging;
using SemanticSearch.Application.Common.Interfaces;

namespace SemanticSearch.Application.Indexing.Commands;

public sealed class IndexProjectCommandHandler : IRequestHandler<IndexProjectCommand, IndexProjectResponse>
{
    private readonly IIndexingQueue _indexingQueue;
    private readonly ILogger<IndexProjectCommandHandler> _logger;

    public IndexProjectCommandHandler(IIndexingQueue indexingQueue, ILogger<IndexProjectCommandHandler> logger)
    {
        _indexingQueue = indexingQueue;
        _logger = logger;
    }

    public async Task<IndexProjectResponse> Handle(IndexProjectCommand request, CancellationToken cancellationToken)
    {
        await _indexingQueue.EnqueueAsync(request, cancellationToken);
        _logger.LogInformation("Queued indexing job for project '{ProjectKey}' at '{ProjectPath}'", request.ProjectKey, request.ProjectPath);

        return new IndexProjectResponse(
            request.ProjectKey,
            "queued",
            $"Indexing queued for project '{request.ProjectKey}'. Use GET /api/search/status/{request.ProjectKey} to check progress.");
    }
}
