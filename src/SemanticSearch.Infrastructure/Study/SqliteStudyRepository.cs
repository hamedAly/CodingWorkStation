using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Study;

public sealed class SqliteStudyRepository : IStudyRepository
{
    private readonly string _connectionString;

    public SqliteStudyRepository(string databasePath)
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

    public async Task<IReadOnlyList<StudyBook>> GetAllBooksAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Author, Description, FileName, FilePath, PageCount, LastReadPage, CreatedAt, UpdatedAt
            FROM StudyBooks
            ORDER BY UpdatedAt DESC, Title ASC;
            """;

        var results = new List<StudyBook>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadBook(reader));

        return results;
    }

    public async Task<StudyBook?> GetBookByIdAsync(string bookId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, Author, Description, FileName, FilePath, PageCount, LastReadPage, CreatedAt, UpdatedAt
            FROM StudyBooks
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", bookId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadBook(reader) : null;
    }

    public async Task InsertBookAsync(StudyBook book, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO StudyBooks (Id, Title, Author, Description, FileName, FilePath, PageCount, LastReadPage, CreatedAt, UpdatedAt)
            VALUES (@Id, @Title, @Author, @Description, @FileName, @FilePath, @PageCount, @LastReadPage, @CreatedAt, @UpdatedAt);
            """;
        command.Parameters.AddWithValue("@Id", book.Id);
        command.Parameters.AddWithValue("@Title", book.Title);
        command.Parameters.AddWithValue("@Author", ToDbValue(book.Author));
        command.Parameters.AddWithValue("@Description", ToDbValue(book.Description));
        command.Parameters.AddWithValue("@FileName", book.FileName);
        command.Parameters.AddWithValue("@FilePath", book.FilePath);
        command.Parameters.AddWithValue("@PageCount", book.PageCount);
        command.Parameters.AddWithValue("@LastReadPage", book.LastReadPage);
        command.Parameters.AddWithValue("@CreatedAt", book.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", book.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateBookAsync(StudyBook book, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudyBooks
            SET Title = @Title,
                Author = @Author,
                Description = @Description,
                FileName = @FileName,
                FilePath = @FilePath,
                PageCount = @PageCount,
                LastReadPage = @LastReadPage,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", book.Id);
        command.Parameters.AddWithValue("@Title", book.Title);
        command.Parameters.AddWithValue("@Author", ToDbValue(book.Author));
        command.Parameters.AddWithValue("@Description", ToDbValue(book.Description));
        command.Parameters.AddWithValue("@FileName", book.FileName);
        command.Parameters.AddWithValue("@FilePath", book.FilePath);
        command.Parameters.AddWithValue("@PageCount", book.PageCount);
        command.Parameters.AddWithValue("@LastReadPage", book.LastReadPage);
        command.Parameters.AddWithValue("@UpdatedAt", book.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM StudyBooks WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", bookId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateLastReadPageAsync(string bookId, int page, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudyBooks
            SET LastReadPage = @LastReadPage,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", bookId);
        command.Parameters.AddWithValue("@LastReadPage", page);
        command.Parameters.AddWithValue("@UpdatedAt", DateTime.UtcNow.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyChapter>> GetChaptersByBookIdAsync(string bookId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BookId, Title, StartPage, EndPage, SortOrder, AudioFileName, AudioFilePath, Notes, CreatedAt
            FROM StudyChapters
            WHERE BookId = @BookId
            ORDER BY SortOrder ASC, StartPage ASC;
            """;
        command.Parameters.AddWithValue("@BookId", bookId);

        var results = new List<StudyChapter>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadChapter(reader));

        return results;
    }

    public async Task<StudyChapter?> GetChapterByIdAsync(string chapterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BookId, Title, StartPage, EndPage, SortOrder, AudioFileName, AudioFilePath, Notes, CreatedAt
            FROM StudyChapters
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", chapterId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadChapter(reader) : null;
    }

    public async Task InsertChapterAsync(StudyChapter chapter, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO StudyChapters (Id, BookId, Title, StartPage, EndPage, SortOrder, AudioFileName, AudioFilePath, Notes, CreatedAt)
            VALUES (@Id, @BookId, @Title, @StartPage, @EndPage, @SortOrder, @AudioFileName, @AudioFilePath, @Notes, @CreatedAt);
            """;
        command.Parameters.AddWithValue("@Id", chapter.Id);
        command.Parameters.AddWithValue("@BookId", chapter.BookId);
        command.Parameters.AddWithValue("@Title", chapter.Title);
        command.Parameters.AddWithValue("@StartPage", chapter.StartPage);
        command.Parameters.AddWithValue("@EndPage", chapter.EndPage);
        command.Parameters.AddWithValue("@SortOrder", chapter.SortOrder);
        command.Parameters.AddWithValue("@AudioFileName", ToDbValue(chapter.AudioFileName));
        command.Parameters.AddWithValue("@AudioFilePath", ToDbValue(chapter.AudioFilePath));
        command.Parameters.AddWithValue("@Notes", ToDbValue(chapter.Notes));
        command.Parameters.AddWithValue("@CreatedAt", chapter.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateChapterAsync(StudyChapter chapter, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudyChapters
            SET Title = @Title,
                StartPage = @StartPage,
                EndPage = @EndPage,
                SortOrder = @SortOrder,
                AudioFileName = @AudioFileName,
                AudioFilePath = @AudioFilePath,
                Notes = @Notes
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", chapter.Id);
        command.Parameters.AddWithValue("@Title", chapter.Title);
        command.Parameters.AddWithValue("@StartPage", chapter.StartPage);
        command.Parameters.AddWithValue("@EndPage", chapter.EndPage);
        command.Parameters.AddWithValue("@SortOrder", chapter.SortOrder);
        command.Parameters.AddWithValue("@AudioFileName", ToDbValue(chapter.AudioFileName));
        command.Parameters.AddWithValue("@AudioFilePath", ToDbValue(chapter.AudioFilePath));
        command.Parameters.AddWithValue("@Notes", ToDbValue(chapter.Notes));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteChapterAsync(string chapterId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM StudyChapters WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", chapterId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyPlan>> GetAllPlansAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, BookId, StartDate, EndDate, Status, SkipWeekends, CreatedAt, UpdatedAt
            FROM StudyPlans
            ORDER BY StartDate DESC, Title ASC;
            """;

        var results = new List<StudyPlan>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadPlan(reader));

        return results;
    }

    public async Task<StudyPlan?> GetPlanByIdAsync(string planId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, BookId, StartDate, EndDate, Status, SkipWeekends, CreatedAt, UpdatedAt
            FROM StudyPlans
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", planId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadPlan(reader) : null;
    }

    public async Task InsertPlanAsync(StudyPlan plan, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO StudyPlans (Id, Title, BookId, StartDate, EndDate, Status, SkipWeekends, CreatedAt, UpdatedAt)
            VALUES (@Id, @Title, @BookId, @StartDate, @EndDate, @Status, @SkipWeekends, @CreatedAt, @UpdatedAt);
            """;
        command.Parameters.AddWithValue("@Id", plan.Id);
        command.Parameters.AddWithValue("@Title", plan.Title);
        command.Parameters.AddWithValue("@BookId", ToDbValue(plan.BookId));
        command.Parameters.AddWithValue("@StartDate", plan.StartDate.ToString("O"));
        command.Parameters.AddWithValue("@EndDate", plan.EndDate.ToString("O"));
        command.Parameters.AddWithValue("@Status", plan.Status);
        command.Parameters.AddWithValue("@SkipWeekends", plan.SkipWeekends ? 1 : 0);
        command.Parameters.AddWithValue("@CreatedAt", plan.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", plan.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdatePlanAsync(StudyPlan plan, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudyPlans
            SET Title = @Title,
                BookId = @BookId,
                StartDate = @StartDate,
                EndDate = @EndDate,
                Status = @Status,
                SkipWeekends = @SkipWeekends,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", plan.Id);
        command.Parameters.AddWithValue("@Title", plan.Title);
        command.Parameters.AddWithValue("@BookId", ToDbValue(plan.BookId));
        command.Parameters.AddWithValue("@StartDate", plan.StartDate.ToString("O"));
        command.Parameters.AddWithValue("@EndDate", plan.EndDate.ToString("O"));
        command.Parameters.AddWithValue("@Status", plan.Status);
        command.Parameters.AddWithValue("@SkipWeekends", plan.SkipWeekends ? 1 : 0);
        command.Parameters.AddWithValue("@UpdatedAt", plan.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeletePlanAsync(string planId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM StudyPlans WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", planId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByPlanIdAsync(string planId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, PlanId, ChapterId, Title, ScheduledDate, Status, CompletedDate, SortOrder, CreatedAt
            FROM StudyPlanItems
            WHERE PlanId = @PlanId
            ORDER BY ScheduledDate ASC, SortOrder ASC;
            """;
        command.Parameters.AddWithValue("@PlanId", planId);

        var results = new List<StudyPlanItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadPlanItem(reader));

        return results;
    }

    public async Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByDateAsync(DateTime date, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, PlanId, ChapterId, Title, ScheduledDate, Status, CompletedDate, SortOrder, CreatedAt
            FROM StudyPlanItems
            WHERE date(ScheduledDate) = date(@ScheduledDate)
            ORDER BY SortOrder ASC, Title ASC;
            """;
        command.Parameters.AddWithValue("@ScheduledDate", date.ToString("O"));

        var results = new List<StudyPlanItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadPlanItem(reader));

        return results;
    }

    public async Task InsertPlanItemsAsync(IReadOnlyList<StudyPlanItem> items, CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
        var transaction = (SqliteTransaction)dbTransaction;

        foreach (var item in items)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO StudyPlanItems (Id, PlanId, ChapterId, Title, ScheduledDate, Status, CompletedDate, SortOrder, CreatedAt)
                VALUES (@Id, @PlanId, @ChapterId, @Title, @ScheduledDate, @Status, @CompletedDate, @SortOrder, @CreatedAt);
                """;
            command.Parameters.AddWithValue("@Id", item.Id);
            command.Parameters.AddWithValue("@PlanId", item.PlanId);
            command.Parameters.AddWithValue("@ChapterId", ToDbValue(item.ChapterId));
            command.Parameters.AddWithValue("@Title", item.Title);
            command.Parameters.AddWithValue("@ScheduledDate", item.ScheduledDate.ToString("O"));
            command.Parameters.AddWithValue("@Status", item.Status);
            command.Parameters.AddWithValue("@CompletedDate", ToDbValue(item.CompletedDate));
            command.Parameters.AddWithValue("@SortOrder", item.SortOrder);
            command.Parameters.AddWithValue("@CreatedAt", item.CreatedAt.ToString("O"));
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdatePlanItemStatusAsync(string itemId, string status, DateTime? completedDate, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudyPlanItems
            SET Status = @Status,
                CompletedDate = @CompletedDate
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", itemId);
        command.Parameters.AddWithValue("@Status", status);
        command.Parameters.AddWithValue("@CompletedDate", ToDbValue(completedDate));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StudyPlanItem>> GetCalendarItemsAsync(int year, int month, CancellationToken cancellationToken = default)
    {
        var start = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
        var end = start.AddMonths(1);

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, PlanId, ChapterId, Title, ScheduledDate, Status, CompletedDate, SortOrder, CreatedAt
            FROM StudyPlanItems
            WHERE ScheduledDate >= @StartDate AND ScheduledDate < @EndDate
            ORDER BY ScheduledDate ASC, SortOrder ASC;
            """;
        command.Parameters.AddWithValue("@StartDate", start.ToString("O"));
        command.Parameters.AddWithValue("@EndDate", end.ToString("O"));

        var results = new List<StudyPlanItem>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadPlanItem(reader));

        return results;
    }

    public async Task InsertSessionAsync(StudySession session, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO StudySessions (Id, BookId, ChapterId, SessionType, StartedAt, EndedAt, DurationMinutes, IsPomodoroSession, FocusDurationMinutes, CreatedAt)
            VALUES (@Id, @BookId, @ChapterId, @SessionType, @StartedAt, @EndedAt, @DurationMinutes, @IsPomodoroSession, @FocusDurationMinutes, @CreatedAt);
            """;
        command.Parameters.AddWithValue("@Id", session.Id);
        command.Parameters.AddWithValue("@BookId", ToDbValue(session.BookId));
        command.Parameters.AddWithValue("@ChapterId", ToDbValue(session.ChapterId));
        command.Parameters.AddWithValue("@SessionType", session.SessionType);
        command.Parameters.AddWithValue("@StartedAt", session.StartedAt.ToString("O"));
        command.Parameters.AddWithValue("@EndedAt", ToDbValue(session.EndedAt));
        command.Parameters.AddWithValue("@DurationMinutes", session.DurationMinutes.HasValue ? session.DurationMinutes.Value : DBNull.Value);
        command.Parameters.AddWithValue("@IsPomodoroSession", session.IsPomodoroSession ? 1 : 0);
        command.Parameters.AddWithValue("@FocusDurationMinutes", session.FocusDurationMinutes.HasValue ? session.FocusDurationMinutes.Value : DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", session.CreatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateSessionEndAsync(string sessionId, DateTime endedAt, int durationMinutes, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE StudySessions
            SET EndedAt = @EndedAt,
                DurationMinutes = @DurationMinutes
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", sessionId);
        command.Parameters.AddWithValue("@EndedAt", endedAt.ToString("O"));
        command.Parameters.AddWithValue("@DurationMinutes", durationMinutes);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<StudySession?> GetSessionByIdAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, BookId, ChapterId, SessionType, StartedAt, EndedAt, DurationMinutes, IsPomodoroSession, FocusDurationMinutes, CreatedAt
            FROM StudySessions
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadSession(reader) : null;
    }

    public async Task<int> GetStudyStreakDaysAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT DISTINCT date(StartedAt)
            FROM StudySessions
            ORDER BY date(StartedAt) DESC;
            """;

        var dates = new List<DateOnly>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            dates.Add(DateOnly.Parse(reader.GetString(0)));

        var streak = 0;
        var current = DateOnly.FromDateTime(DateTime.UtcNow.Date);
        foreach (var date in dates)
        {
            if (date != current.AddDays(-streak))
                break;

            streak++;
        }

        return streak;
    }

    public async Task<IReadOnlyList<(DateTime Date, double Hours)>> GetWeeklyStudyHoursAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT date(StartedAt) AS StudyDate, COALESCE(SUM(DurationMinutes), 0) / 60.0
            FROM StudySessions
            WHERE StartedAt >= @StartDate
            GROUP BY date(StartedAt)
            ORDER BY StudyDate ASC;
            """;
        command.Parameters.AddWithValue("@StartDate", DateTime.UtcNow.Date.AddDays(-6).ToString("O"));

        var results = new List<(DateTime Date, double Hours)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add((DateTime.Parse(reader.GetString(0)), reader.GetDouble(1)));

        return results;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static StudyBook ReadBook(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Title = reader.GetString(1),
        Author = ParseNullableString(reader, 2),
        Description = ParseNullableString(reader, 3),
        FileName = reader.GetString(4),
        FilePath = reader.GetString(5),
        PageCount = reader.GetInt32(6),
        LastReadPage = reader.GetInt32(7),
        CreatedAt = DateTime.Parse(reader.GetString(8)),
        UpdatedAt = DateTime.Parse(reader.GetString(9))
    };

    private static StudyChapter ReadChapter(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        BookId = reader.GetString(1),
        Title = reader.GetString(2),
        StartPage = reader.GetInt32(3),
        EndPage = reader.GetInt32(4),
        SortOrder = reader.GetInt32(5),
        AudioFileName = ParseNullableString(reader, 6),
        AudioFilePath = ParseNullableString(reader, 7),
        Notes = ParseNullableString(reader, 8),
        CreatedAt = DateTime.Parse(reader.GetString(9))
    };

    private static StudyPlan ReadPlan(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Title = reader.GetString(1),
        BookId = ParseNullableString(reader, 2),
        StartDate = DateTime.Parse(reader.GetString(3)),
        EndDate = DateTime.Parse(reader.GetString(4)),
        Status = reader.GetString(5),
        SkipWeekends = reader.GetInt32(6) == 1,
        CreatedAt = DateTime.Parse(reader.GetString(7)),
        UpdatedAt = DateTime.Parse(reader.GetString(8))
    };

    private static StudyPlanItem ReadPlanItem(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        PlanId = reader.GetString(1),
        ChapterId = ParseNullableString(reader, 2),
        Title = reader.GetString(3),
        ScheduledDate = DateTime.Parse(reader.GetString(4)),
        Status = reader.GetString(5),
        CompletedDate = ParseNullableDateTime(reader, 6),
        SortOrder = reader.GetInt32(7),
        CreatedAt = DateTime.Parse(reader.GetString(8))
    };

    private static StudySession ReadSession(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        BookId = ParseNullableString(reader, 1),
        ChapterId = ParseNullableString(reader, 2),
        SessionType = reader.GetString(3),
        StartedAt = DateTime.Parse(reader.GetString(4)),
        EndedAt = ParseNullableDateTime(reader, 5),
        DurationMinutes = reader.IsDBNull(6) ? null : reader.GetInt32(6),
        IsPomodoroSession = reader.GetInt32(7) == 1,
        FocusDurationMinutes = reader.IsDBNull(8) ? null : reader.GetInt32(8),
        CreatedAt = DateTime.Parse(reader.GetString(9))
    };

    private static object ToDbValue(string? value) => value is null ? DBNull.Value : value;

    private static object ToDbValue(DateTime? value) => value is null ? DBNull.Value : value.Value.ToString("O");

    private static string? ParseNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? ParseNullableDateTime(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : DateTime.Parse(reader.GetString(ordinal));
}
