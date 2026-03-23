namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record DependencyAnalysisRunResponse(
    string RunId,
    string ProjectKey,
    string Status,
    DateTime RequestedUtc,
    DateTime? StartedUtc,
    DateTime? CompletedUtc,
    int TotalFilesScanned,
    int TotalNodesFound,
    int TotalEdgesFound,
    string? FailureReason);
