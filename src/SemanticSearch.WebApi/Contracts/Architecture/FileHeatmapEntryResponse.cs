namespace SemanticSearch.WebApi.Contracts.Architecture;

public sealed record FileHeatmapEntryResponse(
    string RelativeFilePath,
    string FileName,
    int TotalLines,
    int StructuralDuplicateCount,
    int SemanticDuplicateCount,
    double DuplicationDensity);
