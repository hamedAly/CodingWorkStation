using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.ProjectTree;

public sealed class ProjectTreeBuilder : IProjectTreeService
{
    private readonly IProjectFileRepository _projectFileRepository;

    public ProjectTreeBuilder(IProjectFileRepository projectFileRepository)
    {
        _projectFileRepository = projectFileRepository;
    }

    public async Task<IReadOnlyList<ProjectTreeNode>> GetTreeAsync(
        string projectKey,
        CancellationToken cancellationToken = default)
    {
        var files = await _projectFileRepository.ListFilesAsync(projectKey, cancellationToken);
        var root = new MutableNode(string.Empty, string.Empty, ProjectTreeNodeType.Directory, null);

        foreach (var file in files)
            AddFile(root, file.RelativeFilePath);

        return root.Children
            .OrderBy(node => node.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapNode)
            .ToList();
    }

    private static void AddFile(MutableNode root, string relativeFilePath)
    {
        var segments = relativeFilePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var current = root;
        var currentPath = string.Empty;

        for (var i = 0; i < segments.Length; i++)
        {
            var segment = segments[i];
            currentPath = string.IsNullOrEmpty(currentPath) ? segment : $"{currentPath}/{segment}";
            var isFile = i == segments.Length - 1;
            var existing = current.Children.FirstOrDefault(child => child.Name == segment);

            if (existing is null)
            {
                existing = new MutableNode(
                    currentPath,
                    segment,
                    isFile ? ProjectTreeNodeType.File : ProjectTreeNodeType.Directory,
                    isFile ? relativeFilePath : null);
                current.Children.Add(existing);
            }

            current = existing;
        }
    }

    private static ProjectTreeNode MapNode(MutableNode node) => new(
        node.Path,
        node.Name,
        node.NodeType,
        node.RelativeFilePath,
        node.Children
            .OrderBy(child => child.NodeType)
            .ThenBy(child => child.Name, StringComparer.OrdinalIgnoreCase)
            .Select(MapNode)
            .ToList());

    private sealed class MutableNode
    {
        public MutableNode(string path, string name, ProjectTreeNodeType nodeType, string? relativeFilePath)
        {
            Path = path;
            Name = name;
            NodeType = nodeType;
            RelativeFilePath = relativeFilePath;
        }

        public string Path { get; }
        public string Name { get; }
        public ProjectTreeNodeType NodeType { get; }
        public string? RelativeFilePath { get; }
        public List<MutableNode> Children { get; } = [];
    }
}
