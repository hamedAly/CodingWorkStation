using System.Threading.Channels;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Application.Indexing.Commands;

namespace SemanticSearch.Infrastructure.Indexing;

public sealed class IndexingChannel : IIndexingQueue
{
    private readonly Channel<IndexProjectCommand> _channel =
        Channel.CreateUnbounded<IndexProjectCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

    public ChannelReader<IndexProjectCommand> Reader => _channel.Reader;

    public ValueTask EnqueueAsync(IndexProjectCommand command, CancellationToken cancellationToken = default)
        => _channel.Writer.WriteAsync(command, cancellationToken);
}
