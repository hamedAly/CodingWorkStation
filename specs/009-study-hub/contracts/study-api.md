# API Contracts: Study Hub

**Feature**: 009-study-hub  
**Pattern**: MediatR CQRS — each endpoint dispatches one command or query  
**Route prefix**: `api/study`  
**Controller**: `StudyController.cs`  
**DTO location**: `SemanticSearch.WebApi/Contracts/Study/StudyDtos.cs`

---

## Library & Reading

### 1. List Books

```
GET /api/study/books
```

**Description**: Retrieve all books in the study library.

**MediatR**: `ListBooksQuery` → `ListBooksQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record BookSummaryResponse(
    string Id,
    string Title,
    string? Author,
    int PageCount,
    int LastReadPage,
    int ChapterCount,
    DateTime CreatedAt);
```

Returns `IReadOnlyList<BookSummaryResponse>`.

---

### 2. Get Book

```
GET /api/study/books/{bookId}
```

**Description**: Retrieve a single book with its chapters.

**Path Parameters**:
- `bookId` (string, required): Book GUID

**MediatR**: `GetBookQuery` → `GetBookQueryHandler`

**Response** `200 OK`:
```csharp
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
```

**Response** `404 Not Found`: Book not found

---

### 3. Add Book

```
POST /api/study/books
Content-Type: multipart/form-data
```

**Description**: Upload a PDF and create a new book entry.

**MediatR**: `AddBookCommand` → `AddBookCommandHandler`

**Request** (multipart form):
```csharp
public sealed record AddBookCommand(
    string Title,
    string? Author,
    string? Description,
    IFormFile PdfFile) : IRequest<BookDetailResponse>;
```

**Validation** (FluentValidation):
- `Title`: Required, non-empty, max 500 chars
- `PdfFile`: Required, content type must be `application/pdf`, max 200 MB
- `PdfFile`: Magic bytes must start with `%PDF-`
- `Author`: Max 300 chars (if provided)
- `Description`: Max 2000 chars (if provided)

**Response** `201 Created`: `BookDetailResponse` (with empty Chapters list)  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 4. Update Book

```
PUT /api/study/books/{bookId}
Content-Type: application/json
```

**Description**: Update book metadata (not the PDF file).

**Path Parameters**:
- `bookId` (string, required): Book GUID

**MediatR**: `UpdateBookCommand` → `UpdateBookCommandHandler`

**Request**:
```csharp
public sealed record UpdateBookRequest(
    string Title,
    string? Author,
    string? Description);
```

**Validation**: Same as AddBookCommand for text fields.

**Response** `200 OK`: `BookDetailResponse`  
**Response** `404 Not Found`: Book not found  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 5. Delete Book

```
DELETE /api/study/books/{bookId}
```

**Description**: Delete a book and all associated data (chapters, audio files, plan associations). PDF file is deleted from disk.

**Path Parameters**:
- `bookId` (string, required): Book GUID

**MediatR**: `DeleteBookCommand` → `DeleteBookCommandHandler`

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Book not found

---

### 6. Serve Book PDF

```
GET /api/study/books/{bookId}/pdf
```

**Description**: Stream the PDF file for in-browser rendering via PDF.js.

**Path Parameters**:
- `bookId` (string, required): Book GUID

**Response** `200 OK`: File stream with `Content-Type: application/pdf`  
**Response** `404 Not Found`: Book or file not found

**Note**: No MediatR — direct file serving via `PhysicalFile()`.

---

### 7. Update Last-Read Page

```
PATCH /api/study/books/{bookId}/last-read-page
Content-Type: application/json
```

**Description**: Persist the user's current reading position.

**Path Parameters**:
- `bookId` (string, required): Book GUID

**MediatR**: `UpdateLastReadPageCommand` → `UpdateLastReadPageCommandHandler`

**Request**:
```csharp
public sealed record UpdateLastReadPageRequest(int Page);
```

**Validation**:
- `Page`: Required, ≥ 1, ≤ book's PageCount

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Book not found

---

## Chapters

### 8. Add Chapter

```
POST /api/study/books/{bookId}/chapters
Content-Type: application/json
```

**Description**: Add a chapter to a book.

**MediatR**: `AddChapterCommand` → `AddChapterCommandHandler`

**Request**:
```csharp
public sealed record AddChapterRequest(
    string Title,
    int StartPage,
    int EndPage);
```

**Validation**:
- `Title`: Required, non-empty, max 500 chars
- `StartPage`: ≥ 1, ≤ book's PageCount
- `EndPage`: ≥ StartPage, ≤ book's PageCount

**Response** `201 Created`: `ChapterResponse`  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 9. Update Chapter

```
PUT /api/study/books/{bookId}/chapters/{chapterId}
Content-Type: application/json
```

**Description**: Update chapter title or page range.

**MediatR**: `UpdateChapterCommand` → `UpdateChapterCommandHandler`

**Request**:
```csharp
public sealed record UpdateChapterRequest(
    string Title,
    int StartPage,
    int EndPage);
```

**Validation**: Same as AddChapterRequest.

**Response** `200 OK`: `ChapterResponse`  
**Response** `404 Not Found`: Chapter not found

---

### 10. Delete Chapter

```
DELETE /api/study/books/{bookId}/chapters/{chapterId}
```

**Description**: Delete a chapter. Linked flashcards remain in their deck (orphaned). Plan items set to Skipped.

**MediatR**: `DeleteChapterCommand` → `DeleteChapterCommandHandler`

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Chapter not found

---

### 11. Update Chapter Notes

```
PUT /api/study/books/{bookId}/chapters/{chapterId}/notes
Content-Type: application/json
```

**Description**: Set or update notes for a chapter (used as AI flashcard generation input).

**MediatR**: `UpdateChapterNotesCommand` → `UpdateChapterNotesCommandHandler`

**Request**:
```csharp
public sealed record UpdateChapterNotesRequest(string? Notes);
```

**Validation**:
- `Notes`: Max 10000 chars

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Chapter not found

---

### 12. Upload Chapter Audio

```
POST /api/study/books/{bookId}/chapters/{chapterId}/audio
Content-Type: multipart/form-data
```

**Description**: Attach an audio file to a chapter. Replaces existing audio if present.

**MediatR**: `UploadChapterAudioCommand` → `UploadChapterAudioCommandHandler`

**Request** (multipart form):
```csharp
public sealed record UploadChapterAudioCommand(
    string BookId,
    string ChapterId,
    IFormFile AudioFile) : IRequest;
```

**Validation**:
- `AudioFile`: Required, content type must be `audio/mpeg`, `audio/wav`, or `audio/mp4`, max 100 MB

**Response** `204 No Content`: Success  
**Response** `400 Bad Request`: `ValidationProblemDetails`  
**Response** `404 Not Found`: Chapter not found

---

### 13. Serve Chapter Audio

```
GET /api/study/books/{bookId}/chapters/{chapterId}/audio
```

**Description**: Stream the audio file for in-browser playback.

**Response** `200 OK`: File stream with appropriate `Content-Type`  
**Response** `404 Not Found`: Chapter or audio not found

**Note**: No MediatR — direct file serving via `PhysicalFile()`.

---

## Study Plans

### 14. List Study Plans

```
GET /api/study/plans
```

**Description**: Retrieve all study plans with summary stats.

**MediatR**: `ListStudyPlansQuery` → `ListStudyPlansQueryHandler`

**Response** `200 OK`:
```csharp
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
```

Returns `IReadOnlyList<StudyPlanSummaryResponse>`.

---

### 15. Get Study Plan

```
GET /api/study/plans/{planId}
```

**Description**: Retrieve a study plan with all its items.

**MediatR**: `GetStudyPlanQuery` → `GetStudyPlanQueryHandler`

**Response** `200 OK`:
```csharp
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
```

**Response** `404 Not Found`: Plan not found

---

### 16. Create Study Plan

```
POST /api/study/plans
Content-Type: application/json
```

**Description**: Create a new study plan.

**MediatR**: `CreateStudyPlanCommand` → `CreateStudyPlanCommandHandler`

**Request**:
```csharp
public sealed record CreateStudyPlanRequest(
    string Title,
    string? BookId,
    DateTime StartDate,
    DateTime EndDate,
    bool SkipWeekends);
```

**Validation**:
- `Title`: Required, non-empty, max 500 chars
- `EndDate`: ≥ StartDate
- `BookId`: Must reference existing book (if provided)

**Response** `201 Created`: `StudyPlanDetailResponse` (empty items)  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 17. Auto-Generate Plan Items

```
POST /api/study/plans/{planId}/auto-generate
Content-Type: application/json
```

**Description**: Distribute a book's chapters evenly across the plan's date range.

**MediatR**: `AutoGeneratePlanItemsCommand` → `AutoGeneratePlanItemsCommandHandler`

**Prerequisites**: Plan must have a BookId. Book must have chapters.

**Response** `200 OK`: `StudyPlanDetailResponse` (with generated items)  
**Response** `400 Bad Request`: Plan has no book, or book has no chapters  
**Response** `404 Not Found`: Plan not found

---

### 18. Update Plan Item Status

```
PATCH /api/study/plans/{planId}/items/{itemId}/status
Content-Type: application/json
```

**Description**: Update the status of a plan item.

**MediatR**: `UpdatePlanItemStatusCommand` → `UpdatePlanItemStatusCommandHandler`

**Request**:
```csharp
public sealed record UpdatePlanItemStatusRequest(string Status);
```

**Validation**:
- `Status`: Required, must be one of: Pending, InProgress, Done, Skipped

**Response** `200 OK`: `PlanItemResponse`  
**Response** `404 Not Found`: Plan or item not found

---

### 19. Get Today's Study Items

```
GET /api/study/today
```

**Description**: Retrieve all plan items and due flashcards for today across all active plans.

**MediatR**: `GetTodayStudyItemsQuery` → `GetTodayStudyItemsQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record TodayStudyItemsResponse(
    IReadOnlyList<PlanItemResponse> PlanItems,
    int DueFlashCardCount,
    IReadOnlyList<string> DueDeckIds);
```

---

### 20. Get Calendar Data

```
GET /api/study/calendar?year={year}&month={month}
```

**Description**: Retrieve plan items grouped by day for a given month.

**MediatR**: `GetCalendarDataQuery` → `GetCalendarDataQueryHandler`

**Query Parameters**:
- `year` (int, required): Year (e.g., 2026)
- `month` (int, required): Month (1–12)

**Response** `200 OK`:
```csharp
public sealed record CalendarDayResponse(
    DateTime Date,
    IReadOnlyList<CalendarItemResponse> Items);

public sealed record CalendarItemResponse(
    string Id,
    string Title,
    string Status,
    string? PlanTitle);
```

Returns `IReadOnlyList<CalendarDayResponse>`.

---

## Flashcards & Spaced Repetition

### 21. List Decks

```
GET /api/study/decks
```

**Description**: Retrieve all flashcard decks with card counts.

**MediatR**: `ListDecksQuery` → `ListDecksQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record DeckSummaryResponse(
    string Id,
    string Title,
    string? BookId,
    string? BookTitle,
    int TotalCards,
    int DueCards);
```

Returns `IReadOnlyList<DeckSummaryResponse>`.

---

### 22. Get Deck

```
GET /api/study/decks/{deckId}
```

**Description**: Retrieve a deck with all its cards.

**MediatR**: `GetDeckQuery` → `GetDeckQueryHandler`

**Response** `200 OK`:
```csharp
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
```

**Response** `404 Not Found`: Deck not found

---

### 23. Create Deck

```
POST /api/study/decks
Content-Type: application/json
```

**MediatR**: `CreateDeckCommand` → `CreateDeckCommandHandler`

**Request**:
```csharp
public sealed record CreateDeckRequest(
    string Title,
    string? BookId);
```

**Validation**:
- `Title`: Required, non-empty, max 500 chars
- `BookId`: Must reference existing book (if provided)

**Response** `201 Created`: `DeckDetailResponse` (empty cards)  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 24. Add Flashcard

```
POST /api/study/decks/{deckId}/cards
Content-Type: application/json
```

**MediatR**: `AddFlashCardCommand` → `AddFlashCardCommandHandler`

**Request**:
```csharp
public sealed record AddFlashCardRequest(
    string Front,
    string Back,
    string? ChapterId);
```

**Validation**:
- `Front`: Required, non-empty, max 5000 chars
- `Back`: Required, non-empty, max 5000 chars

**Response** `201 Created`: `FlashCardResponse`  
**Response** `400 Bad Request`: `ValidationProblemDetails`

---

### 25. Update Flashcard

```
PUT /api/study/decks/{deckId}/cards/{cardId}
Content-Type: application/json
```

**MediatR**: `UpdateFlashCardCommand` → `UpdateFlashCardCommandHandler`

**Request**: Same as `AddFlashCardRequest`.

**Response** `200 OK`: `FlashCardResponse`  
**Response** `404 Not Found`: Card not found

---

### 26. Delete Flashcard

```
DELETE /api/study/decks/{deckId}/cards/{cardId}
```

**MediatR**: `DeleteFlashCardCommand` → `DeleteFlashCardCommandHandler`

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Card not found

---

### 27. Delete Deck

```
DELETE /api/study/decks/{deckId}
```

**MediatR**: `DeleteDeckCommand` → `DeleteDeckCommandHandler`

**Description**: Delete a deck and all its cards and review history.

**Response** `204 No Content`: Success  
**Response** `404 Not Found`: Deck not found

---

### 28. Get Due Cards for Review

```
GET /api/study/review/due
```

**Description**: Retrieve all flashcards due for review today, across all decks.

**MediatR**: `GetDueCardsQuery` → `GetDueCardsQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record DueCardsResponse(
    int TotalDue,
    IReadOnlyList<DueCardResponse> Cards);

public sealed record DueCardResponse(
    string Id,
    string DeckId,
    string DeckTitle,
    string Front,
    string Back,
    int CurrentInterval,
    int Repetitions,
    double EaseFactor);
```

---

### 29. Review Card

```
POST /api/study/review/{cardId}
Content-Type: application/json
```

**Description**: Submit a review rating for a flashcard. SM-2 algorithm computes the new interval and next review date.

**MediatR**: `ReviewCardCommand` → `ReviewCardCommandHandler`

**Request**:
```csharp
public sealed record ReviewCardRequest(int Quality);
```

**Validation**:
- `Quality`: Required, 0–5

**Response** `200 OK`:
```csharp
public sealed record ReviewResultResponse(
    string CardId,
    int NewInterval,
    double NewEaseFactor,
    int NewRepetitions,
    DateTime NextReviewDate);
```

**Response** `404 Not Found`: Card not found

---

### 30. Generate Flashcards from Chapter (AI)

```
POST /api/study/decks/{deckId}/generate-from-chapter/{chapterId}
```

**Description**: Use the local AI model to generate flashcards from chapter notes. Non-streaming; returns generated cards.

**MediatR**: `GenerateCardsFromChapterCommand` → `GenerateCardsFromChapterCommandHandler`

**Prerequisites**: Chapter must have non-empty Notes. AI model must be available.

**Response** `200 OK`:
```csharp
public sealed record GeneratedCardsResponse(
    int GeneratedCount,
    IReadOnlyList<FlashCardResponse> Cards);
```

**Response** `400 Bad Request`: Chapter has no notes  
**Response** `503 Service Unavailable`: AI model not available

---

### 31. Get Review Statistics

```
GET /api/study/review/stats
```

**Description**: Retrieve review analytics for the dashboard.

**MediatR**: `GetReviewStatsQuery` → `GetReviewStatsQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record ReviewStatsResponse(
    int TotalCardsReviewed,
    double RetentionRate,
    IReadOnlyList<ReviewForecastDay> Forecast,
    IReadOnlyList<DailyReviewCount> RecentHistory);

public sealed record ReviewForecastDay(
    DateTime Date,
    int DueCount);

public sealed record DailyReviewCount(
    DateTime Date,
    int Count,
    double AccuracyPercent);
```

---

## Study Sessions & Analytics

### 32. Start Study Session

```
POST /api/study/sessions
Content-Type: application/json
```

**MediatR**: `StartStudySessionCommand` → `StartStudySessionCommandHandler`

**Request**:
```csharp
public sealed record StartStudySessionRequest(
    string SessionType,
    string? BookId,
    string? ChapterId,
    bool IsPomodoro,
    int? FocusDurationMinutes);
```

**Validation**:
- `SessionType`: Required, one of: Reading, Review, Listening
- `FocusDurationMinutes`: Required if IsPomodoro is true, > 0, ≤ 120

**Response** `201 Created`:
```csharp
public sealed record StudySessionResponse(
    string Id,
    string SessionType,
    string? BookId,
    string? ChapterId,
    DateTime StartedAt,
    DateTime? EndedAt,
    int? DurationMinutes,
    bool IsPomodoroSession,
    int? FocusDurationMinutes);
```

---

### 33. End Study Session

```
PATCH /api/study/sessions/{sessionId}/end
```

**Description**: End an active study session and record the duration.

**MediatR**: `EndStudySessionCommand` → `EndStudySessionCommandHandler`

**Response** `200 OK`: `StudySessionResponse` (with EndedAt and DurationMinutes set)  
**Response** `404 Not Found`: Session not found

---

### 34. Get Study Dashboard

```
GET /api/study/dashboard
```

**Description**: Retrieve aggregated analytics for the study dashboard.

**MediatR**: `GetStudyDashboardQuery` → `GetStudyDashboardQueryHandler`

**Response** `200 OK`:
```csharp
public sealed record StudyDashboardResponse(
    int StudyStreakDays,
    double WeeklyStudyHours,
    IReadOnlyList<DailyStudyHours> WeeklyChart,
    int TotalDueCards,
    int TotalDuePlanItems,
    double RetentionRate,
    IReadOnlyList<BookProgressResponse> BookProgress);

public sealed record DailyStudyHours(
    DateTime Date,
    double Hours);

public sealed record BookProgressResponse(
    string BookId,
    string BookTitle,
    int CompletedChapters,
    int TotalChapters,
    double ProgressPercent);
```

---

## New DTO Summary

All DTOs in `Contracts/Study/StudyDtos.cs`:

```csharp
// Books
public sealed record BookSummaryResponse(...);
public sealed record BookDetailResponse(...);
public sealed record ChapterResponse(...);
public sealed record UpdateBookRequest(...);
public sealed record AddChapterRequest(...);
public sealed record UpdateChapterRequest(...);
public sealed record UpdateChapterNotesRequest(...);
public sealed record UpdateLastReadPageRequest(...);

// Plans
public sealed record StudyPlanSummaryResponse(...);
public sealed record StudyPlanDetailResponse(...);
public sealed record PlanItemResponse(...);
public sealed record CreateStudyPlanRequest(...);
public sealed record UpdatePlanItemStatusRequest(...);
public sealed record TodayStudyItemsResponse(...);
public sealed record CalendarDayResponse(...);
public sealed record CalendarItemResponse(...);

// Flashcards
public sealed record DeckSummaryResponse(...);
public sealed record DeckDetailResponse(...);
public sealed record FlashCardResponse(...);
public sealed record CreateDeckRequest(...);
public sealed record AddFlashCardRequest(...);
public sealed record ReviewCardRequest(...);
public sealed record ReviewResultResponse(...);
public sealed record DueCardsResponse(...);
public sealed record DueCardResponse(...);
public sealed record GeneratedCardsResponse(...);
public sealed record ReviewStatsResponse(...);
public sealed record ReviewForecastDay(...);
public sealed record DailyReviewCount(...);

// Sessions & Dashboard
public sealed record StartStudySessionRequest(...);
public sealed record StudySessionResponse(...);
public sealed record StudyDashboardResponse(...);
public sealed record DailyStudyHours(...);
public sealed record BookProgressResponse(...);
```

---

## Repository Interface Methods

### IStudyRepository

```csharp
// Books
Task<IReadOnlyList<StudyBook>> GetAllBooksAsync(CancellationToken ct);
Task<StudyBook?> GetBookByIdAsync(string bookId, CancellationToken ct);
Task InsertBookAsync(StudyBook book, CancellationToken ct);
Task UpdateBookAsync(StudyBook book, CancellationToken ct);
Task DeleteBookAsync(string bookId, CancellationToken ct);
Task UpdateLastReadPageAsync(string bookId, int page, CancellationToken ct);

// Chapters
Task<IReadOnlyList<StudyChapter>> GetChaptersByBookIdAsync(string bookId, CancellationToken ct);
Task<StudyChapter?> GetChapterByIdAsync(string chapterId, CancellationToken ct);
Task InsertChapterAsync(StudyChapter chapter, CancellationToken ct);
Task UpdateChapterAsync(StudyChapter chapter, CancellationToken ct);
Task DeleteChapterAsync(string chapterId, CancellationToken ct);

// Plans
Task<IReadOnlyList<StudyPlan>> GetAllPlansAsync(CancellationToken ct);
Task<StudyPlan?> GetPlanByIdAsync(string planId, CancellationToken ct);
Task InsertPlanAsync(StudyPlan plan, CancellationToken ct);
Task UpdatePlanAsync(StudyPlan plan, CancellationToken ct);
Task DeletePlanAsync(string planId, CancellationToken ct);

// Plan Items
Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByPlanIdAsync(string planId, CancellationToken ct);
Task<IReadOnlyList<StudyPlanItem>> GetPlanItemsByDateAsync(DateTime date, CancellationToken ct);
Task InsertPlanItemsAsync(IReadOnlyList<StudyPlanItem> items, CancellationToken ct);
Task UpdatePlanItemStatusAsync(string itemId, string status, DateTime? completedDate, CancellationToken ct);
Task<IReadOnlyList<StudyPlanItem>> GetCalendarItemsAsync(int year, int month, CancellationToken ct);

// Sessions
Task InsertSessionAsync(StudySession session, CancellationToken ct);
Task UpdateSessionEndAsync(string sessionId, DateTime endedAt, int durationMinutes, CancellationToken ct);
Task<StudySession?> GetSessionByIdAsync(string sessionId, CancellationToken ct);
Task<int> GetStudyStreakDaysAsync(CancellationToken ct);
Task<IReadOnlyList<(DateTime Date, double Hours)>> GetWeeklyStudyHoursAsync(CancellationToken ct);
```

### IFlashCardRepository

```csharp
// Decks
Task<IReadOnlyList<FlashCardDeck>> GetAllDecksAsync(CancellationToken ct);
Task<FlashCardDeck?> GetDeckByIdAsync(string deckId, CancellationToken ct);
Task InsertDeckAsync(FlashCardDeck deck, CancellationToken ct);
Task UpdateDeckAsync(FlashCardDeck deck, CancellationToken ct);
Task DeleteDeckAsync(string deckId, CancellationToken ct);

// Cards
Task<IReadOnlyList<FlashCard>> GetCardsByDeckIdAsync(string deckId, CancellationToken ct);
Task<FlashCard?> GetCardByIdAsync(string cardId, CancellationToken ct);
Task InsertCardAsync(FlashCard card, CancellationToken ct);
Task InsertCardsAsync(IReadOnlyList<FlashCard> cards, CancellationToken ct);
Task UpdateCardAsync(FlashCard card, CancellationToken ct);
Task DeleteCardAsync(string cardId, CancellationToken ct);
Task<IReadOnlyList<FlashCard>> GetDueCardsAsync(DateTime asOfDate, CancellationToken ct);
Task<int> GetDueCardCountAsync(DateTime asOfDate, CancellationToken ct);

// Reviews
Task InsertReviewAsync(CardReview review, CancellationToken ct);
Task<double> GetRetentionRateAsync(int lastNDays, CancellationToken ct);
Task<IReadOnlyList<(DateTime Date, int Count)>> GetReviewForecastAsync(int days, CancellationToken ct);
Task<IReadOnlyList<(DateTime Date, int Count, double Accuracy)>> GetRecentReviewHistoryAsync(int days, CancellationToken ct);
```
