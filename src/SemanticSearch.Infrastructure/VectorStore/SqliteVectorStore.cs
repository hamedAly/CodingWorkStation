using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;
using SemanticSearch.Application.Common.Interfaces;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;
using SemanticSearch.Domain.ValueObjects;
using SemanticSearch.Infrastructure.Common;

namespace SemanticSearch.Infrastructure.VectorStore;

public sealed class SqliteVectorStore : IProjectWorkspaceRepository, IProjectFileRepository, IQualityRepository, IDependencyRepository
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
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = SqliteSchemaInitializer.Schema;
        await command.ExecuteNonQueryAsync(cancellationToken);
        await EnsureColumnAsync(connection, "IndexingRuns", "TotalFilesPlanned", "INTEGER NOT NULL DEFAULT 0", cancellationToken);
        await EnsureColumnAsync(connection, "IndexingRuns", "CurrentFilePath", "TEXT NULL", cancellationToken);
    }

    public async Task<IReadOnlyList<ProjectWorkspace>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectKey, SourceRootPath, Status, TotalFiles, TotalSegments, LastIndexedUtc, LastRunId, LastError
            FROM ProjectWorkspaces
            ORDER BY COALESCE(LastIndexedUtc, '') DESC, ProjectKey;
            """;

        var results = new List<ProjectWorkspace>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadWorkspace(reader));

        return results;
    }

    public async Task<ProjectWorkspace?> GetAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectKey, SourceRootPath, Status, TotalFiles, TotalSegments, LastIndexedUtc, LastRunId, LastError
            FROM ProjectWorkspaces
            WHERE ProjectKey = @ProjectKey;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadWorkspace(reader) : null;
    }

    public async Task UpsertAsync(ProjectWorkspace workspace, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO ProjectWorkspaces (
                ProjectKey, SourceRootPath, Status, TotalFiles, TotalSegments, LastIndexedUtc, LastRunId, LastError
            )
            VALUES (
                @ProjectKey, @SourceRootPath, @Status, @TotalFiles, @TotalSegments, @LastIndexedUtc, @LastRunId, @LastError
            )
            ON CONFLICT(ProjectKey) DO UPDATE SET
                SourceRootPath = excluded.SourceRootPath,
                Status = excluded.Status,
                TotalFiles = excluded.TotalFiles,
                TotalSegments = excluded.TotalSegments,
                LastIndexedUtc = excluded.LastIndexedUtc,
                LastRunId = excluded.LastRunId,
                LastError = excluded.LastError;
            """;
        command.Parameters.AddWithValue("@ProjectKey", workspace.ProjectKey);
        command.Parameters.AddWithValue("@SourceRootPath", workspace.SourceRootPath);
        command.Parameters.AddWithValue("@Status", workspace.Status.ToString());
        command.Parameters.AddWithValue("@TotalFiles", workspace.TotalFiles);
        command.Parameters.AddWithValue("@TotalSegments", workspace.TotalSegments);
        command.Parameters.AddWithValue("@LastIndexedUtc", ToDbValue(workspace.LastIndexedUtc));
        command.Parameters.AddWithValue("@LastRunId", ToDbValue(workspace.LastRunId));
        command.Parameters.AddWithValue("@LastError", ToDbValue(workspace.LastError));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IndexingRun?> GetRunAsync(string runId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RunId, ProjectKey, RunType, Status, RequestedUtc, StartedUtc, CompletedUtc, RequestedFilePath,
                   TotalFilesPlanned, FilesScanned, FilesIndexed, FilesSkipped, SegmentsWritten, WarningCount, CurrentFilePath, FailureReason
            FROM IndexingRuns
            WHERE RunId = @RunId;
            """;
        command.Parameters.AddWithValue("@RunId", runId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRun(reader) : null;
    }

    public async Task<IndexingRun?> GetActiveRunAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RunId, ProjectKey, RunType, Status, RequestedUtc, StartedUtc, CompletedUtc, RequestedFilePath,
                   TotalFilesPlanned, FilesScanned, FilesIndexed, FilesSkipped, SegmentsWritten, WarningCount, CurrentFilePath, FailureReason
            FROM IndexingRuns
            WHERE ProjectKey = @ProjectKey AND Status IN ('Queued', 'Running', 'Paused')
            ORDER BY RequestedUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRun(reader) : null;
    }

    public async Task UpsertRunAsync(IndexingRun run, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO IndexingRuns (
                RunId, ProjectKey, RunType, Status, RequestedUtc, StartedUtc, CompletedUtc, RequestedFilePath,
                TotalFilesPlanned, FilesScanned, FilesIndexed, FilesSkipped, SegmentsWritten, WarningCount, CurrentFilePath, FailureReason
            )
            VALUES (
                @RunId, @ProjectKey, @RunType, @Status, @RequestedUtc, @StartedUtc, @CompletedUtc, @RequestedFilePath,
                @TotalFilesPlanned, @FilesScanned, @FilesIndexed, @FilesSkipped, @SegmentsWritten, @WarningCount, @CurrentFilePath, @FailureReason
            )
            ON CONFLICT(RunId) DO UPDATE SET
                Status = excluded.Status,
                StartedUtc = excluded.StartedUtc,
                CompletedUtc = excluded.CompletedUtc,
                RequestedFilePath = excluded.RequestedFilePath,
                TotalFilesPlanned = excluded.TotalFilesPlanned,
                FilesScanned = excluded.FilesScanned,
                FilesIndexed = excluded.FilesIndexed,
                FilesSkipped = excluded.FilesSkipped,
                SegmentsWritten = excluded.SegmentsWritten,
                WarningCount = excluded.WarningCount,
                CurrentFilePath = excluded.CurrentFilePath,
                FailureReason = excluded.FailureReason;
            """;
        command.Parameters.AddWithValue("@RunId", run.RunId);
        command.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
        command.Parameters.AddWithValue("@RunType", run.RunType.ToString());
        command.Parameters.AddWithValue("@Status", run.Status.ToString());
        command.Parameters.AddWithValue("@RequestedUtc", run.RequestedUtc.ToString("O"));
        command.Parameters.AddWithValue("@StartedUtc", ToDbValue(run.StartedUtc));
        command.Parameters.AddWithValue("@CompletedUtc", ToDbValue(run.CompletedUtc));
        command.Parameters.AddWithValue("@RequestedFilePath", ToDbValue(run.RequestedFilePath));
        command.Parameters.AddWithValue("@TotalFilesPlanned", run.TotalFilesPlanned);
        command.Parameters.AddWithValue("@FilesScanned", run.FilesScanned);
        command.Parameters.AddWithValue("@FilesIndexed", run.FilesIndexed);
        command.Parameters.AddWithValue("@FilesSkipped", run.FilesSkipped);
        command.Parameters.AddWithValue("@SegmentsWritten", run.SegmentsWritten);
        command.Parameters.AddWithValue("@WarningCount", run.WarningCount);
        command.Parameters.AddWithValue("@CurrentFilePath", ToDbValue(run.CurrentFilePath));
        command.Parameters.AddWithValue("@FailureReason", ToDbValue(run.FailureReason));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<IndexedFile>> ListFilesAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectKey, RelativeFilePath, AbsoluteFilePath, FileName, Extension, Checksum, SizeBytes,
                   LastModifiedUtc, LastIndexedUtc, SegmentCount, Availability
            FROM IndexedFiles
            WHERE ProjectKey = @ProjectKey
            ORDER BY RelativeFilePath;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        var results = new List<IndexedFile>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadFile(reader));

        return results;
    }

    public async Task<IndexedFile?> GetFileAsync(
        string projectKey,
        string relativeFilePath,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectKey, RelativeFilePath, AbsoluteFilePath, FileName, Extension, Checksum, SizeBytes,
                   LastModifiedUtc, LastIndexedUtc, SegmentCount, Availability
            FROM IndexedFiles
            WHERE ProjectKey = @ProjectKey AND RelativeFilePath = @RelativeFilePath;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);
        command.Parameters.AddWithValue("@RelativeFilePath", relativeFilePath);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadFile(reader) : null;
    }

    public async Task UpsertFileAsync(IndexedFile indexedFile, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO IndexedFiles (
                ProjectKey, RelativeFilePath, AbsoluteFilePath, FileName, Extension, Checksum, SizeBytes,
                LastModifiedUtc, LastIndexedUtc, SegmentCount, Availability
            )
            VALUES (
                @ProjectKey, @RelativeFilePath, @AbsoluteFilePath, @FileName, @Extension, @Checksum, @SizeBytes,
                @LastModifiedUtc, @LastIndexedUtc, @SegmentCount, @Availability
            )
            ON CONFLICT(ProjectKey, RelativeFilePath) DO UPDATE SET
                AbsoluteFilePath = excluded.AbsoluteFilePath,
                FileName = excluded.FileName,
                Extension = excluded.Extension,
                Checksum = excluded.Checksum,
                SizeBytes = excluded.SizeBytes,
                LastModifiedUtc = excluded.LastModifiedUtc,
                LastIndexedUtc = excluded.LastIndexedUtc,
                SegmentCount = excluded.SegmentCount,
                Availability = excluded.Availability;
            """;
        command.Parameters.AddWithValue("@ProjectKey", indexedFile.ProjectKey);
        command.Parameters.AddWithValue("@RelativeFilePath", indexedFile.RelativeFilePath);
        command.Parameters.AddWithValue("@AbsoluteFilePath", indexedFile.AbsoluteFilePath);
        command.Parameters.AddWithValue("@FileName", indexedFile.FileName);
        command.Parameters.AddWithValue("@Extension", indexedFile.Extension);
        command.Parameters.AddWithValue("@Checksum", indexedFile.Checksum);
        command.Parameters.AddWithValue("@SizeBytes", indexedFile.SizeBytes);
        command.Parameters.AddWithValue("@LastModifiedUtc", indexedFile.LastModifiedUtc.ToString("O"));
        command.Parameters.AddWithValue("@LastIndexedUtc", indexedFile.LastIndexedUtc.ToString("O"));
        command.Parameters.AddWithValue("@SegmentCount", indexedFile.SegmentCount);
        command.Parameters.AddWithValue("@Availability", indexedFile.Availability.ToString());
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteFileAsync(string projectKey, string relativeFilePath, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        await DeleteFileContentAsync(connection, (SqliteTransaction)transaction, projectKey, relativeFilePath, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    public async Task DeleteFilesMissingFromSetAsync(
        string projectKey,
        IReadOnlySet<string> keepRelativePaths,
        CancellationToken cancellationToken = default)
    {
        var files = await ListFilesAsync(projectKey, cancellationToken);
        var toDelete = files
            .Where(file => !keepRelativePaths.Contains(file.RelativeFilePath))
            .Select(file => file.RelativeFilePath)
            .ToList();

        if (toDelete.Count == 0)
            return;

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var relativeFilePath in toDelete)
            await DeleteFileContentAsync(connection, (SqliteTransaction)transaction, projectKey, relativeFilePath, cancellationToken);

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task ReplaceSegmentsAsync(
        string projectKey,
        string relativeFilePath,
        IReadOnlyList<SearchSegment> segments,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var deleteCommand = connection.CreateCommand())
        {
            deleteCommand.Transaction = (SqliteTransaction)transaction;
            deleteCommand.CommandText = """
                DELETE FROM SearchSegments
                WHERE ProjectKey = @ProjectKey AND RelativeFilePath = @RelativeFilePath;
                """;
            deleteCommand.Parameters.AddWithValue("@ProjectKey", projectKey);
            deleteCommand.Parameters.AddWithValue("@RelativeFilePath", relativeFilePath);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var segment in segments)
        {
            await using var insertCommand = connection.CreateCommand();
            insertCommand.Transaction = (SqliteTransaction)transaction;
            insertCommand.CommandText = """
                INSERT INTO SearchSegments (
                    SegmentId, ProjectKey, RelativeFilePath, SegmentOrder, StartLine, EndLine, Content, SnippetPreview,
                    ContentHash, EmbeddingVector, TokenCount, CreatedUtc
                )
                VALUES (
                    @SegmentId, @ProjectKey, @RelativeFilePath, @SegmentOrder, @StartLine, @EndLine, @Content, @SnippetPreview,
                    @ContentHash, @EmbeddingVector, @TokenCount, @CreatedUtc
                );
                """;
            insertCommand.Parameters.AddWithValue("@SegmentId", segment.SegmentId);
            insertCommand.Parameters.AddWithValue("@ProjectKey", segment.ProjectKey);
            insertCommand.Parameters.AddWithValue("@RelativeFilePath", segment.RelativeFilePath);
            insertCommand.Parameters.AddWithValue("@SegmentOrder", segment.SegmentOrder);
            insertCommand.Parameters.AddWithValue("@StartLine", segment.StartLine);
            insertCommand.Parameters.AddWithValue("@EndLine", segment.EndLine);
            insertCommand.Parameters.AddWithValue("@Content", segment.Content);
            insertCommand.Parameters.AddWithValue("@SnippetPreview", segment.SnippetPreview);
            insertCommand.Parameters.AddWithValue("@ContentHash", segment.ContentHash);
            insertCommand.Parameters.AddWithValue("@EmbeddingVector", EmbeddingToBytes(segment.EmbeddingVector));
            insertCommand.Parameters.AddWithValue("@TokenCount", segment.TokenCount);
            insertCommand.Parameters.AddWithValue("@CreatedUtc", segment.CreatedUtc.ToString("O"));
            await insertCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SearchSegment>> ListSegmentsAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT SegmentId, ProjectKey, RelativeFilePath, SegmentOrder, StartLine, EndLine, Content, SnippetPreview,
                   ContentHash, EmbeddingVector, TokenCount, CreatedUtc
            FROM SearchSegments
            WHERE ProjectKey = @ProjectKey
            ORDER BY RelativeFilePath, SegmentOrder;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        var results = new List<SearchSegment>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadSegment(reader));

        return results;
    }

    public async Task<QualitySummarySnapshot?> GetSummaryAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT ProjectKey, RunId, QualityGrade, TotalLinesOfCode, UniqueLineCount, StructuralDuplicateLineCount,
                   SemanticDuplicateLineCount, DuplicationPercent, StructuralFindingCount, SemanticFindingCount, LastAnalyzedUtc
            FROM QualitySummarySnapshots
            WHERE ProjectKey = @ProjectKey;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSummary(reader) : null;
    }

    public async Task<IReadOnlyList<DuplicationFinding>> ListFindingsAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FindingId, ProjectKey, RunId, Type, Severity, SimilarityScore, MatchingLineCount,
                   NormalizedFingerprint, LeftRegionId, RightRegionId, CreatedUtc
            FROM DuplicationFindings
            WHERE ProjectKey = @ProjectKey
            ORDER BY SimilarityScore DESC, MatchingLineCount DESC, CreatedUtc DESC;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        var results = new List<DuplicationFinding>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadFinding(reader));

        return results;
    }

    public async Task<DuplicationFinding?> GetFindingAsync(string projectKey, string findingId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT FindingId, ProjectKey, RunId, Type, Severity, SimilarityScore, MatchingLineCount,
                   NormalizedFingerprint, LeftRegionId, RightRegionId, CreatedUtc
            FROM DuplicationFindings
            WHERE ProjectKey = @ProjectKey AND FindingId = @FindingId;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);
        command.Parameters.AddWithValue("@FindingId", findingId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadFinding(reader) : null;
    }

    public async Task<IReadOnlyList<CodeRegion>> ListRegionsAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RegionId, ProjectKey, RelativeFilePath, StartLine, EndLine, Snippet, ContentHash, SourceSegmentId, Availability
            FROM CodeRegions
            WHERE ProjectKey = @ProjectKey;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        var results = new List<CodeRegion>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadRegion(reader));

        return results;
    }

    public async Task<CodeRegion?> GetRegionAsync(string projectKey, string regionId, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RegionId, ProjectKey, RelativeFilePath, StartLine, EndLine, Snippet, ContentHash, SourceSegmentId, Availability
            FROM CodeRegions
            WHERE ProjectKey = @ProjectKey AND RegionId = @RegionId;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);
        command.Parameters.AddWithValue("@RegionId", regionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadRegion(reader) : null;
    }

    public async Task<QualityAnalysisRun?> GetLatestAnalysisRunAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RunId, ProjectKey, RequestedModes, Status, RequestedUtc, StartedUtc, CompletedUtc,
                   TotalFilesScanned, TotalLinesAnalyzed, StructuralFindingCount, SemanticFindingCount, FailureReason
            FROM QualityAnalysisRuns
            WHERE ProjectKey = @ProjectKey
            ORDER BY RequestedUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadQualityRun(reader) : null;
    }

    public async Task ReplaceSnapshotAsync(
        QualityAnalysisRun run,
        QualitySummarySnapshot summary,
        IReadOnlyList<DuplicationFinding> findings,
        IReadOnlyList<CodeRegion> regions,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        await using (var insertRun = connection.CreateCommand())
        {
            insertRun.Transaction = (SqliteTransaction)transaction;
            insertRun.CommandText = """
                INSERT INTO QualityAnalysisRuns (
                    RunId, ProjectKey, RequestedModes, Status, RequestedUtc, StartedUtc, CompletedUtc,
                    TotalFilesScanned, TotalLinesAnalyzed, StructuralFindingCount, SemanticFindingCount, FailureReason
                ) VALUES (
                    @RunId, @ProjectKey, @RequestedModes, @Status, @RequestedUtc, @StartedUtc, @CompletedUtc,
                    @TotalFilesScanned, @TotalLinesAnalyzed, @StructuralFindingCount, @SemanticFindingCount, @FailureReason
                );
                """;
            insertRun.Parameters.AddWithValue("@RunId", run.RunId);
            insertRun.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            insertRun.Parameters.AddWithValue("@RequestedModes", run.RequestedModes);
            insertRun.Parameters.AddWithValue("@Status", run.Status.ToString());
            insertRun.Parameters.AddWithValue("@RequestedUtc", run.RequestedUtc.ToString("O"));
            insertRun.Parameters.AddWithValue("@StartedUtc", ToDbValue(run.StartedUtc));
            insertRun.Parameters.AddWithValue("@CompletedUtc", ToDbValue(run.CompletedUtc));
            insertRun.Parameters.AddWithValue("@TotalFilesScanned", run.TotalFilesScanned);
            insertRun.Parameters.AddWithValue("@TotalLinesAnalyzed", run.TotalLinesAnalyzed);
            insertRun.Parameters.AddWithValue("@StructuralFindingCount", run.StructuralFindingCount);
            insertRun.Parameters.AddWithValue("@SemanticFindingCount", run.SemanticFindingCount);
            insertRun.Parameters.AddWithValue("@FailureReason", ToDbValue(run.FailureReason));
            await insertRun.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var tableName in new[] { "DuplicationFindings", "CodeRegions" })
        {
            await using var deleteCommand = connection.CreateCommand();
            deleteCommand.Transaction = (SqliteTransaction)transaction;
            deleteCommand.CommandText = $"DELETE FROM {tableName} WHERE ProjectKey = @ProjectKey;";
            deleteCommand.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            await deleteCommand.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var deleteSummary = connection.CreateCommand())
        {
            deleteSummary.Transaction = (SqliteTransaction)transaction;
            deleteSummary.CommandText = "DELETE FROM QualitySummarySnapshots WHERE ProjectKey = @ProjectKey;";
            deleteSummary.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            await deleteSummary.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var region in regions)
        {
            await using var insertRegion = connection.CreateCommand();
            insertRegion.Transaction = (SqliteTransaction)transaction;
            insertRegion.CommandText = """
                INSERT INTO CodeRegions (
                    RegionId, ProjectKey, RelativeFilePath, StartLine, EndLine, Snippet, ContentHash, SourceSegmentId, Availability
                ) VALUES (
                    @RegionId, @ProjectKey, @RelativeFilePath, @StartLine, @EndLine, @Snippet, @ContentHash, @SourceSegmentId, @Availability
                );
                """;
            insertRegion.Parameters.AddWithValue("@RegionId", region.RegionId);
            insertRegion.Parameters.AddWithValue("@ProjectKey", region.ProjectKey);
            insertRegion.Parameters.AddWithValue("@RelativeFilePath", region.RelativeFilePath);
            insertRegion.Parameters.AddWithValue("@StartLine", region.StartLine);
            insertRegion.Parameters.AddWithValue("@EndLine", region.EndLine);
            insertRegion.Parameters.AddWithValue("@Snippet", region.Snippet);
            insertRegion.Parameters.AddWithValue("@ContentHash", region.ContentHash);
            insertRegion.Parameters.AddWithValue("@SourceSegmentId", ToDbValue(region.SourceSegmentId));
            insertRegion.Parameters.AddWithValue("@Availability", region.Availability.ToString());
            await insertRegion.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var finding in findings)
        {
            await using var insertFinding = connection.CreateCommand();
            insertFinding.Transaction = (SqliteTransaction)transaction;
            insertFinding.CommandText = """
                INSERT INTO DuplicationFindings (
                    FindingId, ProjectKey, RunId, Type, Severity, SimilarityScore, MatchingLineCount,
                    NormalizedFingerprint, LeftRegionId, RightRegionId, CreatedUtc
                ) VALUES (
                    @FindingId, @ProjectKey, @RunId, @Type, @Severity, @SimilarityScore, @MatchingLineCount,
                    @NormalizedFingerprint, @LeftRegionId, @RightRegionId, @CreatedUtc
                );
                """;
            insertFinding.Parameters.AddWithValue("@FindingId", finding.FindingId);
            insertFinding.Parameters.AddWithValue("@ProjectKey", finding.ProjectKey);
            insertFinding.Parameters.AddWithValue("@RunId", finding.RunId);
            insertFinding.Parameters.AddWithValue("@Type", finding.Type.ToString());
            insertFinding.Parameters.AddWithValue("@Severity", finding.Severity.ToString());
            insertFinding.Parameters.AddWithValue("@SimilarityScore", finding.SimilarityScore);
            insertFinding.Parameters.AddWithValue("@MatchingLineCount", finding.MatchingLineCount);
            insertFinding.Parameters.AddWithValue("@NormalizedFingerprint", ToDbValue(finding.NormalizedFingerprint));
            insertFinding.Parameters.AddWithValue("@LeftRegionId", finding.LeftRegionId);
            insertFinding.Parameters.AddWithValue("@RightRegionId", finding.RightRegionId);
            insertFinding.Parameters.AddWithValue("@CreatedUtc", finding.CreatedUtc.ToString("O"));
            await insertFinding.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertSummary = connection.CreateCommand())
        {
            insertSummary.Transaction = (SqliteTransaction)transaction;
            insertSummary.CommandText = """
                INSERT INTO QualitySummarySnapshots (
                    ProjectKey, RunId, QualityGrade, TotalLinesOfCode, UniqueLineCount, StructuralDuplicateLineCount,
                    SemanticDuplicateLineCount, DuplicationPercent, StructuralFindingCount, SemanticFindingCount, LastAnalyzedUtc
                ) VALUES (
                    @ProjectKey, @RunId, @QualityGrade, @TotalLinesOfCode, @UniqueLineCount, @StructuralDuplicateLineCount,
                    @SemanticDuplicateLineCount, @DuplicationPercent, @StructuralFindingCount, @SemanticFindingCount, @LastAnalyzedUtc
                );
                """;
            insertSummary.Parameters.AddWithValue("@ProjectKey", summary.ProjectKey);
            insertSummary.Parameters.AddWithValue("@RunId", summary.RunId);
            insertSummary.Parameters.AddWithValue("@QualityGrade", summary.QualityGrade.ToString());
            insertSummary.Parameters.AddWithValue("@TotalLinesOfCode", summary.TotalLinesOfCode);
            insertSummary.Parameters.AddWithValue("@UniqueLineCount", summary.UniqueLineCount);
            insertSummary.Parameters.AddWithValue("@StructuralDuplicateLineCount", summary.StructuralDuplicateLineCount);
            insertSummary.Parameters.AddWithValue("@SemanticDuplicateLineCount", summary.SemanticDuplicateLineCount);
            insertSummary.Parameters.AddWithValue("@DuplicationPercent", summary.DuplicationPercent);
            insertSummary.Parameters.AddWithValue("@StructuralFindingCount", summary.StructuralFindingCount);
            insertSummary.Parameters.AddWithValue("@SemanticFindingCount", summary.SemanticFindingCount);
            insertSummary.Parameters.AddWithValue("@LastAnalyzedUtc", summary.LastAnalyzedUtc.ToString("O"));
            await insertSummary.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }
    public static string ComputeSegmentId(string projectKey, string relativeFilePath, int startLine)
    {
        var raw = $"{projectKey}|{relativeFilePath}|{startLine}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public static string ComputeContentHash(string content)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(TextSanitizer.Sanitize(content)));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    // ── IDependencyRepository ───────────────────────────────────────────────

    public async Task<DependencyAnalysisRun?> GetLatestRunAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT RunId, ProjectKey, Status, RequestedUtc, StartedUtc, CompletedUtc,
                   TotalFilesScanned, TotalNodesFound, TotalEdgesFound, FailureReason
            FROM DependencyAnalysisRuns
            WHERE ProjectKey = @ProjectKey
            ORDER BY RequestedUtc DESC
            LIMIT 1;
            """;
        command.Parameters.AddWithValue("@ProjectKey", projectKey);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDependencyRun(reader) : null;
    }

    public async Task<IReadOnlyList<DependencyNode>> ListNodesAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        var latestRun = await GetLatestRunAsync(projectKey, cancellationToken);
        if (latestRun is null) return [];

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT NodeId, ProjectKey, RunId, Name, FullName, Kind, Namespace, FilePath, StartLine, ParentNodeId
            FROM DependencyNodes
            WHERE RunId = @RunId;
            """;
        command.Parameters.AddWithValue("@RunId", latestRun.RunId);

        var results = new List<DependencyNode>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadDependencyNode(reader));
        return results;
    }

    public async Task<IReadOnlyList<DependencyEdge>> ListEdgesAsync(string projectKey, CancellationToken cancellationToken = default)
    {
        var latestRun = await GetLatestRunAsync(projectKey, cancellationToken);
        if (latestRun is null) return [];

        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT EdgeId, ProjectKey, RunId, SourceNodeId, TargetNodeId, RelationshipType
            FROM DependencyEdges
            WHERE RunId = @RunId;
            """;
        command.Parameters.AddWithValue("@RunId", latestRun.RunId);

        var results = new List<DependencyEdge>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadDependencyEdge(reader));
        return results;
    }

    public async Task ReplaceDependencyGraphAsync(
        DependencyAnalysisRun run,
        IReadOnlyList<DependencyNode> nodes,
        IReadOnlyList<DependencyEdge> edges,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        // Delete previous data for this project
        foreach (var table in new[] { "DependencyEdges", "DependencyNodes", "DependencyAnalysisRuns" })
        {
            await using var deleteCmd = connection.CreateCommand();
            deleteCmd.Transaction = (SqliteTransaction)transaction;
            deleteCmd.CommandText = $"DELETE FROM {table} WHERE ProjectKey = @ProjectKey;";
            deleteCmd.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            await deleteCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var insertRun = connection.CreateCommand())
        {
            insertRun.Transaction = (SqliteTransaction)transaction;
            insertRun.CommandText = """
                INSERT INTO DependencyAnalysisRuns (
                    RunId, ProjectKey, Status, RequestedUtc, StartedUtc, CompletedUtc,
                    TotalFilesScanned, TotalNodesFound, TotalEdgesFound, FailureReason
                ) VALUES (
                    @RunId, @ProjectKey, @Status, @RequestedUtc, @StartedUtc, @CompletedUtc,
                    @TotalFilesScanned, @TotalNodesFound, @TotalEdgesFound, @FailureReason
                );
                """;
            insertRun.Parameters.AddWithValue("@RunId", run.RunId);
            insertRun.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            insertRun.Parameters.AddWithValue("@Status", run.Status.ToString());
            insertRun.Parameters.AddWithValue("@RequestedUtc", run.RequestedUtc.ToString("O"));
            insertRun.Parameters.AddWithValue("@StartedUtc", ToDbValue(run.StartedUtc));
            insertRun.Parameters.AddWithValue("@CompletedUtc", ToDbValue(run.CompletedUtc));
            insertRun.Parameters.AddWithValue("@TotalFilesScanned", run.TotalFilesScanned);
            insertRun.Parameters.AddWithValue("@TotalNodesFound", run.TotalNodesFound);
            insertRun.Parameters.AddWithValue("@TotalEdgesFound", run.TotalEdgesFound);
            insertRun.Parameters.AddWithValue("@FailureReason", ToDbValue(run.FailureReason));
            await insertRun.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var node in nodes)
        {
            await using var insertNode = connection.CreateCommand();
            insertNode.Transaction = (SqliteTransaction)transaction;
            insertNode.CommandText = """
                INSERT INTO DependencyNodes (
                    NodeId, ProjectKey, RunId, Name, FullName, Kind, Namespace, FilePath, StartLine, ParentNodeId
                ) VALUES (
                    @NodeId, @ProjectKey, @RunId, @Name, @FullName, @Kind, @Namespace, @FilePath, @StartLine, @ParentNodeId
                );
                """;
            insertNode.Parameters.AddWithValue("@NodeId", node.NodeId);
            insertNode.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            insertNode.Parameters.AddWithValue("@RunId", run.RunId);
            insertNode.Parameters.AddWithValue("@Name", node.Name);
            insertNode.Parameters.AddWithValue("@FullName", node.FullName);
            insertNode.Parameters.AddWithValue("@Kind", node.Kind.ToString());
            insertNode.Parameters.AddWithValue("@Namespace", node.Namespace);
            insertNode.Parameters.AddWithValue("@FilePath", node.FilePath);
            insertNode.Parameters.AddWithValue("@StartLine", node.StartLine);
            insertNode.Parameters.AddWithValue("@ParentNodeId", ToDbValue(node.ParentNodeId));
            await insertNode.ExecuteNonQueryAsync(cancellationToken);
        }

        foreach (var edge in edges)
        {
            await using var insertEdge = connection.CreateCommand();
            insertEdge.Transaction = (SqliteTransaction)transaction;
            insertEdge.CommandText = """
                INSERT INTO DependencyEdges (
                    EdgeId, ProjectKey, RunId, SourceNodeId, TargetNodeId, RelationshipType
                ) VALUES (
                    @EdgeId, @ProjectKey, @RunId, @SourceNodeId, @TargetNodeId, @RelationshipType
                );
                """;
            insertEdge.Parameters.AddWithValue("@EdgeId", edge.EdgeId);
            insertEdge.Parameters.AddWithValue("@ProjectKey", run.ProjectKey);
            insertEdge.Parameters.AddWithValue("@RunId", run.RunId);
            insertEdge.Parameters.AddWithValue("@SourceNodeId", edge.SourceNodeId);
            insertEdge.Parameters.AddWithValue("@TargetNodeId", edge.TargetNodeId);
            insertEdge.Parameters.AddWithValue("@RelationshipType", edge.RelationshipType.ToString());
            await insertEdge.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private static DependencyAnalysisRun ReadDependencyRun(SqliteDataReader reader) => new()
    {
        RunId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        Status = Enum.Parse<DependencyAnalysisStatus>(reader.GetString(2)),
        RequestedUtc = DateTime.Parse(reader.GetString(3)),
        StartedUtc = ParseNullableDateTime(reader, 4),
        CompletedUtc = ParseNullableDateTime(reader, 5),
        TotalFilesScanned = reader.GetInt32(6),
        TotalNodesFound = reader.GetInt32(7),
        TotalEdgesFound = reader.GetInt32(8),
        FailureReason = ParseNullableString(reader, 9)
    };

    private static DependencyNode ReadDependencyNode(SqliteDataReader reader) => new()
    {
        NodeId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RunId = reader.GetString(2),
        Name = reader.GetString(3),
        FullName = reader.GetString(4),
        Kind = Enum.Parse<DependencyNodeKind>(reader.GetString(5)),
        Namespace = reader.GetString(6),
        FilePath = reader.GetString(7),
        StartLine = reader.GetInt32(8),
        ParentNodeId = ParseNullableString(reader, 9)
    };

    private static DependencyEdge ReadDependencyEdge(SqliteDataReader reader) => new()
    {
        EdgeId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RunId = reader.GetString(2),
        SourceNodeId = reader.GetString(3),
        TargetNodeId = reader.GetString(4),
        RelationshipType = Enum.Parse<DependencyRelationshipType>(reader.GetString(5))
    };

    private static ProjectWorkspace ReadWorkspace(SqliteDataReader reader) => new()
    {
        ProjectKey = reader.GetString(0),
        SourceRootPath = reader.GetString(1),
        Status = Enum.Parse<ProjectStatus>(reader.GetString(2)),
        TotalFiles = reader.GetInt32(3),
        TotalSegments = reader.GetInt32(4),
        LastIndexedUtc = ParseNullableDateTime(reader, 5),
        LastRunId = ParseNullableString(reader, 6),
        LastError = ParseNullableString(reader, 7)
    };

    private static IndexingRun ReadRun(SqliteDataReader reader) => new()
    {
        RunId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RunType = Enum.Parse<IndexingRunType>(reader.GetString(2)),
        Status = Enum.Parse<IndexingRunState>(reader.GetString(3)),
        RequestedUtc = DateTime.Parse(reader.GetString(4)),
        StartedUtc = ParseNullableDateTime(reader, 5),
        CompletedUtc = ParseNullableDateTime(reader, 6),
        RequestedFilePath = ParseNullableString(reader, 7),
        TotalFilesPlanned = reader.GetInt32(8),
        FilesScanned = reader.GetInt32(9),
        FilesIndexed = reader.GetInt32(10),
        FilesSkipped = reader.GetInt32(11),
        SegmentsWritten = reader.GetInt32(12),
        WarningCount = reader.GetInt32(13),
        CurrentFilePath = ParseNullableString(reader, 14),
        FailureReason = ParseNullableString(reader, 15)
    };

    private static QualityAnalysisRun ReadQualityRun(SqliteDataReader reader) => new()
    {
        RunId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RequestedModes = reader.GetString(2),
        Status = Enum.Parse<QualityAnalysisStatus>(reader.GetString(3)),
        RequestedUtc = DateTime.Parse(reader.GetString(4)),
        StartedUtc = ParseNullableDateTime(reader, 5),
        CompletedUtc = ParseNullableDateTime(reader, 6),
        TotalFilesScanned = reader.GetInt32(7),
        TotalLinesAnalyzed = reader.GetInt32(8),
        StructuralFindingCount = reader.GetInt32(9),
        SemanticFindingCount = reader.GetInt32(10),
        FailureReason = ParseNullableString(reader, 11)
    };

    private static QualitySummarySnapshot ReadSummary(SqliteDataReader reader) => new()
    {
        ProjectKey = reader.GetString(0),
        RunId = reader.GetString(1),
        QualityGrade = Enum.Parse<QualityGrade>(reader.GetString(2)),
        TotalLinesOfCode = reader.GetInt32(3),
        UniqueLineCount = reader.GetInt32(4),
        StructuralDuplicateLineCount = reader.GetInt32(5),
        SemanticDuplicateLineCount = reader.GetInt32(6),
        DuplicationPercent = reader.GetDouble(7),
        StructuralFindingCount = reader.GetInt32(8),
        SemanticFindingCount = reader.GetInt32(9),
        LastAnalyzedUtc = DateTime.Parse(reader.GetString(10))
    };

    private static DuplicationFinding ReadFinding(SqliteDataReader reader) => new()
    {
        FindingId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RunId = reader.GetString(2),
        Type = Enum.Parse<DuplicationType>(reader.GetString(3)),
        Severity = Enum.Parse<DuplicationSeverity>(reader.GetString(4)),
        SimilarityScore = reader.GetDouble(5),
        MatchingLineCount = reader.GetInt32(6),
        NormalizedFingerprint = ParseNullableString(reader, 7),
        LeftRegionId = reader.GetString(8),
        RightRegionId = reader.GetString(9),
        CreatedUtc = DateTime.Parse(reader.GetString(10))
    };

    private static CodeRegion ReadRegion(SqliteDataReader reader) => new()
    {
        RegionId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RelativeFilePath = reader.GetString(2),
        StartLine = reader.GetInt32(3),
        EndLine = reader.GetInt32(4),
        Snippet = reader.GetString(5),
        ContentHash = reader.GetString(6),
        SourceSegmentId = ParseNullableString(reader, 7),
        Availability = Enum.Parse<CodeRegionAvailability>(reader.GetString(8))
    };
    private static async Task EnsureColumnAsync(
        SqliteConnection connection,
        string tableName,
        string columnName,
        string definition,
        CancellationToken cancellationToken)
    {
        await using var infoCommand = connection.CreateCommand();
        infoCommand.CommandText = $"PRAGMA table_info({tableName});";

        var exists = false;
        await using (var reader = await infoCommand.ExecuteReaderAsync(cancellationToken))
        {
            while (await reader.ReadAsync(cancellationToken))
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.OrdinalIgnoreCase))
                {
                    exists = true;
                    break;
                }
            }
        }

        if (exists)
            return;

        await using var alterCommand = connection.CreateCommand();
        alterCommand.CommandText = $"ALTER TABLE {tableName} ADD COLUMN {columnName} {definition};";
        await alterCommand.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IndexedFile ReadFile(SqliteDataReader reader) => new()
    {
        ProjectKey = reader.GetString(0),
        RelativeFilePath = reader.GetString(1),
        AbsoluteFilePath = reader.GetString(2),
        FileName = reader.GetString(3),
        Extension = reader.GetString(4),
        Checksum = reader.GetString(5),
        SizeBytes = reader.GetInt64(6),
        LastModifiedUtc = DateTime.Parse(reader.GetString(7)),
        LastIndexedUtc = DateTime.Parse(reader.GetString(8)),
        SegmentCount = reader.GetInt32(9),
        Availability = Enum.Parse<ProjectFileAvailability>(reader.GetString(10))
    };

    private static SearchSegment ReadSegment(SqliteDataReader reader) => new()
    {
        SegmentId = reader.GetString(0),
        ProjectKey = reader.GetString(1),
        RelativeFilePath = reader.GetString(2),
        SegmentOrder = reader.GetInt32(3),
        StartLine = reader.GetInt32(4),
        EndLine = reader.GetInt32(5),
        Content = reader.GetString(6),
        SnippetPreview = reader.GetString(7),
        ContentHash = reader.GetString(8),
        EmbeddingVector = BytesToEmbedding((byte[])reader["EmbeddingVector"]),
        TokenCount = reader.GetInt32(10),
        CreatedUtc = DateTime.Parse(reader.GetString(11))
    };

    private static object ToDbValue(string? value) => value is null ? DBNull.Value : value;

    private static object ToDbValue(DateTime? value) => value is null ? DBNull.Value : value.Value.ToString("O");

    private static string? ParseNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? ParseNullableDateTime(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : DateTime.Parse(reader.GetString(ordinal));

    private static byte[] EmbeddingToBytes(float[] embedding)
        => MemoryMarshal.AsBytes(embedding.AsSpan()).ToArray();

    private static float[] BytesToEmbedding(byte[] bytes)
    {
        var floats = new float[bytes.Length / sizeof(float)];
        MemoryMarshal.Cast<byte, float>(bytes.AsSpan()).CopyTo(floats.AsSpan());
        return floats;
    }

    private static async Task DeleteFileContentAsync(
        SqliteConnection connection,
        SqliteTransaction transaction,
        string projectKey,
        string relativeFilePath,
        CancellationToken cancellationToken)
    {
        await using var deleteSegments = connection.CreateCommand();
        deleteSegments.Transaction = transaction;
        deleteSegments.CommandText = """
            DELETE FROM SearchSegments
            WHERE ProjectKey = @ProjectKey AND RelativeFilePath = @RelativeFilePath;
            """;
        deleteSegments.Parameters.AddWithValue("@ProjectKey", projectKey);
        deleteSegments.Parameters.AddWithValue("@RelativeFilePath", relativeFilePath);
        await deleteSegments.ExecuteNonQueryAsync(cancellationToken);

        await using var deleteFile = connection.CreateCommand();
        deleteFile.Transaction = transaction;
        deleteFile.CommandText = """
            DELETE FROM IndexedFiles
            WHERE ProjectKey = @ProjectKey AND RelativeFilePath = @RelativeFilePath;
            """;
        deleteFile.Parameters.AddWithValue("@ProjectKey", projectKey);
        deleteFile.Parameters.AddWithValue("@RelativeFilePath", relativeFilePath);
        await deleteFile.ExecuteNonQueryAsync(cancellationToken);
    }
}




