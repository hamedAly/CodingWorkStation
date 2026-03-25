# Research: TFS Integration & Background Automation (Slack/Hangfire)

**Feature**: 007-tfs-slack-automation  
**Date**: 2026-03-25

## R-001: TFS REST API — Work Items (WIQL)

**Decision**: Use WIQL (Work Item Query Language) via `POST /_apis/wit/wiql` to find assigned items, then batch-fetch details via `GET /_apis/wit/workitems?ids=...`.

**Rationale**: WIQL supports `@me` context for the authenticated user and allows filtering by type (Task, Bug) and state. The two-step approach (query IDs first, then fetch details) is the standard TFS API pattern and allows field selection to minimize payload.

**Alternatives considered**:
- Direct work item list endpoint (`GET /_apis/wit/workitems`): Requires knowing IDs upfront — not suitable for "assigned to me" queries.
- OData feed (`_odata/v4.0-preview/WorkItems`): Only available in Azure DevOps Services (cloud), not on-premises TFS.

**Key details**:
- API version: `7.1` (Azure DevOps 2022+); fallback to `6.1` (TFS 2017+).
- WIQL query: `SELECT [System.Id] FROM WorkItems WHERE [System.AssignedTo] = @me AND [System.WorkItemType] IN ('Task', 'Bug')`.
- Batch fetch: `GET /_apis/wit/workitems?ids=1,2,3&$expand=none&api-version=7.1` (up to 200 IDs per request).
- Fields needed: `System.Id`, `System.Title`, `System.State`, `System.WorkItemType`, `System.AssignedTo`, `System.AreaPath`, `System.Description`.

---

## R-002: TFS REST API — Pull Requests

**Decision**: Query pull requests with two separate API calls — one for authored PRs (`searchCriteria.creatorId`) and one for reviewing PRs (`searchCriteria.reviewerId`) — then merge and deduplicate results.

**Rationale**: The TFS/Azure DevOps REST API does not support filtering by "author OR reviewer" in a single request. Two calls with client-side merge is the standard approach.

**Alternatives considered**:
- Single query with `searchCriteria.includeLinks=true`: Does not filter by reviewer.
- GraphQL (Azure DevOps): Not available on TFS on-premises.

**Key details**:
- Author PRs: `GET /{project}/_apis/git/pullrequests?searchCriteria.creatorId={userId}&searchCriteria.status=active&api-version=7.1`
- Reviewer PRs: `GET /{project}/_apis/git/pullrequests?searchCriteria.reviewerId={userId}&searchCriteria.status=active&api-version=7.1`
- Reviewer vote values: `-10` (rejected), `-5` (waiting for author), `0` (no vote), `5` (approved with suggestions), `10` (approved).
- Need to resolve user identity ID first via `GET /_apis/connectionData` or use the connection's authenticated identity.

---

## R-003: TFS REST API — Authentication

**Decision**: Use HTTP Basic Authentication with empty username and PAT as password.

**Rationale**: This is the standard authentication method for TFS/Azure DevOps REST API. It works for both cloud (Azure DevOps Services) and on-premises (TFS 2015+).

**Alternatives considered**:
- OAuth 2.0: Requires Azure AD app registration, more complex setup, overkill for single-user dashboard.
- NTLM: Works for on-prem TFS but not Azure DevOps Services; would require Windows-specific configuration.

**Key details**:
- Header: `Authorization: Basic {base64(":"+pat)}`.
- Required PAT scopes: Work Items (Read), Code (Read), Identity (Read).
- HTTP 401/403 responses indicate expired/invalid PAT — surface error to user.

---

## R-004: TFS REST API — Contribution Heatmap

**Decision**: Aggregate contributions from three sources — commits, work item updates, and PR activity — and cache daily totals in memory (refresh on demand).

**Rationale**: TFS has no single "contributions" endpoint. Aggregation from multiple sources is the only option. Caching avoids repeated expensive API calls (potentially 365+ days × 3 sources).

**Alternatives considered**:
- Azure DevOps Analytics/OData service: Only available in Azure DevOps Services (cloud), not on-prem TFS.
- Git log parsing: Requires local clone access, not available via REST API for all repos.

**Key details**:
- Commits: `GET /{project}/_apis/git/repositories/{repoId}/commits?searchCriteria.author={email}&searchCriteria.fromDate={12moAgo}&api-version=7.1`
- Work item updates: Query via WIQL with `[System.ChangedDate] >= @startOfDay(-365)` and `[System.ChangedBy] = @me`, then count unique (date, workItemId) pairs.
- PR activity: Use created date from PR queries.
- Cache strategy: Store daily counts in a `Dictionary<DateOnly, int>`; refresh on explicit user action.

---

## R-005: Hangfire Setup

**Decision**: Use Hangfire with in-memory storage (`Hangfire.InMemory` NuGet package). Dashboard exposed at `/hangfire`.

**Rationale**: In-memory storage is explicitly requested in the spec and appropriate for a single-user dashboard. No persistent database tables needed for job state. Recurring jobs are re-registered on every application startup.

**Alternatives considered**:
- `Hangfire.MemoryStorage`: Outdated/community package; `Hangfire.InMemory` is the officially recommended in-memory provider.
- SQLite storage for Hangfire: Would add persistence but is overkill for a single-user dashboard and adds schema complexity.
- Quartz.NET: More lightweight but lacks the built-in dashboard that Hangfire provides.

**Key details**:
- NuGet packages: `Hangfire.Core`, `Hangfire.AspNetCore`, `Hangfire.InMemory`.
- Program.cs registration: `services.AddHangfire(config => config.UseInMemoryStorage()); services.AddHangfireServer();`
- Dashboard: `app.UseHangfireDashboard("/hangfire");`
- Recurring job registration happens in startup (after `app.Build()`).
- Recurring jobs survive in-memory as long as the app runs; re-registered on restart.
- Cron: Standup = `"30 9 * * MON-FRI"`; Midnight fetcher = `"0 0 * * *"`.

---

## R-006: Slack Web API Integration

**Decision**: Use Slack's Web API via HTTP POST with Bearer token authentication. Two endpoints: `chat.postMessage` for standup messages and `users.profile.set` for prayer status.

**Rationale**: Slack's Web API is the standard integration method. No WebSocket or Events API needed since we only push data (no listening for events).

**Alternatives considered**:
- Slack Incoming Webhooks: Simpler for posting messages but cannot update user profile/status.
- Slack Bolt SDK: Full-featured but heavyweight; overkill when we only need 2 API calls.

**Key details**:
- Post message: `POST https://slack.com/api/chat.postMessage` with JSON body `{ "channel": "C...", "text": "..." }`.
- Set status: `POST https://slack.com/api/users.profile.set` with JSON body `{ "profile": { "status_text": "Praying", "status_emoji": ":mosque:", "status_expiration": <unix_timestamp> } }`.
- Auth header: `Authorization: Bearer xoxb-...` (bot token) or `xoxp-...` (user token — needed for `users.profile.set`).
- **Important**: `users.profile:write` requires a **user token** (`xoxp-`), not a bot token. The standup `chat:write` can use either.
- Bot scopes: `chat:write`. User token scopes: `users.profile:write`.
- Status expiration: Unix timestamp = `DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds()`.

---

## R-007: Aladhan Prayer Times API

**Decision**: Use the free Aladhan API (`/v1/timingsByCity`) to fetch daily prayer times. Parse 5 prayer times (Fajr, Dhuhr, Asr, Maghrib, Isha) from the response.

**Rationale**: Free, public, no authentication required. Supports city-based lookup which is user-friendly. Well-documented and reliable.

**Alternatives considered**:
- IslamicFinder API: Requires API key registration.
- Local calculation library: More complex; depends on astronomical calculations and requires latitude/longitude.

**Key details**:
- Endpoint: `GET https://api.aladhan.com/v1/timingsByCity/{dd-mm-yyyy}?city={city}&country={country}&method=5`
- Response: `data.timings` object with keys `Fajr`, `Dhuhr`, `Asr`, `Maghrib`, `Isha` (HH:mm strings in local time).
- Method 5 = Muslim World League (default, widely accepted).
- No rate limit documented; keep requests to 1/day (midnight fetcher).
- Parse HH:mm strings into `TimeOnly`, combine with today's `DateOnly` to get exact `DateTime` for scheduling.

---

## R-008: Credential Encryption

**Decision**: Use ASP.NET Core Data Protection API (`IDataProtector`) to encrypt PATs and API tokens before storing in SQLite.

**Rationale**: Built into ASP.NET Core (no additional NuGet needed). Uses DPAPI on Windows, providing machine-level encryption. Keys are automatically managed and rotated. Perfect for a single-user local application.

**Alternatives considered**:
- AES encryption with hardcoded key: Insecure; key management burden.
- Azure Key Vault: Requires cloud connectivity; overkill for local dashboard.
- Windows Credential Manager: Platform-specific API, less portable.

**Key details**:
- `IDataProtectionProvider` is already available in ASP.NET Core DI.
- Create purpose-specific protector: `provider.CreateProtector("SemanticSearch.Credentials")`.
- Encrypt: `protector.Protect(plaintext)` → Base64 string stored in SQLite.
- Decrypt: `protector.Unprotect(ciphertext)` → original plaintext.
- Keys stored in user profile directory by default (persisted across restarts).

---

## R-009: HttpClient Architecture

**Decision**: Use `IHttpClientFactory` with named clients for each external service (TFS, Slack, Aladhan).

**Rationale**: `IHttpClientFactory` is the .NET standard for managing HttpClient lifetime, connection pooling, and preventing socket exhaustion. Named clients allow per-service configuration (base URL, timeout, default headers).

**Alternatives considered**:
- Single shared HttpClient: Cannot have different base URLs/auth headers per service.
- Typed clients: Cleaner API but more ceremony; named clients are simpler for this scope.

**Key details**:
- Named clients: `"TfsClient"`, `"SlackClient"`, `"AladhanClient"`.
- TFS client: Dynamic base URL (from stored credentials); auth header set per-request after decrypting PAT.
- Slack client: Base URL `https://slack.com/api/`; auth header set per-request after decrypting token.
- Aladhan client: Base URL `https://api.aladhan.com/v1/`; no auth needed.
- Timeouts: 15s for TFS/Slack, 10s for Aladhan.
- All clients created via `_httpClientFactory.CreateClient("name")`.
