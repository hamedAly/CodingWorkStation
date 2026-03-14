using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.FileSystem;

public sealed class ProjectScanner : IProjectScanner
{
    private readonly ILogger<ProjectScanner> _logger;

    public ProjectScanner(ILogger<ProjectScanner> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<string> ScanProject(
        string projectPath,
        IReadOnlySet<string> excludedDirs,
        IReadOnlySet<string> allowedExtensions)
    {
        if (!Directory.Exists(projectPath))
        {
            _logger.LogWarning("Project directory does not exist: {ProjectPath}", projectPath);
            return Array.Empty<string>();
        }

        var results = new List<string>();
        ScanDirectory(projectPath, excludedDirs, allowedExtensions, results);
        _logger.LogInformation("Scanned {Count} files in {ProjectPath}", results.Count, projectPath);
        return results;
    }

    private void ScanDirectory(
        string directory,
        IReadOnlySet<string> excludedDirs,
        IReadOnlySet<string> allowedExtensions,
        List<string> results)
    {
        try
        {
            foreach (var file in Directory.EnumerateFiles(directory))
            {
                var ext = Path.GetExtension(file);
                if (allowedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    results.Add(file);
            }

            foreach (var subDir in Directory.EnumerateDirectories(directory))
            {
                var dirName = Path.GetFileName(subDir);
                if (excludedDirs.Contains(dirName, StringComparer.OrdinalIgnoreCase))
                    continue;

                ScanDirectory(subDir, excludedDirs, allowedExtensions, results);
            }
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Access denied scanning directory: {Directory}", directory);
        }
        catch (IOException ex)
        {
            _logger.LogWarning(ex, "IO error scanning directory: {Directory}", directory);
        }
    }
}
