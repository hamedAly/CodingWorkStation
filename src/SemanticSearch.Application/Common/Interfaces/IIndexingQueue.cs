using SemanticSearch.Application.Indexing.Commands;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IIndexingQueue
{
    ValueTask EnqueueAsync(IndexProjectCommand command, CancellationToken cancellationToken = default);
}
