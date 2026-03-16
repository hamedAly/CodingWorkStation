using SemanticSearch.Application.Common.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IIndexingQueue
{
    ValueTask EnqueueAsync(IndexingWorkItem workItem, CancellationToken cancellationToken = default);
}
