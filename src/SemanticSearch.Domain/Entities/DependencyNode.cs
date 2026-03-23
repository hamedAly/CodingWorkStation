using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Entities;

public sealed class DependencyNode
{
    public string NodeId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RunId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public DependencyNodeKind Kind { get; init; }
    public string Namespace { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public string? ParentNodeId { get; init; }
}
