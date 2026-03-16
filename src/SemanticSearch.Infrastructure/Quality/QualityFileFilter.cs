namespace SemanticSearch.Infrastructure.Quality;

public sealed class QualityFileFilter
{
    private static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".cs", ".ts", ".tsx", ".js", ".jsx",
        ".py", ".rb", ".go", ".rs", ".java",
        ".cpp", ".c", ".h", ".hpp",
        ".sql", ".razor"
    };

    private static readonly string[] ExcludedPathMarkers =
    [
        "/.git/",
        "/.vs/",
        "/.venv/",
        "/venv/",
        "/node_modules/",
        "/packages/",
        "/dist/",
        "/build/",
        "/target/",
        "/coverage/",
        "/out/",
        "/debug/",
        "/release/",
        "/obj/",
        "/bin/",
        "/.specify/",
        "/specs/",
        "/publish/",
        "/wwwroot/lib/",
        "/test/",
        "/tests/",
        "/__tests__/",
        "/__snapshots__/"
    ];

    public bool ShouldAnalyze(string relativeFilePath, string? scopePath = null)
    {
        if (string.IsNullOrWhiteSpace(relativeFilePath))
        {
            return false;
        }

        var normalizedPath = relativeFilePath.Replace('\\', '/');
        var rootedPath = normalizedPath.StartsWith("/", StringComparison.Ordinal)
            ? normalizedPath
            : $"/{normalizedPath}";
        var fileName = Path.GetFileName(normalizedPath);
        var extension = Path.GetExtension(fileName);

        if (!SupportedExtensions.Contains(extension))
        {
            return false;
        }

        if (!IsInScope(rootedPath, scopePath))
        {
            return false;
        }

        if (ExcludedPathMarkers.Any(marker => rootedPath.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return false;
        }

        if (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".g.i.cs", StringComparison.OrdinalIgnoreCase) ||
            fileName.EndsWith(".generated.cs", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalizedPath.IndexOf("/Migrations/", StringComparison.OrdinalIgnoreCase) >= 0 &&
            (fileName.EndsWith(".Designer.cs", StringComparison.OrdinalIgnoreCase) ||
             fileName.EndsWith("ModelSnapshot.cs", StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        return true;
    }

    private static bool IsInScope(string rootedPath, string? scopePath)
    {
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            return true;
        }

        var normalizedScope = scopePath.Replace('\\', '/').Trim('/');
        if (normalizedScope.Length == 0)
        {
            return true;
        }

        var rootedScope = $"/{normalizedScope}";
        return rootedPath.Equals(rootedScope, StringComparison.OrdinalIgnoreCase) ||
               rootedPath.StartsWith($"{rootedScope}/", StringComparison.OrdinalIgnoreCase);
    }
}
