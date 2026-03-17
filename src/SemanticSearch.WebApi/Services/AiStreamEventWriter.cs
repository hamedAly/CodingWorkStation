using System.Text;
using System.Text.Json;
using SemanticSearch.Application.Quality.Assistant.Models;
using SemanticSearch.WebApi.Contracts.Quality;

namespace SemanticSearch.WebApi.Services;

public sealed class AiStreamEventWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(
        HttpResponse response,
        IAsyncEnumerable<AiStreamEventModel> stream,
        CancellationToken cancellationToken = default)
    {
        response.ContentType = "application/x-ndjson";
        response.Headers.CacheControl = "no-store";

        await foreach (var item in stream.WithCancellation(cancellationToken))
        {
            var payload = new AiStreamEventResponse(
                item.SessionId,
                item.EventType,
                item.Sequence,
                item.MarkdownDelta,
                item.Message,
                item.OccurredAtUtc);

            var line = JsonSerializer.Serialize(payload, JsonOptions);
            await response.WriteAsync(line, cancellationToken);
            await response.WriteAsync("\n", cancellationToken);
            await response.Body.FlushAsync(cancellationToken);
        }
    }
}
