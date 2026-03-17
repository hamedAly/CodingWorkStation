namespace SemanticSearch.WebApi.Contracts.Quality;

public sealed record AssistantStatusResponse(
    string Status,
    string ModelLabel,
    DateTime CheckedAtUtc,
    string? FailureReason);

public sealed record ProjectPlanStreamRequest(string ProjectKey, string RunId);

public sealed record FindingFixStreamRequest(string ProjectKey, string FindingId);

public sealed record AiStreamEventResponse(
    string SessionId,
    string EventType,
    int Sequence,
    string? MarkdownDelta,
    string? Message,
    DateTime OccurredAtUtc);
