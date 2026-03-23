using System.Text;
using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Infrastructure.Architecture;

/// <summary>
/// Generates a Mermaid erDiagram from the live SQLite schema using PRAGMA introspection.
/// </summary>
public sealed class SqliteErDiagramGenerator : IErDiagramGenerator
{
    private readonly string _connectionString;

    public SqliteErDiagramGenerator(string databasePath)
    {
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Cache = SqliteCacheMode.Shared
        }.ToString();
    }

    public async Task<ErDiagramResult> GenerateAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // List all user tables
        var tables = new List<string>();
        await using (var cmd = connection.CreateCommand())
        {
            cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%' ORDER BY name;";
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            while (await reader.ReadAsync(cancellationToken))
                tables.Add(reader.GetString(0));
        }

        var sb = new StringBuilder();
        sb.AppendLine("erDiagram");

        // Collect FK relationships for relationship lines
        var relationships = new List<(string FromTable, string FromColumn, string ToTable, string ToColumn)>();

        foreach (var table in tables)
        {
            // Get column info
            var columns = new List<(string Name, string Type, bool NotNull, bool IsPk)>();
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info(\"{table}\");";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var colName = reader.GetString(1);
                    var colType = reader.GetString(2);
                    var notNull = reader.GetInt32(3) == 1;
                    var isPk = reader.GetInt32(5) > 0;
                    columns.Add((colName, string.IsNullOrWhiteSpace(colType) ? "TEXT" : colType, notNull, isPk));
                }
            }

            // Get FK info
            await using (var cmd = connection.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA foreign_key_list(\"{table}\");";
                await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
                while (await reader.ReadAsync(cancellationToken))
                {
                    var toTable = reader.GetString(2);
                    var fromCol = reader.GetString(3);
                    var toCol = reader.GetString(4);
                    relationships.Add((table, fromCol, toTable, toCol));
                }
            }

            // Emit entity block
            sb.AppendLine($"    {EscapeEntityName(table)} {{");
            foreach (var (colName, colType, notNull, isPk) in columns)
            {
                var safeType = EscapeMermaidType(colType);
                var constraint = isPk ? " PK" : notNull ? "" : "";
                sb.AppendLine($"        {safeType} {EscapeColumnName(colName)}{constraint}");
            }
            sb.AppendLine("    }");
        }

        // Emit relationships
        foreach (var (fromTable, fromCol, toTable, toCol) in relationships)
        {
            sb.AppendLine($"    {EscapeEntityName(toTable)} ||--o{{ {EscapeEntityName(fromTable)} : \"{toCol} -> {fromCol}\"");
        }

        return new ErDiagramResult(sb.ToString(), tables.Count, relationships.Count);
    }

    private static string EscapeEntityName(string name) =>
        name.Replace(" ", "_").Replace("-", "_");

    private static string EscapeColumnName(string name) =>
        name.Replace(" ", "_");

    private static string EscapeMermaidType(string sqlType)
    {
        // Mermaid erDiagram attribute types must be alphanumeric
        var upper = sqlType.ToUpperInvariant();
        return upper switch
        {
            "INTEGER" or "INT" => "int",
            "REAL" or "FLOAT" or "DOUBLE" => "float",
            "BLOB" => "bytes",
            _ => "string"
        };
    }
}
