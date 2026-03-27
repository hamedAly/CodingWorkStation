using Microsoft.Data.Sqlite;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Study;

public sealed class SqliteFlashCardRepository : IFlashCardRepository
{
    private readonly string _connectionString;

    public SqliteFlashCardRepository(string databasePath)
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

    public async Task<IReadOnlyList<FlashCardDeck>> GetAllDecksAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, BookId, CreatedAt, UpdatedAt
            FROM FlashCardDecks
            ORDER BY UpdatedAt DESC, Title ASC;
            """;

        var results = new List<FlashCardDeck>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadDeck(reader));

        return results;
    }

    public async Task<FlashCardDeck?> GetDeckByIdAsync(string deckId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, Title, BookId, CreatedAt, UpdatedAt
            FROM FlashCardDecks
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", deckId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadDeck(reader) : null;
    }

    public async Task InsertDeckAsync(FlashCardDeck deck, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO FlashCardDecks (Id, Title, BookId, CreatedAt, UpdatedAt)
            VALUES (@Id, @Title, @BookId, @CreatedAt, @UpdatedAt);
            """;
        command.Parameters.AddWithValue("@Id", deck.Id);
        command.Parameters.AddWithValue("@Title", deck.Title);
        command.Parameters.AddWithValue("@BookId", ToDbValue(deck.BookId));
        command.Parameters.AddWithValue("@CreatedAt", deck.CreatedAt.ToString("O"));
        command.Parameters.AddWithValue("@UpdatedAt", deck.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task UpdateDeckAsync(FlashCardDeck deck, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE FlashCardDecks
            SET Title = @Title,
                BookId = @BookId,
                UpdatedAt = @UpdatedAt
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", deck.Id);
        command.Parameters.AddWithValue("@Title", deck.Title);
        command.Parameters.AddWithValue("@BookId", ToDbValue(deck.BookId));
        command.Parameters.AddWithValue("@UpdatedAt", deck.UpdatedAt.ToString("O"));
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteDeckAsync(string deckId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FlashCardDecks WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", deckId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FlashCard>> GetCardsByDeckIdAsync(string deckId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, DeckId, ChapterId, Front, Back, Interval, Repetitions, EaseFactor, NextReviewDate, LastReviewDate, CreatedAt
            FROM FlashCards
            WHERE DeckId = @DeckId
            ORDER BY NextReviewDate ASC, CreatedAt ASC;
            """;
        command.Parameters.AddWithValue("@DeckId", deckId);

        var results = new List<FlashCard>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadCard(reader));

        return results;
    }

    public async Task<FlashCard?> GetCardByIdAsync(string cardId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, DeckId, ChapterId, Front, Back, Interval, Repetitions, EaseFactor, NextReviewDate, LastReviewDate, CreatedAt
            FROM FlashCards
            WHERE Id = @Id;
            """;
        command.Parameters.AddWithValue("@Id", cardId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? ReadCard(reader) : null;
    }

    public async Task InsertCardAsync(FlashCard card, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO FlashCards (Id, DeckId, ChapterId, Front, Back, Interval, Repetitions, EaseFactor, NextReviewDate, LastReviewDate, CreatedAt)
            VALUES (@Id, @DeckId, @ChapterId, @Front, @Back, @Interval, @Repetitions, @EaseFactor, @NextReviewDate, @LastReviewDate, @CreatedAt);
            """;
        PopulateCardParameters(command, card);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task InsertCardsAsync(IReadOnlyList<FlashCard> cards, CancellationToken cancellationToken = default)
    {
        if (cards.Count == 0)
            return;

        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);
        var transaction = (SqliteTransaction)dbTransaction;

        foreach (var card in cards)
        {
            await using var command = connection.CreateCommand();
            command.Transaction = transaction;
            command.CommandText = """
                INSERT INTO FlashCards (Id, DeckId, ChapterId, Front, Back, Interval, Repetitions, EaseFactor, NextReviewDate, LastReviewDate, CreatedAt)
                VALUES (@Id, @DeckId, @ChapterId, @Front, @Back, @Interval, @Repetitions, @EaseFactor, @NextReviewDate, @LastReviewDate, @CreatedAt);
                """;
            PopulateCardParameters(command, card);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task UpdateCardAsync(FlashCard card, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            UPDATE FlashCards
            SET DeckId = @DeckId,
                ChapterId = @ChapterId,
                Front = @Front,
                Back = @Back,
                Interval = @Interval,
                Repetitions = @Repetitions,
                EaseFactor = @EaseFactor,
                NextReviewDate = @NextReviewDate,
                LastReviewDate = @LastReviewDate
            WHERE Id = @Id;
            """;
        PopulateCardParameters(command, card);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task DeleteCardAsync(string cardId, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "DELETE FROM FlashCards WHERE Id = @Id;";
        command.Parameters.AddWithValue("@Id", cardId);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FlashCard>> GetDueCardsAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT Id, DeckId, ChapterId, Front, Back, Interval, Repetitions, EaseFactor, NextReviewDate, LastReviewDate, CreatedAt
            FROM FlashCards
            WHERE date(NextReviewDate) <= date(@AsOfDate)
            ORDER BY NextReviewDate ASC, CreatedAt ASC;
            """;
        command.Parameters.AddWithValue("@AsOfDate", asOfDate.ToString("O"));

        var results = new List<FlashCard>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add(ReadCard(reader));

        return results;
    }

    public async Task<int> GetDueCardCountAsync(DateTime asOfDate, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM FlashCards WHERE date(NextReviewDate) <= date(@AsOfDate);";
        command.Parameters.AddWithValue("@AsOfDate", asOfDate.ToString("O"));
        return Convert.ToInt32(await command.ExecuteScalarAsync(cancellationToken));
    }

    public async Task InsertReviewAsync(CardReview review, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            INSERT INTO CardReviews (Id, CardId, Quality, ReviewedAt, PreviousInterval, NewInterval, PreviousEaseFactor, NewEaseFactor)
            VALUES (@Id, @CardId, @Quality, @ReviewedAt, @PreviousInterval, @NewInterval, @PreviousEaseFactor, @NewEaseFactor);
            """;
        command.Parameters.AddWithValue("@Id", review.Id);
        command.Parameters.AddWithValue("@CardId", review.CardId);
        command.Parameters.AddWithValue("@Quality", review.Quality);
        command.Parameters.AddWithValue("@ReviewedAt", review.ReviewedAt.ToString("O"));
        command.Parameters.AddWithValue("@PreviousInterval", review.PreviousInterval);
        command.Parameters.AddWithValue("@NewInterval", review.NewInterval);
        command.Parameters.AddWithValue("@PreviousEaseFactor", review.PreviousEaseFactor);
        command.Parameters.AddWithValue("@NewEaseFactor", review.NewEaseFactor);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<double> GetRetentionRateAsync(int lastNDays, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT
                CASE WHEN COUNT(*) = 0 THEN 0.0
                     ELSE SUM(CASE WHEN Quality >= 3 THEN 1 ELSE 0 END) * 100.0 / COUNT(*)
                END
            FROM CardReviews
            WHERE ReviewedAt >= @StartDate;
            """;
        command.Parameters.AddWithValue("@StartDate", DateTime.UtcNow.Date.AddDays(-lastNDays).ToString("O"));
        var value = await command.ExecuteScalarAsync(cancellationToken);
        return value is null or DBNull ? 0d : Convert.ToDouble(value);
    }

    public async Task<IReadOnlyList<(DateTime Date, int Count)>> GetReviewForecastAsync(int days, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT date(NextReviewDate) AS ReviewDate, COUNT(*)
            FROM FlashCards
            WHERE date(NextReviewDate) >= date(@StartDate)
              AND date(NextReviewDate) < date(@EndDate)
            GROUP BY date(NextReviewDate)
            ORDER BY ReviewDate ASC;
            """;
        command.Parameters.AddWithValue("@StartDate", DateTime.UtcNow.Date.ToString("O"));
        command.Parameters.AddWithValue("@EndDate", DateTime.UtcNow.Date.AddDays(days + 1).ToString("O"));

        var results = new List<(DateTime Date, int Count)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add((DateTime.Parse(reader.GetString(0)), reader.GetInt32(1)));

        return results;
    }

    public async Task<IReadOnlyList<(DateTime Date, int Count, double Accuracy)>> GetRecentReviewHistoryAsync(int days, CancellationToken cancellationToken = default)
    {
        await using var connection = await OpenConnectionAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT date(ReviewedAt) AS ReviewDate,
                   COUNT(*),
                   AVG(CASE WHEN Quality >= 3 THEN 1.0 ELSE 0.0 END) * 100.0
            FROM CardReviews
            WHERE ReviewedAt >= @StartDate
            GROUP BY date(ReviewedAt)
            ORDER BY ReviewDate ASC;
            """;
        command.Parameters.AddWithValue("@StartDate", DateTime.UtcNow.Date.AddDays(-days).ToString("O"));

        var results = new List<(DateTime Date, int Count, double Accuracy)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            results.Add((DateTime.Parse(reader.GetString(0)), reader.GetInt32(1), reader.GetDouble(2)));

        return results;
    }

    private async Task<SqliteConnection> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }

    private static void PopulateCardParameters(SqliteCommand command, FlashCard card)
    {
        command.Parameters.AddWithValue("@Id", card.Id);
        command.Parameters.AddWithValue("@DeckId", card.DeckId);
        command.Parameters.AddWithValue("@ChapterId", ToDbValue(card.ChapterId));
        command.Parameters.AddWithValue("@Front", card.Front);
        command.Parameters.AddWithValue("@Back", card.Back);
        command.Parameters.AddWithValue("@Interval", card.Interval);
        command.Parameters.AddWithValue("@Repetitions", card.Repetitions);
        command.Parameters.AddWithValue("@EaseFactor", card.EaseFactor);
        command.Parameters.AddWithValue("@NextReviewDate", card.NextReviewDate.ToString("O"));
        command.Parameters.AddWithValue("@LastReviewDate", ToDbValue(card.LastReviewDate));
        command.Parameters.AddWithValue("@CreatedAt", card.CreatedAt.ToString("O"));
    }

    private static FlashCardDeck ReadDeck(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        Title = reader.GetString(1),
        BookId = ParseNullableString(reader, 2),
        CreatedAt = DateTime.Parse(reader.GetString(3)),
        UpdatedAt = DateTime.Parse(reader.GetString(4))
    };

    private static FlashCard ReadCard(SqliteDataReader reader) => new()
    {
        Id = reader.GetString(0),
        DeckId = reader.GetString(1),
        ChapterId = ParseNullableString(reader, 2),
        Front = reader.GetString(3),
        Back = reader.GetString(4),
        Interval = reader.GetInt32(5),
        Repetitions = reader.GetInt32(6),
        EaseFactor = reader.GetDouble(7),
        NextReviewDate = DateTime.Parse(reader.GetString(8)),
        LastReviewDate = ParseNullableDateTime(reader, 9),
        CreatedAt = DateTime.Parse(reader.GetString(10))
    };

    private static object ToDbValue(string? value) => value is null ? DBNull.Value : value;

    private static object ToDbValue(DateTime? value) => value is null ? DBNull.Value : value.Value.ToString("O");

    private static string? ParseNullableString(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : reader.GetString(ordinal);

    private static DateTime? ParseNullableDateTime(SqliteDataReader reader, int ordinal)
        => reader.IsDBNull(ordinal) ? null : DateTime.Parse(reader.GetString(ordinal));
}
