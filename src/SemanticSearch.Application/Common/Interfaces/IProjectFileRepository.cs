using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Application.Common.Interfaces;

public interface IProjectFileRepository
{
    Task<IReadOnlyList<IndexedFile>> ListFilesAsync(string projectKey, CancellationToken cancellationToken = default);
    Task<IndexedFile?> GetFileAsync(string projectKey, string relativeFilePath, CancellationToken cancellationToken = default);
    Task UpsertFileAsync(IndexedFile indexedFile, CancellationToken cancellationToken = default);
    Task DeleteFileAsync(string projectKey, string relativeFilePath, CancellationToken cancellationToken = default);
    Task DeleteFilesMissingFromSetAsync(
        string projectKey,
        IReadOnlySet<string> keepRelativePaths,
        CancellationToken cancellationToken = default);
    Task ReplaceSegmentsAsync(
        string projectKey,
        string relativeFilePath,
        IReadOnlyList<SearchSegment> segments,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<SearchSegment>> ListSegmentsAsync(string projectKey, CancellationToken cancellationToken = default);
}
