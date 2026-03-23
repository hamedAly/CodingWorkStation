namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record DependencyGraphResponse(
    string ProjectKey,
    string RunId,
    DateTime AnalyzedUtc,
    int TotalNodes,
    int TotalEdges,
    IReadOnlyList<DependencyNodeResponse> Nodes,
    IReadOnlyList<DependencyEdgeResponse> Edges);
