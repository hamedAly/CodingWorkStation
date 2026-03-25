# Implementation Plan: TFS Integration & Background Automation (Slack/Hangfire)

**Branch**: `007-tfs-slack-automation` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/007-tfs-slack-automation/spec.md`

## Summary

Integrate TFS/Azure DevOps REST API for work item tracking, pull request monitoring, and contribution visualization into the existing Blazor dashboard. Add Hangfire for background job processing with in-memory storage, enabling automated Slack standup messages (weekdays 09:30 AM) and prayer-time Slack status updates (fetched via Aladhan API at midnight, scheduled at exact prayer times). All credentials stored encrypted in SQLite. UI delivered as Blazor Interactive Server components following the existing Clean Architecture pattern.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), MediatR 14.1, FluentValidation 12.1, Hangfire (+ Hangfire.InMemory), Microsoft.Data.Sqlite 9.0, System.Net.Http (HttpClient for TFS/Slack/Aladhan APIs)  
**Storage**: Existing SQLite file database for credential storage (encrypted PAT/tokens); Hangfire uses in-memory storage (no persistence across restarts)  
**Testing**: xUnit (unit tests for services, MediatR handlers; integration tests for API clients)  
**Target Platform**: Windows server / local development machine  
**Project Type**: Web (Blazor Interactive Server — existing project structure)  
**Performance Goals**: Work items / PRs loaded within 5 seconds; background jobs fire within 1 minute of scheduled time  
**Constraints**: Single-user dashboard; TFS server must be reachable over network; Slack bot token with chat:write + users.profile:write scopes required; in-memory job storage means recurring jobs must be re-registered on startup  
**Scale/Scope**: Single user, 1 TFS server, 1 Slack workspace, <100 work items, <50 active PRs, 12 months contribution data

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Check | Status |
|------|-------|--------|
| I. Code Quality | Functions ≤40 lines (soft), ≤60 (hard); classes ≤400 lines; cognitive complexity ≤12 | ✅ Will comply |
| II. Code Organization | Feature-based folder structure; single responsibility per file | ✅ Aligns with existing Application/Infrastructure/WebApi split |
| III. Comments & Documentation | DocBlocks on public APIs; comments for WHY not WHAT | ✅ Will comply |
| IV. DRY | Shared credential storage pattern for TFS + Slack; reuse HttpClient patterns | ✅ Planned |
| V. Type System | Strict types, no `any`; runtime validation on external boundaries (TFS/Slack/Aladhan responses) | ✅ Will comply |
| VI. Testing Standards | Unit 92%+, Integration 75%+ for critical paths | ⚠️ External API mocking needed |
| VII. UI Consistency | Blazor components follow existing design system; loading/error/disabled states | ✅ Will follow existing patterns |
| VIII. Performance Budget | N/A — server-rendered Blazor, no JS bundle concerns | ✅ N/A |
| IX. Git Hygiene | Conventional commits, linear history, branch dies after merge | ✅ Will comply |
| X. Observability | Structured logging on all background jobs and API calls; error traceability | ✅ Planned via FR-022 |
| XI. Security | OWASP compliance; secrets not in git; encrypted credential storage; input validation on all external data | ✅ Core requirement (FR-002, FR-015) |
| XII. Thin Controllers | Controllers ≤15 lines; single MediatR send per action | ✅ Will follow existing pattern |
| XIII. Validation | FluentValidation in Application layer; auto-pipeline via MediatR behavior | ✅ Existing infrastructure |
| XIV. Separation of Concerns | Domain pure; Application depends only on Domain; Infrastructure implements interfaces | ✅ Existing architecture |
| **Tech Stack** | Constitution says: .NET 8, EF Core, React, SQL Server — Project actually uses: .NET 10, SQLite, Blazor Server, no EF Core | ⚠️ DEVIATION — justified: project established this stack in features 001–006; Blazor + SQLite is the project standard |

**Deviation justification**: The constitution's technology stack section specifies .NET 8 / React / SQL Server / EF Core, but the actual project (established across 6 prior features) uses .NET 10 / Blazor Interactive Server / SQLite / raw ADO.NET. This plan follows the **project's actual stack**. The constitution's architectural principles (Clean Architecture, CQRS, MediatR, FluentValidation) are fully honored.

## Project Structure

### Documentation (this feature)

```text
specs/007-tfs-slack-automation/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/
│   └── Entities/
│       ├── TfsCredential.cs              # TFS connection entity
│       ├── SlackCredential.cs            # Slack connection entity
│       └── IntegrationSettings.cs        # Standup message, prayer location
│   └── Interfaces/
│       ├── ICredentialRepository.cs       # CRUD for encrypted credentials
│       └── IIntegrationSettingsRepository.cs
│
├── SemanticSearch.Application/
│   └── Tfs/
│       ├── Commands/
│       │   ├── SaveTfsCredential.cs       # Store/update TFS PAT
│       │   └── DeleteTfsCredential.cs     # Remove stored TFS credential
│       └── Queries/
│           ├── GetMyWorkItems.cs          # Fetch assigned tasks/bugs
│           ├── GetMyPullRequests.cs        # Fetch authored/reviewing PRs
│           └── GetContributionHeatmap.cs   # Fetch 12-month activity data
│   └── Slack/
│       ├── Commands/
│       │   ├── SaveSlackCredential.cs
│       │   ├── DeleteSlackCredential.cs
│       │   └── UpdateStandupMessage.cs
│       └── Queries/
│           └── GetSlackSettings.cs
│   └── BackgroundJobs/
│       ├── StandupJob.cs                  # Daily standup poster
│       ├── PrayerTimeFetcherJob.cs        # Midnight prayer-time fetcher
│       └── PrayerStatusUpdaterJob.cs      # Individual prayer-time status setter
│
├── SemanticSearch.Infrastructure/
│   └── Tfs/
│       ├── TfsApiClient.cs               # HttpClient wrapper for TFS REST API
│       └── TfsQueryBuilder.cs            # WIQL query construction
│   └── Slack/
│       ├── SlackApiClient.cs             # HttpClient wrapper for Slack Web API
│       └── AladhanApiClient.cs           # HttpClient wrapper for prayer times
│   └── Credentials/
│       ├── SqliteCredentialRepository.cs  # Encrypted credential storage
│       └── CredentialEncryption.cs        # DPAPI-based encryption
│   └── DependencyInjection/
│       └── (extend InfrastructureServiceRegistration.cs)
│
├── SemanticSearch.WebApi/
│   └── Controllers/
│       ├── TfsController.cs              # TFS endpoints (work items, PRs, heatmap)
│       └── SlackController.cs            # Slack settings endpoints
│   └── Components/Pages/
│       ├── MyWork.razor                  # Kanban board
│       ├── PullRequestRadar.razor        # PR radar panel
│       ├── ContributionHeatmap.razor     # Heatmap visualization
│       └── IntegrationSettings.razor     # TFS + Slack credential management
│   └── Contracts/
│       ├── TfsContracts.cs               # TFS request/response DTOs
│       └── SlackContracts.cs             # Slack request/response DTOs
```

**Structure Decision**: Follows the existing feature-based folder organization within the established 4-project Clean Architecture (Domain → Application → Infrastructure → WebApi). New folders (Tfs/, Slack/, BackgroundJobs/, Credentials/) are added to each layer. No new projects needed.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| Tech stack deviation (.NET 10/Blazor/SQLite vs constitution's .NET 8/React/SQL Server) | Project's actual stack established across 6 prior features | Switching stacks mid-project would be a full rewrite |
| Hangfire NuGet dependency | Provides battle-tested recurring/delayed job engine with built-in dashboard | Building custom job scheduler would be more complex and less reliable |
| 3 external API clients (TFS, Slack, Aladhan) | Each serves a distinct integration requirement from spec | Combining would violate SRP; each has different auth, base URL, retry logic |
