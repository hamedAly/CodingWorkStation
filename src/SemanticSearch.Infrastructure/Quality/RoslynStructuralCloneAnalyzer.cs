using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.Common;
using SemanticSearch.Infrastructure.VectorStore;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class RoslynStructuralCloneAnalyzer : IStructuralCloneAnalyzer
{
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly QualityFileFilter _fileFilter;
    private readonly StructuralCloneNormalizer _normalizer;
    private readonly StructuralCloneGrouper _grouper;

    public RoslynStructuralCloneAnalyzer(
        IProjectFileRepository projectFileRepository,
        QualityFileFilter fileFilter,
        StructuralCloneNormalizer normalizer,
        StructuralCloneGrouper grouper)
    {
        _projectFileRepository = projectFileRepository;
        _fileFilter = fileFilter;
        _normalizer = normalizer;
        _grouper = grouper;
    }

    public async Task<IReadOnlyList<DetectedCodeClone>> AnalyzeAsync(
        string projectKey,
        string? scopePath,
        int minimumLines,
        int maxFindings,
        CancellationToken cancellationToken = default)
    {
        var files = await _projectFileRepository.ListFilesAsync(projectKey, cancellationToken);
        var candidates = new List<StructuralCandidate>();

        foreach (var file in files.Where(file =>
                     string.Equals(file.Extension, ".cs", StringComparison.OrdinalIgnoreCase) &&
                     _fileFilter.ShouldAnalyze(file.RelativeFilePath, scopePath)))
        {
            if (!File.Exists(file.AbsoluteFilePath))
            {
                continue;
            }

            var textResult = await TextFileLoader.TryReadSanitizedTextAsync(file.AbsoluteFilePath, cancellationToken);
            if (!textResult.Success || textResult.IsBinary || string.IsNullOrWhiteSpace(textResult.Content))
            {
                continue;
            }

            var syntaxTree = CSharpSyntaxTree.ParseText(textResult.Content, cancellationToken: cancellationToken);
            var root = await syntaxTree.GetRootAsync(cancellationToken);
            var lines = textResult.Content.Replace("\r", string.Empty).Split('\n');

            foreach (var node in root.DescendantNodes().Where(IsCandidateNode))
            {
                var lineSpan = node.GetLocation().GetLineSpan();
                var startLine = lineSpan.StartLinePosition.Line + 1;
                var endLine = lineSpan.EndLinePosition.Line + 1;
                var matchingLineCount = Math.Max(1, endLine - startLine + 1);
                if (matchingLineCount < minimumLines)
                {
                    continue;
                }

                var snippet = ExtractSnippet(lines, startLine, endLine);
                if (string.IsNullOrWhiteSpace(snippet))
                {
                    continue;
                }

                var region = new DetectedCodeRegion(
                    file.RelativeFilePath,
                    startLine,
                    endLine,
                    snippet,
                    SqliteVectorStore.ComputeContentHash(snippet));

                var fingerprint = SqliteVectorStore.ComputeContentHash(_normalizer.Normalize(node));
                candidates.Add(new StructuralCandidate(fingerprint, matchingLineCount, region));
            }
        }

        return _grouper.BuildFindings(candidates, maxFindings);
    }

    private static bool IsCandidateNode(SyntaxNode node)
        => node is BaseMethodDeclarationSyntax or AccessorDeclarationSyntax or LocalFunctionStatementSyntax;

    private static string ExtractSnippet(string[] lines, int startLine, int endLine)
    {
        var startIndex = Math.Max(0, startLine - 1);
        var count = Math.Max(0, Math.Min(lines.Length, endLine) - startIndex);
        return count == 0
            ? string.Empty
            : string.Join(Environment.NewLine, lines.Skip(startIndex).Take(count)).TrimEnd();
    }
}
