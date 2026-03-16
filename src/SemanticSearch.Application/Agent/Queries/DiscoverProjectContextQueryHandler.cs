using System.Text;
using System.Text.RegularExpressions;
using MediatR;
using SemanticSearch.Application.Common.Models;
using SemanticSearch.Application.Files.Queries;
using SemanticSearch.Application.Projects.Queries;
using SemanticSearch.Application.Search.Queries;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Agent.Queries;

public sealed class DiscoverProjectContextQueryHandler : IRequestHandler<DiscoverProjectContextQuery, DiscoverProjectContextResponse>
{
    private const int DiscoverySemanticTopK = 24;
    private const int DiscoveryExactTopK = 20;
    private const int DiscoveryFileCount = 5;

    private static readonly string[] IgnoredPathMarkers =
    [
        "/.git/", "/.vs/", "/.venv/", "/bin/", "/obj/", "/node_modules/",
        "/dist/", "/build/", "/target/", "/coverage/", "/out/", "/debug/", "/release/"
    ];

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "a", "an", "and", "are", "as", "at", "be", "bug", "by", "code", "feature", "file",
        "files", "find", "fix", "for", "from", "how", "i", "in", "is", "it", "its", "me",
        "modify", "need", "of", "on", "or", "project", "related", "show", "task", "that",
        "the", "this", "to", "use", "using", "want", "web", "work", "working"
    };

    private readonly ISender _sender;

    public DiscoverProjectContextQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<DiscoverProjectContextResponse> Handle(DiscoverProjectContextQuery request, CancellationToken cancellationToken)
    {
        var searchResponse = await _sender.Send(
            new SearchSemanticQuery(request.Query, request.ProjectKey, DiscoverySemanticTopK),
            cancellationToken);

        var keywords = ExtractKeywords(request.Query);
        var exactResponses = new List<SearchResponse>(keywords.Count);

        foreach (var keyword in keywords)
        {
            exactResponses.Add(await _sender.Send(
                new SearchExactQuery(keyword, request.ProjectKey, false, DiscoveryExactTopK),
                cancellationToken));
        }

        var projectTree = await _sender.Send(new GetProjectTreeQuery(request.ProjectKey), cancellationToken);
        var pathMatches = FindPathMatches(projectTree, keywords);

        var selectedResults = RankFiles(request.Query, keywords, searchResponse.Results, exactResponses, pathMatches)
            .Take(DiscoveryFileCount)
            .ToList();

        var files = new List<(SearchResult Result, ProjectFileContent File)>(selectedResults.Count);

        foreach (var result in selectedResults)
        {
            var file = await _sender.Send(
                new ReadProjectFileQuery(request.ProjectKey, result.RelativeFilePath),
                cancellationToken);

            files.Add((ResolveDisplayResult(result, file, request.Query, keywords), file));
        }

        var markdown = BuildMarkdown(request.ProjectKey, request.Query, files);
        return new DiscoverProjectContextResponse(request.ProjectKey, request.Query, markdown);
    }

    private static IReadOnlyList<SearchResult> RankFiles(
        string query,
        IReadOnlyList<string> keywords,
        IReadOnlyList<SearchResult> semanticResults,
        IReadOnlyList<SearchResponse> exactResponses,
        IReadOnlyList<SearchResult> pathMatches)
    {
        var candidates = new Dictionary<string, RankedCandidate>(StringComparer.OrdinalIgnoreCase);

        for (var index = 0; index < semanticResults.Count; index++)
        {
            var result = semanticResults[index];
            if (ShouldIgnorePath(result.RelativeFilePath))
                continue;

            var candidate = GetOrAdd(candidates, result);
            candidate.SemanticScore = Math.Max(candidate.SemanticScore, result.Score);
            candidate.SemanticRank = Math.Min(candidate.SemanticRank, index);
        }

        foreach (var response in exactResponses)
        {
            for (var index = 0; index < response.Results.Count; index++)
            {
                var result = response.Results[index];
                if (ShouldIgnorePath(result.RelativeFilePath))
                    continue;

                var candidate = GetOrAdd(candidates, result);
                candidate.ExactScore += result.Score;
                candidate.ExactMatches++;
                candidate.BestExactRank = Math.Min(candidate.BestExactRank, index);
            }
        }

        foreach (var result in pathMatches)
        {
            var candidate = GetOrAdd(candidates, result);
            candidate.PathMatchScore = Math.Max(candidate.PathMatchScore, result.Score);
        }

        foreach (var candidate in candidates.Values)
        {
            candidate.CompositeScore = ComputeCompositeScore(query, keywords, candidate);
        }

        return candidates.Values
            .OrderByDescending(candidate => candidate.CompositeScore)
            .ThenByDescending(candidate => candidate.SemanticScore)
            .ThenByDescending(candidate => candidate.PathMatchScore)
            .ThenBy(candidate => candidate.Result.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .Select(candidate => candidate.Result)
            .ToList();
    }

    private static RankedCandidate GetOrAdd(IDictionary<string, RankedCandidate> candidates, SearchResult result)
    {
        if (!candidates.TryGetValue(result.RelativeFilePath, out var candidate))
        {
            candidate = new RankedCandidate(result);
            candidates[result.RelativeFilePath] = candidate;
        }

        if (result.Score > candidate.Result.Score)
            candidate.Result = result;

        return candidate;
    }

    private static double ComputeCompositeScore(string query, IReadOnlyList<string> keywords, RankedCandidate candidate)
    {
        var path = candidate.Result.RelativeFilePath.Replace('\\', '/');
        var pathLower = path.ToLowerInvariant();
        var fileName = Path.GetFileName(path).ToLowerInvariant();
        var fileStem = Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
        var keywordFileHits = 0;

        double score = 0;
        score += candidate.SemanticScore * 0.90;
        score += candidate.ExactScore * 2.20;
        score += candidate.PathMatchScore * 1.80;

        if (candidate.SemanticRank < int.MaxValue)
            score += 0.45 / (candidate.SemanticRank + 1);

        if (candidate.BestExactRank < int.MaxValue)
            score += 0.60 / (candidate.BestExactRank + 1);

        score += Math.Min(candidate.ExactMatches, 4) * 0.12;

        foreach (var keyword in keywords)
        {
            var keywordLower = keyword.ToLowerInvariant();
            if (fileStem.Contains(keywordLower, StringComparison.Ordinal))
            {
                score += 1.10;
                keywordFileHits++;

                if (fileStem.StartsWith(keywordLower, StringComparison.Ordinal) ||
                    fileStem.EndsWith(keywordLower, StringComparison.Ordinal))
                {
                    score += 0.28;
                }

                if (fileStem.Contains("create", StringComparison.Ordinal) ||
                    fileStem.Contains("dialog", StringComparison.Ordinal) ||
                    fileStem.Contains("transition", StringComparison.Ordinal) ||
                    fileStem.Contains("status", StringComparison.Ordinal) ||
                    fileStem.Contains("history", StringComparison.Ordinal) ||
                    fileStem.Contains("workflow", StringComparison.Ordinal))
                {
                    score += 0.30;
                }
            }
            else if (pathLower.Contains($"/{keywordLower}/", StringComparison.Ordinal) ||
                     pathLower.Contains($"/{keywordLower}.", StringComparison.Ordinal) ||
                     pathLower.Contains($"{keywordLower}/", StringComparison.Ordinal) ||
                     pathLower.Contains(keywordLower, StringComparison.Ordinal))
            {
                score += 0.20;
            }
        }

        if (pathLower.Contains("/src/", StringComparison.Ordinal))
            score += 0.25;

        if (pathLower.Contains("/webreport.client/apps/reporting-app/src/", StringComparison.Ordinal) ||
            pathLower.Contains("/webreport/src/", StringComparison.Ordinal))
            score += 0.25;

        if (pathLower.Contains("/components/", StringComparison.Ordinal) ||
            pathLower.Contains("/commands/", StringComparison.Ordinal) ||
            pathLower.Contains("/queries/", StringComparison.Ordinal) ||
            pathLower.Contains("/api/", StringComparison.Ordinal) ||
            pathLower.Contains("/types/", StringComparison.Ordinal))
            score += 0.12;

        if (pathLower.Contains("/components/workflow/", StringComparison.Ordinal) ||
            pathLower.Contains("/commands/create", StringComparison.Ordinal) ||
            pathLower.Contains("/commands/transition", StringComparison.Ordinal))
            score += 0.16;

        if (pathLower.Contains("/specs/", StringComparison.Ordinal) ||
            pathLower.EndsWith(".md", StringComparison.Ordinal))
            score -= 0.18;

        if (pathLower.Contains("/tests/", StringComparison.Ordinal))
            score -= 0.08;

        if (pathLower.Contains("/migrations/", StringComparison.Ordinal) ||
            fileName.EndsWith(".designer.cs", StringComparison.Ordinal))
            score -= 0.35;

        if (keywords.Count == 1 && keywordFileHits == 0)
        {
            if (fileStem is "editor" or "index" or "layout" or "program" or "app")
                score -= 0.55;

            if (pathLower.Contains("/components/editor/", StringComparison.Ordinal))
                score -= 0.25;
        }

        if (query.Contains("ui", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("dialog", StringComparison.OrdinalIgnoreCase) ||
            query.Contains("frontend", StringComparison.OrdinalIgnoreCase))
        {
            if (pathLower.Contains("/webreport.client/", StringComparison.Ordinal))
                score += 0.18;
        }

        return score;
    }

    private static List<SearchResult> FindPathMatches(
        IReadOnlyList<ProjectTreeNode> tree,
        IReadOnlyList<string> keywords)
    {
        var results = new List<SearchResult>();

        foreach (var filePath in EnumerateFilePaths(tree))
        {
            if (ShouldIgnorePath(filePath))
                continue;

            var pathLower = filePath.Replace('\\', '/').ToLowerInvariant();
            var fileStem = Path.GetFileNameWithoutExtension(pathLower);
            double score = 0;

            foreach (var keyword in keywords)
            {
                var keywordLower = keyword.ToLowerInvariant();
                if (fileStem.Contains(keywordLower, StringComparison.Ordinal))
                {
                    score += 1.10;

                    if (fileStem.Contains("dialog", StringComparison.Ordinal) ||
                        fileStem.Contains("create", StringComparison.Ordinal) ||
                        fileStem.Contains("transition", StringComparison.Ordinal) ||
                        fileStem.Contains("workflow", StringComparison.Ordinal) ||
                        fileStem.Contains("status", StringComparison.Ordinal))
                    {
                        score += 0.40;
                    }
                }
                else if (pathLower.Contains(keywordLower, StringComparison.Ordinal))
                {
                    score += 0.20;
                }
            }

            if (score <= 0)
                continue;

            results.Add(new SearchResult(filePath, (float)score, string.Empty, 1, 1, SearchMode.Exact));
        }

        return results;
    }

    private static SearchResult ResolveDisplayResult(
        SearchResult result,
        ProjectFileContent file,
        string query,
        IReadOnlyList<string> keywords)
    {
        if (!NeedsSnippetResolution(result))
            return result;

        var resolved = FindRelevantWindow(file.Content, query, keywords);
        return result with
        {
            Snippet = resolved.Snippet,
            StartLine = resolved.StartLine,
            EndLine = resolved.EndLine
        };
    }

    private static bool NeedsSnippetResolution(SearchResult result)
        => string.IsNullOrWhiteSpace(result.Snippet) || (result.StartLine == 1 && result.EndLine == 1);

    private static (string Snippet, int StartLine, int EndLine) FindRelevantWindow(
        string content,
        string query,
        IReadOnlyList<string> keywords)
    {
        var normalized = content.Replace("\r\n", "\n");
        var lines = normalized.Split('\n');

        if (lines.Length == 0)
            return (string.Empty, 1, 1);

        var phrases = BuildSearchPhrases(query, keywords);
        var bestLineIndex = 0;
        var bestScore = double.MinValue;

        for (var index = 0; index < lines.Length; index++)
        {
            var score = ScoreLine(lines[index], phrases);
            if (score > bestScore)
            {
                bestScore = score;
                bestLineIndex = index;
            }
        }

        if (bestScore <= 0)
        {
            var firstContentLine = Array.FindIndex(lines, line => !string.IsNullOrWhiteSpace(line));
            bestLineIndex = firstContentLine >= 0 ? firstContentLine : 0;
        }

        var startLine = Math.Max(1, bestLineIndex + 1 - 3);
        var endLine = Math.Min(lines.Length, startLine + 11);
        var snippet = string.Join('\n', lines[(startLine - 1)..endLine]).TrimEnd();

        return (snippet, startLine, endLine);
    }

    private static List<string> BuildSearchPhrases(string query, IReadOnlyList<string> keywords)
    {
        var phrases = new List<string>();
        if (!string.IsNullOrWhiteSpace(query))
            phrases.Add(query.Trim());

        phrases.AddRange(keywords);
        return phrases
            .Where(phrase => !string.IsNullOrWhiteSpace(phrase))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(phrase => phrase.Length)
            .ToList();
    }

    private static double ScoreLine(string line, IReadOnlyList<string> phrases)
    {
        if (string.IsNullOrWhiteSpace(line))
            return 0;

        var lineLower = line.ToLowerInvariant();
        double score = 0;

        foreach (var phrase in phrases)
        {
            var phraseLower = phrase.ToLowerInvariant();
            if (!lineLower.Contains(phraseLower, StringComparison.Ordinal))
                continue;

            score += phraseLower.Length >= 10 ? 6.0 : 3.0;

            if (lineLower.Contains($"function {phraseLower}", StringComparison.Ordinal) ||
                lineLower.Contains($"class {phraseLower}", StringComparison.Ordinal) ||
                lineLower.Contains($"interface {phraseLower}", StringComparison.Ordinal) ||
                lineLower.Contains($"record {phraseLower}", StringComparison.Ordinal) ||
                lineLower.Contains($"enum {phraseLower}", StringComparison.Ordinal))
            {
                score += 2.5;
            }
        }

        if (lineLower.Contains("addendum", StringComparison.Ordinal))
            score += 1.5;

        if (lineLower.Contains("create", StringComparison.Ordinal) ||
            lineLower.Contains("transition", StringComparison.Ordinal) ||
            lineLower.Contains("status", StringComparison.Ordinal) ||
            lineLower.Contains("dialog", StringComparison.Ordinal))
        {
            score += 0.8;
        }

        return score;
    }

    private static IEnumerable<string> EnumerateFilePaths(IReadOnlyList<ProjectTreeNode> nodes)
    {
        foreach (var node in nodes)
        {
            if (!string.IsNullOrWhiteSpace(node.RelativeFilePath))
                yield return node.RelativeFilePath;

            foreach (var child in EnumerateFilePaths(node.Children))
                yield return child;
        }
    }

    private static List<string> ExtractKeywords(string query)
    {
        var terms = Regex.Matches(query, "[A-Za-z][A-Za-z0-9_-]{2,}")
            .Select(match => match.Value.Trim())
            .Where(term => !StopWords.Contains(term))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(6)
            .ToList();

        if (terms.Count == 0 && !string.IsNullOrWhiteSpace(query))
            terms.Add(query.Trim());

        return terms;
    }

    private static bool ShouldIgnorePath(string relativeFilePath)
    {
        var normalizedPath = "/" + relativeFilePath.Replace('\\', '/').TrimStart('/');
        return IgnoredPathMarkers.Any(marker => normalizedPath.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildMarkdown(
        string projectKey,
        string query,
        IReadOnlyList<(SearchResult Result, ProjectFileContent File)> files)
    {
        var builder = new StringBuilder();
        builder.AppendLine("# Agent Discovery");
        builder.AppendLine();
        builder.AppendLine($"- Project key: `{EscapeInline(projectKey)}`");
        builder.AppendLine($"- Query: `{EscapeInline(query)}`");
        builder.AppendLine($"- Retrieval mode: `Hybrid (semantic + exact keyword boosts)`");
        builder.AppendLine($"- Files returned: {files.Count}");
        builder.AppendLine();

        if (files.Count == 0)
        {
            builder.AppendLine("No semantic matches were found for this query.");
            return builder.ToString();
        }

        builder.AppendLine("## Ranked Matches");
        builder.AppendLine();

        for (var index = 0; index < files.Count; index++)
        {
            var (result, _) = files[index];
            builder.AppendLine($"{index + 1}. `{EscapeInline(result.RelativeFilePath)}`");
            builder.AppendLine($"   Score: `{result.Score:F4}`");
            builder.AppendLine($"   Best matching lines: `{result.StartLine}-{result.EndLine}`");
        }

        for (var index = 0; index < files.Count; index++)
        {
            var (result, file) = files[index];
            var fence = CreateFence(file.Content);
            var language = GetCodeFenceLanguage(file.RelativeFilePath);

            builder.AppendLine();
            builder.AppendLine($"## File {index + 1}: `{EscapeInline(file.RelativeFilePath)}`");
            builder.AppendLine();
            builder.AppendLine($"- Score: `{result.Score:F4}`");
            builder.AppendLine($"- Best matching lines: `{result.StartLine}-{result.EndLine}`");
            if (file.LastModifiedUtc.HasValue)
                builder.AppendLine($"- Last modified (UTC): `{file.LastModifiedUtc.Value:O}`");
            builder.AppendLine();
            builder.AppendLine($"{fence}{language}");
            builder.AppendLine(file.Content);
            builder.AppendLine(fence);
        }

        return builder.ToString();
    }

    private static string EscapeInline(string value)
        => value.Replace("`", "\\`", StringComparison.Ordinal);

    private static string CreateFence(string content)
    {
        var longestRun = 0;
        var currentRun = 0;

        foreach (var character in content)
        {
            if (character == '`')
            {
                currentRun++;
                longestRun = Math.Max(longestRun, currentRun);
            }
            else
            {
                currentRun = 0;
            }
        }

        return new string('`', Math.Max(3, longestRun + 1));
    }

    private static string GetCodeFenceLanguage(string relativeFilePath)
    {
        var extension = Path.GetExtension(relativeFilePath);

        return extension.ToLowerInvariant() switch
        {
            ".cs" => "csharp",
            ".razor" => "razor",
            ".ts" => "ts",
            ".tsx" => "tsx",
            ".js" => "js",
            ".jsx" => "jsx",
            ".json" => "json",
            ".md" => "md",
            ".css" => "css",
            ".html" => "html",
            ".sql" => "sql",
            ".xml" => "xml",
            ".yml" or ".yaml" => "yaml",
            ".py" => "python",
            _ => string.Empty
        };
    }

    private sealed class RankedCandidate
    {
        public RankedCandidate(SearchResult result)
        {
            Result = result;
        }

        public SearchResult Result { get; set; }
        public float SemanticScore { get; set; }
        public float ExactScore { get; set; }
        public int ExactMatches { get; set; }
        public int SemanticRank { get; set; } = int.MaxValue;
        public int BestExactRank { get; set; } = int.MaxValue;
        public float PathMatchScore { get; set; }
        public double CompositeScore { get; set; }
    }
}
