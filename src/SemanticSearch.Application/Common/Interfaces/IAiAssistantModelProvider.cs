using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IAiAssistantModelProvider
{
    AssistantStatusModel GetStatus();
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    ValueTask<IAiAssistantExecutor> CreateExecutorAsync(CancellationToken cancellationToken = default);
}

public interface IAiAssistantExecutor : IAsyncDisposable
{
    IAsyncEnumerable<string> InferAsync(
        AssistantPromptModel prompt,
        AssistantInferenceOptionsModel options,
        CancellationToken cancellationToken = default);
}
