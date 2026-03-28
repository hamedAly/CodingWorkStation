namespace SemanticSearch.WebApi.Contracts.Study;

public sealed record BookSummaryResponse(
    string Id,
    string Title,
    string? Author,
    int PageCount,
    int LastReadPage,
    int ChapterCount,
    DateTime CreatedAt);

public sealed record BookDetailResponse(
    string Id,
    string Title,
    string? Author,
    string? Description,
    string FileName,
    int PageCount,
    int LastReadPage,
    IReadOnlyList<ChapterResponse> Chapters,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record ChapterResponse(
    string Id,
    string Title,
    int StartPage,
    int EndPage,
    int SortOrder,
    bool HasAudio,
    bool HasNotes);

public sealed record UpdateBookRequest(string Title, string? Author, string? Description);

public sealed record AddChapterRequest(string Title, int StartPage, int EndPage);

public sealed record UpdateChapterRequest(string Title, int StartPage, int EndPage);

public sealed record UpdateChapterNotesRequest(string? Notes);

public sealed record UpdateLastReadPageRequest(int Page);

public sealed record DetectedChapterRequest(string Title, int StartPage, int EndPage);

public sealed record FinalizeBookImportRequest(
    int PageCount,
    IReadOnlyList<DetectedChapterRequest> Chapters,
    string? TableOfContentsText,
    string? PreviewText);

public sealed record StudyPlanSummaryResponse(
    string Id,
    string Title,
    string? BookId,
    string? BookTitle,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    int TotalItems,
    int CompletedItems,
    double ProgressPercent);

public sealed record StudyPlanDetailResponse(
    string Id,
    string Title,
    string? BookId,
    string? BookTitle,
    DateTime StartDate,
    DateTime EndDate,
    string Status,
    bool SkipWeekends,
    IReadOnlyList<PlanItemResponse> Items,
    double ProgressPercent);

public sealed record PlanItemResponse(
    string Id,
    string? ChapterId,
    string Title,
    DateTime ScheduledDate,
    string Status,
    DateTime? CompletedDate,
    int SortOrder);

public sealed record CreateStudyPlanRequest(
    string Title,
    string? BookId,
    DateTime StartDate,
    DateTime EndDate,
    bool SkipWeekends);

public sealed record UpdatePlanItemStatusRequest(string Status);

public sealed record TodayStudyItemsResponse(
    IReadOnlyList<PlanItemResponse> PlanItems,
    int DueFlashCardCount,
    IReadOnlyList<string> DueDeckIds);

public sealed record CalendarDayResponse(DateTime Date, IReadOnlyList<CalendarItemResponse> Items);

public sealed record CalendarItemResponse(string Id, string Title, string Status, string? PlanTitle);

public sealed record DeckSummaryResponse(
    string Id,
    string Title,
    string? BookId,
    string? BookTitle,
    int TotalCards,
    int DueCards);

public sealed record DeckDetailResponse(
    string Id,
    string Title,
    string? BookId,
    string? BookTitle,
    IReadOnlyList<FlashCardResponse> Cards,
    int DueCards);

public sealed record FlashCardResponse(
    string Id,
    string? ChapterId,
    string Front,
    string Back,
    int Interval,
    int Repetitions,
    double EaseFactor,
    DateTime NextReviewDate,
    DateTime? LastReviewDate);

public sealed record CreateDeckRequest(string Title, string? BookId);

public sealed record AddFlashCardRequest(string Front, string Back, string? ChapterId);

public sealed record ReviewCardRequest(int Quality);

public sealed record ReviewResultResponse(
    string CardId,
    int Quality,
    int NewInterval,
    int NewRepetitions,
    double NewEaseFactor,
    DateTime NextReviewDate);

public sealed record DueCardsResponse(IReadOnlyList<DueCardResponse> Cards, int TotalCount);

public sealed record DueCardResponse(
    string CardId,
    string DeckId,
    string DeckTitle,
    string Front,
    string Back,
    int Interval,
    int Repetitions,
    double EaseFactor,
    DateTime NextReviewDate);

public sealed record GeneratedCardsResponse(IReadOnlyList<FlashCardResponse> Cards, int GeneratedCount);

public sealed record ReviewStatsResponse(
    double RetentionRate,
    IReadOnlyList<ReviewForecastDay> Forecast,
    IReadOnlyList<DailyReviewCount> RecentHistory);

public sealed record ReviewForecastDay(DateTime Date, int DueCount);

public sealed record DailyReviewCount(DateTime Date, int Count, double Accuracy);

public sealed record StartStudySessionRequest(
    string SessionType,
    string? BookId,
    string? ChapterId,
    bool IsPomodoro,
    int? FocusDurationMinutes);

public sealed record StudySessionResponse(
    string Id,
    string? BookId,
    string? ChapterId,
    string SessionType,
    DateTime StartedAt,
    DateTime? EndedAt,
    int? DurationMinutes,
    bool IsPomodoroSession,
    int? FocusDurationMinutes);

public sealed record StudyDashboardResponse(
    int StudyStreakDays,
    int DuePlanItemsCount,
    int DueFlashCardCount,
    double RetentionRate,
    IReadOnlyList<DailyStudyHours> WeeklyHours,
    IReadOnlyList<BookProgressResponse> BookProgress);

public sealed record DailyStudyHours(DateTime Date, double Hours);

public sealed record BookProgressResponse(
    string BookId,
    string BookTitle,
    int CompletedChapters,
    int TotalChapters,
    double ProgressPercent);
