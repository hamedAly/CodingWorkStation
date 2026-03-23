namespace SemanticSearch.Domain.ValueObjects;

/// <summary>ER diagram result — generated at request time from SQLite PRAGMA queries, not persisted.</summary>
public sealed record ErDiagramResult(
    string MermaidMarkup,
    int EntityCount,
    int RelationshipCount);
