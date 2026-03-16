namespace SemanticSearch.Domain.ValueObjects;

public sealed record ProjectTreeNode(
    string Path,
    string Name,
    ProjectTreeNodeType NodeType,
    string? RelativeFilePath,
    IReadOnlyList<ProjectTreeNode> Children);
