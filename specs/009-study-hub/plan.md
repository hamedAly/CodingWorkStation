# Implementation Plan: Study Hub

**Branch**: `009-study-hub` | **Date**: 2026-03-26 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/009-study-hub/spec.md`

## Summary

Personal study management system integrated as Phase 5 "Study Hub" in the existing Blazor sidebar. Core capabilities: PDF book library with embedded viewer, chapter-level study plan scheduling with auto-distribution, SM-2 spaced repetition flashcards with AI-powered card generation (via existing LLamaSharp), chapter audio attachments with speed-controlled player, Pomodoro study sessions, analytics dashboard, and daily Slack reminders (via existing Hangfire). Follows the established clean architecture pattern: Domain entities → Application MediatR commands/queries → Infrastructure SQLite persistence → WebApi controllers + Blazor Interactive Server components.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, LLamaSharp (existing), Microsoft.Data.Sqlite (existing), Hangfire (existing), PDF.js (new — client-side JS), Chart.js (existing)  
**Storage**: SQLite file database (existing `data/vectorstore.db`) — 8 new tables; local filesystem for PDF files (`data/study/books/`) and audio files (`data/study/audio/`)  
**Testing**: xUnit (existing test project pattern), manual verification for UI components  
**Target Platform**: Windows desktop (self-hosted Kestrel), modern browser (Blazor Interactive Server)  
**Project Type**: Web application (existing 4-project solution: Domain, Application, Infrastructure, WebApi)  
**Performance Goals**: Dashboard load < 3s with 1,000 cards; card flip + rating < 1s; audio playback start < 2s  
**Constraints**: Single-user personal tool; no cloud sync; PDF max 200 MB; audio max 100 MB; CPU-only AI inference  
**Scale/Scope**: ~35 new files, 8 new SQLite tables, 3 new navigation items, 1 new Hangfire job, 3 new JS interop modules

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Rule | Status | Notes |
|------|------|--------|-------|
| I.3 | Function ≤ 40 lines (soft), 60 (hard) | ✅ PASS | All handlers delegate to services; SM-2 engine is pure logic with small methods |
| I.4 | Class ≤ 400 lines (excl. tests) | ✅ PASS | `SqliteStudyRepository` is the largest new class; split into partial classes or separate repos if needed |
| II.3 | Feature-based folder structure | ✅ PASS | All study code in `Study/` folders per layer (Domain/Entities, Application/Study, Infrastructure/Study, Components/Study) |
| II.4 | Single Responsibility | ✅ PASS | Separate repository interfaces per concern (IStudyRepository, IFlashCardRepository); SpacedRepetitionEngine is pure logic |
| V.5 | Runtime validation on external boundaries | ✅ PASS | FluentValidation on all commands; file upload validation (type, size) on controller endpoints |
| XII.1 | Thin controllers (5–15 lines per action) | ✅ PASS | Controllers only map HTTP → MediatR command → HTTP response; no business logic |
| XII.6 | CQRS: one action → one command/query | ✅ PASS | Every endpoint dispatches exactly one MediatR request |
| XIII.1 | FluentValidation exclusively | ✅ PASS | All validation in Application layer validators; no DataAnnotations |
| XIII.3 | Automatic validation pipeline | ✅ PASS | Existing `ValidationBehavior<,>` pipeline handles all validators automatically |
| XIV.1 | Strict layer separation | ✅ PASS | Domain has zero dependencies; Application depends only on Domain; Infrastructure implements interfaces; WebApi depends on Application |
| XIV.2 | Controllers never know about database | ✅ PASS | Controllers inject only `IMediator` |
| XI.5 | Input validation everywhere | ✅ PASS | File upload: MIME type + extension + size; all commands: FluentValidation; page ranges: bounds checking |
| VII.2 | No hard-coded strings | ⚠️ NOTED | Personal tool — i18n is overkill; UI strings are in Blazor components directly (consistent with all existing features) |
| VII.1 | Design system compliance | ✅ PASS | Reuses existing CSS design tokens, card patterns, tab patterns, and component classes |

**Gate Result**: ✅ ALL GATES PASS — proceed to Phase 0.

### Post-Design Re-evaluation (after Phase 1)

| Gate | Rule | Status | Post-Design Notes |
|------|------|--------|-------------------|
| I.4 | Class ≤ 400 lines | ⚠️ RISK | `SqliteStudyRepository` implements ~30 methods across 2 interfaces. **Mitigation**: Split into `SqliteStudyRepository` (books, chapters, plans, sessions — ~20 methods) and `SqliteFlashCardRepository` (decks, cards, reviews — ~15 methods). Each stays under 400 lines. |
| I.4 | Class ≤ 400 lines | ⚠️ RISK | `StudyController` has 34 endpoints. At 10–15 lines per action + attributes, this reaches ~500 lines. **Mitigation**: Split into `StudyBooksController` (endpoints 1–13), `StudyPlansController` (endpoints 14–20), `StudyCardsController` (endpoints 21–31), `StudySessionsController` (endpoints 32–34). Each controller stays well under 400 lines. |
| XII.1 | Thin controllers | ✅ PASS | Each action remains 5–15 lines regardless of controller split. |
| II.4 | Single Responsibility | ✅ PASS | Splitting controller and repository by sub-domain improves SRP. |
| All other gates | — | ✅ PASS | No changes from pre-design evaluation. |

**Post-Design Gate Result**: ✅ ALL GATES PASS with mitigations applied. Updated project structure reflects the splits.

## Project Structure

### Documentation (this feature)

```text
specs/009-study-hub/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── study-api.md     # REST API contract definitions
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/
│   ├── Entities/
│   │   ├── StudyBook.cs
│   │   ├── StudyChapter.cs
│   │   ├── StudyPlan.cs
│   │   ├── StudyPlanItem.cs
│   │   ├── FlashCardDeck.cs
│   │   ├── FlashCard.cs
│   │   ├── CardReview.cs
│   │   └── StudySession.cs
│   ├── Interfaces/
│   │   ├── IStudyRepository.cs
│   │   └── IFlashCardRepository.cs
│   └── ValueObjects/
│       ├── ReviewQuality.cs
│       ├── StudyPlanStatus.cs
│       ├── PlanItemStatus.cs
│       └── StudySessionType.cs
│
├── SemanticSearch.Application/
│   └── Study/
│       ├── Commands/
│       │   ├── AddBookCommand.cs + Handler
│       │   ├── UpdateBookCommand.cs + Handler
│       │   ├── DeleteBookCommand.cs + Handler
│       │   ├── AddChapterCommand.cs + Handler
│       │   ├── UpdateChapterCommand.cs + Handler
│       │   ├── CreateStudyPlanCommand.cs + Handler
│       │   ├── AutoGeneratePlanItemsCommand.cs + Handler
│       │   ├── UpdatePlanItemStatusCommand.cs + Handler
│       │   ├── CreateDeckCommand.cs + Handler
│       │   ├── AddFlashCardCommand.cs + Handler
│       │   ├── ReviewCardCommand.cs + Handler
│       │   ├── GenerateCardsFromChapterCommand.cs + Handler
│       │   ├── StartStudySessionCommand.cs + Handler
│       │   └── EndStudySessionCommand.cs + Handler
│       ├── Queries/
│       │   ├── GetBookQuery.cs + Handler
│       │   ├── ListBooksQuery.cs + Handler
│       │   ├── GetStudyPlanQuery.cs + Handler
│       │   ├── ListStudyPlansQuery.cs + Handler
│       │   ├── GetTodayStudyItemsQuery.cs + Handler
│       │   ├── GetDueCardsQuery.cs + Handler
│       │   ├── GetDeckQuery.cs + Handler
│       │   ├── GetReviewStatsQuery.cs + Handler
│       │   └── GetStudyDashboardQuery.cs + Handler
│       ├── Validators/
│       │   └── [one validator per command]
│       ├── Services/
│       │   └── SpacedRepetitionEngine.cs
│       └── Models/
│           └── [response DTOs]
│
├── SemanticSearch.Infrastructure/
│   ├── Study/
│   │   ├── SqliteStudyRepository.cs
│   │   └── SqliteFlashCardRepository.cs
│   └── BackgroundJobs/
│       └── StudyReminderJob.cs
│
└── SemanticSearch.WebApi/
    ├── Controllers/
    │   ├── StudyBooksController.cs
    │   ├── StudyPlansController.cs
    │   ├── StudyCardsController.cs
    │   └── StudySessionsController.cs
    ├── Contracts/Study/
    │   └── StudyDtos.cs
    ├── Components/
    │   ├── Pages/
    │   │   ├── Study.razor
    │   │   ├── StudyReview.razor
    │   │   └── StudyDashboardPage.razor
    │   └── Study/
    │       ├── StudyLibrary.razor
    │       ├── BookDetail.razor
    │       ├── PdfViewer.razor
    │       ├── ChapterEditor.razor
    │       ├── StudyPlanBuilder.razor
    │       ├── StudyPlanView.razor
    │       ├── StudyCalendar.razor
    │       ├── TodayStudyPanel.razor
    │       ├── DeckManager.razor
    │       ├── FlashCardReview.razor
    │       ├── CardEditor.razor
    │       ├── ReviewStats.razor
    │       ├── AudioPlayer.razor
    │       ├── BookAudioPlaylist.razor
    │       ├── PomodoroTimer.razor
    │       └── StudyAnalytics.razor
    └── wwwroot/js/
        ├── pdf-viewer.js
        ├── flashcard.js
        └── study-timer.js

data/
├── study/
│   ├── books/{bookId}/    # Uploaded PDFs
│   └── audio/{bookId}/    # Uploaded audio files
```

**Structure Decision**: Extends the existing 4-project Clean Architecture solution. No new projects are created. All study code is organized in `Study/` sub-folders within each existing project, following the established feature-based folder pattern (same as `Quality/`, `Architecture/`, `Tfs/`).

## Complexity Tracking

> No constitution violations requiring justification. All gates pass.
