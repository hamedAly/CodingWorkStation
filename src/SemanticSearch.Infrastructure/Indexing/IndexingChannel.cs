using System.Threading.Channels;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Common.Models;

namespace SemanticSearch.Infrastructure.Indexing;

public sealed class IndexingChannel : IIndexingQueue
{
    private readonly Channel<IndexingWorkItem> _channel =
        Channel.CreateUnbounded<IndexingWorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<IndexingWorkItem> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(IndexingWorkItem workItem, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(workItem, cancellationToken);
}
