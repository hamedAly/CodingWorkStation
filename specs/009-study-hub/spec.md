# Feature Specification: Study Hub

**Feature Branch**: `009-study-hub`  
**Created**: 2026-03-26  
**Status**: Draft  
**Input**: User description: "A complete personal study management system added as Phase 5 'Study Hub' in the sidebar navigation. It brings together PDF-based reading, chapter-level study planning, Anki-style spaced repetition, audio listening, Pomodoro sessions, and AI-powered flashcard generation — with scheduling reminders via Slack."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Manage Study Library and Read PDFs (Priority: P1)

As a self-learner, I upload PDF books to my personal study library, split them into chapters by defining page ranges, and read them in an embedded viewer that remembers where I left off — so I have a single place to organize and work through all my study materials.

**Why this priority**: Without a library of materials, no other feature (plans, flashcards, audio) has content to operate on. This is the foundation that every other capability depends on.

**Independent Test**: Can be fully tested by uploading a PDF, creating chapters, reading pages, closing the app, and reopening to verify the last-read page is restored. Delivers value as a standalone study bookshelf and reader.

**Acceptance Scenarios**:

1. **Given** the Study Hub is open and the library is empty, **When** I click "Add Book" and upload a PDF file with a title and author, **Then** the book appears in my library grid with its metadata and the PDF is stored on the server.
2. **Given** a book exists in the library, **When** I open its detail view, **Then** I see the embedded PDF viewer showing the first page (or my last-read page) and a list of defined chapters.
3. **Given** a book detail view is open, **When** I click "Add Chapter" and enter a title, start page, and end page, **Then** the chapter appears in the chapter list in order and I can click it to jump the PDF viewer to that start page.
4. **Given** I am reading a PDF at page 42, **When** I navigate away and later return to the same book, **Then** the viewer opens at page 42.
5. **Given** I attempt to upload a non-PDF file or a file larger than 200 MB, **When** the upload is submitted, **Then** the system rejects it with a clear error message explaining the allowed format and size limit.
6. **Given** a book exists in the library, **When** I edit its title, author, or description, **Then** the changes are saved and reflected immediately in the library grid and detail view.
7. **Given** a book has chapters, flashcard decks, and study plan items linked to it, **When** I delete the book, **Then** the system warns me about associated data and, upon confirmation, removes the book and all its dependent records.

---

### User Story 2 — Create and Follow a Study Plan (Priority: P2)

As a student preparing for an exam or working through a textbook, I create a study plan that distributes chapters across a date range and track my daily progress through a calendar view — so I stay on schedule and can see at a glance what I need to study today.

**Why this priority**: A structured schedule transforms a passive book collection into an active study program. Without planning, users rely on willpower alone; with it, the system guides daily habits.

**Independent Test**: Can be tested by creating a plan for a book with 10 chapters over 2 weeks, verifying the auto-generated schedule distributes items evenly, then marking items complete and checking that progress updates correctly. Delivers value as a standalone study scheduler.

**Acceptance Scenarios**:

1. **Given** a book with 10 chapters exists, **When** I create a study plan with a start date, end date, and select "auto-schedule", **Then** the system generates one plan item per chapter distributed evenly across the date range.
2. **Given** an auto-scheduled plan is generated, **When** I enable "skip weekends", **Then** no plan items are assigned to Saturday or Sunday and the workload is redistributed across weekdays.
3. **Given** an active study plan exists, **When** I open the calendar view, **Then** I see each day's scheduled items as visual entries on the corresponding calendar date.
4. **Given** it is today's date and I have items due, **When** I open the "Today" panel, **Then** I see a list of all chapters and review tasks due today across all active plans.
5. **Given** a plan item is scheduled for today, **When** I mark it as "Done", **Then** the item status updates, the completion date is recorded, and the overall plan progress percentage increases.
6. **Given** a plan item is no longer relevant, **When** I mark it as "Skipped", **Then** it is excluded from progress calculations but remains visible in the timeline for record-keeping.
7. **Given** I have multiple active plans, **When** I view the plan list, **Then** each plan shows its title, associated book (if any), date range, status, and percentage complete.

---

### User Story 3 — Review Flashcards with Spaced Repetition (Priority: P2)

As a learner who wants to retain what I study, I create flashcard decks (manually or auto-generated from chapter notes using AI), then review due cards in a flip-card interface where I rate my recall — so the system schedules reviews at optimal intervals and I remember material long-term.

**Why this priority**: Spaced repetition is the core differentiator that makes this more than a reading tracker. Retention without review decays rapidly; this feature directly addresses the user's Anki-like requirement and is equally important as scheduling.

**Independent Test**: Can be tested by creating a deck with 5 cards, reviewing them with various quality ratings, and verifying that next-review dates follow the SM-2 algorithm (cards rated well get longer intervals; cards rated poorly reset to 1 day). Delivers value as a standalone flashcard system.

**Acceptance Scenarios**:

1. **Given** I am on the deck manager screen, **When** I create a new deck with a title and optional book association, **Then** the empty deck appears in my deck list.
2. **Given** a deck exists, **When** I add a flashcard with a front (question) and back (answer), **Then** the card is added to the deck with a default review date of today.
3. **Given** I have 15 cards due for review across decks, **When** I open the review session, **Then** the first due card is shown front-face-up in a flip-card interface.
4. **Given** a card is showing its front, **When** I tap or click to flip it, **Then** the card animates to reveal the back (answer) and quality rating buttons (0–5) appear.
5. **Given** I rate a new card with quality 4 ("correct, easy recall"), **When** the rating is submitted, **Then** the card's next review is scheduled 1 day from now (first correct review), and the next due card is shown.
6. **Given** a card has been reviewed 3 times with quality ≥ 3 each time, **When** I review it again with quality 5, **Then** the interval increases according to the SM-2 formula (previous interval multiplied by ease factor) and exceeds 10 days.
7. **Given** a card with a 15-day interval, **When** I review it with quality 1 ("incorrect"), **Then** the interval resets to 1 day and the ease factor decreases (but never below 1.3).
8. **Given** a chapter has notes attached, **When** I click "Generate Cards" on that chapter, **Then** the AI generates question-answer pairs from the notes and adds them to a new or existing deck for my review.

---

### User Story 4 — Attach and Listen to Chapter Audio (Priority: P3)

As a learner who commutes or prefers auditory learning, I attach audio recordings (lectures, audiobook chapters, personal recordings) to each chapter and listen to them through an in-app player with speed controls — so I can study hands-free and reinforce reading with listening.

**Why this priority**: Audio support extends the study experience beyond screen time. It is a valuable enhancement but depends on the library foundation and is not required for the core study loop of reading → planning → reviewing.

**Independent Test**: Can be tested by uploading an audio file to a chapter, playing it back, adjusting speed to 1.5×, pausing, resuming, and verifying playback position is maintained. Delivers value as an audio companion to any book.

**Acceptance Scenarios**:

1. **Given** a chapter exists for a book, **When** I click "Attach Audio" and upload an MP3, WAV, or M4A file (up to 100 MB), **Then** the audio file is stored on the server and a play button appears next to the chapter.
2. **Given** a chapter has audio attached, **When** I click the play button, **Then** the audio player opens with play/pause, a progress bar, current time/duration, and speed controls (0.5×, 0.75×, 1×, 1.25×, 1.5×, 2×).
3. **Given** I am listening at 1× speed, **When** I change the speed to 1.5×, **Then** playback continues from the same position at the new speed without interruption.
4. **Given** a book has audio attached to 5 out of 8 chapters, **When** I open the book audio playlist, **Then** I see a sequential list of all chapters with audio, and I can play them in order or jump to any chapter.
5. **Given** I attempt to upload a file that is not MP3, WAV, or M4A, or exceeds 100 MB, **When** the upload is submitted, **Then** the system rejects it with a clear error describing allowed formats and size.

---

### User Story 5 — Get Daily Study Reminders (Priority: P3)

As a busy professional who studies in my spare time, I receive a daily notification summarizing what I should study today (scheduled chapters and due flashcards) — so I stay accountable and never forget a review session.

**Why this priority**: Notifications are the glue that turns a passive tool into an active habit system. However, the underlying plan and review systems must exist first, making this dependent on P2 features.

**Independent Test**: Can be tested by enabling study reminders in integration settings, having some items due today, triggering the reminder job manually, and verifying a Slack message arrives with the correct count of due items. Delivers value as a daily accountability nudge.

**Acceptance Scenarios**:

1. **Given** study reminders are enabled in integration settings, **When** the configured reminder time arrives (e.g., 8:00 AM daily), **Then** a Slack message is sent summarizing today's due study plan items and due flashcards.
2. **Given** I have 3 chapters scheduled and 12 flashcards due today, **When** the reminder fires, **Then** the message reads something like "📚 Study reminder: 3 chapters scheduled, 12 flashcards due for review today."
3. **Given** I have no study items or flashcards due today, **When** the reminder fires, **Then** no message is sent (avoids notification fatigue).
4. **Given** study reminders are disabled in settings, **When** the scheduled time arrives, **Then** no message is sent.
5. **Given** the reminder settings page is open, **When** I toggle the study reminder on and set a time, **Then** the preference is saved and the recurring job is updated to fire at the new time.

---

### User Story 6 — Track Study Sessions and View Analytics (Priority: P4)

As a learner who wants to build a consistent study habit, I start timed study sessions (including Pomodoro-style focus intervals), track how much time I spend studying each day, and view dashboards showing my streaks, weekly hours, review accuracy, and per-book progress — so I can see my growth and stay motivated.

**Why this priority**: Analytics and session tracking are motivational features that enhance the experience but are not required for the core learning loop. The system works without them; they add the gamification and self-awareness layer.

**Independent Test**: Can be tested by starting a Pomodoro session, completing a 25-minute focus period, ending the session, and verifying the dashboard shows 25 minutes logged for today, a 1-day streak, and updated weekly chart. Delivers value as a study time tracker and motivational dashboard.

**Acceptance Scenarios**:

1. **Given** I am on the study dashboard, **When** I start a Pomodoro session linked to a specific chapter, **Then** a circular countdown timer starts from 25 minutes and counts down to zero.
2. **Given** a Pomodoro focus period completes, **When** the timer reaches zero, **Then** a sound notification plays and the system prompts me to take a 5-minute break or continue with another focus period.
3. **Given** I have completed a study session, **When** I end the session, **Then** the total duration is recorded along with the associated book and chapter (if any), and the session appears in my history.
4. **Given** I have studied every day for 7 consecutive days, **When** I view the dashboard, **Then** my study streak displays "7 days" and a streak-specific visual indicator is shown.
5. **Given** I have multiple study sessions this week, **When** I view the weekly chart, **Then** a bar chart shows study hours per day for the current week.
6. **Given** I have reviewed 200 flashcards over the past month, **When** I view review analytics, **Then** I see my retention rate (percentage of cards rated ≥ 3), a forecast of upcoming due reviews for the next 30 days, and an accuracy trend line.
7. **Given** I have 3 books with study plans, **When** I view the dashboard, **Then** each book shows a progress bar indicating chapters completed vs. total chapters.

---

### Edge Cases

- What happens when a user uploads a corrupted or password-protected PDF? — The system attempts to load the first page; if it fails, it displays an error message and allows the user to re-upload or remove the file.
- What happens when a user defines overlapping chapter page ranges (e.g., Chapter 1: pages 1–20, Chapter 2: pages 15–30)? — The system allows overlapping ranges (chapters may share introductory material) but warns the user visually.
- What happens when the auto-schedule date range is shorter than the number of chapters? — The system assigns multiple chapters per day, distributing them as evenly as possible, and warns the user about the heavy workload per day.
- What happens when all flashcards have future review dates and none are due? — The review session shows an "All caught up! Next review due on [date]" message with a countdown.
- What happens when the AI model is unavailable or not loaded when generating flashcards? — The system shows a clear error ("AI model not available — please check model configuration") and offers manual card creation as a fallback.
- What happens when the user deletes a chapter that has flashcards and plan items linked to it? — The system warns about associated data, and upon confirmation, removes the chapter; linked flashcards remain in their deck (orphaned but still reviewable) and plan items are marked as "Skipped."
- What happens when Slack credentials are not configured but study reminders are enabled? — The reminder job skips sending and logs a warning; the settings page shows an alert that Slack must be configured for reminders to work.
- What happens when the Pomodoro timer is running and the user navigates away from the page? — The timer continues in the background (server-side tracking); the user can return and see the remaining time or see that the session completed while they were away.

## Requirements *(mandatory)*

### Functional Requirements

#### Library & Reading

- **FR-001**: System MUST allow users to add books by providing a title, author (optional), description (optional), and uploading a PDF file.
- **FR-002**: System MUST store uploaded PDF files on the local filesystem and persist book metadata (title, author, description, page count, last-read page) in the database.
- **FR-003**: System MUST render PDF files in an embedded in-app viewer with page-by-page navigation (previous/next, jump to page number).
- **FR-004**: System MUST persist the last-read page per book and restore it when the user returns to that book.
- **FR-005**: System MUST allow users to define chapters within a book by specifying a title, start page, and end page.
- **FR-006**: System MUST display chapters in order and allow the user to click a chapter to jump the PDF viewer to its start page.
- **FR-007**: System MUST validate uploaded files: only PDF format, maximum 200 MB file size.
- **FR-008**: System MUST allow users to edit book metadata (title, author, description) and chapter definitions (title, page ranges) after creation.
- **FR-009**: System MUST allow users to delete books, with a confirmation prompt that warns about associated chapters, flashcards, plans, and audio data.

#### Study Planning

- **FR-010**: System MUST allow users to create study plans with a title, optional book association, start date, and end date.
- **FR-011**: System MUST auto-generate plan items by distributing a book's chapters evenly across the plan's date range when the user chooses "auto-schedule."
- **FR-012**: Auto-scheduling MUST support a "skip weekends" option that avoids assigning items on Saturday and Sunday.
- **FR-013**: System MUST allow users to manually add, edit, reorder, and remove plan items within a plan.
- **FR-014**: System MUST allow users to mark plan items as Done (recording completion date), Skipped, or In Progress.
- **FR-015**: System MUST calculate and display plan progress as a percentage of completed items out of total non-skipped items.
- **FR-016**: System MUST provide a "Today" view showing all plan items and flashcard reviews due on the current date across all active plans.
- **FR-017**: System MUST provide a calendar view showing scheduled plan items per day for the selected month.
- **FR-018**: System MUST support plan statuses: Draft, Active, Paused, and Completed, with the ability to transition between them.

#### Spaced Repetition

- **FR-019**: System MUST allow users to create flashcard decks with a title and optional book association.
- **FR-020**: System MUST allow users to add flashcards to a deck with a front (question) and back (answer) in plain text, and an optional chapter association.
- **FR-021**: System MUST implement the SM-2 spaced repetition algorithm to schedule card reviews: quality ≥ 3 increases the interval; quality < 3 resets to 1 day; ease factor is bounded at a minimum of 1.3.
- **FR-022**: System MUST present due flashcards in a flip-card review interface: show front, user flips to see back, user rates recall quality on a 0–5 scale.
- **FR-023**: System MUST record each review attempt with the quality rating, previous and new interval, and previous and new ease factor.
- **FR-024**: System MUST support AI-powered flashcard generation: given chapter notes or content, the system sends a prompt to the local AI model and creates question-answer card pairs automatically.
- **FR-025**: System MUST allow users to edit or delete individual flashcards and entire decks.

#### Audio

- **FR-026**: System MUST allow users to attach one audio file per chapter by uploading MP3, WAV, or M4A files up to 100 MB each.
- **FR-027**: System MUST provide an in-app audio player with play, pause, progress seeking, time display, and playback speed control (0.5×, 0.75×, 1×, 1.25×, 1.5×, 2×).
- **FR-028**: System MUST provide a book-level audio playlist that plays chapter audio files in chapter order with ability to jump to any chapter.

#### Notifications

- **FR-029**: System MUST support a configurable daily study reminder that sends a Slack message summarizing due plan items and due flashcards.
- **FR-030**: System MUST allow the user to enable/disable study reminders and configure the reminder time from integration settings.
- **FR-031**: System MUST suppress the reminder message when no study items or flashcards are due (no empty notifications).
- **FR-032**: System MUST display a badge on the "Review Cards" navigation item showing the count of flashcards due for review today.

#### Sessions & Analytics

- **FR-033**: System MUST allow users to start, pause, and end timed study sessions, recording the duration, associated book/chapter, and session type (Reading, Review, Listening).
- **FR-034**: System MUST provide a Pomodoro timer with configurable focus duration (default 25 minutes) and break duration (default 5 minutes) that plays an audible notification when each period ends.
- **FR-035**: System MUST display a study dashboard showing: today's due items summary, current study streak (consecutive days with at least one session), weekly study hours chart, review forecast for the next 30 days, per-book progress, and overall retention rate.

### Key Entities

- **Book**: A study material unit with a title, author, description, uploaded PDF file, total page count, and last-read page. Contains zero or more chapters.
- **Chapter**: A logical section within a book defined by a title and page range (start page to end page). May have one attached audio file and one or more linked flashcards.
- **Study Plan**: A time-bounded schedule for working through material. Has a title, optional book association, start/end dates, and a status (Draft, Active, Paused, Completed). Contains ordered plan items.
- **Plan Item**: A single scheduled task within a plan. Linked to a specific date and optionally to a chapter. Has a status (Pending, In Progress, Done, Skipped) and a completion date.
- **Flashcard Deck**: A collection of flashcards grouped by topic, with an optional book association. Contains zero or more cards.
- **Flashcard**: A question-answer pair belonging to a deck. Carries spaced repetition scheduling data: current interval (days), repetition count, ease factor, next review date, and last review date.
- **Card Review**: A historical record of a single flashcard review attempt. Records the quality rating, date, and the before/after values of interval and ease factor.
- **Study Session**: A recorded period of active study. Captures start time, end time, duration, session type (Reading, Review, Listening), and optional associations to a book, chapter, or plan item.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload a book, define chapters, and begin reading within 3 minutes of first opening the Study Hub.
- **SC-002**: Auto-generated study plans distribute chapters across the date range with no more than ±1 item difference between any two days (even distribution).
- **SC-003**: The spaced repetition system correctly implements SM-2: a card reviewed 5 times consecutively with quality 5 reaches an interval of at least 30 days.
- **SC-004**: Flashcard review sessions handle 100+ due cards without noticeable delay — each card flip and rating submission completes in under 1 second from the user's perspective.
- **SC-005**: AI-generated flashcards produce at least 3 meaningful question-answer pairs from a chapter with 500+ words of notes.
- **SC-006**: Audio playback starts within 2 seconds of pressing play, and speed changes apply instantly without restarting the track.
- **SC-007**: Study reminders arrive within 5 minutes of the configured reminder time when Slack credentials are valid.
- **SC-008**: The study dashboard loads and displays all analytics (streak, charts, progress, forecast) within 3 seconds for a user with up to 1,000 flashcards and 50 study sessions.
- **SC-009**: Users who follow their auto-generated study plan and review due flashcards daily achieve a measured retention rate (cards rated ≥ 3) above 80% after 30 days of consistent use.
- **SC-010**: Navigation to the Study Hub is accessible from the sidebar in a single click, with all sub-sections (Library, Review, Dashboard) reachable within 2 clicks.

## Assumptions

- The user operates the application as a single-user personal tool; there is no multi-user authentication or collaboration requirement.
- Slack integration for study reminders reuses the existing Slack credentials already configured in the application's Integration Settings — no separate Slack setup is needed.
- The local AI model (already available for code quality features) is suitable for generating study flashcards from textual chapter notes. If the model is not loaded, AI card generation degrades gracefully to manual-only.
- PDF files are standard, non-encrypted, renderable PDFs. Password-protected or DRM-restricted PDFs are out of scope.
- Audio files are standard consumer formats (MP3, WAV, M4A) that can be played natively by modern browsers.
- The Pomodoro timer tracks sessions server-side so that navigating away from the page does not lose the active session.
- Study data (books, plans, cards, sessions) is stored locally and is not synced to any external service or cloud.
- Data retention is indefinite — the user manages their own data lifecycle by deleting books and plans they no longer need.
