using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.VectorStore;

public sealed class SqliteVectorStore : IVectorStore
{
    private readonly string _connectionString;

    public SqliteVectorStore(string databasePath)
    {
        var directory = Path.GetDirectoryName(databasePath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE IF NOT EXISTS Chunks (
                Id TEXT PRIMARY KEY,
                ProjectKey TEXT NOT NULL,
                FilePath TEXT NOT NULL,
                StartLine INTEGER NOT NULL,
                EndLine INTEGER NOT NULL,
                Content TEXT NOT NULL,
                Embedding BLOB NOT NULL,
                CreatedAt TEXT NOT NULL,
                UNIQUE(ProjectKey, FilePath, StartLine)
            );
            CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey ON Chunks(ProjectKey);
            CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey_FilePath ON Chunks(ProjectKey, FilePath);

            CREATE TABLE IF NOT EXISTS ProjectMetadata (
                ProjectKey TEXT PRIMARY KEY,
                TotalFiles INTEGER NOT NULL DEFAULT 0,
                TotalChunks INTEGER NOT NULL DEFAULT 0,
                LastUpdated TEXT NOT NULL
            );
        ";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpsertChunksAsync(IReadOnlyList<Chunk> chunks, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var tx = await conn.BeginTransactionAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.Transaction = (SqliteTransaction)tx;
        cmd.CommandText = @"
            INSERT INTO Chunks (Id, ProjectKey, FilePath, StartLine, EndLine, Content, Embedding, CreatedAt)
            VALUES (@Id, @ProjectKey, @FilePath, @StartLine, @EndLine, @Content, @Embedding, @CreatedAt)
            ON CONFLICT(Id) DO UPDATE SET
                EndLine = excluded.EndLine,
                Content = excluded.Content,
                Embedding = excluded.Embedding,
                CreatedAt = excluded.CreatedAt;
        ";

        foreach (var chunk in chunks)
        {
            cmd.Parameters.Clear();
            cmd.Parameters.AddWithValue("@Id", chunk.Id);
            cmd.Parameters.AddWithValue("@ProjectKey", chunk.ProjectKey);
            cmd.Parameters.AddWithValue("@FilePath", chunk.FilePath);
            cmd.Parameters.AddWithValue("@StartLine", chunk.StartLine);
            cmd.Parameters.AddWithValue("@EndLine", chunk.EndLine);
            cmd.Parameters.AddWithValue("@Content", chunk.Content);
            cmd.Parameters.AddWithValue("@Embedding", EmbeddingToBytes(chunk.Embedding));
            cmd.Parameters.AddWithValue("@CreatedAt", chunk.CreatedAt.ToString("O"));
            await cmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await tx.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Chunk>> GetChunksByProjectAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT Id, ProjectKey, FilePath, StartLine, EndLine, Content, Embedding, CreatedAt
            FROM Chunks WHERE ProjectKey = @ProjectKey";
        cmd.Parameters.AddWithValue("@ProjectKey", projectKey);

        var chunks = new List<Chunk>();
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var embeddingBlob = (byte[])reader["Embedding"];
            chunks.Add(new Chunk
            {
                Id = reader.GetString(0),
                ProjectKey = reader.GetString(1),
                FilePath = reader.GetString(2),
                StartLine = reader.GetInt32(3),
                EndLine = reader.GetInt32(4),
                Content = reader.GetString(5),
                Embedding = BytesToEmbedding(embeddingBlob),
                CreatedAt = DateTime.Parse(reader.GetString(7))
            });
        }
        return chunks;
    }

    public async Task UpsertProjectMetadataAsync(ProjectMetadata metadata, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO ProjectMetadata (ProjectKey, TotalFiles, TotalChunks, LastUpdated)
            VALUES (@ProjectKey, @TotalFiles, @TotalChunks, @LastUpdated)
            ON CONFLICT(ProjectKey) DO UPDATE SET
                TotalFiles = excluded.TotalFiles,
                TotalChunks = excluded.TotalChunks,
                LastUpdated = excluded.LastUpdated;
        ";
        cmd.Parameters.AddWithValue("@ProjectKey", metadata.ProjectKey);
        cmd.Parameters.AddWithValue("@TotalFiles", metadata.TotalFiles);
        cmd.Parameters.AddWithValue("@TotalChunks", metadata.TotalChunks);
        cmd.Parameters.AddWithValue("@LastUpdated", metadata.LastUpdated.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<ProjectMetadata?> GetProjectMetadataAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT ProjectKey, TotalFiles, TotalChunks, LastUpdated
            FROM ProjectMetadata WHERE ProjectKey = @ProjectKey";
        cmd.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new ProjectMetadata
        {
            ProjectKey = reader.GetString(0),
            TotalFiles = reader.GetInt32(1),
            TotalChunks = reader.GetInt32(2),
            LastUpdated = DateTime.Parse(reader.GetString(3))
        };
    }

    public async Task DeleteStaleChunksAsync(
        string projectKey,
        string filePath,
        IReadOnlySet<string> keepChunkIds,
        CancellationToken cancellationToken = default)
    {
        if (keepChunkIds.Count == 0)
        {
            await DeleteAllFileChunksAsync(projectKey, filePath, cancellationToken);
            return;
        }

        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);

        await using var cmd = conn.CreateCommand();
        var placeholders = string.Join(",", keepChunkIds.Select((_, i) => $"@id{i}"));
        cmd.CommandText = $@"
            DELETE FROM Chunks
            WHERE ProjectKey = @ProjectKey AND FilePath = @FilePath
            AND Id NOT IN ({placeholders});";
        cmd.Parameters.AddWithValue("@ProjectKey", projectKey);
        cmd.Parameters.AddWithValue("@FilePath", filePath);
        int idx = 0;
        foreach (var id in keepChunkIds)
            cmd.Parameters.AddWithValue($"@id{idx++}", id);

        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task DeleteAllFileChunksAsync(string projectKey, string filePath, CancellationToken cancellationToken)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Chunks WHERE ProjectKey = @ProjectKey AND FilePath = @FilePath";
        cmd.Parameters.AddWithValue("@ProjectKey", projectKey);
        cmd.Parameters.AddWithValue("@FilePath", filePath);
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public static string ComputeChunkId(string projectKey, string filePath, int startLine)
    {
        var raw = $"{projectKey}|{filePath}|{startLine}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    private static byte[] EmbeddingToBytes(float[] embedding)
    {
        return MemoryMarshal.AsBytes(embedding.AsSpan()).ToArray();
    }

    private static float[] BytesToEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).CopyTo(floats.AsSpan());
        return floats;
    }
}
