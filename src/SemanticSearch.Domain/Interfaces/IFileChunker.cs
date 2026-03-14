using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

public interface IFileChunker
{
    IReadOnlyList<ChunkInfo> ChunkFile(string filePath, int chunkSize, int overlap);
}
