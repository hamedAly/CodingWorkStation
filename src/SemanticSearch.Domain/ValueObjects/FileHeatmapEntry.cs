namespace SemanticSearch.Domain.ValueObjects;

/// <summary>Per-file heatmap entry — aggregated from existing quality data, not persisted.</summary>
public sealed record FileHeatmapEntry(
    string RelativeFilePath,
    string FileName,
    int TotalLines,
    int StructuralDuplicateCount,
    int SemanticDuplicateCount,
    double DuplicationDensity);
