using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class StructuralCloneNormalizer
{
    public string Normalize(SyntaxNode node)
    {
        var builder = new StringBuilder();

        foreach (var token in node.DescendantTokens(descendIntoTrivia: false))
        {
            var normalized = NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                continue;
            }

            if (builder.Length > 0)
            {
                builder.Append(' ');
            }

            builder.Append(normalized);
        }

        return builder.ToString();
    }

    private static string NormalizeToken(SyntaxToken token)
    {
        if (token.IsKind(SyntaxKind.IdentifierToken))
        {
            return "id";
        }

        if (token.IsKind(SyntaxKind.NumericLiteralToken))
        {
            return "num";
        }

        if (token.IsKind(SyntaxKind.StringLiteralToken) || token.IsKind(SyntaxKind.InterpolatedStringTextToken))
        {
            return "str";
        }

        if (token.IsKind(SyntaxKind.CharacterLiteralToken))
        {
            return "char";
        }

        if (token.IsKind(SyntaxKind.TrueKeyword) || token.IsKind(SyntaxKind.FalseKeyword))
        {
            return "bool";
        }

        if (token.IsKind(SyntaxKind.NullKeyword))
        {
            return "null";
        }

        return token.Text.Trim().ToLowerInvariant();
    }
}
