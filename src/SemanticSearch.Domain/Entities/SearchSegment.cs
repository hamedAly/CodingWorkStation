namespace SemanticSearch.Domain.Entities;

public sealed class SearchSegment
{
    public string SegmentId { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string RelativeFilePath { get; init; } = string.Empty;
    public int SegmentOrder { get; init; }
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public string Content { get; init; } = string.Empty;
    public string SnippetPreview { get; init; } = string.Empty;
    public string ContentHash { get; init; } = string.Empty;
    public float[] EmbeddingVector { get; init; } = Array.Empty<float>();
    public int TokenCount { get; init; }
    public DateTime CreatedUtc { get; init; }
}
