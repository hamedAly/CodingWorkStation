# Tasks: Code Quality Dashboard Foundation

**Input**: Design documents from `/specs/003-duplication-dashboard-foundation/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

**Tests**: No explicit TDD or test-first requirement was stated in the feature specification, so this task list omits standalone test-writing tasks. Validation is captured through each story's independent test criteria and the final smoke-validation tasks.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the existing host, assets, and configuration for quality-analysis work.

- [X] T001 Add quality-analysis configuration for thresholds and limits in `src/SemanticSearch.Application/Common/SemanticSearchOptions.cs`, `src/SemanticSearch.WebApi/appsettings.json`, and `src/SemanticSearch.WebApi/appsettings.Development.json`
- [X] T002 [P] Add Roslyn and local chart asset setup in `src/SemanticSearch.Infrastructure/SemanticSearch.Infrastructure.csproj`, `src/SemanticSearch.WebApi/Components/App.razor`, and `src/SemanticSearch.WebApi/wwwroot/lib/chart.js/chart.umd.js`
- [X] T003 [P] Add quality navigation, page shell, and baseline styles in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`, `src/SemanticSearch.WebApi/Components/Pages/Quality.razor`, and `src/SemanticSearch.WebApi/wwwroot/css/app.css`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared domain, persistence, and service foundations required by every quality user story.

**Critical**: No user story work should start until this phase is complete.

- [X] T004 Expand quality domain entities and enums in `src/SemanticSearch.Domain/Entities/QualityAnalysisRun.cs`, `src/SemanticSearch.Domain/Entities/QualitySummarySnapshot.cs`, `src/SemanticSearch.Domain/Entities/DuplicationFinding.cs`, `src/SemanticSearch.Domain/Entities/CodeRegion.cs`, and `src/SemanticSearch.Domain/ValueObjects/QualityGrade.cs`
- [X] T005 [P] Add shared quality application interfaces in `src/SemanticSearch.Application/Common/Interfaces/IQualityRepository.cs`, `src/SemanticSearch.Application/Common/Interfaces/IStructuralCloneAnalyzer.cs`, and `src/SemanticSearch.Application/Common/Interfaces/ISemanticDuplicationService.cs`
- [X] T006 [P] Extend the SQLite schema and repository mappings for quality runs, summaries, findings, and code regions in `src/SemanticSearch.Infrastructure/VectorStore/SqliteSchemaInitializer.cs` and `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs`
- [X] T007 [P] Implement shared structural-clone analysis services in `src/SemanticSearch.Infrastructure/Quality/RoslynStructuralCloneAnalyzer.cs` and `src/SemanticSearch.Infrastructure/Quality/StructuralCloneNormalizer.cs`
- [X] T008 [P] Implement shared semantic-duplication analysis services in `src/SemanticSearch.Infrastructure/Quality/EmbeddingSemanticDuplicationService.cs` and `src/SemanticSearch.Infrastructure/Quality/SemanticPairSelector.cs`
- [X] T009 Wire quality services, repository access, and presentation registration in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`, `src/SemanticSearch.WebApi/Program.cs`, and `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`

**Checkpoint**: Quality-analysis foundations are ready; user-story implementation can proceed.

---

## Phase 3: User Story 1 - Review Project Quality Snapshot (Priority: P1) MVP

**Goal**: Let an engineering lead open the quality dashboard for an indexed project and immediately see the grade, duplication metrics, chart breakdown, and findings table.

**Independent Test**: Analyze an indexed project, open the quality dashboard, and confirm the hero metrics, breakdown chart, and findings table load together with consistent counts and a clean empty state when no duplicates exist.

- [X] T010 [US1] Implement quality snapshot orchestration plus summary and findings queries in `src/SemanticSearch.Application/Quality/Commands/GenerateQualitySnapshotCommand.cs`, `src/SemanticSearch.Application/Quality/Commands/GenerateQualitySnapshotCommandHandler.cs`, `src/SemanticSearch.Application/Quality/Queries/GetQualitySummaryQuery.cs`, and `src/SemanticSearch.Application/Quality/Queries/ListQualityFindingsQuery.cs`
- [X] T011 [P] [US1] Add validators and centralized scoring rules in `src/SemanticSearch.Application/Quality/Validators/GenerateQualitySnapshotCommandValidator.cs`, `src/SemanticSearch.Application/Quality/Validators/GetQualitySummaryQueryValidator.cs`, `src/SemanticSearch.Application/Quality/Validators/ListQualityFindingsQueryValidator.cs`, and `src/SemanticSearch.Application/Quality/QualityScoringRules.cs`
- [X] T012 [US1] Persist summary snapshots, reconciled duplicate counts, and findings-list projections in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs`, `src/SemanticSearch.Infrastructure/Quality/QualitySummaryBuilder.cs`, and `src/SemanticSearch.Infrastructure/Quality/StructuralCloneGrouper.cs`
- [X] T013 [US1] Expose `GET /api/quality/{projectKey}` and `GET /api/quality/{projectKey}/findings` in `src/SemanticSearch.WebApi/Controllers/QualityController.cs` and `src/SemanticSearch.WebApi/Contracts/Quality/QualityDtos.cs`
- [X] T014 [US1] Build the quality dashboard hero, chart, and findings table in `src/SemanticSearch.WebApi/Components/Pages/Quality.razor`, `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor`, `src/SemanticSearch.WebApi/Components/Quality/QualityHero.razor`, `src/SemanticSearch.WebApi/Components/Quality/QualityBreakdownChart.razor`, and `src/SemanticSearch.WebApi/Components/Quality/QualityFindingsTable.razor`
- [X] T015 [US1] Connect project selection, snapshot loading, empty/error states, and chart rendering in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`, `src/SemanticSearch.WebApi/wwwroot/js/quality-dashboard.js`, and `src/SemanticSearch.WebApi/wwwroot/css/app.css`

**Checkpoint**: User Story 1 is independently functional when the dashboard can load one project's summary and findings without opening the comparison modal.

---

## Phase 4: User Story 2 - Inspect Duplicate Evidence (Priority: P2)

**Goal**: Let a developer open any finding and inspect both code regions side by side with highlighted matching lines and source-availability messaging.

**Independent Test**: From a populated findings table, open one finding and confirm the modal shows both files, both line ranges, highlighted matching lines, and a clear fallback message when source content is unavailable.

- [X] T016 [US2] Implement duplicate-comparison query handling and highlight mapping in `src/SemanticSearch.Application/Quality/Queries/GetDuplicateComparisonQuery.cs`, `src/SemanticSearch.Application/Quality/Queries/GetDuplicateComparisonQueryHandler.cs`, and `src/SemanticSearch.Application/Quality/Validators/GetDuplicateComparisonQueryValidator.cs`
- [X] T017 [P] [US2] Persist code-region snapshot availability and comparison read models in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/Quality/ComparisonHighlightService.cs`
- [X] T018 [US2] Expose `GET /api/quality/{projectKey}/findings/{findingId}` in `src/SemanticSearch.WebApi/Controllers/QualityController.cs` and `src/SemanticSearch.WebApi/Contracts/Quality/QualityDtos.cs`
- [X] T019 [US2] Build the comparison modal and side-by-side code panes in `src/SemanticSearch.WebApi/Components/Quality/DuplicateComparisonModal.razor` and `src/SemanticSearch.WebApi/Components/Quality/CodeRegionPane.razor`
- [X] T020 [US2] Connect finding selection, modal loading/error states, and missing-source messaging in `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor`, `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`, and `src/SemanticSearch.WebApi/wwwroot/css/app.css`

**Checkpoint**: User Story 2 is independently functional when a finding can be opened from the dashboard and reviewed in the comparison modal.

---

## Phase 5: User Story 3 - Retrieve Consistent Quality Findings (Priority: P3)

**Goal**: Let the local developer command center request structural analysis, semantic analysis, and the latest persisted quality snapshot through stable, consistent APIs.

**Independent Test**: Run the structural and semantic analysis endpoints for an indexed project, then request the latest summary and findings and confirm the responses share the same latest run metadata and refreshed counts without stale duplicate rows.

- [X] T021 [US3] Implement explicit structural and semantic analysis command flows in `src/SemanticSearch.Application/Quality/Commands/RunStructuralDuplicationAnalysisCommand.cs`, `src/SemanticSearch.Application/Quality/Commands/RunStructuralDuplicationAnalysisCommandHandler.cs`, `src/SemanticSearch.Application/Quality/Commands/RunSemanticDuplicationAnalysisCommand.cs`, and `src/SemanticSearch.Application/Quality/Commands/RunSemanticDuplicationAnalysisCommandHandler.cs`
- [X] T022 [P] [US3] Add validators and refresh policies for on-demand analysis in `src/SemanticSearch.Application/Quality/Validators/RunStructuralDuplicationAnalysisCommandValidator.cs`, `src/SemanticSearch.Application/Quality/Validators/RunSemanticDuplicationAnalysisCommandValidator.cs`, and `src/SemanticSearch.Application/Quality/QualityRefreshPolicy.cs`
- [X] T023 [US3] Persist per-mode analysis runs, latest-result replacement, and stale-finding cleanup in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/Quality/QualityRunCoordinator.cs`
- [X] T024 [US3] Expose `POST /api/quality/structural` and `POST /api/quality/semantic` in `src/SemanticSearch.WebApi/Controllers/QualityController.cs` and `src/SemanticSearch.WebApi/Contracts/Quality/QualityDtos.cs`
- [X] T025 [US3] Return consistent run metadata and refresh-aware quality responses in `src/SemanticSearch.WebApi/Controllers/QualityController.cs`, `src/SemanticSearch.WebApi/Contracts/Quality/QualityDtos.cs`, and `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`

**Checkpoint**: User Story 3 is independently functional when the command center can trigger analysis and retrieve the latest consistent quality data through the API.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finish the feature with documentation, accessibility polish, and smoke validation across all stories.

- [X] T026 [P] Update operator documentation and usage guidance in `README.md` and `specs/003-duplication-dashboard-foundation/quickstart.md`
- [X] T027 [P] Add accessibility, empty-state, and status-message polish in `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor`, `src/SemanticSearch.WebApi/Components/Quality/DuplicateComparisonModal.razor`, `src/SemanticSearch.WebApi/Components/Quality/QualityFindingsTable.razor`, and `src/SemanticSearch.WebApi/wwwroot/css/app.css`
- [X] T028 Validate the quality contract, quickstart flow, and feature smoke scenarios in `specs/003-duplication-dashboard-foundation/contracts/code-quality.openapi.yaml`, `specs/003-duplication-dashboard-foundation/quickstart.md`, and `specs/003-duplication-dashboard-foundation/spec.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 depends on no prior work.
- Phase 2 depends on Phase 1 and blocks all user-story phases.
- Phases 3-5 depend on Phase 2.
- Phase 6 depends on the completion of the user stories included in the release.

### User Story Dependencies

- US1 depends only on Phase 2 and is the MVP slice.
- US2 depends on US1 for populated findings to open in the modal, though its comparison pipeline can be implemented once Phase 2 is complete.
- US3 depends on Phase 2 for implementation and reuses the summary/finding surfaces established in US1 to verify the latest persisted results.

### Suggested Completion Order

1. Phase 1: Setup
2. Phase 2: Foundational
3. Phase 3: US1
4. Validate MVP
5. Phase 4: US2
6. Phase 5: US3
7. Phase 6: Polish

---

## Parallel Opportunities

- `T002` and `T003` can run in parallel after `T001`.
- `T005`, `T006`, `T007`, and `T008` can run in parallel after `T004`.
- In US1, `T011` can run in parallel with `T010`.
- In US2, `T017` can run in parallel with `T016`.
- In US3, `T022` can run in parallel with `T021`.
- In Phase 6, `T026` and `T027` can run in parallel after the selected stories are complete.

## Parallel Example: User Story 1

```text
Task: T010 Implement quality snapshot orchestration plus summary and findings queries in src/SemanticSearch.Application/Quality/Commands/GenerateQualitySnapshotCommand.cs, src/SemanticSearch.Application/Quality/Commands/GenerateQualitySnapshotCommandHandler.cs, src/SemanticSearch.Application/Quality/Queries/GetQualitySummaryQuery.cs, and src/SemanticSearch.Application/Quality/Queries/ListQualityFindingsQuery.cs
Task: T011 Add validators and centralized scoring rules in src/SemanticSearch.Application/Quality/Validators/GenerateQualitySnapshotCommandValidator.cs, src/SemanticSearch.Application/Quality/Validators/GetQualitySummaryQueryValidator.cs, src/SemanticSearch.Application/Quality/Validators/ListQualityFindingsQueryValidator.cs, and src/SemanticSearch.Application/Quality/QualityScoringRules.cs
```

## Parallel Example: User Story 2

```text
Task: T016 Implement duplicate-comparison query handling and highlight mapping in src/SemanticSearch.Application/Quality/Queries/GetDuplicateComparisonQuery.cs, src/SemanticSearch.Application/Quality/Queries/GetDuplicateComparisonQueryHandler.cs, and src/SemanticSearch.Application/Quality/Validators/GetDuplicateComparisonQueryValidator.cs
Task: T017 Persist code-region snapshot availability and comparison read models in src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs and src/SemanticSearch.Infrastructure/Quality/ComparisonHighlightService.cs
```

## Parallel Example: User Story 3

```text
Task: T021 Implement explicit structural and semantic analysis command flows in src/SemanticSearch.Application/Quality/Commands/RunStructuralDuplicationAnalysisCommand.cs, src/SemanticSearch.Application/Quality/Commands/RunStructuralDuplicationAnalysisCommandHandler.cs, src/SemanticSearch.Application/Quality/Commands/RunSemanticDuplicationAnalysisCommand.cs, and src/SemanticSearch.Application/Quality/Commands/RunSemanticDuplicationAnalysisCommandHandler.cs
Task: T022 Add validators and refresh policies for on-demand analysis in src/SemanticSearch.Application/Quality/Validators/RunStructuralDuplicationAnalysisCommandValidator.cs, src/SemanticSearch.Application/Quality/Validators/RunSemanticDuplicationAnalysisCommandValidator.cs, and src/SemanticSearch.Application/Quality/QualityRefreshPolicy.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1.
2. Complete Phase 2.
3. Complete Phase 3 (US1).
4. Validate the US1 independent test before expanding scope.

### Incremental Delivery

1. Deliver US1 to establish the quality snapshot experience.
2. Deliver US2 to make every finding inspectable in context.
3. Deliver US3 to expose stable on-demand analysis and latest-result retrieval APIs.
4. Finish with Phase 6 polish and smoke validation.

### Notes

- Every task line follows the required checklist format: checkbox, task ID, optional `[P]`, optional story label, and exact file path.
- `[P]` marks tasks that can run in parallel because they touch separate files or depend only on completed shared foundations.
- The suggested MVP scope is Phase 3 (US1) because it delivers the first usable quality dashboard outcome.


