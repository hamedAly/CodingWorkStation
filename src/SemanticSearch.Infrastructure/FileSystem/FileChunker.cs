using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.FileSystem;

public sealed class FileChunker : IFileChunker
{
    private readonly ILogger<FileChunker> _logger;

    public FileChunker(ILogger<FileChunker> logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<ChunkInfo> ChunkFile(string filePath, int chunkSize, int overlap)
    {
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Cannot chunk file that does not exist: {FilePath}", filePath);
            return Array.Empty<ChunkInfo>();
        }

        string content;
        try
        {
            content = File.ReadAllText(filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to read file {FilePath}", filePath);
            return Array.Empty<ChunkInfo>();
        }

        if (string.IsNullOrWhiteSpace(content))
            return Array.Empty<ChunkInfo>();

        // Binary file detection: check first 8KB for null bytes
        if (IsBinaryContent(content))
        {
            _logger.LogDebug("Skipping binary file: {FilePath}", filePath);
            return Array.Empty<ChunkInfo>();
        }

        var lines = content.Split('\n');
        if (lines.Length == 0)
            return Array.Empty<ChunkInfo>();

        var chunks = new List<ChunkInfo>();
        var stride = chunkSize - overlap;

        if (lines.Length <= chunkSize)
        {
            chunks.Add(new ChunkInfo(filePath, content, 1, lines.Length));
            return chunks;
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

        return chunks;
    }

    private static bool IsBinaryContent(string content)
    {
        var checkLength = Math.Min(content.Length, 8192);
        for (int i = 0; i < checkLength; i++)
        {
            if (content[i] == '\0')
                return true;
        }
        return false;
    }
}
