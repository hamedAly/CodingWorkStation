namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record DependencyNodeResponse(
    string NodeId,
    string Name,
    string FullName,
    string Kind,
    string Namespace,
    string FilePath,
    int StartLine,
    string? ParentNodeId);
