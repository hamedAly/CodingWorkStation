namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record DependencyEdgeResponse(
    string EdgeId,
    string SourceNodeId,
    string TargetNodeId,
    string RelationshipType,
    string SourceNodeName,
    string TargetNodeName);
