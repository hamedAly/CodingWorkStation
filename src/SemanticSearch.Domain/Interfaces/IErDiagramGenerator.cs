using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

/// <summary>Generates a Mermaid.js ER diagram from the live SQLite database schema.</summary>
public interface IErDiagramGenerator
{
    Task<ErDiagramResult> GenerateAsync(CancellationToken cancellationToken = default);
}
