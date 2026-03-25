using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Credentials;

public sealed class SqliteIntegrationSettingsRepository : IIntegrationSettingsRepository
{
    private readonly string _connectionString;

    public SqliteIntegrationSettingsRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<IntegrationSettings?> GetAsync(CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            SELECT SettingsId, StandupMessage, StandupEnabled, PrayerCity, PrayerCountry, PrayerMethod, PrayerEnabled, UpdatedUtc
            FROM IntegrationSettings WHERE SettingsId = 'default';
            """;
        await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new IntegrationSettings
        {
            SettingsId = reader.GetString(0),
            StandupMessage = reader.GetString(1),
            StandupEnabled = reader.GetInt64(2) != 0,
            PrayerCity = reader.GetString(3),
            PrayerCountry = reader.GetString(4),
            PrayerMethod = (int)reader.GetInt64(5),
            PrayerEnabled = reader.GetInt64(6) != 0,
            UpdatedUtc = DateTime.Parse(reader.GetString(7))
        };
    }

    public async Task SaveAsync(IntegrationSettings settings, CancellationToken cancellationToken = default)
    {
        await using var conn = new SqliteConnection(_connectionString);
        await conn.OpenAsync(cancellationToken);
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO IntegrationSettings (SettingsId, StandupMessage, StandupEnabled, PrayerCity, PrayerCountry, PrayerMethod, PrayerEnabled, UpdatedUtc)
            VALUES (@id, @standup, @standupEnabled, @city, @country, @method, @prayerEnabled, @updated)
            ON CONFLICT(SettingsId) DO UPDATE SET
                StandupMessage = excluded.StandupMessage,
                StandupEnabled = excluded.StandupEnabled,
                PrayerCity = excluded.PrayerCity,
                PrayerCountry = excluded.PrayerCountry,
                PrayerMethod = excluded.PrayerMethod,
                PrayerEnabled = excluded.PrayerEnabled,
                UpdatedUtc = excluded.UpdatedUtc;
            """;
        cmd.Parameters.AddWithValue("@id", settings.SettingsId);
        cmd.Parameters.AddWithValue("@standup", settings.StandupMessage);
        cmd.Parameters.AddWithValue("@standupEnabled", settings.StandupEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@city", settings.PrayerCity);
        cmd.Parameters.AddWithValue("@country", settings.PrayerCountry);
        cmd.Parameters.AddWithValue("@method", settings.PrayerMethod);
        cmd.Parameters.AddWithValue("@prayerEnabled", settings.PrayerEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("@updated", settings.UpdatedUtc.ToString("O"));
        await cmd.ExecuteNonQueryAsync(cancellationToken);
    }
}
