# Research: Study Hub

**Feature**: 009-study-hub  
**Date**: 2026-03-26  
**Status**: Complete — all unknowns resolved

## R-001: PDF Rendering in Blazor Interactive Server

**Decision**: PDF.js (Mozilla) loaded client-side via JS interop

**Rationale**: 
- PDF.js is the industry-standard open-source PDF renderer (used by Firefox)
- Client-side rendering avoids server CPU load for page rendering
- Supports page navigation, zoom, text selection, and annotations out of the box
- Works within Blazor Interactive Server via `IJSRuntime.InvokeVoidAsync`
- The project already uses this JS interop pattern for Vis.js, Chart.js, and Mermaid.js
- No NuGet package needed — load the pre-built JS/CSS from `wwwroot/lib/pdfjs/`

**Alternatives considered**:
- **Server-side PDF rendering (PdfSharp, iText7)**: Adds server load, cannot display interactive viewer, requires sending rendered images to client
- **`<embed>` / `<iframe>` with browser native**: No control over page navigation, no resume-reading persistence, inconsistent across browsers
- **Syncfusion/Telerik PDF viewers**: Commercial licenses, heavy bundle sizes, unnecessary for personal tool

**Integration pattern**:
```
wwwroot/lib/pdfjs/         → PDF.js distribution (pdf.js + pdf.worker.js)
wwwroot/js/pdf-viewer.js   → Custom interop module
  - window.studyPdfViewer.renderPage(containerId, pdfUrl, pageNumber)
  - window.studyPdfViewer.getPageCount(pdfUrl) → int
  - window.studyPdfViewer.destroy(containerId)

PdfViewer.razor → Blazor component
  - OnAfterRenderAsync: invoke renderPage
  - DotNetObjectReference callback: OnPageChanged → persist LastReadPage
```

**PDF file serving**: ASP.NET Core `PhysicalFileProvider` mapped to `data/study/books/` with a dedicated `GET /api/study/books/{bookId}/pdf` endpoint that validates bookId exists and returns the file with `Content-Type: application/pdf`. This avoids exposing the raw filesystem path.

---

## R-002: SM-2 Spaced Repetition Algorithm

**Decision**: Implement SM-2 (SuperMemo 2) as a pure C# static class

**Rationale**:
- SM-2 is the most widely documented and battle-tested spaced repetition algorithm (Piotr Wozniak, 1987)
- Anki's core scheduling is based on SM-2 (with modifications)
- Simple enough to implement correctly in ~30 lines of logic
- Pure function: `(currentCard, qualityRating) → (newInterval, newEaseFactor, newRepetitions, nextReviewDate)`
- No external dependencies; testable with standard xUnit assertions

**Algorithm specification**:
```
Input: quality q (0–5), current interval I, repetition count n, ease factor EF

If q ≥ 3 (correct response):
  If n = 0: I = 1
  If n = 1: I = 6
  If n ≥ 2: I = round(I × EF)
  n = n + 1

If q < 3 (incorrect):
  n = 0
  I = 1

EF = EF + (0.1 − (5 − q) × (0.08 + (5 − q) × 0.02))
EF = max(1.3, EF)

NextReviewDate = today + I days
```

**Alternatives considered**:
- **FSRS (Free Spaced Repetition Scheduler)**: More modern and accurate, but significantly more complex (requires neural network or lookup tables). Can be swapped in later without schema changes since the schema stores interval/EF/nextReviewDate generically.
- **Leitner System**: Box-based (1, 2, 4, 8 days). Simpler but less adaptive — doesn't account for individual card difficulty.
- **SM-18 (SuperMemo 18)**: Proprietary, poorly documented, overkill for personal use.

---

## R-003: File Upload Infrastructure for PDF and Audio

**Decision**: `IFormFile` with streaming upload to `data/study/` directories, explicit size limit configuration in `Program.cs`

**Rationale**:
- The project currently has no file upload endpoints — this is the first
- ASP.NET Core's default request body limit is 30 MB (Kestrel); PDFs can be up to 200 MB
- Must explicitly configure `FormOptions.MultipartBodyLengthLimit` and Kestrel `MaxRequestBodySize`
- Files stored under `ContentRootPath/data/study/` following the existing `ContentRootPath`-based path strategy
- Directory structure: `data/study/books/{bookId}/{filename}.pdf`, `data/study/audio/{bookId}/{chapterId}.{ext}`

**Configuration needed in Program.cs**:
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

**Validation strategy**:
- Server-side: Check `IFormFile.ContentType` against allowlist (`application/pdf`, `audio/mpeg`, `audio/wav`, `audio/mp4`)
- Server-side: Check `IFormFile.Length` against per-type limits (200 MB PDF, 100 MB audio)
- Server-side: Verify file header magic bytes (PDF: `%PDF-`, audio: format-specific)
- Client-side: Blazor `InputFile` component with `accept=".pdf"` / `accept=".mp3,.wav,.m4a"` attributes

**Alternatives considered**:
- **Streaming directly to disk (without IFormFile buffering)**: More memory-efficient for very large files, but adds complexity. 200 MB is within acceptable IFormFile range for a single-user tool.
- **Storing files in SQLite as BLOBs**: Poor performance for large files, bloats database, harder to serve.
- **External storage (S3, Azure Blob)**: Unnecessary for a personal local tool.

---

## R-004: AI-Powered Flashcard Generation

**Decision**: Reuse existing `IAiAssistantModelProvider` + `LlamaStreamingAssistantService` pattern with a new prompt builder for flashcards

**Rationale**:
- The project already has a fully working LLamaSharp integration with Qwen 2.5 Coder model
- The `IAiAssistantModelProvider` handles lazy model loading, thread-safe initialization, and executor creation
- Only need a new `IStudyAssistantService` interface with a `GenerateFlashCardsAsync` method
- Non-streaming approach is better for flashcards (need complete JSON response to parse into card entities)

**Prompt design**:
```
System: You are an expert educator. Given study notes, create flashcard question-answer pairs.
Return a JSON array of objects with "question" and "answer" fields.
Create concise, focused cards that test understanding, not just memorization.
Output ONLY valid JSON, no markdown formatting.

User: Create flashcards from these chapter notes:
Title: {chapterTitle}
Book: {bookTitle}
Notes:
{chapterNotes}

Generate between 3 and 10 flashcards depending on content density.
```

**Non-streaming approach**: Collect all tokens into a single string, then parse as JSON. This differs from the existing streaming pattern but is simpler for structured output. If JSON parsing fails, return an error and suggest manual card creation.

**Alternatives considered**:
- **Streaming + incremental JSON parsing**: Overly complex for this use case; flashcard generation is a batch operation, not interactive
- **External API (OpenAI, Claude)**: Requires API keys and network; the project philosophy is local-first
- **Regex-based extraction from text**: Fragile, low quality; AI generates much better question-answer pairs

---

## R-005: Audio Playback in Blazor

**Decision**: Native HTML5 `<audio>` element with CSS-styled custom controls via Blazor component

**Rationale**:
- MP3, WAV, and M4A are natively supported by all modern browsers via `<audio>`
- No JS library needed — HTML5 Audio API provides play/pause, currentTime, duration, playbackRate
- Speed control via `playbackRate` property (0.5–2.0) is supported natively
- Blazor can bind to audio events via JS interop for progress tracking
- Audio files served via `GET /api/study/chapters/{chapterId}/audio` endpoint (same pattern as PDF serving)

**Alternatives considered**:
- **Howler.js**: Full JS audio library — overkill for simple playback with speed control
- **Wavesurfer.js**: Waveform visualization — nice but unnecessary complexity
- **Server-side transcoding**: Not needed; browsers handle MP3/WAV/M4A natively

---

## R-006: Study Calendar UI

**Decision**: Custom lightweight calendar grid built with Blazor components and existing CSS design system

**Rationale**:
- The project avoids heavy JS libraries when Blazor components suffice (consistent with existing approach)
- A monthly calendar grid is a simple 7-column CSS grid with date cells
- Study plan items render as small colored pills within date cells
- No drag-and-drop needed (plan items are assigned via the plan builder, not the calendar)
- Reuses existing design tokens (`--accent`, `--surface`, `--radius-sm`, `.card`, `.status-pill`)

**Alternatives considered**:
- **FullCalendar.js**: Feature-rich but heavy (150 KB+), requires JS interop, adds dependency management overhead
- **Blazor third-party calendar (Radzen, MudBlazor)**: Introduces new component library dependency; project uses custom CSS design system

---

## R-007: Pomodoro Timer Architecture

**Decision**: Server-side timer tracking with JS interop for visual countdown and audio notification

**Rationale**:
- Blazor Interactive Server maintains a persistent SignalR circuit — server-side state survives page navigation
- Timer state (start time, duration, session type) stored in a scoped service or component state
- Visual countdown rendered via JS interop (`setInterval` in `study-timer.js`) for smooth 1-second ticks
- When timer completes, JS plays a notification sound and invokes a DotNet callback
- If user navigates away, server-side state knows the session is active; on return, JS resumes visual countdown from the remaining time

**Timer flow**:
```
User clicks "Start" → Blazor stores StartedUtc + DurationMinutes in component state
  → JS interop: startTimer(totalSeconds, dotNetRef)
  → JS setInterval(1s): update visual, invoke tick callback
  → When remaining = 0: play sound, invoke DotNet "OnTimerComplete"
  → DotNet handler: create StudySession record, prompt for break
```

**Alternatives considered**:
- **Pure server-side timer (System.Timers.Timer)**: Requires pushing updates via SignalR; less smooth visual updates, more server resources
- **Pure client-side timer**: Loses state on page navigation; no server-side session tracking
- **Web Workers**: Overkill; `setInterval` is sufficient for a single countdown

---

## R-008: Separate SqliteStudyRepository vs. Extending SqliteVectorStore

**Decision**: Create a new `SqliteStudyRepository` class implementing `IStudyRepository` and `IFlashCardRepository`

**Rationale**:
- `SqliteVectorStore` already implements 4+ interfaces (`IProjectWorkspaceRepository`, `IProjectFileRepository`, `IQualityRepository`, `IDependencyRepository`) and is ~800 lines
- Adding 8 more tables' worth of CRUD would push it well past the 400-line constitution limit
- A separate class with its own connection string (same database file) maintains single responsibility
- Registered as singleton in DI (same as `SqliteVectorStore`), sharing the same database path
- Schema initialization still happens in `SqliteSchemaInitializer` (single source of truth for all tables)

**Alternatives considered**:
- **Extending SqliteVectorStore**: Violates class length limit; harder to navigate; mixed concerns
- **Separate SQLite database file**: Unnecessary complexity; joins across databases are harder in SQLite
- **EF Core instead of raw SQLite**: Project uses raw `Microsoft.Data.Sqlite` everywhere; introducing EF Core for one feature creates inconsistency

---

## R-009: Navigation Badge for Due Cards

**Decision**: Query due-card count on `NavMenu` initialization and refresh via a lightweight periodic check

**Rationale**:
- `NavMenu.razor` is rendered once per circuit (Blazor Interactive Server)
- On initialization, query `IFlashCardRepository.GetDueCardCountAsync()` to get today's due count
- Display as a small badge number on the "Review Cards" navigation item (reuse `.sidebar-item-badge` CSS)
- Refresh every 5 minutes via `System.Threading.Timer` in the component (or on return from review page)
- Lightweight: single `SELECT COUNT(*) FROM FlashCards WHERE NextReviewDate <= date('now')` query

**Alternatives considered**:
- **Real-time SignalR push**: Overkill for a single-user tool; polling is sufficient
- **No badge (just show count on review page)**: Misses the "nudge" value of seeing due cards in the sidebar
