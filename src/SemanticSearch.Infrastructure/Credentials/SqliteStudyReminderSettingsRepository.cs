using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Credentials;

public sealed class SqliteStudyReminderSettingsRepository : IStudyReminderSettingsRepository
{
    private readonly string _connectionString;

    public SqliteStudyReminderSettingsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<StudyReminderSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT SettingsId, Enabled, ReminderTime, UpdatedUtc FROM StudyReminderSettings WHERE SettingsId = 'default';";
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
            return null;

        return new StudyReminderSettings
        {
            SettingsId = reader.GetString(0),
            Enabled = reader.GetInt64(1) != 0,
            ReminderTime = reader.GetString(2),
            UpdatedUtc = DateTime.Parse(reader.GetString(3))
        };
    }

    public async Task SaveAsync(StudyReminderSettings settings, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO StudyReminderSettings (SettingsId, Enabled, ReminderTime, UpdatedUtc)
            VALUES (@id, @enabled, @time, @updated)
            ON CONFLICT(SettingsId) DO UPDATE SET
                Enabled = excluded.Enabled,
                ReminderTime = excluded.ReminderTime,
                UpdatedUtc = excluded.UpdatedUtc;
            """;
        cmd.Parameters.AddWithValue("@id", settings.SettingsId);
        cmd.Parameters.AddWithValue("@enabled", settings.Enabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@time", settings.ReminderTime);
        cmd.Parameters.AddWithValue("@updated", settings.UpdatedUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}