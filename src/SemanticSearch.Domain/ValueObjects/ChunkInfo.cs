namespace SemanticSearch.Domain.ValueObjects;

public sealed record ChunkInfo(string FilePath, string Content, int StartLine, int EndLine);
