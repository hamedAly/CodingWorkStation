using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Infrastructure.Quality;

public sealed class EmbeddingSemanticDuplicationService : ISemanticDuplicationService
{
    private readonly IEmbeddingService _embeddingService;
    private readonly IProjectFileRepository _projectFileRepository;
    private readonly SemanticPairSelector _pairSelector;
    private readonly SemanticSegmentNormalizer _segmentNormalizer;

    public EmbeddingSemanticDuplicationService(
        IEmbeddingService embeddingService,
        IProjectFileRepository projectFileRepository,
        SemanticPairSelector pairSelector,
        SemanticSegmentNormalizer segmentNormalizer)
    {
        _embeddingService = embeddingService;
        _projectFileRepository = projectFileRepository;
        _pairSelector = pairSelector;
        _segmentNormalizer = segmentNormalizer;
    }

    public async Task<IReadOnlyList<DetectedCodeClone>> AnalyzeAsync(
        string projectKey,
        string? scopePath,
        double threshold,
        int maxPairs,
        int maxFindings,
        CancellationToken cancellationToken = default)
    {
        var segments = await _projectFileRepository.ListSegmentsAsync(projectKey, cancellationToken);
        var pairs = _pairSelector.SelectPairs(segments, maxPairs, scopePath);
        var normalizedEmbeddingCache = new Dictionary<string, float[]>(StringComparer.OrdinalIgnoreCase);
        var findings = new List<DetectedCodeClone>();

        foreach (var pair in pairs)
        {
            var rawScore = CosineSimilarity(pair.Left.EmbeddingVector, pair.Right.EmbeddingVector);
            var normalizedScore = 0d;

            if (rawScore < threshold)
            {
                var leftEmbedding = await GetNormalizedEmbeddingAsync(pair.Left, normalizedEmbeddingCache, cancellationToken);
                var rightEmbedding = await GetNormalizedEmbeddingAsync(pair.Right, normalizedEmbeddingCache, cancellationToken);
                normalizedScore = CosineSimilarity(leftEmbedding, rightEmbedding);
            }

            var score = Math.Max(rawScore, normalizedScore);
            if (score < threshold)
            {
                continue;
            }

            findings.Add(new DetectedCodeClone(
                DuplicationType.Semantic,
                score,
                Math.Min(
                    Math.Max(1, pair.Left.EndLine - pair.Left.StartLine + 1),
                    Math.Max(1, pair.Right.EndLine - pair.Right.StartLine + 1)),
                new DetectedCodeRegion(
                    pair.Left.RelativeFilePath,
                    pair.Left.StartLine,
                    pair.Left.EndLine,
                    pair.Left.Content,
                    pair.Left.ContentHash,
                    pair.Left.SegmentId),
                new DetectedCodeRegion(
                    pair.Right.RelativeFilePath,
                    pair.Right.StartLine,
                    pair.Right.EndLine,
                    pair.Right.Content,
                    pair.Right.ContentHash,
                    pair.Right.SegmentId)));
        }

        return findings
            .OrderByDescending(finding => finding.SimilarityScore)
            .ThenByDescending(finding => finding.MatchingLineCount)
            .Take(maxFindings)
            .ToList();
    }

    private async Task<float[]> GetNormalizedEmbeddingAsync(
        SearchSegment segment,
        IDictionary<string, float[]> cache,
        CancellationToken cancellationToken)
    {
        if (cache.TryGetValue(segment.SegmentId, out var embedding))
        {
            return embedding;
        }

        var normalizedText = _segmentNormalizer.Normalize(segment);
        embedding = await _embeddingService.GenerateEmbeddingAsync(normalizedText, cancellationToken);
        cache[segment.SegmentId] = embedding;
        return embedding;
    }

    private static double CosineSimilarity(float[] left, float[] right)
    {
        var length = Math.Min(left.Length, right.Length);
        if (length == 0)
        {
            return 0d;
        }

        double dot = 0d;
        double leftNorm = 0d;
        double rightNorm = 0d;
        for (var index = 0; index < length; index++)
        {
            dot += left[index] * right[index];
            leftNorm += left[index] * left[index];
            rightNorm += right[index] * right[index];
        }

        if (leftNorm <= 0d || rightNorm <= 0d)
        {
            return 0d;
        }

        return dot / (Math.Sqrt(leftNorm) * Math.Sqrt(rightNorm));
    }
}
