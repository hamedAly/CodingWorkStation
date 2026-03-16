using Microsoft.CodeAnalysis.CSharp;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Infrastructure.Common;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class SemanticSegmentNormalizer
{
    private readonly StructuralCloneNormalizer _structuralCloneNormalizer;

    public SemanticSegmentNormalizer(StructuralCloneNormalizer structuralCloneNormalizer)
    {
        _structuralCloneNormalizer = structuralCloneNormalizer;
    }

    public string Normalize(SearchSegment segment)
    {
        var content = TextSanitizer.Sanitize(segment.Content);
        if (string.IsNullOrWhiteSpace(content))
        {
            return " ";
        }

        if (string.Equals(Path.GetExtension(segment.RelativeFilePath), ".cs", StringComparison.OrdinalIgnoreCase))
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(content);
            var root = syntaxTree.GetRoot();
            var normalized = _structuralCloneNormalizer.Normalize(root);
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        return CollapseWhitespace(content);
    }

    private static string CollapseWhitespace(string content)
        => string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
