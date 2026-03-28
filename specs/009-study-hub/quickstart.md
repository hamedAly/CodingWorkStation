# Quickstart: Study Hub

**Feature**: 009-study-hub  
**Date**: 2026-03-26

---

## Prerequisites

1. **.NET 10 SDK** installed
2. **Tailwind CSS CLI** available at `tools/tailwindcss.exe` (already in repo)
3. **PDF.js distribution** placed in `src/SemanticSearch.WebApi/wwwroot/lib/pdfjs/` (see setup below)
4. **LLamaSharp model** at `models/llm/qwen2.5-coder-instruct.gguf` (already in repo — required for AI flashcard generation only)

## One-Time Setup

### PDF.js Installation

```bash
# Download PDF.js pre-built distribution (v4.x recommended)
# From: https://mozilla.github.io/pdf.js/getting_started/#download
# Extract to:
src/SemanticSearch.WebApi/wwwroot/lib/pdfjs/
├── pdf.min.mjs
├── pdf.worker.min.mjs
└── pdf.sandbox.min.mjs
```

### Data Directories

Created automatically on first upload, but can be pre-created:

```bash
mkdir -p data/study/books
mkdir -p data/study/audio
```

### Kestrel Configuration

The following is added to `Program.cs` to support large file uploads:

```csharp
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 209_715_200; // 200 MB
});
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = 209_715_200; // 200 MB
});
```

## Build & Run

```bash
cd src/SemanticSearch.WebApi
dotnet run
```

Navigate to `https://localhost:5001/study` (or the configured port).

## What to Verify

### Library & Reading (US1 — P1)
- [ ] Study Hub page loads from sidebar Phase 5 navigation
- [ ] "Add Book" button opens upload form (title, author, description, PDF file)
- [ ] PDF upload succeeds for valid files < 200 MB
- [ ] Non-PDF files and files > 200 MB are rejected with clear error
- [ ] Book appears in library grid after upload
- [ ] Clicking a book opens detail view with embedded PDF viewer
- [ ] PDF viewer shows first page (or last-read page on return)
- [ ] Page navigation (prev/next/jump) works
- [ ] Adding chapters with title/start/end page works
- [ ] Clicking a chapter jumps PDF viewer to that page
- [ ] Navigating away and returning restores last-read page
- [ ] Book metadata (title, author, description) can be edited
- [ ] Deleting a book shows confirmation warning, removes book + files

### Study Plans (US2 — P2)
- [ ] Create plan with title, optional book, start/end date
- [ ] Auto-generate distributes chapters evenly across date range
- [ ] "Skip weekends" excludes Saturday/Sunday from schedule
- [ ] Calendar view shows items on correct dates
- [ ] "Today" panel shows items due today across all plans
- [ ] Marking items Done/Skipped/InProgress updates status
- [ ] Progress percentage updates correctly (excludes skipped)
- [ ] Plans show correct status (Draft, Active, Paused, Completed)

### Spaced Repetition (US3 — P2)
- [ ] Create deck with title and optional book association
- [ ] Add flashcard with front/back text
- [ ] Review interface shows front, click-to-flip reveals back
- [ ] Quality rating buttons (0–5) appear after flip
- [ ] Rating 4+ on new card → next review in 1 day
- [ ] Rating < 3 → interval resets to 1 day
- [ ] Consecutive correct reviews increase interval per SM-2
- [ ] "Generate Cards" from chapter notes produces AI-generated cards
- [ ] AI unavailable shows error + manual fallback
- [ ] Cards and decks can be edited and deleted

### Audio (US4 — P3)
- [ ] Upload MP3/WAV/M4A to chapter (max 100 MB)
- [ ] Invalid format/size rejected with clear error
- [ ] Audio player with play/pause/seek/time display
- [ ] Speed control (0.5×–2×) changes speed without restart
- [ ] Book audio playlist shows chapters with audio in order

### Notifications (US5 — P3)
- [ ] Study reminder toggle in integration settings
- [ ] Configurable reminder time
- [ ] Slack message sent at configured time with due item counts
- [ ] No message sent when nothing is due
- [ ] No message when reminders disabled

### Sessions & Analytics (US6 — P4)
- [ ] Start Pomodoro session (25 min default) linked to chapter
- [ ] Circular countdown timer counts down
- [ ] Sound plays when timer reaches zero
- [ ] Ending session records duration in history
- [ ] Study streak shows consecutive days
- [ ] Weekly bar chart shows study hours per day
- [ ] Review analytics: retention rate, forecast, accuracy trend
- [ ] Per-book progress bars on dashboard

## API Endpoints to Test

```bash
# Books
GET    /api/study/books
POST   /api/study/books                        # multipart/form-data
GET    /api/study/books/{id}
PUT    /api/study/books/{id}
DELETE /api/study/books/{id}
GET    /api/study/books/{id}/pdf
PATCH  /api/study/books/{id}/last-read-page

# Chapters
POST   /api/study/books/{id}/chapters
PUT    /api/study/books/{id}/chapters/{chId}
DELETE /api/study/books/{id}/chapters/{chId}
PUT    /api/study/books/{id}/chapters/{chId}/notes
POST   /api/study/books/{id}/chapters/{chId}/audio    # multipart/form-data
GET    /api/study/books/{id}/chapters/{chId}/audio

# Plans
GET    /api/study/plans
POST   /api/study/plans
GET    /api/study/plans/{id}
POST   /api/study/plans/{id}/auto-generate
PATCH  /api/study/plans/{id}/items/{itemId}/status

# Today & Calendar
GET    /api/study/today
GET    /api/study/calendar?year=2026&month=4

# Flashcards
GET    /api/study/decks
POST   /api/study/decks
GET    /api/study/decks/{id}
DELETE /api/study/decks/{id}
POST   /api/study/decks/{id}/cards
PUT    /api/study/decks/{id}/cards/{cardId}
DELETE /api/study/decks/{id}/cards/{cardId}
POST   /api/study/decks/{id}/generate-from-chapter/{chId}

# Review
GET    /api/study/review/due
POST   /api/study/review/{cardId}
GET    /api/study/review/stats

# Sessions & Dashboard
POST   /api/study/sessions
PATCH  /api/study/sessions/{id}/end
GET    /api/study/dashboard
```

## Key Files

| File | Purpose |
|------|---------|
| **Domain** | |
| `Domain/Entities/StudyBook.cs` | Book entity |
| `Domain/Entities/StudyChapter.cs` | Chapter entity with audio reference |
| `Domain/Entities/StudyPlan.cs` | Plan entity |
| `Domain/Entities/StudyPlanItem.cs` | Plan item with status tracking |
| `Domain/Entities/FlashCardDeck.cs` | Deck entity |
| `Domain/Entities/FlashCard.cs` | Card with SM-2 scheduling fields |
| `Domain/Entities/CardReview.cs` | Review history record |
| `Domain/Entities/StudySession.cs` | Timed session record |
| `Domain/Interfaces/IStudyRepository.cs` | Books, chapters, plans, sessions persistence |
| `Domain/Interfaces/IFlashCardRepository.cs` | Decks, cards, reviews persistence |
| `Domain/ValueObjects/ReviewQuality.cs` | SM-2 quality rating (0–5) |
| **Application** | |
| `Application/Study/Commands/` | All MediatR commands + handlers |
| `Application/Study/Queries/` | All MediatR queries + handlers |
| `Application/Study/Validators/` | FluentValidation validators |
| `Application/Study/Services/SpacedRepetitionEngine.cs` | SM-2 algorithm (pure logic) |
| **Infrastructure** | |
| `Infrastructure/Study/SqliteStudyRepository.cs` | SQLite implementation of both repository interfaces |
| `Infrastructure/BackgroundJobs/StudyReminderJob.cs` | Hangfire daily Slack reminder |
| **WebApi** | |
| `WebApi/Controllers/StudyController.cs` | REST API (34 endpoints) |
| `WebApi/Contracts/Study/StudyDtos.cs` | All request/response DTOs |
| `WebApi/Components/Pages/Study.razor` | Main Study Hub page (library) |
| `WebApi/Components/Pages/StudyReview.razor` | Flashcard review page |
| `WebApi/Components/Pages/StudyDashboardPage.razor` | Analytics dashboard page |
| `WebApi/Components/Study/*.razor` | All Study sub-components |
| **JS Interop** | |
| `wwwroot/lib/pdfjs/` | PDF.js library distribution |
| `wwwroot/js/pdf-viewer.js` | PDF rendering interop |
| `wwwroot/js/flashcard.js` | Card flip animation interop |
| `wwwroot/js/study-timer.js` | Pomodoro countdown interop |
| **Data** | |
| `data/study/books/{bookId}/` | Uploaded PDF files |
| `data/study/audio/{bookId}/` | Uploaded audio files |

## SQLite Schema

8 new tables added to `SqliteSchemaInitializer.cs`:
- `StudyBooks`, `StudyChapters`
- `StudyPlans`, `StudyPlanItems`
- `FlashCardDecks`, `FlashCards`, `CardReviews`
- `StudySessions`

Full DDL is in [data-model.md](data-model.md#sqlite-schema).

## Hangfire Job

New recurring job `study-daily-reminder` registered in `BackgroundJobRegistration.cs`:
- Fires daily at user-configured time (default: disabled)
- Queries due plan items + due flashcards
- Sends summary via existing `ISlackApiClient`
- Skips silently when nothing is due or Slack not configured
