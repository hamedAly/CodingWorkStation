namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record ErDiagramResponse(
    string MermaidMarkup,
    int EntityCount,
    int RelationshipCount);
