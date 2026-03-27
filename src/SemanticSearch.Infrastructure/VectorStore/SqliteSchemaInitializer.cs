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

        CREATE TABLE IF NOT EXISTS DependencyAnalysisRuns (
            RunId               TEXT PRIMARY KEY,
            ProjectKey          TEXT NOT NULL,
            Status              TEXT NOT NULL DEFAULT 'Queued',
            RequestedUtc        TEXT NOT NULL,
            StartedUtc          TEXT NULL,
            CompletedUtc        TEXT NULL,
            TotalFilesScanned   INTEGER NOT NULL DEFAULT 0,
            TotalNodesFound     INTEGER NOT NULL DEFAULT 0,
            TotalEdgesFound     INTEGER NOT NULL DEFAULT 0,
            FailureReason       TEXT NULL,
            FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey)
        );
        CREATE INDEX IF NOT EXISTS IX_DependencyAnalysisRuns_ProjectKey ON DependencyAnalysisRuns(ProjectKey);

        CREATE TABLE IF NOT EXISTS DependencyNodes (
            NodeId          TEXT PRIMARY KEY,
            ProjectKey      TEXT NOT NULL,
            RunId           TEXT NOT NULL,
            Name            TEXT NOT NULL,
            FullName        TEXT NOT NULL,
            Kind            TEXT NOT NULL,
            Namespace       TEXT NOT NULL,
            FilePath        TEXT NOT NULL,
            StartLine       INTEGER NOT NULL,
            ParentNodeId    TEXT NULL,
            FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey),
            FOREIGN KEY (RunId) REFERENCES DependencyAnalysisRuns(RunId)
        );
        CREATE INDEX IF NOT EXISTS IX_DependencyNodes_ProjectKey ON DependencyNodes(ProjectKey);
        CREATE INDEX IF NOT EXISTS IX_DependencyNodes_RunId ON DependencyNodes(RunId);

        CREATE TABLE IF NOT EXISTS DependencyEdges (
            EdgeId              TEXT PRIMARY KEY,
            ProjectKey          TEXT NOT NULL,
            RunId               TEXT NOT NULL,
            SourceNodeId        TEXT NOT NULL,
            TargetNodeId        TEXT NOT NULL,
            RelationshipType    TEXT NOT NULL,
            FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey),
            FOREIGN KEY (RunId) REFERENCES DependencyAnalysisRuns(RunId)
        );
        CREATE INDEX IF NOT EXISTS IX_DependencyEdges_ProjectKey ON DependencyEdges(ProjectKey);
        CREATE INDEX IF NOT EXISTS IX_DependencyEdges_RunId ON DependencyEdges(RunId);

        CREATE TABLE IF NOT EXISTS TfsCredentials (
            CredentialId TEXT PRIMARY KEY,
            ServerUrl TEXT NOT NULL,
            EncryptedPat TEXT NOT NULL,
            Username TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS SlackCredentials (
            CredentialId TEXT PRIMARY KEY,
            EncryptedBotToken TEXT NOT NULL,
            EncryptedUserToken TEXT NULL,
            DefaultChannel TEXT NOT NULL,
            CreatedUtc TEXT NOT NULL,
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS IntegrationSettings (
            SettingsId TEXT PRIMARY KEY,
            StandupMessage TEXT NOT NULL DEFAULT '',
            StandupEnabled INTEGER NOT NULL DEFAULT 0,
            PrayerCity TEXT NOT NULL DEFAULT '',
            PrayerCountry TEXT NOT NULL DEFAULT '',
            PrayerMethod INTEGER NOT NULL DEFAULT 4,
            PrayerEnabled INTEGER NOT NULL DEFAULT 0,
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StudyReminderSettings (
            SettingsId TEXT PRIMARY KEY,
            Enabled INTEGER NOT NULL DEFAULT 0,
            ReminderTime TEXT NOT NULL DEFAULT '08:00',
            UpdatedUtc TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StudyBooks (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            Author TEXT NULL,
            Description TEXT NULL,
            FileName TEXT NOT NULL,
            FilePath TEXT NOT NULL,
            PageCount INTEGER NOT NULL,
            LastReadPage INTEGER NOT NULL DEFAULT 1,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );

        CREATE TABLE IF NOT EXISTS StudyChapters (
            Id TEXT PRIMARY KEY,
            BookId TEXT NOT NULL REFERENCES StudyBooks(Id) ON DELETE CASCADE,
            Title TEXT NOT NULL,
            StartPage INTEGER NOT NULL,
            EndPage INTEGER NOT NULL,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            AudioFileName TEXT NULL,
            AudioFilePath TEXT NULL,
            Notes TEXT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StudyChapters_BookId ON StudyChapters(BookId);

        CREATE TABLE IF NOT EXISTS StudyPlans (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            BookId TEXT NULL REFERENCES StudyBooks(Id) ON DELETE SET NULL,
            StartDate TEXT NOT NULL,
            EndDate TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Draft',
            SkipWeekends INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StudyPlans_BookId ON StudyPlans(BookId);

        CREATE TABLE IF NOT EXISTS StudyPlanItems (
            Id TEXT PRIMARY KEY,
            PlanId TEXT NOT NULL REFERENCES StudyPlans(Id) ON DELETE CASCADE,
            ChapterId TEXT NULL REFERENCES StudyChapters(Id) ON DELETE SET NULL,
            Title TEXT NOT NULL,
            ScheduledDate TEXT NOT NULL,
            Status TEXT NOT NULL DEFAULT 'Pending',
            CompletedDate TEXT NULL,
            SortOrder INTEGER NOT NULL DEFAULT 0,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StudyPlanItems_PlanId ON StudyPlanItems(PlanId);
        CREATE INDEX IF NOT EXISTS IX_StudyPlanItems_ScheduledDate ON StudyPlanItems(ScheduledDate);

        CREATE TABLE IF NOT EXISTS FlashCardDecks (
            Id TEXT PRIMARY KEY,
            Title TEXT NOT NULL,
            BookId TEXT NULL REFERENCES StudyBooks(Id) ON DELETE SET NULL,
            CreatedAt TEXT NOT NULL,
            UpdatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_FlashCardDecks_BookId ON FlashCardDecks(BookId);

        CREATE TABLE IF NOT EXISTS FlashCards (
            Id TEXT PRIMARY KEY,
            DeckId TEXT NOT NULL REFERENCES FlashCardDecks(Id) ON DELETE CASCADE,
            ChapterId TEXT NULL REFERENCES StudyChapters(Id) ON DELETE SET NULL,
            Front TEXT NOT NULL,
            Back TEXT NOT NULL,
            Interval INTEGER NOT NULL DEFAULT 0,
            Repetitions INTEGER NOT NULL DEFAULT 0,
            EaseFactor REAL NOT NULL DEFAULT 2.5,
            NextReviewDate TEXT NOT NULL,
            LastReviewDate TEXT NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_FlashCards_DeckId ON FlashCards(DeckId);
        CREATE INDEX IF NOT EXISTS IX_FlashCards_NextReviewDate ON FlashCards(NextReviewDate);

        CREATE TABLE IF NOT EXISTS CardReviews (
            Id TEXT PRIMARY KEY,
            CardId TEXT NOT NULL REFERENCES FlashCards(Id) ON DELETE CASCADE,
            Quality INTEGER NOT NULL,
            ReviewedAt TEXT NOT NULL,
            PreviousInterval INTEGER NOT NULL,
            NewInterval INTEGER NOT NULL,
            PreviousEaseFactor REAL NOT NULL,
            NewEaseFactor REAL NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_CardReviews_CardId ON CardReviews(CardId);
        CREATE INDEX IF NOT EXISTS IX_CardReviews_ReviewedAt ON CardReviews(ReviewedAt);

        CREATE TABLE IF NOT EXISTS StudySessions (
            Id TEXT PRIMARY KEY,
            BookId TEXT NULL REFERENCES StudyBooks(Id) ON DELETE SET NULL,
            ChapterId TEXT NULL REFERENCES StudyChapters(Id) ON DELETE SET NULL,
            SessionType TEXT NOT NULL,
            StartedAt TEXT NOT NULL,
            EndedAt TEXT NULL,
            DurationMinutes INTEGER NULL,
            IsPomodoroSession INTEGER NOT NULL DEFAULT 0,
            FocusDurationMinutes INTEGER NULL,
            CreatedAt TEXT NOT NULL
        );
        CREATE INDEX IF NOT EXISTS IX_StudySessions_StartedAt ON StudySessions(StartedAt);
        CREATE INDEX IF NOT EXISTS IX_StudySessions_BookId ON StudySessions(BookId);
        """;
}
