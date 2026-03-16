using System.Security.Cryptography;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.FileSystem;

public sealed class ProjectCatalogService
{
    public string ToRelativePath(string sourceRootPath, string absoluteFilePath)
    {
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(sourceRootPath));
        var normalizedPath = Path.GetFullPath(absoluteFilePath);

        if (!normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"'{absoluteFilePath}' is not inside '{sourceRootPath}'.");

        return Path.GetRelativePath(sourceRootPath, absoluteFilePath).Replace('\\', '/');
    }

    public string ToAbsolutePath(string sourceRootPath, string relativeFilePath)
    {
        var combined = Path.GetFullPath(Path.Combine(sourceRootPath, relativeFilePath));
        var normalizedRoot = EnsureTrailingSeparator(Path.GetFullPath(sourceRootPath));

        if (!combined.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"'{relativeFilePath}' resolves outside '{sourceRootPath}'.");

        return combined;
    }

    public async Task<IndexedFile> CreateIndexedFileAsync(
        string projectKey,
        string sourceRootPath,
        string absoluteFilePath,
        int segmentCount,
        CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(absoluteFilePath);
        var relativeFilePath = ToRelativePath(sourceRootPath, absoluteFilePath);

        return new IndexedFile
        {
            ProjectKey = projectKey,
            RelativeFilePath = relativeFilePath,
            AbsoluteFilePath = fileInfo.FullName,
            FileName = fileInfo.Name,
            Extension = fileInfo.Extension,
            Checksum = await ComputeChecksumAsync(absoluteFilePath, cancellationToken),
            SizeBytes = fileInfo.Exists ? fileInfo.Length : 0,
            LastModifiedUtc = fileInfo.Exists ? fileInfo.LastWriteTimeUtc : DateTime.UtcNow,
            LastIndexedUtc = DateTime.UtcNow,
            SegmentCount = segmentCount,
            Availability = fileInfo.Exists ? ProjectFileAvailability.Available : ProjectFileAvailability.Missing
        };
    }

    private static async Task<string> ComputeChecksumAsync(string filePath, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(filePath);
        var hash = await SHA256.HashDataAsync(stream, cancellationToken);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static string EnsureTrailingSeparator(string path)
        => path.EndsWith(Path.DirectorySeparatorChar) ? path : $"{path}{Path.DirectorySeparatorChar}";
}
