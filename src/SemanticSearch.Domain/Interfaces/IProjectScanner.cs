namespace SemanticSearch.Domain.Interfaces;

public interface IProjectScanner
{
    IReadOnlyList<string> ScanProject(
        string projectPath,
        IReadOnlySet<string> excludedDirs,
        IReadOnlySet<string> allowedExtensions);
}
