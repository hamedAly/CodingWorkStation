using SemanticSearch.Application.Common;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Quality.Assistant.Models;

namespace SemanticSearch.Infrastructure.Quality.Assistant;

public sealed class DuplicateSnippetLimiter
{
    private readonly int _maxCharacters;

    public DuplicateSnippetLimiter(Microsoft.Extensions.Options.IOptions<SemanticSearchOptions> options)
    {
        _maxCharacters = Math.Max(1000, options.Value.Assistant.MaxDuplicateSnippetCharacters);
    }

    public (string Left, string Right, bool Truncated) Bound(FindingFixRequestModel request)
    {
        if (string.IsNullOrWhiteSpace(request.LeftSnippet) || string.IsNullOrWhiteSpace(request.RightSnippet))
        {
            throw new PayloadTooLargeException("The selected duplicate comparison does not include enough source content to generate a refactoring proposal.");
        }

        var combinedLength = request.LeftSnippet.Length + request.RightSnippet.Length;
        if (combinedLength <= _maxCharacters)
        {
            return (request.LeftSnippet, request.RightSnippet, false);
        }

        var budgetPerSnippet = Math.Max(400, _maxCharacters / 2);
        return (Trim(request.LeftSnippet, budgetPerSnippet), Trim(request.RightSnippet, budgetPerSnippet), true);
    }

    private static string Trim(string content, int maxCharacters)
    {
        if (content.Length <= maxCharacters)
        {
            return content;
        }

        const string notice = "\n// ... snippet truncated for local prompt budget ...\n";
        var preservedLength = Math.Max(100, (maxCharacters - notice.Length) / 2);
        return string.Concat(
            content[..preservedLength],
            notice,
            content[^preservedLength..]);
    }
}
