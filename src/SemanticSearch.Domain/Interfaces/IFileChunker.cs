using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

public interface IFileChunker
{
    FileChunkingResult ChunkFile(string filePath, int chunkSize, int overlap);
}
