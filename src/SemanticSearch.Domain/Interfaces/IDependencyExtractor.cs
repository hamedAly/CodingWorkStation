using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

/// <summary>Extracts class/method dependency relationships from C# source files.</summary>
public interface IDependencyExtractor
{
    Task<DependencyExtractionResult> ExtractAsync(string projectKey, CancellationToken cancellationToken = default);
}

/// <summary>Result of a Roslyn dependency extraction pass.</summary>
public sealed record DependencyExtractionResult(
    IReadOnlyList<DependencyNode> Nodes,
    IReadOnlyList<DependencyEdge> Edges,
    int FilesScanned);
