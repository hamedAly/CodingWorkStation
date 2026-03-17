using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Infrastructure.Quality.Assistant;

public sealed class AssistantReadinessService
{
    private readonly Lock _lock = new();
    private AssistantStatusModel _status = new("Initializing", "Local assistant", DateTime.UtcNow, "The local model has not been initialized yet.");

    public AssistantStatusModel GetStatus()
    {
        lock (_lock)
        {
            return _status;
        }
    }

    public void MarkInitializing(string modelLabel)
        => SetStatus("Initializing", modelLabel, null);

    public void MarkReady(string modelLabel)
        => SetStatus("Ready", modelLabel, null);

    public void MarkUnavailable(string modelLabel, string reason)
        => SetStatus("Unavailable", modelLabel, reason);

    public void MarkFailed(string modelLabel, string reason)
        => SetStatus("Failed", modelLabel, reason);

    private void SetStatus(string status, string modelLabel, string? failureReason)
    {
        lock (_lock)
        {
            _status = new AssistantStatusModel(status, modelLabel, DateTime.UtcNow, failureReason);
        }
    }
}
