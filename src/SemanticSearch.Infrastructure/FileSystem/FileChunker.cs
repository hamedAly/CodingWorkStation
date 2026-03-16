using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.Common;

namespace SemanticSearch.Infrastructure.FileSystem;

public sealed class FileChunker : IFileChunker
{
    private readonly ILogger<FileChunker> _logger;

    public FileChunker(ILogger<FileChunker> logger)
    {
        _logger = logger;
    }

    public FileChunkingResult ChunkFile(string filePath, int chunkSize, int overlap)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Cannot chunk file that does not exist: {FilePath}", filePath);
            return FileChunkingResult.Skip("The file does not exist.", shouldWarn: true);
        }

        if (!TextFileLoader.TryReadSanitizedText(filePath, out var content, out var isBinary, out var failureReason))
        {
            if (isBinary)
            {
                _logger.LogDebug("Skipping non-text file during indexing: {FilePath}", filePath);
                return FileChunkingResult.Skip(failureReason);
            }

            _logger.LogWarning("Failed to read file {FilePath}: {FailureReason}", filePath, failureReason);
            return FileChunkingResult.Skip(failureReason, shouldWarn: true);
        }

        if (string.IsNullOrWhiteSpace(content))
            return FileChunkingResult.Skip("The file is empty or whitespace.");

        var lines = content.Replace("\r\n", "\n").Split('\n');
        if (lines.Length == 0)
            return FileChunkingResult.Skip("The file has no readable lines.");

        var chunks = new List<ChunkInfo>();
        var stride = chunkSize - overlap;
        if (stride <= 0)
            stride = chunkSize;

        if (lines.Length <= chunkSize)
        {
            chunks.Add(new ChunkInfo(filePath, string.Join('\n', lines), 1, lines.Length));
            return FileChunkingResult.Success(chunks);
        }

        for (int startIndex = 0; startIndex < lines.Length; startIndex += stride)
        {
            var endIndex = Math.Min(startIndex + chunkSize, lines.Length);
            var chunkLines = lines[startIndex..endIndex];
            var chunkContent = string.Join('\n', chunkLines);

            if (!string.IsNullOrWhiteSpace(chunkContent))
            {
                chunks.Add(new ChunkInfo(
                    FilePath: filePath,
                    Content: chunkContent,
                    StartLine: startIndex + 1,       // 1-based
                    EndLine: endIndex));              // 1-based inclusive
            }

            if (endIndex == lines.Length)
                break;
        }

        return FileChunkingResult.Success(chunks);
    }
}
