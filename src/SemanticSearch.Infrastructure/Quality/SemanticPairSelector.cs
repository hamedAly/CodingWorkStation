using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Infrastructure.Quality;

public sealed record SemanticCandidatePair(SearchSegment Left, SearchSegment Right);

public sealed class SemanticPairSelector
{
    private readonly QualityFileFilter _fileFilter;

    public SemanticPairSelector(QualityFileFilter fileFilter)
    {
        _fileFilter = fileFilter;
    }

    public IReadOnlyList<SemanticCandidatePair> SelectPairs(
        IReadOnlyList<SearchSegment> segments,
        int maxPairs,
        string? scopePath = null)
    {
        if (maxPairs <= 0)
        {
            return [];
        }

        var ordered = segments
            .Where(segment =>
                segment.EmbeddingVector.Length > 0 &&
                _fileFilter.ShouldAnalyze(segment.RelativeFilePath, scopePath))
            .OrderBy(segment => segment.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(segment => segment.StartLine)
            .ToArray();
        var topPairs = new PriorityQueue<ScoredSemanticPair, (double Score, int Length)>();

        for (var leftIndex = 0; leftIndex < ordered.Length - 1; leftIndex++)
        {
            var left = ordered[leftIndex];
            var leftLength = Math.Max(1, left.EndLine - left.StartLine + 1);

            for (var rightIndex = leftIndex + 1; rightIndex < ordered.Length; rightIndex++)
            {
                var right = ordered[rightIndex];
                if (left.RelativeFilePath == right.RelativeFilePath && left.StartLine == right.StartLine)
                {
                    continue;
                }

                if (left.ContentHash == right.ContentHash)
                {
                    continue;
                }

                var rightLength = Math.Max(1, right.EndLine - right.StartLine + 1);
                if (!AreComparableLengths(leftLength, rightLength))
                {
                    continue;
                }

                var score = DotProduct(left.EmbeddingVector, right.EmbeddingVector);
                if (score <= 0d)
                {
                    continue;
                }

                var pair = new ScoredSemanticPair(left, right, score, Math.Min(leftLength, rightLength));
                var priority = (pair.Score, pair.MatchingLineCount);

                if (topPairs.Count < maxPairs)
                {
                    topPairs.Enqueue(pair, priority);
                    continue;
                }

                var lowest = topPairs.Peek();
                if (pair.Score > lowest.Score ||
                    (Math.Abs(pair.Score - lowest.Score) < 0.0001d && pair.MatchingLineCount > lowest.MatchingLineCount))
                {
                    topPairs.Dequeue();
                    topPairs.Enqueue(pair, priority);
                }
            }
        }

        return topPairs.UnorderedItems
            .Select(item => item.Element)
            .OrderByDescending(pair => pair.Score)
            .ThenByDescending(pair => pair.MatchingLineCount)
            .ThenBy(pair => pair.Left.RelativeFilePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(pair => pair.Left.StartLine)
            .Take(maxPairs)
            .Select(pair => new SemanticCandidatePair(pair.Left, pair.Right))
            .ToList();
    }

    private static bool AreComparableLengths(int leftLength, int rightLength)
    {
        var shorter = Math.Min(leftLength, rightLength);
        var longer = Math.Max(leftLength, rightLength);
        return longer <= shorter * 2;
    }

    private static double DotProduct(float[] left, float[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        double total = 0d;

        for (var index = 0; index < length; index++)
        {
            total += left[index] * right[index];
        }

        return total;
    }

    private sealed record ScoredSemanticPair(SearchSegment Left, SearchSegment Right, double Score, int MatchingLineCount);
}

