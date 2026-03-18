using SemanticSearch.WebApi.Models.Navigation;

namespace SemanticSearch.WebApi.Components.Layout;

public static class BreadcrumbMap
{
    private static readonly Dictionary<string, string> RouteLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        { "/", "Home" },
        { "/dashboard", "Dashboard" },
        { "/quality", "Quality Analysis" },
        { "/indexing", "Indexing" },
        { "/search", "Search" },
        { "/explorer", "Explorer" },
        { "/assistant", "AI Assistant" },
    };

    public static IReadOnlyList<BreadcrumbSegment> GetBreadcrumbs(string path)
    {
        var normalizedPath = "/" + path.Trim('/');

        if (normalizedPath == "/")
            return [new BreadcrumbSegment("Home", null, IsCurrent: true)];

        var label = RouteLabels.TryGetValue(normalizedPath, out var mapped)
            ? mapped
            : ToTitleCase(normalizedPath.TrimStart('/').Split('/').Last());

        return
        [
            new BreadcrumbSegment("Home", "/", IsCurrent: false),
            new BreadcrumbSegment(label, null, IsCurrent: true),
        ];
    }

    private static string ToTitleCase(string segment)
    {
        if (string.IsNullOrEmpty(segment)) return segment;
        return char.ToUpperInvariant(segment[0]) + segment[1..];
    }
}
