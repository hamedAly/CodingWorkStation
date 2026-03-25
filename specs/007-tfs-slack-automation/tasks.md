# Tasks: TFS Integration & Background Automation (Slack/Hangfire)

**Input**: Design documents from `/specs/007-tfs-slack-automation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not requested — test tasks omitted.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Install NuGet packages, add configuration options, wire up Hangfire and HttpClient factory in Program.cs

- [X] T001 Add Hangfire NuGet packages (Hangfire.Core, Hangfire.AspNetCore, Hangfire.InMemory) to `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj`
- [X] T002 Add Integration configuration options class to `src/SemanticSearch.Application/Common/IntegrationOptions.cs` with properties for DefaultPrayerMethod, StandupCron, PrayerFetchCron, TfsApiVersion
- [X] T003 [P] Add Integration section to `src/SemanticSearch.WebApi/appsettings.json` and `src/SemanticSearch.WebApi/appsettings.Development.json` under the SemanticSearch configuration
- [X] T004 Register Hangfire services (AddHangfire with UseInMemoryStorage, AddHangfireServer), bind IntegrationOptions, register named HttpClients (TfsClient, SlackClient, AladhanClient) via IHttpClientFactory, and add UseHangfireDashboard("/hangfire") in `src/SemanticSearch.WebApi/Program.cs`

**Checkpoint**: Application builds and starts with Hangfire dashboard accessible at `/hangfire`, no recurring jobs registered yet

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, repository interfaces, credential encryption, SQLite schema, and infrastructure implementations that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 [P] Create TfsCredential domain entity in `src/SemanticSearch.Domain/Entities/TfsCredential.cs` with properties: CredentialId, ServerUrl, EncryptedPat, Username, CreatedUtc, UpdatedUtc
- [X] T006 [P] Create SlackCredential domain entity in `src/SemanticSearch.Domain/Entities/SlackCredential.cs` with properties: CredentialId, EncryptedBotToken, EncryptedUserToken, DefaultChannel, CreatedUtc, UpdatedUtc
- [X] T007 [P] Create IntegrationSettings domain entity in `src/SemanticSearch.Domain/Entities/IntegrationSettings.cs` with properties: SettingsId, StandupMessage, StandupEnabled, PrayerCity, PrayerCountry, PrayerMethod, PrayerEnabled, UpdatedUtc
- [X] T008 [P] Create ICredentialRepository interface in `src/SemanticSearch.Domain/Interfaces/ICredentialRepository.cs` with methods for TFS and Slack credential CRUD (GetTfsCredentialAsync, SaveTfsCredentialAsync, DeleteTfsCredentialAsync, GetSlackCredentialAsync, SaveSlackCredentialAsync, DeleteSlackCredentialAsync)
- [X] T009 [P] Create IIntegrationSettingsRepository interface in `src/SemanticSearch.Domain/Interfaces/IIntegrationSettingsRepository.cs` with GetAsync and SaveAsync methods
- [X] T010 [P] Create ICredentialEncryption interface in `src/SemanticSearch.Domain/Interfaces/ICredentialEncryption.cs` with Encrypt and Decrypt methods
- [X] T011 Implement CredentialEncryption service using ASP.NET Core Data Protection API (IDataProtector with purpose "SemanticSearch.Credentials") in `src/SemanticSearch.Infrastructure/Credentials/CredentialEncryption.cs`
- [X] T012 Add credential and integration settings tables (TfsCredentials, SlackCredentials, IntegrationSettings) to the SQLite schema in `src/SemanticSearch.Infrastructure/VectorStore/SqliteSchemaInitializer.cs`
- [X] T013 Implement SqliteCredentialRepository (ICredentialRepository) with encrypted storage using ICredentialEncryption in `src/SemanticSearch.Infrastructure/Credentials/SqliteCredentialRepository.cs`
- [X] T014 Implement SqliteIntegrationSettingsRepository (IIntegrationSettingsRepository) in `src/SemanticSearch.Infrastructure/Credentials/SqliteIntegrationSettingsRepository.cs`
- [X] T015 Register all new services (ICredentialEncryption, ICredentialRepository, IIntegrationSettingsRepository, ITfsApiClient, ISlackApiClient, IAladhanApiClient) and named HttpClients in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

**Checkpoint**: Foundation ready — credential storage, encryption, and SQLite schema operational. User story implementation can begin.

---

## Phase 3: User Story 2 — Securely Store TFS Credentials (Priority: P1) 🎯 MVP

**Goal**: Users can save, update, test, and delete TFS server URL + PAT via the dashboard. Credentials stored encrypted in SQLite.

**Independent Test**: Navigate to Integration Settings page, enter TFS credentials, click Test Connection, verify success. Update PAT, verify new token is used. Delete credentials, verify removal.

### Implementation

- [X] T016 [P] [US2] Create TFS request/response contract DTOs (SaveTfsCredentialRequest, TfsCredentialStatusResponse, TestConnectionResponse, SaveCredentialResponse, DeleteCredentialResponse) in `src/SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs`
- [X] T017 [P] [US2] Create SaveTfsCredentialCommand with FluentValidation validator (ServerUrl required + valid URL, Pat required) in `src/SemanticSearch.Application/Tfs/Commands/SaveTfsCredential.cs`
- [X] T018 [P] [US2] Create DeleteTfsCredentialCommand with handler in `src/SemanticSearch.Application/Tfs/Commands/DeleteTfsCredential.cs`
- [X] T019 [P] [US2] Create GetTfsCredentialStatusQuery with handler in `src/SemanticSearch.Application/Tfs/Queries/GetTfsCredentialStatus.cs`
- [X] T020 [US2] Create TestTfsConnectionQuery with handler that decrypts stored PAT and calls TFS `/_apis/connectionData` endpoint in `src/SemanticSearch.Application/Tfs/Queries/TestTfsConnection.cs`
- [X] T021 [US2] Create ITfsApiClient interface with TestConnectionAsync method in `src/SemanticSearch.Domain/Interfaces/ITfsApiClient.cs`
- [X] T022 [US2] Implement TfsApiClient.TestConnectionAsync using IHttpClientFactory named client "TfsClient" with Basic Auth (empty user + PAT) in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs`
- [X] T023 [US2] Create TfsController with endpoints: GET/POST/DELETE /api/tfs/credentials plus POST /api/tfs/credentials/test using MediatR in `src/SemanticSearch.WebApi/Controllers/TfsController.cs`
- [X] T024 [US2] Create IntegrationSettings.razor Blazor page with TFS credential form (server URL, PAT, username), test connection button, save/delete actions calling WorkspaceApiClient in `src/SemanticSearch.WebApi/Components/Pages/IntegrationSettings.razor`

**Checkpoint**: TFS credentials can be saved, tested, updated, and deleted through the UI

---

## Phase 4: User Story 1 — View My Assigned Work Items on Kanban Board (Priority: P1)

**Goal**: Dashboard shows a "My Work" Kanban board with columns per work item state (New, Active, Resolved, Closed), each populated with work items assigned to the user from TFS.

**Independent Test**: With valid TFS credentials saved, navigate to My Work page, verify work items appear grouped by state. Click a work item to see details and TFS link.

### Implementation

- [X] T025 [P] [US1] Add WIQL query builder helper (builds SELECT query for assigned Tasks/Bugs) in `src/SemanticSearch.Infrastructure/Tfs/TfsQueryBuilder.cs`
- [X] T026 [P] [US1] Create WorkItemsResponse and WorkItemResponse contract DTOs in `src/SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs` (append to existing file)
- [X] T027 [US1] Add GetAssignedWorkItemsAsync method to ITfsApiClient interface in `src/SemanticSearch.Domain/Interfaces/ITfsApiClient.cs` — WIQL query followed by batch work item fetch
- [X] T028 [US1] Implement TfsApiClient.GetAssignedWorkItemsAsync: POST WIQL query to get IDs, then GET /\_apis/wit/workitems?ids=... to fetch details, parse JSON response into domain models in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs`
- [X] T029 [US1] Create GetMyWorkItemsQuery with handler that calls ITfsApiClient.GetAssignedWorkItemsAsync in `src/SemanticSearch.Application/Tfs/Queries/GetMyWorkItems.cs`
- [X] T030 [US1] Add GET /api/tfs/workitems endpoint to TfsController that sends GetMyWorkItemsQuery via MediatR and maps to WorkItemsResponse in `src/SemanticSearch.WebApi/Controllers/TfsController.cs`
- [X] T031 [US1] Add TFS work items API methods to WorkspaceApiClient (GetWorkItemsAsync) in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`
- [X] T032 [US1] Create MyWork.razor Kanban board Blazor page with state columns (New, Active, Resolved, Closed), work item cards showing title/type/area, click-to-expand detail panel with TFS link, loading/error/empty states in `src/SemanticSearch.WebApi/Components/Pages/MyWork.razor`

**Checkpoint**: "My Work" board shows assigned work items in state columns. Clicking a card shows detail with a link to TFS.

---

## Phase 5: User Story 4 — Background Job Dashboard (Priority: P2)

**Goal**: Hangfire dashboard accessible at `/hangfire` with recurring jobs visible. This phase registers the recurring job stubs.

**Independent Test**: Navigate to `/hangfire`, verify the dashboard loads and shows registered recurring jobs.

### Implementation

- [X] T033 [US4] Create BackgroundJobRegistration static class that registers recurring jobs (standup-daily with cron "30 9 * * MON-FRI", prayer-time-fetcher with cron "0 0 * * *") using RecurringJob.AddOrUpdate, called from Program.cs after app.Build() in `src/SemanticSearch.Infrastructure/BackgroundJobs/BackgroundJobRegistration.cs`
- [X] T034 [US4] Create stub StandupJob class with Execute method (logs "Standup job executed" — real implementation in US3) in `src/SemanticSearch.Infrastructure/BackgroundJobs/StandupJob.cs`
- [X] T035 [P] [US4] Create stub PrayerTimeFetcherJob class with Execute method (logs "Prayer fetcher executed" — real implementation in US6) in `src/SemanticSearch.Infrastructure/BackgroundJobs/PrayerTimeFetcherJob.cs`
- [X] T036 [US4] Call BackgroundJobRegistration.RegisterAll from Program.cs after app build, passing IServiceProvider for DI resolution in `src/SemanticSearch.WebApi/Program.cs`

**Checkpoint**: `/hangfire` dashboard shows two recurring jobs (standup-daily, prayer-time-fetcher) with their schedules

---

## Phase 6: User Story 3 — Automated Daily Standup Message to Slack (Priority: P2)

**Goal**: System posts a configurable standup message to a Slack channel every weekday at 09:30 AM. User can configure Slack credentials and standup message via the settings page.

**Independent Test**: Save Slack credentials and a standup message. Either wait for 09:30 AM on a weekday or manually trigger the job. Verify message appears in the Slack channel.

### Implementation

- [X] T037 [P] [US3] Create Slack request/response contract DTOs (SaveSlackCredentialRequest, SlackCredentialStatusResponse, IntegrationSettingsResponse, UpdateIntegrationSettingsRequest, TriggerJobResponse) in `src/SemanticSearch.WebApi/Contracts/Slack/SlackContracts.cs`
- [X] T038 [P] [US3] Create ISlackApiClient interface with PostMessageAsync and TestConnectionAsync methods in `src/SemanticSearch.Domain/Interfaces/ISlackApiClient.cs`
- [X] T039 [P] [US3] Create SaveSlackCredentialCommand with FluentValidation validator (BotToken starts with xoxb-, UserToken starts with xoxp- if present, DefaultChannel starts with C) in `src/SemanticSearch.Application/Slack/Commands/SaveSlackCredential.cs`
- [X] T040 [P] [US3] Create DeleteSlackCredentialCommand with handler in `src/SemanticSearch.Application/Slack/Commands/DeleteSlackCredential.cs`
- [X] T041 [P] [US3] Create GetSlackCredentialStatusQuery with handler in `src/SemanticSearch.Application/Slack/Queries/GetSlackCredentialStatus.cs`
- [X] T042 [P] [US3] Create TestSlackConnectionQuery with handler calling ISlackApiClient.TestConnectionAsync in `src/SemanticSearch.Application/Slack/Queries/TestSlackConnection.cs`
- [X] T043 [P] [US3] Create UpdateIntegrationSettingsCommand with FluentValidation validator (StandupMessage required when StandupEnabled, PrayerCity/Country required when PrayerEnabled, PrayerMethod 1–15) in `src/SemanticSearch.Application/Slack/Commands/UpdateIntegrationSettings.cs`
- [X] T044 [P] [US3] Create GetIntegrationSettingsQuery with handler in `src/SemanticSearch.Application/Slack/Queries/GetIntegrationSettings.cs`
- [X] T045 [US3] Implement SlackApiClient with PostMessageAsync (POST chat.postMessage with Bearer bot token) and TestConnectionAsync (POST auth.test) using IHttpClientFactory "SlackClient" in `src/SemanticSearch.Infrastructure/Slack/SlackApiClient.cs`
- [X] T046 [US3] Implement StandupJob.Execute: load IntegrationSettings (check StandupEnabled), load SlackCredential, decrypt bot token, call ISlackApiClient.PostMessageAsync with StandupMessage to DefaultChannel, log success/failure in `src/SemanticSearch.Infrastructure/BackgroundJobs/StandupJob.cs`
- [X] T047 [US3] Create TriggerJobCommand with handler that enqueues a one-time job by name (standup, prayer-fetch, prayer-status) via BackgroundJob.Enqueue in `src/SemanticSearch.Application/Slack/Commands/TriggerJob.cs`
- [X] T048 [US3] Create SlackController with endpoints: GET/POST/DELETE /api/slack/credentials, POST /api/slack/credentials/test in `src/SemanticSearch.WebApi/Controllers/SlackController.cs`
- [X] T049 [US3] Create IntegrationController with endpoints: GET/PUT /api/integration/settings, POST /api/integration/jobs/{jobName}/trigger in `src/SemanticSearch.WebApi/Controllers/IntegrationController.cs`
- [X] T050 [US3] Add Slack credential form section (bot token, user token, default channel, test button, save/delete) to IntegrationSettings.razor in `src/SemanticSearch.WebApi/Components/Pages/IntegrationSettings.razor`
- [X] T051 [US3] Add standup settings section (standup message textarea, enabled toggle, manual trigger button) to IntegrationSettings.razor in `src/SemanticSearch.WebApi/Components/Pages/IntegrationSettings.razor`
- [X] T052 [US3] Add Slack and integration API methods to WorkspaceApiClient (GetSlackCredentialStatusAsync, SaveSlackCredentialAsync, TestSlackConnectionAsync, GetIntegrationSettingsAsync, UpdateIntegrationSettingsAsync, TriggerJobAsync) in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`

**Checkpoint**: Slack credentials configurable, standup job fires on schedule and posts to Slack channel, manual trigger works from settings page

---

## Phase 7: User Story 5 — Pull Requests Radar (Priority: P2)

**Goal**: Dashboard shows a panel listing all active PRs where the user is author or reviewer, with reviewer vote statuses.

**Independent Test**: With valid TFS credentials and active PRs, navigate to Pull Request Radar page. Verify PRs are listed with title, branches, reviewers, and vote statuses. Click a PR to see detail with TFS link.

### Implementation

- [X] T053 [P] [US5] Create PullRequestsResponse, PullRequestResponse, ReviewerResponse contract DTOs in `src/SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs` (append to existing file)
- [X] T054 [US5] Add GetActivePullRequestsAsync method to ITfsApiClient interface in `src/SemanticSearch.Domain/Interfaces/ITfsApiClient.cs` — two calls (author + reviewer), merge and deduplicate
- [X] T055 [US5] Implement TfsApiClient.GetActivePullRequestsAsync: GET pullrequests by creatorId + reviewerId, merge results, parse reviewer votes into VoteLabel strings in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs`
- [X] T056 [US5] Create GetMyPullRequestsQuery with handler calling ITfsApiClient.GetActivePullRequestsAsync in `src/SemanticSearch.Application/Tfs/Queries/GetMyPullRequests.cs`
- [X] T057 [US5] Add GET /api/tfs/pullrequests endpoint to TfsController mapping to PullRequestsResponse in `src/SemanticSearch.WebApi/Controllers/TfsController.cs`
- [X] T058 [US5] Add TFS PR API method to WorkspaceApiClient (GetPullRequestsAsync) in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`
- [X] T059 [US5] Create PullRequestRadar.razor Blazor page showing PR cards with title, source→target branch, status badge, reviewer avatars with vote-status indicators (approved/rejected/waiting/no-vote), click-to-detail with TFS link, empty state message in `src/SemanticSearch.WebApi/Components/Pages/PullRequestRadar.razor`

**Checkpoint**: PR Radar shows active pull requests with reviewer vote statuses

---

## Phase 8: User Story 6 — Automated Prayer Time Status Updates (Priority: P3)

**Goal**: Midnight job fetches prayer times from Aladhan API, schedules 5 jobs at exact prayer times that set Slack status to "Praying" 🕌 with 30-minute expiration.

**Independent Test**: Set prayer city/country in settings. Manually trigger the prayer-fetch job. Verify 5 scheduled jobs appear in Hangfire dashboard. Manually trigger a prayer-status job, verify Slack status is updated.

### Implementation

- [X] T060 [P] [US6] Create IAladhanApiClient interface with GetPrayerTimesAsync(city, country, method) returning PrayerTimesResult in `src/SemanticSearch.Domain/Interfaces/IAladhanApiClient.cs`
- [X] T061 [P] [US6] Add SetUserStatusAsync method to ISlackApiClient interface (statusText, statusEmoji, statusExpirationUnix) in `src/SemanticSearch.Domain/Interfaces/ISlackApiClient.cs`
- [X] T062 [US6] Implement AladhanApiClient: GET /v1/timingsByCity/{date}?city=...&country=...&method=..., parse JSON response extracting Fajr/Dhuhr/Asr/Maghrib/Isha as TimeOnly values in `src/SemanticSearch.Infrastructure/Slack/AladhanApiClient.cs`
- [X] T063 [US6] Implement SlackApiClient.SetUserStatusAsync: POST users.profile.set with Bearer user token, JSON body with status_text="Praying", status_emoji=":mosque:", status_expiration=unix timestamp (now+30min) in `src/SemanticSearch.Infrastructure/Slack/SlackApiClient.cs`
- [X] T064 [US6] Create PrayerStatusUpdaterJob with Execute method: loads SlackCredential, decrypts user token, calls ISlackApiClient.SetUserStatusAsync, logs result in `src/SemanticSearch.Infrastructure/BackgroundJobs/PrayerStatusUpdaterJob.cs`
- [X] T065 [US6] Implement PrayerTimeFetcherJob.Execute: load IntegrationSettings (check PrayerEnabled), call IAladhanApiClient.GetPrayerTimesAsync, for each of 5 prayer times calculate delay from now and schedule PrayerStatusUpdaterJob via BackgroundJob.Schedule at the exact time in `src/SemanticSearch.Infrastructure/BackgroundJobs/PrayerTimeFetcherJob.cs`
- [X] T066 [US6] Add prayer settings section (city, country, method dropdown, enabled toggle, manual trigger button) to IntegrationSettings.razor in `src/SemanticSearch.WebApi/Components/Pages/IntegrationSettings.razor`
- [X] T067 [US6] Update BackgroundJobRegistration to include PrayerStatusUpdaterJob registration for manual triggering in `src/SemanticSearch.Infrastructure/BackgroundJobs/BackgroundJobRegistration.cs`

**Checkpoint**: Midnight fetcher schedules 5 prayer-time jobs, each updates Slack status with 🕌 emoji and 30-minute expiration

---

## Phase 9: User Story 7 — Contribution Heatmap (Priority: P3)

**Goal**: Dashboard shows a GitHub-style contribution heatmap with 12 months of TFS activity (commits, work item updates, PR activity) as colored squares with tooltips.

**Independent Test**: With valid TFS credentials and historical activity, navigate to Contribution Heatmap page. Verify colored squares appear proportional to daily activity. Hover over a square to see date and count.

### Implementation

- [X] T068 [P] [US7] Create ContributionHeatmapResponse and ContributionDayResponse contract DTOs in `src/SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs` (append to existing file)
- [X] T069 [US7] Add GetContributionDataAsync(months) method to ITfsApiClient interface in `src/SemanticSearch.Domain/Interfaces/ITfsApiClient.cs`
- [X] T070 [US7] Implement TfsApiClient.GetContributionDataAsync: aggregate commits (GET git/repositories/commits by author), work item changes (WIQL by ChangedBy + ChangedDate), and PR creation dates into daily counts, compute intensity levels 0–4 in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs`
- [X] T071 [US7] Create GetContributionHeatmapQuery with handler calling ITfsApiClient.GetContributionDataAsync in `src/SemanticSearch.Application/Tfs/Queries/GetContributionHeatmap.cs`
- [X] T072 [US7] Add GET /api/tfs/contributions endpoint with optional months query parameter to TfsController in `src/SemanticSearch.WebApi/Controllers/TfsController.cs`
- [X] T073 [US7] Add contribution heatmap API method to WorkspaceApiClient (GetContributionHeatmapAsync) in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`
- [X] T074 [US7] Create ContributionHeatmap.razor Blazor page rendering a 12-month grid of squares (7 rows × 52 columns), colored by intensity level (level 0 = empty/gray, level 4 = darkest green), with hover tooltips showing date and count, total contributions summary, and empty state message in `src/SemanticSearch.WebApi/Components/Pages/ContributionHeatmap.razor`

**Checkpoint**: Heatmap renders 12 months of TFS activity with interactive tooltips

---

## Phase 10: Polish & Cross-Cutting Concerns

**Purpose**: Navigation integration, error handling consistency, and final validation

- [X] T075 [P] Add navigation links for MyWork, PullRequestRadar, ContributionHeatmap, and IntegrationSettings to the dashboard sidebar/nav in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`
- [X] T076 [P] Add structured logging (ILogger) to all three background jobs (StandupJob, PrayerTimeFetcherJob, PrayerStatusUpdaterJob) for success/failure/skip outcomes in their respective files under `src/SemanticSearch.Infrastructure/BackgroundJobs/`
- [X] T077 Run quickstart.md verification checklist — validate all endpoints respond correctly, Hangfire dashboard loads, credentials can be stored/tested/deleted, background jobs fire
- [X] T078 Verify all Blazor pages handle loading states (spinner), error states (error banner with retry), and empty states (informational message) consistently across MyWork, PullRequestRadar, ContributionHeatmap, and IntegrationSettings

**Checkpoint**: Feature complete — all pages navigable, jobs running, credentials encrypted, error handling consistent

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1 (Setup)**: No dependencies — start immediately
- **Phase 2 (Foundational)**: Depends on Phase 1 — BLOCKS all user stories
- **Phase 3 (US2 — TFS Credentials)**: Depends on Phase 2 — BLOCKS Phase 4 (US1 needs credentials)
- **Phase 4 (US1 — Kanban Board)**: Depends on Phase 3 (needs saved TFS credentials for API calls)
- **Phase 5 (US4 — Hangfire Dashboard)**: Depends on Phase 2 only — can run parallel with Phase 3/4
- **Phase 6 (US3 — Slack Standup)**: Depends on Phase 2 + Phase 5 (needs Hangfire + credential infra)
- **Phase 7 (US5 — PR Radar)**: Depends on Phase 3 (needs TFS credentials) — can run parallel with Phase 4
- **Phase 8 (US6 — Prayer Status)**: Depends on Phase 6 (needs Slack integration + Hangfire)
- **Phase 9 (US7 — Contribution Heatmap)**: Depends on Phase 3 (needs TFS credentials) — can run parallel with Phase 4/7
- **Phase 10 (Polish)**: Depends on all previous phases

### User Story Dependencies

- **US2 (TFS Credentials)**: Foundation only — no other story dependency
- **US1 (Kanban Board)**: Depends on US2 (needs stored TFS credentials)
- **US4 (Hangfire Dashboard)**: Foundation only — no story dependency
- **US3 (Slack Standup)**: Depends on US4 (needs Hangfire running)
- **US5 (PR Radar)**: Depends on US2 (needs stored TFS credentials) — parallel with US1
- **US6 (Prayer Status)**: Depends on US3 + US4 (needs Slack + Hangfire)
- **US7 (Contribution Heatmap)**: Depends on US2 (needs stored TFS credentials) — parallel with US1, US5

### Within Each User Story

- Contracts/DTOs before handlers
- Domain interfaces before infrastructure implementations
- MediatR commands/queries before controllers
- Controllers before Blazor pages
- WorkspaceApiClient methods before Blazor pages that call them

### Parallel Opportunities

**After Phase 2 (Foundation) completes, these can run in parallel:**
- Phase 3 (US2 — TFS Credentials) ‖ Phase 5 (US4 — Hangfire Dashboard)

**After Phase 3 (TFS Credentials) completes:**
- Phase 4 (US1 — Kanban Board) ‖ Phase 7 (US5 — PR Radar) ‖ Phase 9 (US7 — Heatmap)

**After Phase 5 (Hangfire) + Phase 2:**
- Phase 6 (US3 — Slack Standup)

**After Phase 6 (Slack Standup):**
- Phase 8 (US6 — Prayer Status)

---

## Parallel Example: After Foundation

```text
Thread A: Phase 3 (US2) → Phase 4 (US1) → Phase 7 (US5) → Phase 9 (US7)
Thread B: Phase 5 (US4) → Phase 6 (US3) → Phase 8 (US6)
          ↓
          Phase 10 (Polish) — after both threads complete
```

---

## Implementation Strategy

### MVP First (US2 + US1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: US2 — TFS Credentials
4. Complete Phase 4: US1 — Kanban Board
5. **STOP and VALIDATE**: TFS credentials work, work items display on Kanban board
6. Deploy/demo if ready

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US2 (TFS Credentials) → Credentials UI functional (MVP prerequisite)
3. US1 (Kanban Board) → **MVP** — Work items visible
4. US4 (Hangfire Dashboard) → Job infrastructure visible
5. US3 (Slack Standup) → Daily automation active
6. US5 (PR Radar) → Code review awareness
7. US6 (Prayer Status) → Personal automation
8. US7 (Contribution Heatmap) → Historical insights
9. Polish → Feature complete

### Suggested MVP Scope

**Phase 1 + Phase 2 + Phase 3 (US2) + Phase 4 (US1)** = 32 tasks for MVP
