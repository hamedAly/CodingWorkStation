namespace SemanticSearch.Infrastructure.VectorStore;

public static class SqliteSchemaInitializer
{
    public const string Schema = """
        CREATE TABLE IF NOT EXISTS ProjectWorkspaces (
            ProjectKey TEXT PRIMARY KEY,
            SourceRootPath TEXT NOT NULL,
            Status TEXT NOT NULL,
            TotalFiles INTEGER NOT NULL DEFAULT 0,
            TotalSegments INTEGER NOT NULL DEFAULT 0,
            LastIndexedUtc TEXT NULL,
            LastRunId TEXT NULL,
            LastError TEXT NULL
        );

        CREATE TABLE IF NOT EXISTS IndexingRuns (
            RunId TEXT PRIMARY KEY,
            ProjectKey TEXT NOT NULL,
            RunType TEXT NOT NULL,
            Status TEXT NOT NULL,
            RequestedUtc TEXT NOT NULL,
            StartedUtc TEXT NULL,
            CompletedUtc TEXT NULL,
            RequestedFilePath TEXT NULL,
            TotalFilesPlanned INTEGER NOT NULL DEFAULT 0,
            FilesScanned INTEGER NOT NULL DEFAULT 0,
            FilesIndexed INTEGER NOT NULL DEFAULT 0,
            FilesSkipped INTEGER NOT NULL DEFAULT 0,
            SegmentsWritten INTEGER NOT NULL DEFAULT 0,
            WarningCount INTEGER NOT NULL DEFAULT 0,
            CurrentFilePath TEXT NULL,
            FailureReason TEXT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_IndexingRuns_ProjectKey ON IndexingRuns(ProjectKey);
        CREATE INDEX IF NOT EXISTS IX_IndexingRuns_ProjectKey_Status ON IndexingRuns(ProjectKey, Status);

        CREATE TABLE IF NOT EXISTS IndexedFiles (
            ProjectKey TEXT NOT NULL,
            RelativeFilePath TEXT NOT NULL,
            AbsoluteFilePath TEXT NOT NULL,
            FileName TEXT NOT NULL,
            Extension TEXT NOT NULL,
            Checksum TEXT NOT NULL,
            SizeBytes INTEGER NOT NULL,
            LastModifiedUtc TEXT NOT NULL,
            LastIndexedUtc TEXT NOT NULL,
            SegmentCount INTEGER NOT NULL DEFAULT 0,
            Availability TEXT NOT NULL,
            PRIMARY KEY (ProjectKey, RelativeFilePath)
        );
        CREATE INDEX IF NOT EXISTS IX_IndexedFiles_ProjectKey ON IndexedFiles(ProjectKey);

        CREATE TABLE IF NOT EXISTS SearchSegments (
            SegmentId TEXT PRIMARY KEY,
            ProjectKey TEXT NOT NULL,
            RelativeFilePath TEXT NOT NULL,
            SegmentOrder INTEGER NOT NULL,
            StartLine INTEGER NOT NULL,
            EndLine INTEGER NOT NULL,
            Content TEXT NOT NULL,
            SnippetPreview TEXT NOT NULL,
            ContentHash TEXT NOT NULL,
            EmbeddingVector BLOB NOT NULL,
            TokenCount INTEGER NOT NULL,
            CreatedUtc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_SearchSegments_ProjectKey ON SearchSegments(ProjectKey);
        CREATE INDEX IF NOT EXISTS IX_SearchSegments_ProjectKey_FilePath ON SearchSegments(ProjectKey, RelativeFilePath);

        CREATE TABLE IF NOT EXISTS QualityAnalysisRuns (
            RunId TEXT PRIMARY KEY,
            ProjectKey TEXT NOT NULL,
            RequestedModes TEXT NOT NULL,
            Status TEXT NOT NULL,
            RequestedUtc TEXT NOT NULL,
            StartedUtc TEXT NULL,
            CompletedUtc TEXT NULL,
            TotalFilesScanned INTEGER NOT NULL DEFAULT 0,
            TotalLinesAnalyzed INTEGER NOT NULL DEFAULT 0,
            StructuralFindingCount INTEGER NOT NULL DEFAULT 0,
            SemanticFindingCount INTEGER NOT NULL DEFAULT 0,
            FailureReason TEXT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_QualityAnalysisRuns_ProjectKey ON QualityAnalysisRuns(ProjectKey);

        CREATE TABLE IF NOT EXISTS QualitySummarySnapshots (
            ProjectKey TEXT PRIMARY KEY,
            RunId TEXT NOT NULL,
            QualityGrade TEXT NOT NULL,
            TotalLinesOfCode INTEGER NOT NULL DEFAULT 0,
            UniqueLineCount INTEGER NOT NULL DEFAULT 0,
            StructuralDuplicateLineCount INTEGER NOT NULL DEFAULT 0,
            SemanticDuplicateLineCount INTEGER NOT NULL DEFAULT 0,
            DuplicationPercent REAL NOT NULL DEFAULT 0,
            StructuralFindingCount INTEGER NOT NULL DEFAULT 0,
            SemanticFindingCount INTEGER NOT NULL DEFAULT 0,
            LastAnalyzedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS CodeRegions (
            RegionId TEXT PRIMARY KEY,
            ProjectKey TEXT NOT NULL,
            RelativeFilePath TEXT NOT NULL,
            StartLine INTEGER NOT NULL,
            EndLine INTEGER NOT NULL,
            Snippet TEXT NOT NULL,
            ContentHash TEXT NOT NULL,
            SourceSegmentId TEXT NULL,
            Availability TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_CodeRegions_ProjectKey ON CodeRegions(ProjectKey);

        CREATE TABLE IF NOT EXISTS DuplicationFindings (
            FindingId TEXT PRIMARY KEY,
            ProjectKey TEXT NOT NULL,
            RunId TEXT NOT NULL,
            Type TEXT NOT NULL,
            Severity TEXT NOT NULL,
            SimilarityScore REAL NOT NULL,
            MatchingLineCount INTEGER NOT NULL,
            NormalizedFingerprint TEXT NULL,
            LeftRegionId TEXT NOT NULL,
            RightRegionId TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_DuplicationFindings_ProjectKey ON DuplicationFindings(ProjectKey);
        CREATE INDEX IF NOT EXISTS IX_DuplicationFindings_ProjectKey_RunId ON DuplicationFindings(ProjectKey, RunId);
        """;
}
