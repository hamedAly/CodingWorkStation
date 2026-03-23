using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class DependencyEdge
{
    public string EdgeId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public string SourceNodeId { get; init; } = string.Empty;
    public string TargetNodeId { get; init; } = string.Empty;
    public DependencyRelationshipType RelationshipType { get; init; }
}
