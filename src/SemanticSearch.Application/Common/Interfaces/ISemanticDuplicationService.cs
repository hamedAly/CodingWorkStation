using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Common.Interfaces;

public interface ISemanticDuplicationService
{
    Task<IReadOnlyList<DetectedCodeClone>> AnalyzeAsync(
        string projectKey,
        string? scopePath,
        double threshold,
        int maxPairs,
        int maxFindings,
        CancellationToken cancellationToken = default);
}
