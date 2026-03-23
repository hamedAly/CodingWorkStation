namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record FileHeatmapResponse(
    string ProjectKey,
    int TotalFiles,
    IReadOnlyList<FileHeatmapEntryResponse> Entries);
