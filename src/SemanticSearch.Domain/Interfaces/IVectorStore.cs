using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Domain.Interfaces;

public interface IVectorStore
{
    Task UpsertChunksAsync(IReadOnlyList<Chunk> chunks, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Chunk>> GetChunksByProjectAsync(string projectKey, CancellationToken cancellationToken = default);
    Task UpsertProjectMetadataAsync(ProjectMetadata metadata, CancellationToken cancellationToken = default);
    Task<ProjectMetadata?> GetProjectMetadataAsync(string projectKey, CancellationToken cancellationToken = default);
    Task DeleteStaleChunksAsync(string projectKey, string filePath, IReadOnlySet<string> keepChunkIds, CancellationToken cancellationToken = default);
    Task InitializeAsync(CancellationToken cancellationToken = default);
}
