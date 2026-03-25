# Data Model: TFS Integration & Background Automation (Slack/Hangfire)

**Feature**: 007-tfs-slack-automation  
**Date**: 2026-03-25

## Overview

This feature introduces two categories of data:
1. **Persisted entities** — stored in the existing SQLite database (credentials, integration settings).
2. **Transient models** — fetched from external APIs at runtime, not persisted (work items, PRs, prayer times, contribution data).

---

## Persisted Entities (SQLite)

### TfsCredential

Stores the user's TFS/Azure DevOps connection details. Single row (one credential set per user).

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| CredentialId | TEXT | PK, default `"tfs-default"` | Fixed identifier for the single-user credential |
| ServerUrl | TEXT | NOT NULL | TFS/Azure DevOps server base URL (e.g., `https://tfs.company.com/tfs/DefaultCollection`) |
| EncryptedPat | TEXT | NOT NULL | PAT encrypted via ASP.NET Core Data Protection API |
| Username | TEXT | NULL | Optional display name / email for the TFS user |
| CreatedUtc | TEXT | NOT NULL | ISO 8601 timestamp of initial creation |
| UpdatedUtc | TEXT | NOT NULL | ISO 8601 timestamp of last update |

**SQL**:
```sql
CREATE TABLE IF NOT EXISTS TfsCredentials (
    CredentialId TEXT PRIMARY KEY,
    ServerUrl TEXT NOT NULL,
    EncryptedPat TEXT NOT NULL,
    Username TEXT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL
);
```

---

### SlackCredential

Stores the user's Slack integration details. Single row.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| CredentialId | TEXT | PK, default `"slack-default"` | Fixed identifier |
| EncryptedBotToken | TEXT | NOT NULL | Bot token (`xoxb-...`) encrypted via Data Protection API |
| EncryptedUserToken | TEXT | NULL | User token (`xoxp-...`) encrypted — needed for `users.profile.set` |
| DefaultChannel | TEXT | NOT NULL | Slack channel ID for standup messages (e.g., `C0123456789`) |
| CreatedUtc | TEXT | NOT NULL | ISO 8601 timestamp |
| UpdatedUtc | TEXT | NOT NULL | ISO 8601 timestamp |

**SQL**:
```sql
CREATE TABLE IF NOT EXISTS SlackCredentials (
    CredentialId TEXT PRIMARY KEY,
    EncryptedBotToken TEXT NOT NULL,
    EncryptedUserToken TEXT NULL,
    DefaultChannel TEXT NOT NULL,
    CreatedUtc TEXT NOT NULL,
    UpdatedUtc TEXT NOT NULL
);
```

---

### IntegrationSettings

Stores configurable settings for automations. Single row.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| SettingsId | TEXT | PK, default `"integration-default"` | Fixed identifier |
| StandupMessage | TEXT | NOT NULL, default `""` | Preset message content for daily standup |
| StandupEnabled | INTEGER | NOT NULL, default `1` | Whether standup job is active (0/1) |
| PrayerCity | TEXT | NULL | City name for Aladhan API (e.g., `"Cairo"`) |
| PrayerCountry | TEXT | NULL | Country name/code for Aladhan API (e.g., `"Egypt"`) |
| PrayerMethod | INTEGER | NOT NULL, default `5` | Calculation method (5 = Muslim World League) |
| PrayerEnabled | INTEGER | NOT NULL, default `1` | Whether prayer-time jobs are active (0/1) |
| UpdatedUtc | TEXT | NOT NULL | ISO 8601 timestamp |

**SQL**:
```sql
CREATE TABLE IF NOT EXISTS IntegrationSettings (
    SettingsId TEXT PRIMARY KEY,
    StandupMessage TEXT NOT NULL DEFAULT '',
    StandupEnabled INTEGER NOT NULL DEFAULT 1,
    PrayerCity TEXT NULL,
    PrayerCountry TEXT NULL,
    PrayerMethod INTEGER NOT NULL DEFAULT 5,
    PrayerEnabled INTEGER NOT NULL DEFAULT 1,
    UpdatedUtc TEXT NOT NULL
);
```

---

## Transient Models (API Responses — Not Persisted)

### TfsWorkItem

Fetched from TFS REST API via WIQL query. Displayed on the Kanban board.

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| Id | int | `System.Id` | TFS work item ID |
| Title | string | `System.Title` | Work item title |
| State | string | `System.State` | Current state (New, Active, Resolved, Closed) |
| WorkItemType | string | `System.WorkItemType` | "Task" or "Bug" |
| AssignedTo | string | `System.AssignedTo` | Display name of assigned user |
| AreaPath | string | `System.AreaPath` | Area/team path |
| Description | string | `System.Description` | HTML description body |
| Url | string | `_links.html.href` | Direct link to work item in TFS web UI |

---

### TfsPullRequest

Fetched from TFS Git Pull Requests API. Displayed on the PR Radar.

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| PullRequestId | int | `pullRequestId` | PR identifier |
| Title | string | `title` | PR title |
| SourceBranch | string | `sourceRefName` | Source branch (e.g., `refs/heads/feature/x`) |
| TargetBranch | string | `targetRefName` | Target branch (e.g., `refs/heads/main`) |
| Status | string | `status` | "active", "completed", "abandoned" |
| CreatedBy | string | `createdBy.displayName` | Author display name |
| CreationDate | DateTime | `creationDate` | When the PR was created |
| Url | string | `url` | Direct link to PR in TFS web UI |
| Reviewers | List&lt;PullRequestReviewer&gt; | `reviewers[]` | List of assigned reviewers |

### PullRequestReviewer

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| DisplayName | string | `displayName` | Reviewer's display name |
| Vote | int | `vote` | -10=rejected, -5=waiting, 0=no vote, 5=approved w/suggestions, 10=approved |
| VoteLabel | string | (computed) | Human-readable vote status |
| ImageUrl | string | `imageUrl` | Reviewer's avatar URL |

---

### ContributionDay

Aggregated from multiple TFS API sources. Displayed on the heatmap.

| Field | Type | Description |
|-------|------|-------------|
| Date | DateOnly | The calendar date |
| Count | int | Total contributions for that day (commits + work item updates + PR activity) |
| Level | int | Intensity level 0–4 for heatmap coloring |

---

### PrayerTimesResponse

Fetched from Aladhan API. Used to schedule individual prayer-time jobs.

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| Fajr | TimeOnly | `data.timings.Fajr` | Dawn prayer time |
| Dhuhr | TimeOnly | `data.timings.Dhuhr` | Noon prayer time |
| Asr | TimeOnly | `data.timings.Asr` | Afternoon prayer time |
| Maghrib | TimeOnly | `data.timings.Maghrib` | Sunset prayer time |
| Isha | TimeOnly | `data.timings.Isha` | Night prayer time |

---

## Entity Relationships

```
TfsCredential (1) ──uses──> TfsApiClient ──fetches──> TfsWorkItem (many)
                                          ──fetches──> TfsPullRequest (many)
                                          ──fetches──> ContributionDay (many)

SlackCredential (1) ──uses──> SlackApiClient ──posts──> Standup Message
                                              ──sets──> User Profile Status

IntegrationSettings (1) ──configures──> StandupJob (recurring)
                        ──configures──> PrayerTimeFetcherJob (recurring)

PrayerTimeFetcherJob ──fetches──> Aladhan API ──returns──> PrayerTimesResponse
                     ──schedules──> PrayerStatusUpdaterJob (5 per day)
```

## State Transitions

### Work Item States (Kanban Columns)

```
New → Active → Resolved → Closed
       ↑          ↓
       └──────────┘  (Reactivated)
```

### Pull Request Status

```
Active → Completed (merged)
       → Abandoned (closed without merge)
```

### Background Job Lifecycle

```
Scheduled → Processing → Succeeded
                       → Failed → (auto-retry up to 3×) → Failed (permanent)
```

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| TfsCredential | ServerUrl | Required, must be valid HTTP(S) URL |
| TfsCredential | PAT (pre-encryption) | Required, non-empty string |
| SlackCredential | BotToken (pre-encryption) | Required, must start with `xoxb-` |
| SlackCredential | UserToken (pre-encryption) | Optional, must start with `xoxp-` if provided |
| SlackCredential | DefaultChannel | Required, must start with `C` (channel ID format) |
| IntegrationSettings | StandupMessage | Required when StandupEnabled = true |
| IntegrationSettings | PrayerCity | Required when PrayerEnabled = true |
| IntegrationSettings | PrayerCountry | Required when PrayerEnabled = true |
| IntegrationSettings | PrayerMethod | Must be 1–15 (valid Aladhan calculation methods) |
