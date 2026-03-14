namespace SemanticSearch.Domain.Entities;

public sealed class Chunk
{
    public string Id { get; init; } = string.Empty;
    public string ProjectKey { get; init; } = string.Empty;
    public string FilePath { get; init; } = string.Empty;
    public int StartLine { get; init; }
    public int EndLine { get; init; }
    public string Content { get; init; } = string.Empty;
    public float[] Embedding { get; init; } = Array.Empty<float>();
    public DateTime CreatedAt { get; init; }
}
