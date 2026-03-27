# Tasks: Study Hub

**Input**: Design documents from `/specs/009-study-hub/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not requested — test tasks omitted.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Domain entities, value objects, repository interfaces, DTOs, folder structure, SQLite schema, and configuration for file uploads

- [X] T001 Create StudyBook entity in `src/SemanticSearch.Domain/Entities/StudyBook.cs` — sealed record with Id (string), Title, Author (string?), Description (string?), FileName, FilePath, PageCount (int), LastReadPage (int), CreatedAt (DateTime), UpdatedAt (DateTime)
- [X] T002 [P] Create StudyChapter entity in `src/SemanticSearch.Domain/Entities/StudyChapter.cs` — sealed record with Id, BookId, Title, StartPage, EndPage, SortOrder, AudioFileName (string?), AudioFilePath (string?), Notes (string?), CreatedAt
- [X] T003 [P] Create StudyPlan entity in `src/SemanticSearch.Domain/Entities/StudyPlan.cs` — sealed record with Id, Title, BookId (string?), StartDate, EndDate, Status, SkipWeekends (bool), CreatedAt, UpdatedAt
- [X] T004 [P] Create StudyPlanItem entity in `src/SemanticSearch.Domain/Entities/StudyPlanItem.cs` — sealed record with Id, PlanId, ChapterId (string?), Title, ScheduledDate, Status, CompletedDate (DateTime?), SortOrder, CreatedAt
- [X] T005 [P] Create FlashCardDeck entity in `src/SemanticSearch.Domain/Entities/FlashCardDeck.cs` — sealed record with Id, Title, BookId (string?), CreatedAt, UpdatedAt
- [X] T006 [P] Create FlashCard entity in `src/SemanticSearch.Domain/Entities/FlashCard.cs` — sealed record with Id, DeckId, ChapterId (string?), Front, Back, Interval (int), Repetitions (int), EaseFactor (double), NextReviewDate, LastReviewDate (DateTime?), CreatedAt
- [X] T007 [P] Create CardReview entity in `src/SemanticSearch.Domain/Entities/CardReview.cs` — sealed record with Id, CardId, Quality (int), ReviewedAt, PreviousInterval, NewInterval, PreviousEaseFactor (double), NewEaseFactor (double)
- [X] T008 [P] Create StudySession entity in `src/SemanticSearch.Domain/Entities/StudySession.cs` — sealed record with Id, BookId (string?), ChapterId (string?), SessionType, StartedAt, EndedAt (DateTime?), DurationMinutes (int?), IsPomodoroSession (bool), FocusDurationMinutes (int?), CreatedAt
- [X] T009 [P] Create value objects in `src/SemanticSearch.Domain/ValueObjects/` — ReviewQuality.cs (static class with int constants 0–5 and display names), StudyPlanStatus.cs (static class: Draft, Active, Paused, Completed), PlanItemStatus.cs (static class: Pending, InProgress, Done, Skipped), StudySessionType.cs (static class: Reading, Review, Listening)
- [X] T010 [P] Create IStudyRepository interface in `src/SemanticSearch.Domain/Interfaces/IStudyRepository.cs` — methods for books CRUD, chapters CRUD, plans CRUD, plan items CRUD (by plan, by date, by calendar month), sessions CRUD, study streak, weekly hours as defined in contracts/study-api.md repository section
- [X] T011 [P] Create IFlashCardRepository interface in `src/SemanticSearch.Domain/Interfaces/IFlashCardRepository.cs` — methods for decks CRUD, cards CRUD (by deck, by due date), reviews insert, due card count, retention rate, review forecast, recent review history as defined in contracts/study-api.md repository section
- [X] T012 [P] Create all Study DTOs in `src/SemanticSearch.WebApi/Contracts/Study/StudyDtos.cs` — all sealed records as defined in contracts/study-api.md: BookSummaryResponse, BookDetailResponse, ChapterResponse, UpdateBookRequest, AddChapterRequest, UpdateChapterRequest, UpdateChapterNotesRequest, UpdateLastReadPageRequest, StudyPlanSummaryResponse, StudyPlanDetailResponse, PlanItemResponse, CreateStudyPlanRequest, UpdatePlanItemStatusRequest, TodayStudyItemsResponse, CalendarDayResponse, CalendarItemResponse, DeckSummaryResponse, DeckDetailResponse, FlashCardResponse, CreateDeckRequest, AddFlashCardRequest, ReviewCardRequest, ReviewResultResponse, DueCardsResponse, DueCardResponse, GeneratedCardsResponse, ReviewStatsResponse, ReviewForecastDay, DailyReviewCount, StartStudySessionRequest, StudySessionResponse, StudyDashboardResponse, DailyStudyHours, BookProgressResponse
- [X] T013 Add 8 new Study Hub table CREATE statements and indexes to `src/SemanticSearch.Infrastructure/Common/SqliteSchemaInitializer.cs` — StudyBooks, StudyChapters, StudyPlans, StudyPlanItems, FlashCardDecks, FlashCards, CardReviews, StudySessions with all columns, FKs, and indexes as defined in data-model.md SQLite Schema section
- [X] T014 Configure file upload size limits in `src/SemanticSearch.WebApi/Program.cs` — add `FormOptions.MultipartBodyLengthLimit = 209_715_200` (200 MB) and `Kestrel.Limits.MaxRequestBodySize = 209_715_200` as per research.md R-003
- [X] T015 [P] Create Application Study folder structure with empty marker: `src/SemanticSearch.Application/Study/Commands/`, `src/SemanticSearch.Application/Study/Queries/`, `src/SemanticSearch.Application/Study/Validators/`, `src/SemanticSearch.Application/Study/Services/`, `src/SemanticSearch.Application/Study/Models/`

**Checkpoint**: Solution builds. All 8 entities, 4 value objects, 2 repository interfaces, all DTOs, SQLite schema, and upload config in place. No runtime functionality yet.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: SQLite repository implementations, DI registration, SpacedRepetitionEngine, and navigation — core infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T016 Implement SqliteStudyRepository in `src/SemanticSearch.Infrastructure/Study/SqliteStudyRepository.cs` — implements IStudyRepository; singleton with connection string from existing DatabasePath pattern; all books CRUD, chapters CRUD, plans CRUD, plan items CRUD (by plan, by date, by calendar month), sessions CRUD, study streak calculation (consecutive days with sessions backwards from today), weekly study hours aggregation
- [X] T017 [P] Implement SqliteFlashCardRepository in `src/SemanticSearch.Infrastructure/Study/SqliteFlashCardRepository.cs` — implements IFlashCardRepository; singleton with same DatabasePath; all decks CRUD, cards CRUD (by deck, due cards query by date), bulk card insert, reviews insert, due card count, retention rate (cards rated ≥ 3 in last N days), review forecast (due per day for next N days), recent review history with accuracy
- [X] T018 Implement SpacedRepetitionEngine in `src/SemanticSearch.Application/Study/Services/SpacedRepetitionEngine.cs` — static class with pure function `Calculate(int quality, int currentInterval, int repetitions, double easeFactor)` returning `(int NewInterval, int NewRepetitions, double NewEaseFactor, DateTime NextReviewDate)` implementing SM-2 algorithm as specified in research.md R-002
- [X] T019 Register Study services in DI in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs` — register SqliteStudyRepository as singleton implementing IStudyRepository, SqliteFlashCardRepository as singleton implementing IFlashCardRepository
- [X] T020 Add Phase 5 "Study Hub" navigation group to `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor.cs` — add new PhaseGroup with items: "Study Library" → /study (book icon), "Review Cards" → /study/review (card icon), "Study Dashboard" → /study/dashboard (chart icon); add a book/graduation-cap SVG icon to `src/SemanticSearch.WebApi/Components/Layout/Icons.cs`
- [X] T021 [P] Add Study Hub routes to `src/SemanticSearch.WebApi/Components/Layout/BreadcrumbMap.cs` — add entries: "/study" → "Study Library", "/study/review" → "Review Cards", "/study/dashboard" → "Study Dashboard"

**Checkpoint**: `dotnet build` succeeds. Repositories instantiated via DI. Navigation shows Phase 5 "Study Hub" with 3 items. SM-2 engine ready. No pages or controllers yet.

---

## Phase 3: User Story 1 — Manage Study Library and Read PDFs (Priority: P1) 🎯 MVP

**Goal**: Upload PDF books, define chapters, read in embedded PDF.js viewer with last-read-page persistence. Complete CRUD for books and chapters.

**Independent Test**: Upload a PDF book, create chapters, navigate pages, close and reopen to verify last-read-page restores. Edit book metadata. Delete a book and confirm files removed.

### Implementation for User Story 1

- [X] T022 [P] [US1] Create PDF.js interop module in `src/SemanticSearch.WebApi/wwwroot/js/pdf-viewer.js` — window.studyPdfViewer namespace with functions: init(containerId, pdfUrl, initialPage, dotNetRef) to load PDF.js and render page into a canvas, renderPage(containerId, pageNumber) for navigation, getPageCount() returning int, destroy(containerId) for cleanup; DotNetObjectReference callback invocations: OnPageChanged(pageNumber) when user navigates
- [X] T023 [P] [US1] Implement AddBookCommand + handler in `src/SemanticSearch.Application/Study/Commands/AddBookCommand.cs` — command takes Title, Author?, Description?, IFormFile PdfFile; handler generates GUID, creates directory `data/study/books/{id}/`, saves PDF to disk, reads page count via PDF magic bytes or basic header parsing, inserts StudyBook via IStudyRepository, returns BookDetailResponse
- [X] T024 [P] [US1] Implement AddBookCommandValidator in `src/SemanticSearch.Application/Study/Validators/AddBookCommandValidator.cs` — validates Title required max 500, Author max 300, Description max 2000, PdfFile required with ContentType == application/pdf and Length ≤ 209_715_200, magic bytes check for %PDF- header
- [X] T025 [P] [US1] Implement UpdateBookCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdateBookCommand.cs` — command takes BookId, Title, Author?, Description?; handler fetches book, updates metadata via IStudyRepository, returns BookDetailResponse
- [X] T026 [P] [US1] Implement DeleteBookCommand + handler in `src/SemanticSearch.Application/Study/Commands/DeleteBookCommand.cs` — handler deletes book from DB (CASCADE removes chapters, plan items set null), deletes PDF file and audio directory from disk
- [X] T027 [P] [US1] Implement UpdateLastReadPageCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdateLastReadPageCommand.cs` — command takes BookId, Page (int); handler validates page ≤ PageCount, updates via IStudyRepository
- [X] T028 [P] [US1] Implement AddChapterCommand + handler in `src/SemanticSearch.Application/Study/Commands/AddChapterCommand.cs` — command takes BookId, Title, StartPage, EndPage; handler sets SortOrder based on existing chapter count, inserts via IStudyRepository, returns ChapterResponse
- [X] T029 [P] [US1] Implement UpdateChapterCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdateChapterCommand.cs` — command takes BookId, ChapterId, Title, StartPage, EndPage; handler updates via IStudyRepository
- [X] T030 [P] [US1] Implement DeleteChapterCommand + handler in `src/SemanticSearch.Application/Study/Commands/DeleteChapterCommand.cs` — handler deletes chapter from DB, removes audio file from disk if present, linked flashcards keep ChapterId=null, plan items set to Skipped
- [X] T031 [P] [US1] Implement UpdateChapterNotesCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdateChapterNotesCommand.cs` — command takes BookId, ChapterId, Notes (string?); handler updates chapter.Notes via IStudyRepository
- [X] T032 [P] [US1] Implement validators for chapter commands in `src/SemanticSearch.Application/Study/Validators/ChapterValidators.cs` — AddChapterCommandValidator (Title required max 500, StartPage ≥ 1, EndPage ≥ StartPage), UpdateChapterCommandValidator (same), UpdateChapterNotesCommandValidator (Notes max 10000)
- [X] T033 [P] [US1] Implement GetBookQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetBookQuery.cs` — fetches book + chapters from IStudyRepository, maps to BookDetailResponse
- [X] T034 [P] [US1] Implement ListBooksQuery + handler in `src/SemanticSearch.Application/Study/Queries/ListBooksQuery.cs` — fetches all books from IStudyRepository, counts chapters per book, maps to IReadOnlyList<BookSummaryResponse>
- [X] T035 [US1] Create StudyBooksController in `src/SemanticSearch.WebApi/Controllers/StudyBooksController.cs` — route prefix `api/study/books`; endpoints: GET (ListBooksQuery), POST multipart (AddBookCommand), GET {bookId} (GetBookQuery), PUT {bookId} (UpdateBookCommand), DELETE {bookId} (DeleteBookCommand), GET {bookId}/pdf (PhysicalFile serving), PATCH {bookId}/last-read-page (UpdateLastReadPageCommand), POST {bookId}/chapters (AddChapterCommand), PUT {bookId}/chapters/{chapterId} (UpdateChapterCommand), DELETE {bookId}/chapters/{chapterId} (DeleteChapterCommand), PUT {bookId}/chapters/{chapterId}/notes (UpdateChapterNotesCommand); each action ≤ 15 lines
- [X] T036 [US1] Add StudyBooks API proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — GetBooksAsync, GetBookAsync(id), AddBookAsync(multipart), UpdateBookAsync(id, request), DeleteBookAsync(id), GetBookPdfUrlAsync(id), UpdateLastReadPageAsync(id, page), AddChapterAsync(bookId, request), UpdateChapterAsync(bookId, chapterId, request), DeleteChapterAsync(bookId, chapterId), UpdateChapterNotesAsync(bookId, chapterId, notes)
- [X] T037 [P] [US1] Create PdfViewer.razor component in `src/SemanticSearch.WebApi/Components/Study/PdfViewer.razor` — parameters: string BookId, string PdfUrl, int InitialPage, int PageCount; on AfterRenderAsync invokes pdf-viewer.js init; previous/next page buttons and page number input; DotNetObjectReference callback OnPageChanged calls WorkspaceApiClient.UpdateLastReadPageAsync; dispose calls destroy
- [X] T038 [P] [US1] Create ChapterEditor.razor component in `src/SemanticSearch.WebApi/Components/Study/ChapterEditor.razor` — parameters: string BookId, IReadOnlyList<ChapterResponse> Chapters, EventCallback<int> OnChapterPageJump; renders chapter list with title, page range, edit/delete buttons; "Add Chapter" form with title, start page, end page; click chapter → invokes OnChapterPageJump(startPage); edit/delete chapter inline
- [X] T039 [US1] Create BookDetail.razor component in `src/SemanticSearch.WebApi/Components/Study/BookDetail.razor` — parameters: string BookId; fetches BookDetailResponse via WorkspaceApiClient; renders book metadata (editable title, author, description), PdfViewer with pdf URL and last-read page, ChapterEditor with chapters; chapter jump callback navigates PdfViewer; delete button with confirmation modal
- [X] T040 [US1] Create StudyLibrary.razor component in `src/SemanticSearch.WebApi/Components/Study/StudyLibrary.razor` — fetches books via WorkspaceApiClient.GetBooksAsync; renders grid of book cards (title, author, page count, chapter count); "Add Book" button opens upload form with InputFile accept=".pdf", title, author, description fields; on submit calls WorkspaceApiClient.AddBookAsync; click card navigates to BookDetail
- [X] T041 [US1] Create Study.razor page in `src/SemanticSearch.WebApi/Components/Pages/Study.razor` — @page "/study" and @page "/study/book/{BookId}" routes; when BookId is null shows StudyLibrary, when BookId is set shows BookDetail; uses existing page layout pattern
- [X] T042 [US1] Add Study Hub CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — .study-library-grid (responsive grid of book cards), .study-book-card (card with hover effect), .study-book-detail (split layout: PDF viewer + sidebar), .study-pdf-viewer (canvas container with navigation controls), .study-chapter-list (ordered chapter items with click-to-jump), .study-upload-form (file input + metadata fields), .study-page-nav (prev/next/jump controls below viewer)

**Checkpoint**: Full book CRUD operational. PDF upload, viewing, chapter definition, last-read-page persistence all functional. Navigate to `/study`, upload a PDF, create chapters, read and navigate pages, close and reopen to see restored position.

---

## Phase 4: User Story 2 — Create and Follow a Study Plan (Priority: P2)

**Goal**: Create study plans tied to books, auto-generate scheduled items from chapters, track progress via calendar and today views.

**Independent Test**: Create a plan for a book with 10 chapters over 2 weeks, verify auto-schedule distributes evenly (with skip-weekends option), mark items Done/Skipped, confirm progress percentage updates. Open calendar view to see items on correct dates.

### Implementation for User Story 2

- [X] T043 [P] [US2] Implement CreateStudyPlanCommand + handler in `src/SemanticSearch.Application/Study/Commands/CreateStudyPlanCommand.cs` — command takes Title, BookId?, StartDate, EndDate, SkipWeekends; handler creates plan with Status=Draft, inserts via IStudyRepository, returns StudyPlanDetailResponse
- [X] T044 [P] [US2] Implement AutoGeneratePlanItemsCommand + handler in `src/SemanticSearch.Application/Study/Commands/AutoGeneratePlanItemsCommand.cs` — command takes PlanId; handler fetches plan + book chapters, calculates available dates (excluding weekends if SkipWeekends), distributes chapters evenly using round-robin with ±1 tolerance, creates StudyPlanItem per chapter, bulk inserts, returns StudyPlanDetailResponse
- [X] T045 [P] [US2] Implement UpdatePlanItemStatusCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdatePlanItemStatusCommand.cs` — command takes PlanId, ItemId, Status; handler validates status transition, sets CompletedDate when status=Done, updates via IStudyRepository, returns PlanItemResponse
- [X] T046 [P] [US2] Implement validators for plan commands in `src/SemanticSearch.Application/Study/Validators/PlanValidators.cs` — CreateStudyPlanCommandValidator (Title required max 500, EndDate ≥ StartDate), UpdatePlanItemStatusCommandValidator (Status must be one of Pending/InProgress/Done/Skipped)
- [X] T047 [P] [US2] Implement GetStudyPlanQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetStudyPlanQuery.cs` — fetches plan + items from IStudyRepository, calculates progress percent (completed / non-skipped), maps to StudyPlanDetailResponse
- [X] T048 [P] [US2] Implement ListStudyPlansQuery + handler in `src/SemanticSearch.Application/Study/Queries/ListStudyPlansQuery.cs` — fetches all plans, counts items and completed per plan, maps to IReadOnlyList<StudyPlanSummaryResponse>
- [X] T049 [P] [US2] Implement GetTodayStudyItemsQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetTodayStudyItemsQuery.cs` — fetches plan items for today's date (Pending/InProgress) from IStudyRepository, gets due card count from IFlashCardRepository, maps to TodayStudyItemsResponse
- [X] T050 [P] [US2] Implement GetCalendarDataQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetCalendarDataQuery.cs` — takes year, month; fetches plan items for that month from IStudyRepository.GetCalendarItemsAsync, groups by date, maps to IReadOnlyList<CalendarDayResponse>
- [X] T051 [US2] Create StudyPlansController in `src/SemanticSearch.WebApi/Controllers/StudyPlansController.cs` — route prefix `api/study`; endpoints: GET plans (ListStudyPlansQuery), POST plans (CreateStudyPlanCommand), GET plans/{planId} (GetStudyPlanQuery), POST plans/{planId}/auto-generate (AutoGeneratePlanItemsCommand), PATCH plans/{planId}/items/{itemId}/status (UpdatePlanItemStatusCommand), GET today (GetTodayStudyItemsQuery), GET calendar?year&month (GetCalendarDataQuery); each action ≤ 15 lines
- [X] T052 [US2] Add StudyPlans API proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — GetStudyPlansAsync, CreateStudyPlanAsync(request), GetStudyPlanAsync(id), AutoGeneratePlanItemsAsync(planId), UpdatePlanItemStatusAsync(planId, itemId, status), GetTodayStudyItemsAsync, GetCalendarDataAsync(year, month)
- [X] T053 [P] [US2] Create StudyPlanBuilder.razor component in `src/SemanticSearch.WebApi/Components/Study/StudyPlanBuilder.razor` — form: title, book dropdown (from WorkspaceApiClient.GetBooksAsync), start/end date pickers, skip-weekends toggle; on submit creates plan; if book selected shows "Auto-Generate" button that calls auto-generate endpoint; displays generated items list
- [X] T054 [P] [US2] Create StudyPlanView.razor component in `src/SemanticSearch.WebApi/Components/Study/StudyPlanView.razor` — parameters: string PlanId; fetches StudyPlanDetailResponse; displays plan header (title, dates, status, progress bar), list of plan items grouped by date with status badges (Pending/InProgress/Done/Skipped), status change buttons per item, progress percentage
- [X] T055 [P] [US2] Create StudyCalendar.razor component in `src/SemanticSearch.WebApi/Components/Study/StudyCalendar.razor` — parameters: none (fetches internally); renders month navigation (prev/next month); 7-column CSS grid calendar with day cells; each cell shows date number and plan item pills (colored by status) from GetCalendarDataAsync; clicking an item navigates to its plan; uses existing design tokens
- [X] T056 [P] [US2] Create TodayStudyPanel.razor component in `src/SemanticSearch.WebApi/Components/Study/TodayStudyPanel.razor` — fetches TodayStudyItemsResponse; shows today's date header, list of due plan items with status change buttons, due flashcard count with "Start Review" link to /study/review; empty state "Nothing due today!"
- [X] T057 [US2] Integrate plan components into Study.razor page in `src/SemanticSearch.WebApi/Components/Pages/Study.razor` — add @page "/study/plans" and @page "/study/plans/{PlanId}" routes; add tab navigation within Study Hub: Library | Plans | Calendar | Today; Plans tab shows plan list + create button, clicking a plan shows StudyPlanView; Calendar tab shows StudyCalendar; Today tab shows TodayStudyPanel
- [X] T058 [US2] Add study plan CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — .study-plan-list (plan summary cards), .study-plan-progress (progress bar), .study-plan-item (item row with status badge and action buttons), .study-calendar (7-column grid), .study-calendar-cell (day cell with date header and item pills), .study-calendar-pill (small colored status pill), .study-today-panel (card with due items list), .study-tab-nav (tab navigation bar)

**Checkpoint**: Plans CRUD, auto-generation with even distribution and skip-weekends, calendar view, today panel all functional. Progress tracking works as items are marked Done/Skipped.

---

## Phase 5: User Story 3 — Review Flashcards with Spaced Repetition (Priority: P2)

**Goal**: Create decks, add cards (manually and via AI), review due cards with flip animation and SM-2 scheduling.

**Independent Test**: Create a deck with 5 cards, review all with quality 4, verify next-review dates are 1 day out. Review again next day with quality 5, verify intervals match SM-2 (6 days). Rate a card quality 1 and verify interval resets to 1. Test AI generation from chapter with notes.

### Implementation for User Story 3

- [X] T059 [P] [US3] Implement CreateDeckCommand + handler in `src/SemanticSearch.Application/Study/Commands/CreateDeckCommand.cs` — command takes Title, BookId?; handler inserts via IFlashCardRepository, returns DeckDetailResponse
- [X] T060 [P] [US3] Implement AddFlashCardCommand + handler in `src/SemanticSearch.Application/Study/Commands/AddFlashCardCommand.cs` — command takes DeckId, Front, Back, ChapterId?; handler creates card with defaults (Interval=0, Rep=0, EF=2.5, NextReviewDate=today), inserts via IFlashCardRepository, returns FlashCardResponse
- [X] T061 [P] [US3] Implement UpdateFlashCardCommand + handler in `src/SemanticSearch.Application/Study/Commands/UpdateFlashCardCommand.cs` — updates Front, Back, ChapterId; returns FlashCardResponse
- [X] T062 [P] [US3] Implement DeleteFlashCardCommand + handler in `src/SemanticSearch.Application/Study/Commands/DeleteFlashCardCommand.cs` — deletes card (CASCADE removes reviews)
- [X] T063 [P] [US3] Implement DeleteDeckCommand + handler in `src/SemanticSearch.Application/Study/Commands/DeleteDeckCommand.cs` — deletes deck (CASCADE removes all cards and their reviews)
- [X] T064 [P] [US3] Implement ReviewCardCommand + handler in `src/SemanticSearch.Application/Study/Commands/ReviewCardCommand.cs` — command takes CardId, Quality (0–5); handler fetches card, calls SpacedRepetitionEngine.Calculate, creates CardReview record with before/after values, updates card with new Interval/Repetitions/EaseFactor/NextReviewDate/LastReviewDate, returns ReviewResultResponse
- [X] T065 [P] [US3] Implement GenerateCardsFromChapterCommand + handler in `src/SemanticSearch.Application/Study/Commands/GenerateCardsFromChapterCommand.cs` — command takes DeckId, ChapterId; handler fetches chapter notes, validates non-empty, calls IAiAssistantModelProvider to get executor, sends flashcard generation prompt (from research.md R-004), collects full response, parses JSON array of {question, answer}, creates FlashCard per pair, bulk inserts via IFlashCardRepository, returns GeneratedCardsResponse; handles model unavailable (503) and JSON parse failure (400) gracefully
- [X] T066 [P] [US3] Implement validators for flashcard commands in `src/SemanticSearch.Application/Study/Validators/FlashCardValidators.cs` — CreateDeckCommandValidator (Title required max 500), AddFlashCardCommandValidator (Front/Back required max 5000), ReviewCardCommandValidator (Quality 0–5)
- [X] T067 [P] [US3] Implement GetDeckQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetDeckQuery.cs` — fetches deck + cards, counts due, maps to DeckDetailResponse
- [X] T068 [P] [US3] Implement ListDecksQuery + handler in `src/SemanticSearch.Application/Study/Queries/ListDecksQuery.cs` — fetches all decks with total card count and due count, maps to IReadOnlyList<DeckSummaryResponse>
- [X] T069 [P] [US3] Implement GetDueCardsQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetDueCardsQuery.cs` — fetches due cards across all decks for today, includes deck title, maps to DueCardsResponse
- [X] T070 [P] [US3] Implement GetReviewStatsQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetReviewStatsQuery.cs` — fetches retention rate (30 days), review forecast (30 days), recent review history (30 days) from IFlashCardRepository, maps to ReviewStatsResponse
- [X] T071 [US3] Create StudyCardsController in `src/SemanticSearch.WebApi/Controllers/StudyCardsController.cs` — route prefix `api/study`; endpoints: GET decks (ListDecksQuery), POST decks (CreateDeckCommand), GET decks/{deckId} (GetDeckQuery), DELETE decks/{deckId} (DeleteDeckCommand), POST decks/{deckId}/cards (AddFlashCardCommand), PUT decks/{deckId}/cards/{cardId} (UpdateFlashCardCommand), DELETE decks/{deckId}/cards/{cardId} (DeleteFlashCardCommand), POST decks/{deckId}/generate-from-chapter/{chapterId} (GenerateCardsFromChapterCommand), GET review/due (GetDueCardsQuery), POST review/{cardId} (ReviewCardCommand), GET review/stats (GetReviewStatsQuery); each action ≤ 15 lines
- [X] T072 [US3] Add StudyCards API proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — GetDecksAsync, CreateDeckAsync(request), GetDeckAsync(id), DeleteDeckAsync(id), AddFlashCardAsync(deckId, request), UpdateFlashCardAsync(deckId, cardId, request), DeleteFlashCardAsync(deckId, cardId), GenerateCardsFromChapterAsync(deckId, chapterId), GetDueCardsAsync, ReviewCardAsync(cardId, quality), GetReviewStatsAsync
- [X] T073 [P] [US3] Create flashcard flip interop module in `src/SemanticSearch.WebApi/wwwroot/js/flashcard.js` — window.studyFlashCard namespace with functions: flipCard(cardElementId) toggles .flipped CSS class with 3D transform animation, resetCard(cardElementId) removes .flipped class
- [X] T074 [P] [US3] Create DeckManager.razor component in `src/SemanticSearch.WebApi/Components/Study/DeckManager.razor` — fetches decks via WorkspaceApiClient.GetDecksAsync; grid of deck cards (title, book, total/due counts); "Create Deck" button with form (title, optional book dropdown); click deck navigates to deck detail; deck detail view shows card list with edit/delete per card, "Add Card" form (front, back, optional chapter dropdown)
- [X] T075 [P] [US3] Create CardEditor.razor component in `src/SemanticSearch.WebApi/Components/Study/CardEditor.razor` — parameters: string DeckId, FlashCardResponse? EditingCard, EventCallback OnSaved; form with Front (textarea), Back (textarea), optional ChapterId dropdown; handles both create and edit; "Generate from Chapter" button that calls AI generation endpoint and shows results
- [X] T076 [US3] Create FlashCardReview.razor component in `src/SemanticSearch.WebApi/Components/Study/FlashCardReview.razor` — fetches due cards via WorkspaceApiClient.GetDueCardsAsync; shows progress counter (e.g., "3 of 15"); renders current card front in flip-card container; click/tap flips via JS interop to show back; shows 6 quality rating buttons (0–5 with labels from ReviewQuality); on rate: calls ReviewCardAsync, shows result (new interval, next date), advances to next card; "All caught up" state when no cards due
- [X] T077 [US3] Create StudyReview.razor page in `src/SemanticSearch.WebApi/Components/Pages/StudyReview.razor` — @page "/study/review"; tab navigation: Review | Decks | Stats; Review tab shows FlashCardReview component; Decks tab shows DeckManager; Stats tab shows ReviewStats (placeholder, completed in US6)
- [X] T078 [US3] Add flashcard CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — .study-deck-grid (responsive deck cards), .study-deck-card (deck summary with due badge), .study-flash-card (perspective container for 3D flip), .study-flash-card-inner (transition transform with backface-visibility), .study-flash-card-front/.study-flash-card-back (card faces), .study-flash-card.flipped (rotateY 180deg), .study-rating-buttons (row of 6 quality buttons with color gradient green→red), .study-review-progress (counter bar), .study-card-editor (form layout for front/back textareas)
- [X] T079 [US3] Add due-card badge to NavMenu "Review Cards" item in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor` — on initialization query IFlashCardRepository.GetDueCardCountAsync via WorkspaceApiClient; display badge number on the Review Cards nav item using .sidebar-item-badge CSS; refresh count on 5-minute timer and after navigation to review page

**Checkpoint**: Full flashcard system operational. Create decks, add cards manually, AI-generate cards from chapter notes, review with flip animation and SM-2 scheduling, due-card badge in sidebar.

---

## Phase 6: User Story 4 — Attach and Listen to Chapter Audio (Priority: P3)

**Goal**: Upload audio files to chapters, play with speed controls, book-level playlist.

**Independent Test**: Upload an MP3 to a chapter, play it back, adjust speed to 1.5×, pause and resume. Open book audio playlist to see and play chapters with audio.

### Implementation for User Story 4

- [X] T080 [P] [US4] Implement UploadChapterAudioCommand + handler in `src/SemanticSearch.Application/Study/Commands/UploadChapterAudioCommand.cs` — command takes BookId, ChapterId, IFormFile AudioFile; handler creates directory `data/study/audio/{bookId}/`, saves file as `{chapterId}.{ext}`, updates chapter AudioFileName and AudioFilePath via IStudyRepository; replaces existing audio if present (deletes old file)
- [X] T081 [P] [US4] Implement UploadChapterAudioCommandValidator in `src/SemanticSearch.Application/Study/Validators/AudioValidators.cs` — validates AudioFile required, ContentType in (audio/mpeg, audio/wav, audio/mp4), Length ≤ 104_857_600 (100 MB)
- [X] T082 [US4] Add audio upload and serve endpoints to StudyBooksController in `src/SemanticSearch.WebApi/Controllers/StudyBooksController.cs` — POST {bookId}/chapters/{chapterId}/audio (multipart, UploadChapterAudioCommand), GET {bookId}/chapters/{chapterId}/audio (PhysicalFile serving with correct Content-Type from file extension)
- [X] T083 [US4] Add audio API proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — UploadChapterAudioAsync(bookId, chapterId, file), GetChapterAudioUrlAsync(bookId, chapterId)
- [X] T084 [P] [US4] Create AudioPlayer.razor component in `src/SemanticSearch.WebApi/Components/Study/AudioPlayer.razor` — parameters: string AudioUrl; renders HTML5 <audio> element with custom controls: play/pause button, progress bar (seekable), current time / duration display, speed dropdown (0.5×, 0.75×, 1×, 1.25×, 1.5×, 2×); JS interop for playbackRate changes; styled with existing design tokens
- [X] T085 [P] [US4] Create BookAudioPlaylist.razor component in `src/SemanticSearch.WebApi/Components/Study/BookAudioPlaylist.razor` — parameters: string BookId, IReadOnlyList<ChapterResponse> Chapters; filters chapters with HasAudio; renders sequential list with chapter title and play button; clicking chapter loads its audio URL into AudioPlayer; auto-advance to next chapter when current finishes
- [X] T086 [US4] Integrate audio components into BookDetail.razor in `src/SemanticSearch.WebApi/Components/Study/BookDetail.razor` — add "Attach Audio" upload button per chapter in ChapterEditor; show play icon on chapters with audio; add "Audio Playlist" tab/section that shows BookAudioPlaylist; AudioPlayer appears below PDF viewer or in a docked position when playing
- [X] T087 [US4] Add audio CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — .study-audio-player (custom audio controls bar), .study-audio-progress (seekable progress bar), .study-audio-speed (speed dropdown button), .study-audio-playlist (chapter list with play indicators), .study-audio-playing (highlight for currently playing chapter)

**Checkpoint**: Audio upload, playback with speed controls, and book-level playlist all functional. Upload MP3 to chapter, play at various speeds, navigate playlist.

---

## Phase 7: User Story 5 — Get Daily Study Reminders (Priority: P3)

**Goal**: Configurable daily Slack reminder summarizing due plan items and flashcards.

**Independent Test**: Enable study reminders in integration settings, have items due today, trigger the Hangfire job manually, verify Slack message arrives with correct counts. Disable reminders and verify no message sent.

### Implementation for User Story 5

- [X] T088 [P] [US5] Implement StudyReminderJob in `src/SemanticSearch.Infrastructure/BackgroundJobs/StudyReminderJob.cs` — Hangfire recurring job; queries IStudyRepository.GetPlanItemsByDateAsync(today) for Pending/InProgress items, IFlashCardRepository.GetDueCardCountAsync(today) for due cards; if both zero, skip silently; otherwise format message "📚 Study reminder: {N} chapters scheduled, {M} flashcards due for review today." and send via existing ISlackApiClient; log warning if Slack credentials not configured
- [X] T089 [US5] Register study-daily-reminder recurring job in `src/SemanticSearch.Infrastructure/BackgroundJobs/BackgroundJobRegistration.cs` — conditionally register based on user preference (stored in SQLite settings or a StudySettings credential entry); default: disabled; when enabled, schedule at user-configured time using Hangfire RecurringJob.AddOrUpdate with cron expression
- [X] T090 [US5] Add study reminder settings UI to integration settings page — add a "Study Reminders" section to the existing Integration settings page (wherever Slack/TFS credentials are configured); toggle enable/disable, time picker for reminder hour; save to SQLite settings; on save, update Hangfire recurring job schedule or remove if disabled
- [X] T091 [US5] Add study reminder preference storage — add study_reminder_enabled (boolean) and study_reminder_time (string HH:mm) to the existing credentials/settings storage pattern in `src/SemanticSearch.Infrastructure/Credentials/` or a new StudySettings table in SqliteSchemaInitializer; read on job execution and settings page load

**Checkpoint**: Study reminders configurable and functional. Toggle on, set time, verify Slack message. Toggle off, verify no message.

---

## Phase 8: User Story 6 — Track Study Sessions and View Analytics (Priority: P4)

**Goal**: Pomodoro timer, study session tracking, analytics dashboard with streak, weekly hours, retention rate, and per-book progress.

**Independent Test**: Start a Pomodoro session, wait for timer (or use short duration for testing), end session, verify dashboard shows time logged, 1-day streak, and weekly chart updated. Review some flashcards, verify retention rate appears.

### Implementation for User Story 6

- [X] T092 [P] [US6] Implement StartStudySessionCommand + handler in `src/SemanticSearch.Application/Study/Commands/StartStudySessionCommand.cs` — command takes SessionType, BookId?, ChapterId?, IsPomodoro, FocusDurationMinutes?; handler creates StudySession with StartedAt=now, inserts via IStudyRepository, returns StudySessionResponse
- [X] T093 [P] [US6] Implement EndStudySessionCommand + handler in `src/SemanticSearch.Application/Study/Commands/EndStudySessionCommand.cs` — command takes SessionId; handler fetches session, sets EndedAt=now, calculates DurationMinutes, updates via IStudyRepository, returns StudySessionResponse
- [X] T094 [P] [US6] Implement validators for session commands in `src/SemanticSearch.Application/Study/Validators/SessionValidators.cs` — StartStudySessionCommandValidator (SessionType must be Reading/Review/Listening; FocusDurationMinutes required if IsPomodoro, must be 1–120)
- [X] T095 [P] [US6] Implement GetStudyDashboardQuery + handler in `src/SemanticSearch.Application/Study/Queries/GetStudyDashboardQuery.cs` — aggregates: study streak days from IStudyRepository.GetStudyStreakDaysAsync, weekly chart from GetWeeklyStudyHoursAsync, total due cards from IFlashCardRepository.GetDueCardCountAsync, due plan items count, retention rate from GetRetentionRateAsync(30), per-book progress (completed vs total chapters across plans), maps to StudyDashboardResponse
- [X] T096 [US6] Create StudySessionsController in `src/SemanticSearch.WebApi/Controllers/StudySessionsController.cs` — route prefix `api/study`; endpoints: POST sessions (StartStudySessionCommand), PATCH sessions/{sessionId}/end (EndStudySessionCommand), GET dashboard (GetStudyDashboardQuery); each action ≤ 15 lines
- [X] T097 [US6] Add StudySessions API proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — StartStudySessionAsync(request), EndStudySessionAsync(sessionId), GetStudyDashboardAsync
- [X] T098 [P] [US6] Create Pomodoro timer interop module in `src/SemanticSearch.WebApi/wwwroot/js/study-timer.js` — window.studyTimer namespace with functions: startTimer(totalSeconds, dotNetRef) starts setInterval countdown updating visual, pauseTimer() pauses countdown, resumeTimer() resumes, getRemaining() returns seconds left, destroy() clears interval; on zero: play notification sound (inline AudioContext beep), invoke DotNet callback OnTimerComplete
- [X] T099 [P] [US6] Create PomodoroTimer.razor component in `src/SemanticSearch.WebApi/Components/Study/PomodoroTimer.razor` — parameters: EventCallback<StudySessionResponse> OnSessionComplete; circular countdown display (SVG circle with stroke-dashoffset animation), start/pause buttons, configurable focus duration (default 25 min) and break duration (default 5 min); on start: calls StartStudySessionAsync; JS interop starts visual countdown; on complete: calls EndStudySessionAsync, shows break prompt; maintains server-side session state so navigation doesn't lose timer
- [X] T100 [P] [US6] Create ReviewStats.razor component in `src/SemanticSearch.WebApi/Components/Study/ReviewStats.razor` — fetches ReviewStatsResponse via WorkspaceApiClient.GetReviewStatsAsync; displays retention rate percentage (large number), review forecast chart (due cards per day for next 30 days using Chart.js bar chart via existing JS interop), recent accuracy trend line
- [X] T101 [P] [US6] Create StudyAnalytics.razor component in `src/SemanticSearch.WebApi/Components/Study/StudyAnalytics.razor` — fetches StudyDashboardResponse; displays: study streak (flame icon + day count), weekly study hours bar chart (Chart.js via existing interop), today's due summary (card with plan items count + flashcard count), per-book progress bars (book title + chapters completed/total)
- [X] T102 [US6] Create StudyDashboardPage.razor page in `src/SemanticSearch.WebApi/Components/Pages/StudyDashboardPage.razor` — @page "/study/dashboard"; layout: top row = PomodoroTimer + TodayStudyPanel side by side; bottom row = StudyAnalytics (streak, weekly chart, book progress) + ReviewStats (retention, forecast); connects PomodoroTimer.OnSessionComplete to refresh dashboard data
- [X] T103 [US6] Wire ReviewStats into StudyReview.razor Stats tab in `src/SemanticSearch.WebApi/Components/Pages/StudyReview.razor` — replace Stats tab placeholder with ReviewStats component
- [X] T104 [US6] Add analytics CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — .study-pomodoro (circular timer with SVG), .study-pomodoro-controls (start/pause/reset buttons), .study-streak (flame icon with number), .study-dashboard-grid (responsive dashboard layout), .study-progress-bar (book progress bar fill), .study-analytics-card (metric card with large number and label), .study-chart-container (Chart.js wrapper)

**Checkpoint**: Pomodoro timer, sessions, and full analytics dashboard functional. Start session, complete timer, see streak and weekly hours update. Review stats show retention rate and forecast.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Integration refinements across all user stories

- [X] T105 [P] Ensure data directory creation on startup — add auto-creation of `data/study/books/` and `data/study/audio/` directories in `src/SemanticSearch.WebApi/Program.cs` if they don't exist, using existing ContentRootPath pattern
- [X] T106 [P] Add overlapping chapter page range warning in ChapterEditor.razor in `src/SemanticSearch.WebApi/Components/Study/ChapterEditor.razor` — when adding/editing a chapter, check if page range overlaps with existing chapters and show a yellow warning banner (not blocking)
- [X] T107 [P] Handle corrupted PDF edge case in PdfViewer.razor `src/SemanticSearch.WebApi/Components/Study/PdfViewer.razor` — if PDF.js init fails (corrupt/password-protected), show error message with option to re-upload or remove book
- [X] T108 [P] Handle "all caught up" state in FlashCardReview.razor `src/SemanticSearch.WebApi/Components/Study/FlashCardReview.razor` — when no cards are due, show "All caught up! Next review due on {date}" with countdown, fetched from the earliest NextReviewDate across all cards
- [ ] T109 Run quickstart.md validation — verify all checklist items from quickstart.md work end-to-end: upload book, create chapters, create plan, auto-generate, review cards, audio upload/play, Pomodoro session, dashboard analytics

**Checkpoint**: All edge cases handled, data directories auto-created, quickstart validation passes.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (entities, interfaces, DTOs, schema exist) — BLOCKS all user stories
- **US1 (Phase 3)**: Depends on Phase 2 — provides book/chapter foundation needed by all other stories
- **US2 (Phase 4)**: Depends on Phase 2 + books/chapters from US1
- **US3 (Phase 5)**: Depends on Phase 2 + books/chapters from US1 (for book association and AI generation from chapter notes)
- **US4 (Phase 6)**: Depends on Phase 2 + chapters from US1 (audio attaches to chapters)
- **US5 (Phase 7)**: Depends on Phase 2 + US2 (plan items) + US3 (due flashcards) for meaningful reminders
- **US6 (Phase 8)**: Depends on Phase 2 + US3 (review stats) + optionally US2 (plan progress for dashboard)
- **Polish (Phase 9)**: Depends on all desired user stories being complete

### User Story Dependencies

- **US1 (P1)**: Can start after Phase 2 — no dependencies on other stories. **MVP milestone.**
- **US2 (P2)**: Requires US1 books/chapters — can start after US1 T041 (Study.razor page exists with book data)
- **US3 (P2)**: Requires US1 books/chapters for book association — can start after US1 T035 (controller endpoints exist); AI generation needs chapter notes (T031)
- **US4 (P3)**: Requires US1 chapters — can start after US1 T035 (chapters endpoint)
- **US5 (P3)**: Requires US2 plan items + US3 due cards — start after both US2 and US3 are complete
- **US6 (P4)**: Requires US3 review stats — can start after US3 T070 (review stats query)

### Within Each User Story

- Commands + validators before controllers
- Queries before Blazor components that call them
- Controllers before WorkspaceApiClient proxy methods
- JS interop modules before Blazor components that use them
- Core components before page-level composition

### Parallel Opportunities

- All Phase 1 entity/VO/interface tasks (T001–T012) can run in parallel
- Phase 2: T016 and T017 (repositories) can run in parallel; T020 and T021 can run in parallel
- US1: All commands/queries (T023–T034) can run in parallel; then controller T035 → proxy T036
- US2: All commands/queries (T043–T050) can run in parallel; then controller T051 → proxy T052
- US3: All commands/queries (T059–T070) can run in parallel; then controller T071 → proxy T072
- US4: T080–T081 in parallel, then T082–T083 sequential, T084–T085 in parallel
- US6: T092–T095 in parallel, then T096–T097 sequential, T098–T101 in parallel

---

## Parallel Example: User Story 1

```bash
# Launch all commands + queries in parallel (different files):
T023: "AddBookCommand in Application/Study/Commands/AddBookCommand.cs"
T024: "AddBookCommandValidator in Application/Study/Validators/AddBookCommandValidator.cs"
T025: "UpdateBookCommand in Application/Study/Commands/UpdateBookCommand.cs"
T026: "DeleteBookCommand in Application/Study/Commands/DeleteBookCommand.cs"
T027: "UpdateLastReadPageCommand in Application/Study/Commands/UpdateLastReadPageCommand.cs"
T028: "AddChapterCommand in Application/Study/Commands/AddChapterCommand.cs"
T029: "UpdateChapterCommand in Application/Study/Commands/UpdateChapterCommand.cs"
T030: "DeleteChapterCommand in Application/Study/Commands/DeleteChapterCommand.cs"
T031: "UpdateChapterNotesCommand in Application/Study/Commands/UpdateChapterNotesCommand.cs"
T032: "Chapter validators in Application/Study/Validators/ChapterValidators.cs"
T033: "GetBookQuery in Application/Study/Queries/GetBookQuery.cs"
T034: "ListBooksQuery in Application/Study/Queries/ListBooksQuery.cs"
T037: "PdfViewer.razor in Components/Study/PdfViewer.razor"
T038: "ChapterEditor.razor in Components/Study/ChapterEditor.razor"

# Then sequential (depends on commands/queries existing):
T035: StudyBooksController
T036: WorkspaceApiClient proxy methods

# Then sequential (depends on controller + proxy):
T039: BookDetail.razor
T040: StudyLibrary.razor
T041: Study.razor page
T042: CSS styles
```

---

## Parallel Example: User Story 3

```bash
# Launch all commands + queries in parallel (different files):
T059–T070: All deck/card/review commands and queries

# JS interop in parallel with above:
T073: flashcard.js

# Then sequential:
T071: StudyCardsController
T072: WorkspaceApiClient proxy methods

# Then components (can parallel):
T074: DeckManager.razor
T075: CardEditor.razor

# Then sequential composition:
T076: FlashCardReview.razor
T077: StudyReview.razor page
T078: CSS styles
T079: NavMenu badge
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T015) — all entities, interfaces, DTOs, schema
2. Complete Phase 2: Foundational (T016–T021) — repositories, DI, SM-2 engine, navigation
3. Complete Phase 3: User Story 1 (T022–T042) — library, PDF viewer, chapters
4. **STOP and VALIDATE**: Upload a book, create chapters, read PDF, navigate, close/reopen
5. Delivers value as a personal PDF bookshelf with chapter organization

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. **US1** → Test independently → **MVP: PDF study library**
3. **US2** → Test independently → Study plans with calendar and scheduling
4. **US3** → Test independently → Flashcards with SM-2 spaced repetition + AI generation
5. **US4** → Test independently → Audio attachments and playlist
6. **US5** → Test independently → Daily Slack study reminders
7. **US6** → Test independently → Pomodoro timer + analytics dashboard
8. Polish → Edge cases, validation, quickstart verification

### Parallel Strategy

With capacity for parallel work:
1. Setup + Foundational completed sequentially (blocking)
2. US1 completed first (other stories depend on books/chapters)
3. After US1 completes:
   - Stream A: US2 (plans/calendar) → US5 (reminders, depends on US2+US3)
   - Stream B: US3 (flashcards) → US6 (analytics, depends on US3 stats)
   - Stream C: US4 (audio — largely independent after US1)
4. Polish after all stories complete

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks in the same phase
- [Story] label maps task to specific user story for traceability
- Each user story builds on the book/chapter foundation from US1
- Controllers split into 4 (StudyBooksController, StudyPlansController, StudyCardsController, StudySessionsController) per constitution I.4 (≤400 lines)
- Repositories split into 2 (SqliteStudyRepository, SqliteFlashCardRepository) per constitution I.4
- File paths use `ContentRootPath/data/study/` following existing project conventions
- PDF.js loaded from `wwwroot/lib/pdfjs/` — must be downloaded separately (see quickstart.md)
- Commit after each task or logical group; stop at any checkpoint to validate
