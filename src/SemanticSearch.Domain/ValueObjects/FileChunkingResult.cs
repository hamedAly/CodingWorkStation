namespace SemanticSearch.Domain.ValueObjects;

public sealed record FileChunkingResult(
    IReadOnlyList<ChunkInfo> Chunks,
    bool IsSkipped,
    bool ShouldWarn,
    string? SkipReason)
{
    public static FileChunkingResult Success(IReadOnlyList<ChunkInfo> chunks)
        => new(chunks, false, false, null);

    public static FileChunkingResult Skip(string? reason = null, bool shouldWarn = false)
        => new(Array.Empty<ChunkInfo>(), true, shouldWarn, reason);
}
