using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IStructuralCloneAnalyzer
{
    Task<IReadOnlyList<DetectedCodeClone>> AnalyzeAsync(
        string projectKey,
        string? scopePath,
        int minimumLines,
        int maxFindings,
        CancellationToken cancellationToken = default);
}
