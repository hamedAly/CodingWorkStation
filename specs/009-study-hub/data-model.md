# Data Model: Study Hub

**Feature**: 009-study-hub  
**Date**: 2026-03-26

---

## Entities

### StudyBook

A study material unit backed by an uploaded PDF file.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| Title | string | Yes | NotEmpty, MaxLength(500) | Book title |
| Author | string? | No | MaxLength(300) | Book author |
| Description | string? | No | MaxLength(2000) | Optional description or notes |
| FileName | string | Yes | NotEmpty | Original uploaded PDF filename |
| FilePath | string | Yes | NotEmpty | Server-side path relative to `data/study/books/` |
| PageCount | int | Yes | > 0 | Total pages (read from PDF on upload) |
| LastReadPage | int | Yes | ≥ 1 | Last page the user was reading (default 1) |
| CreatedAt | DateTime | Yes | Auto-set | Upload timestamp (UTC) |
| UpdatedAt | DateTime | Yes | Auto-set | Last modification timestamp (UTC) |

**Relationships**: One-to-many with StudyChapter, StudyPlan, FlashCardDeck.

---

### StudyChapter

A logical section within a book defined by a page range. May have one audio attachment.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| BookId | string | Yes | FK → StudyBook.Id | Parent book |
| Title | string | Yes | NotEmpty, MaxLength(500) | Chapter title |
| StartPage | int | Yes | ≥ 1, ≤ PageCount | First page of chapter |
| EndPage | int | Yes | ≥ StartPage, ≤ PageCount | Last page of chapter |
| SortOrder | int | Yes | ≥ 0 | Display ordering within book |
| AudioFileName | string? | No | MaxLength(300) | Original uploaded audio filename (null if no audio) |
| AudioFilePath | string? | No | MaxLength(1000) | Server-side path relative to `data/study/audio/` |
| Notes | string? | No | MaxLength(10000) | User notes for this chapter (input for AI flashcard generation) |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |

**Relationships**: Belongs to StudyBook. One-to-many with FlashCard (optional association). One-to-many with StudyPlanItem (optional association).

---

### StudyPlan

A time-bounded schedule for working through study material.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| Title | string | Yes | NotEmpty, MaxLength(500) | Plan name |
| BookId | string? | No | FK → StudyBook.Id or null | Optional associated book |
| StartDate | DateTime | Yes | Valid date | Schedule start date |
| EndDate | DateTime | Yes | ≥ StartDate | Schedule end date |
| Status | string | Yes | One of: Draft, Active, Paused, Completed | Current plan status |
| SkipWeekends | bool | Yes | — | Whether auto-schedule avoids weekends |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |
| UpdatedAt | DateTime | Yes | Auto-set | Last modification timestamp (UTC) |

**Relationships**: Optional belongs-to StudyBook. One-to-many with StudyPlanItem.

---

### StudyPlanItem

A single scheduled task within a study plan.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| PlanId | string | Yes | FK → StudyPlan.Id | Parent plan |
| ChapterId | string? | No | FK → StudyChapter.Id or null | Optional linked chapter |
| Title | string | Yes | NotEmpty, MaxLength(500) | Item description (auto-filled from chapter title if linked) |
| ScheduledDate | DateTime | Yes | Valid date | Date this item is due |
| Status | string | Yes | One of: Pending, InProgress, Done, Skipped | Current item status |
| CompletedDate | DateTime? | No | Set when Status → Done | Actual completion date |
| SortOrder | int | Yes | ≥ 0 | Ordering within same day |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |

**Relationships**: Belongs to StudyPlan. Optional belongs-to StudyChapter.

---

### FlashCardDeck

A collection of flashcards grouped by topic.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| Title | string | Yes | NotEmpty, MaxLength(500) | Deck name |
| BookId | string? | No | FK → StudyBook.Id or null | Optional associated book |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |
| UpdatedAt | DateTime | Yes | Auto-set | Last modification timestamp (UTC) |

**Relationships**: Optional belongs-to StudyBook. One-to-many with FlashCard.

---

### FlashCard

A question-answer pair with SM-2 spaced repetition scheduling data.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| DeckId | string | Yes | FK → FlashCardDeck.Id | Parent deck |
| ChapterId | string? | No | FK → StudyChapter.Id or null | Optional chapter association |
| Front | string | Yes | NotEmpty, MaxLength(5000) | Question / prompt side |
| Back | string | Yes | NotEmpty, MaxLength(5000) | Answer side |
| Interval | int | Yes | ≥ 0, default 0 | Current review interval in days |
| Repetitions | int | Yes | ≥ 0, default 0 | Number of consecutive correct reviews |
| EaseFactor | double | Yes | ≥ 1.3, default 2.5 | SM-2 ease factor |
| NextReviewDate | DateTime | Yes | Default today | Next scheduled review date |
| LastReviewDate | DateTime? | No | — | Date of most recent review (null if never reviewed) |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |

**Relationships**: Belongs to FlashCardDeck. Optional belongs-to StudyChapter. One-to-many with CardReview.

---

### CardReview

A historical record of a single flashcard review attempt.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| CardId | string | Yes | FK → FlashCard.Id | Reviewed card |
| Quality | int | Yes | 0–5 | SM-2 quality rating |
| ReviewedAt | DateTime | Yes | Auto-set | Review timestamp (UTC) |
| PreviousInterval | int | Yes | ≥ 0 | Interval before this review |
| NewInterval | int | Yes | ≥ 0 | Interval after this review |
| PreviousEaseFactor | double | Yes | ≥ 1.3 | Ease factor before this review |
| NewEaseFactor | double | Yes | ≥ 1.3 | Ease factor after this review |

**Relationships**: Belongs to FlashCard.

---

### StudySession

A recorded period of active study.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Id | string | Yes | GUID, auto-generated | Primary key |
| BookId | string? | No | FK → StudyBook.Id or null | Optional associated book |
| ChapterId | string? | No | FK → StudyChapter.Id or null | Optional associated chapter |
| SessionType | string | Yes | One of: Reading, Review, Listening | Type of study activity |
| StartedAt | DateTime | Yes | Auto-set | Session start timestamp (UTC) |
| EndedAt | DateTime? | No | ≥ StartedAt | Session end timestamp (null if in progress) |
| DurationMinutes | int? | No | ≥ 0 | Computed: (EndedAt - StartedAt) in minutes |
| IsPomodoroSession | bool | Yes | Default false | Whether this was a Pomodoro focus session |
| FocusDurationMinutes | int? | No | > 0 | Pomodoro focus period length (null if not Pomodoro) |
| CreatedAt | DateTime | Yes | Auto-set | Creation timestamp (UTC) |

**Relationships**: Optional belongs-to StudyBook. Optional belongs-to StudyChapter.

---

## Value Objects

### ReviewQuality

Enum-like value object for SM-2 quality ratings.

| Value | Name | Description |
|-------|------|-------------|
| 0 | CompleteBlackout | Total failure to recall |
| 1 | Incorrect | Incorrect, but recognized answer on reveal |
| 2 | IncorrectEasy | Incorrect, but answer felt easy once seen |
| 3 | CorrectDifficult | Correct, but with significant difficulty |
| 4 | CorrectHesitation | Correct, with some hesitation |
| 5 | Perfect | Perfect recall, no hesitation |

### StudyPlanStatus

| Value | Description |
|-------|-------------|
| Draft | Plan created but not yet started |
| Active | Plan is being followed |
| Paused | Plan temporarily on hold |
| Completed | All items done or plan manually marked complete |

### PlanItemStatus

| Value | Description |
|-------|-------------|
| Pending | Not yet started |
| InProgress | Currently being worked on |
| Done | Completed (CompletedDate is set) |
| Skipped | Excluded from progress calculation |

### StudySessionType

| Value | Description |
|-------|-------------|
| Reading | PDF reading activity |
| Review | Flashcard review activity |
| Listening | Audio listening activity |

---

## SQLite Schema

All tables are created by `SqliteSchemaInitializer.cs` and accessed via `SqliteStudyRepository`.

```sql
CREATE TABLE IF NOT EXISTS StudyBooks (
    Id              TEXT PRIMARY KEY,
    Title           TEXT NOT NULL,
    Author          TEXT,
    Description     TEXT,
    FileName        TEXT NOT NULL,
    FilePath        TEXT NOT NULL,
    PageCount       INTEGER NOT NULL,
    LastReadPage    INTEGER NOT NULL DEFAULT 1,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE TABLE IF NOT EXISTS StudyChapters (
    Id              TEXT PRIMARY KEY,
    BookId          TEXT NOT NULL REFERENCES StudyBooks(Id) ON DELETE CASCADE,
    Title           TEXT NOT NULL,
    StartPage       INTEGER NOT NULL,
    EndPage         INTEGER NOT NULL,
    SortOrder       INTEGER NOT NULL DEFAULT 0,
    AudioFileName   TEXT,
    AudioFilePath   TEXT,
    Notes           TEXT,
    CreatedAt       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_StudyChapters_BookId ON StudyChapters(BookId);

CREATE TABLE IF NOT EXISTS StudyPlans (
    Id              TEXT PRIMARY KEY,
    Title           TEXT NOT NULL,
    BookId          TEXT REFERENCES StudyBooks(Id) ON DELETE SET NULL,
    StartDate       TEXT NOT NULL,
    EndDate         TEXT NOT NULL,
    Status          TEXT NOT NULL DEFAULT 'Draft',
    SkipWeekends    INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_StudyPlans_BookId ON StudyPlans(BookId);

CREATE TABLE IF NOT EXISTS StudyPlanItems (
    Id              TEXT PRIMARY KEY,
    PlanId          TEXT NOT NULL REFERENCES StudyPlans(Id) ON DELETE CASCADE,
    ChapterId       TEXT REFERENCES StudyChapters(Id) ON DELETE SET NULL,
    Title           TEXT NOT NULL,
    ScheduledDate   TEXT NOT NULL,
    Status          TEXT NOT NULL DEFAULT 'Pending',
    CompletedDate   TEXT,
    SortOrder       INTEGER NOT NULL DEFAULT 0,
    CreatedAt       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_StudyPlanItems_PlanId ON StudyPlanItems(PlanId);
CREATE INDEX IF NOT EXISTS IX_StudyPlanItems_ScheduledDate ON StudyPlanItems(ScheduledDate);

CREATE TABLE IF NOT EXISTS FlashCardDecks (
    Id              TEXT PRIMARY KEY,
    Title           TEXT NOT NULL,
    BookId          TEXT REFERENCES StudyBooks(Id) ON DELETE SET NULL,
    CreatedAt       TEXT NOT NULL,
    UpdatedAt       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_FlashCardDecks_BookId ON FlashCardDecks(BookId);

CREATE TABLE IF NOT EXISTS FlashCards (
    Id              TEXT PRIMARY KEY,
    DeckId          TEXT NOT NULL REFERENCES FlashCardDecks(Id) ON DELETE CASCADE,
    ChapterId       TEXT REFERENCES StudyChapters(Id) ON DELETE SET NULL,
    Front           TEXT NOT NULL,
    Back            TEXT NOT NULL,
    Interval        INTEGER NOT NULL DEFAULT 0,
    Repetitions     INTEGER NOT NULL DEFAULT 0,
    EaseFactor      REAL NOT NULL DEFAULT 2.5,
    NextReviewDate  TEXT NOT NULL,
    LastReviewDate  TEXT,
    CreatedAt       TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_FlashCards_DeckId ON FlashCards(DeckId);
CREATE INDEX IF NOT EXISTS IX_FlashCards_NextReviewDate ON FlashCards(NextReviewDate);

CREATE TABLE IF NOT EXISTS CardReviews (
    Id                  TEXT PRIMARY KEY,
    CardId              TEXT NOT NULL REFERENCES FlashCards(Id) ON DELETE CASCADE,
    Quality             INTEGER NOT NULL,
    ReviewedAt          TEXT NOT NULL,
    PreviousInterval    INTEGER NOT NULL,
    NewInterval         INTEGER NOT NULL,
    PreviousEaseFactor  REAL NOT NULL,
    NewEaseFactor       REAL NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_CardReviews_CardId ON CardReviews(CardId);
CREATE INDEX IF NOT EXISTS IX_CardReviews_ReviewedAt ON CardReviews(ReviewedAt);

CREATE TABLE IF NOT EXISTS StudySessions (
    Id                      TEXT PRIMARY KEY,
    BookId                  TEXT REFERENCES StudyBooks(Id) ON DELETE SET NULL,
    ChapterId               TEXT REFERENCES StudyChapters(Id) ON DELETE SET NULL,
    SessionType             TEXT NOT NULL,
    StartedAt               TEXT NOT NULL,
    EndedAt                 TEXT,
    DurationMinutes         INTEGER,
    IsPomodoroSession       INTEGER NOT NULL DEFAULT 0,
    FocusDurationMinutes    INTEGER,
    CreatedAt               TEXT NOT NULL
);

CREATE INDEX IF NOT EXISTS IX_StudySessions_StartedAt ON StudySessions(StartedAt);
CREATE INDEX IF NOT EXISTS IX_StudySessions_BookId ON StudySessions(BookId);
```

---

## State Transitions

### Study Plan Status

```
Draft → Active → Completed
  ↓       ↓ ↑
  └→ Active  Paused
```

| From | To | Trigger |
|------|----|---------|
| Draft | Active | User activates plan |
| Active | Paused | User pauses plan |
| Paused | Active | User resumes plan |
| Active | Completed | All non-skipped items Done, or user manually completes |
| Draft | _(deleted)_ | User deletes draft plan |

### Plan Item Status

```
Pending → InProgress → Done
   ↓          ↓
   Skipped    Skipped
```

| From | To | Trigger |
|------|----|---------|
| Pending | InProgress | User starts working on item |
| Pending | Done | User marks item complete directly |
| Pending | Skipped | User skips item |
| InProgress | Done | User completes item (sets CompletedDate) |
| InProgress | Skipped | User skips item |

### Flashcard Review Lifecycle

```
New Card (Interval=0, Rep=0, EF=2.5)
    │
    ▼
  Review
    │
    ├─ quality ≥ 3 ──→ Interval grows (SM-2 formula)
    │                    Rep increments
    │                    EF adjusts
    │                    NextReviewDate = today + new interval
    │
    └─ quality < 3 ──→ Interval resets to 1
                        Rep resets to 0
                        EF decreases (min 1.3)
                        NextReviewDate = tomorrow
```

**SM-2 interval progression for quality=4 (EF=2.5)**:
| Review # | Interval (days) |
|----------|----------------|
| 1st correct | 1 |
| 2nd correct | 6 |
| 3rd correct | 15 |
| 4th correct | 38 |
| 5th correct | 94 |

---

## Dashboard Aggregate Queries

These are read-only computed values, not stored entities.

| Metric | Query Description |
|--------|------------------|
| Due Cards Count | `SELECT COUNT(*) FROM FlashCards WHERE NextReviewDate <= date('now')` |
| Study Streak | Count consecutive days (backwards from today) where `StudySessions` has at least one entry |
| Weekly Hours | `SELECT date(StartedAt) as Day, SUM(DurationMinutes)/60.0 as Hours FROM StudySessions WHERE StartedAt >= date('now', '-7 days') GROUP BY Day` |
| Retention Rate | `SELECT COUNT(*) FILTER (WHERE Quality >= 3) * 100.0 / COUNT(*) FROM CardReviews WHERE ReviewedAt >= date('now', '-30 days')` |
| Review Forecast | For each of next 30 days: `SELECT COUNT(*) FROM FlashCards WHERE NextReviewDate = date('now', '+N days')` |
| Per-Book Progress | `SELECT COUNT(*) FILTER (WHERE Status = 'Done'), COUNT(*) FILTER (WHERE Status != 'Skipped') FROM StudyPlanItems WHERE PlanId IN (SELECT Id FROM StudyPlans WHERE BookId = ?)` |
| Today Due Items | `SELECT * FROM StudyPlanItems WHERE ScheduledDate = date('now') AND Status IN ('Pending','InProgress')` UNION due flashcards |
