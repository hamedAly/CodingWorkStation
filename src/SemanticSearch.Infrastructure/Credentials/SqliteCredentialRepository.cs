using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Credentials;

public sealed class SqliteCredentialRepository : ICredentialRepository
{
    private readonly string _connectionString;
    private readonly ICredentialEncryption _encryption;

    public SqliteCredentialRepository(string connectionString, ICredentialEncryption encryption)
    {
        _connectionString = connectionString;
        _encryption = encryption;
    }

    public async Task<TfsCredential?> GetTfsCredentialAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CredentialId, ServerUrl, EncryptedPat, Username, CreatedUtc, UpdatedUtc FROM TfsCredentials LIMIT 1;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new TfsCredential
        {
            CredentialId = reader.GetString(0),
            ServerUrl = reader.GetString(1),
            EncryptedPat = reader.GetString(2),
            Username = reader.GetString(3),
            CreatedUtc = DateTime.Parse(reader.GetString(4)),
            UpdatedUtc = DateTime.Parse(reader.GetString(5))
        };
    }

    public async Task SaveTfsCredentialAsync(TfsCredential credential, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO TfsCredentials (CredentialId, ServerUrl, EncryptedPat, Username, CreatedUtc, UpdatedUtc)
            VALUES (@id, @url, @pat, @user, @created, @updated)
            ON CONFLICT(CredentialId) DO UPDATE SET
                ServerUrl = excluded.ServerUrl,
                EncryptedPat = excluded.EncryptedPat,
                Username = excluded.Username,
                UpdatedUtc = excluded.UpdatedUtc;
            """;
        cmd.Parameters.AddWithValue("@id", credential.CredentialId);
        cmd.Parameters.AddWithValue("@url", credential.ServerUrl);
        cmd.Parameters.AddWithValue("@pat", credential.EncryptedPat);
        cmd.Parameters.AddWithValue("@user", credential.Username);
        cmd.Parameters.AddWithValue("@created", credential.CreatedUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@updated", credential.UpdatedUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteTfsCredentialAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM TfsCredentials;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<SlackCredential?> GetSlackCredentialAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT CredentialId, EncryptedBotToken, EncryptedUserToken, DefaultChannel, CreatedUtc, UpdatedUtc FROM SlackCredentials LIMIT 1;";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new SlackCredential
        {
            CredentialId = reader.GetString(0),
            EncryptedBotToken = reader.GetString(1),
            EncryptedUserToken = reader.IsDBNull(2) ? null : reader.GetString(2),
            DefaultChannel = reader.GetString(3),
            CreatedUtc = DateTime.Parse(reader.GetString(4)),
            UpdatedUtc = DateTime.Parse(reader.GetString(5))
        };
    }

    public async Task SaveSlackCredentialAsync(SlackCredential credential, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO SlackCredentials (CredentialId, EncryptedBotToken, EncryptedUserToken, DefaultChannel, CreatedUtc, UpdatedUtc)
            VALUES (@id, @bot, @user, @channel, @created, @updated)
            ON CONFLICT(CredentialId) DO UPDATE SET
                EncryptedBotToken = excluded.EncryptedBotToken,
                EncryptedUserToken = excluded.EncryptedUserToken,
                DefaultChannel = excluded.DefaultChannel,
                UpdatedUtc = excluded.UpdatedUtc;
            """;
        cmd.Parameters.AddWithValue("@id", credential.CredentialId);
        cmd.Parameters.AddWithValue("@bot", credential.EncryptedBotToken);
        cmd.Parameters.AddWithValue("@user", (object?)credential.EncryptedUserToken ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@channel", credential.DefaultChannel);
        cmd.Parameters.AddWithValue("@created", credential.CreatedUtc.ToString("O"));
        cmd.Parameters.AddWithValue("@updated", credential.UpdatedUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteSlackCredentialAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM SlackCredentials;";
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
