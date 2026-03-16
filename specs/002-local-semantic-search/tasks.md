# Tasks: Local Semantic Search Workspace

**Input**: Design documents from `/specs/002-local-semantic-search/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: No explicit TDD/test-first requirement was stated in the feature specification, so this task list does not include standalone test-writing tasks. Validation is captured through each story's independent test criteria and the final smoke-validation tasks.

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Prepare the existing solution host for a combined local UI + API delivery model.

- [X] T001 Update Blazor hosting dependencies and static-web-asset settings in `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj`
- [X] T002 [P] Add UI, indexing, and absolute-path configuration sections in `src/SemanticSearch.WebApi/appsettings.json` and `src/SemanticSearch.WebApi/appsettings.Development.json`
- [X] T003 [P] Create shared presentation imports, layout shell, and base styles in `src/SemanticSearch.WebApi/Components/_Imports.razor`, `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor`, and `src/SemanticSearch.WebApi/wwwroot/css/app.css`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared domain, persistence, and presentation foundations required by every user story.

**Critical**: No user story work should start until this phase is complete.

- [X] T004 Expand workspace domain models in `src/SemanticSearch.Domain/Entities/ProjectWorkspace.cs`, `src/SemanticSearch.Domain/Entities/IndexedFile.cs`, `src/SemanticSearch.Domain/Entities/IndexingRun.cs`, and `src/SemanticSearch.Domain/ValueObjects/ProjectTreeNode.cs`
- [X] T005 [P] Add shared application interfaces in `src/SemanticSearch.Application/Common/Interfaces/IProjectWorkspaceRepository.cs`, `src/SemanticSearch.Application/Common/Interfaces/IProjectFileRepository.cs`, `src/SemanticSearch.Application/Common/Interfaces/IProjectTreeService.cs`, and `src/SemanticSearch.Application/Common/Interfaces/IProjectFileReader.cs`
- [X] T006 [P] Extend SQLite schema bootstrap and repository infrastructure in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/VectorStore/SqliteSchemaInitializer.cs`
- [X] T007 [P] Implement shared filesystem metadata and tree support in `src/SemanticSearch.Infrastructure/FileSystem/ProjectCatalogService.cs`, `src/SemanticSearch.Infrastructure/FileSystem/ProjectFileReader.cs`, and `src/SemanticSearch.Infrastructure/ProjectTree/ProjectTreeBuilder.cs`
- [X] T008 Update background indexing orchestration, per-project locking, and run-state tracking in `src/SemanticSearch.Infrastructure/Indexing/IndexingChannel.cs` and `src/SemanticSearch.Infrastructure/Indexing/IndexingWorker.cs`
- [X] T009 Wire repositories, options, structured logging, and problem responses in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`, `src/SemanticSearch.WebApi/Program.cs`, and `src/SemanticSearch.WebApi/Middleware/ExceptionHandlingMiddleware.cs`
- [X] T010 [P] Add a shared UI API client and workspace navigation state in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` and `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`

**Checkpoint**: Foundation ready; user-story implementation can proceed.

---

## Phase 3: User Story 1 - Index a Project Workspace (Priority: P1)

**Goal**: Let a user or AI agent register a local project path under a project key and run a duplicate-safe full indexing workflow.

**Independent Test**: Submit a valid `projectPath` and `projectKey`, confirm the request is accepted, confirm the project workspace is created, and verify indexing transitions to completion without duplicate searchable records on re-run.

- [X] T011 [US1] Implement the full project indexing command flow in `src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommand.cs`, `src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommandHandler.cs`, and `src/SemanticSearch.Application/Indexing/Validators/IndexProjectCommandValidator.cs`
- [X] T012 [P] [US1] Update project scanning and chunk-generation rules in `src/SemanticSearch.Infrastructure/FileSystem/ProjectScanner.cs` and `src/SemanticSearch.Infrastructure/FileSystem/FileChunker.cs`
- [X] T013 [US1] Persist project workspace creation and duplicate-safe reindex behavior in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/Indexing/IndexingWorker.cs`
- [X] T014 [US1] Expose `POST /api/project/index` in `src/SemanticSearch.WebApi/Controllers/ProjectController.cs` and `src/SemanticSearch.WebApi/Contracts/Projects/IndexProjectDtos.cs`
- [X] T015 [US1] Build the indexing page and form in `src/SemanticSearch.WebApi/Components/Pages/Indexing.razor` and `src/SemanticSearch.WebApi/Components/Indexing/IndexProjectForm.razor`
- [X] T016 [US1] Connect indexing submissions and queued/running/success/error UI states in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` and `src/SemanticSearch.WebApi/Components/Indexing/IndexProjectStatus.razor`

**Checkpoint**: User Story 1 is independently functional when a new project can be indexed from the UI or API.

---

## Phase 4: User Story 2 - Search Within a Selected Project (Priority: P1)

**Goal**: Let a user or AI agent run semantic and exact searches within one selected project workspace and review ranked, readable results.

**Independent Test**: With one indexed project available, run both semantic and exact searches for the selected project key and confirm the results are ranked, isolated to that project, and displayed with readable snippets.

- [X] T017 [US2] Implement semantic search query handling in `src/SemanticSearch.Application/Search/Queries/SearchSemanticQuery.cs`, `src/SemanticSearch.Application/Search/Queries/SearchSemanticQueryHandler.cs`, and `src/SemanticSearch.Application/Search/Validators/SearchSemanticQueryValidator.cs`
- [X] T018 [P] [US2] Implement exact-search query handling in `src/SemanticSearch.Application/Search/Queries/SearchExactQuery.cs`, `src/SemanticSearch.Application/Search/Queries/SearchExactQueryHandler.cs`, and `src/SemanticSearch.Application/Search/Validators/SearchExactQueryValidator.cs`
- [X] T019 [US2] Extend ranked semantic and exact search persistence in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/Search/ExactSearchService.cs`
- [X] T020 [US2] Expose `POST /api/search/semantic` and `POST /api/search/exact` in `src/SemanticSearch.WebApi/Controllers/SearchController.cs` and `src/SemanticSearch.WebApi/Contracts/Search/SearchDtos.cs`
- [X] T021 [US2] Build the search workspace page in `src/SemanticSearch.WebApi/Components/Pages/Search.razor` and `src/SemanticSearch.WebApi/Components/Search/SearchWorkspace.razor`
- [X] T022 [US2] Render search results with mode toggle, scores, and readable snippets in `src/SemanticSearch.WebApi/Components/Search/SearchResults.razor` and `src/SemanticSearch.WebApi/wwwroot/css/app.css`

**Checkpoint**: User Story 2 is independently functional when search works from both the UI and API for one selected project key.

---

## Phase 5: User Story 3 - Explore Project Structure and Read Files (Priority: P2)

**Goal**: Let a user browse the indexed project tree and open full file contents without leaving the workspace.

**Independent Test**: Select an indexed project, open the explorer, expand folders, open a file, and confirm the full contents render correctly; verify that missing-file errors are shown without breaking the rest of the explorer.

- [X] T023 [US3] Implement project-tree and file-read query handling in `src/SemanticSearch.Application/Projects/Queries/GetProjectTreeQuery.cs`, `src/SemanticSearch.Application/Files/Queries/ReadProjectFileQuery.cs`, and their validator files under `src/SemanticSearch.Application/Projects/Validators/` and `src/SemanticSearch.Application/Files/Validators/`
- [X] T024 [P] [US3] Implement indexed tree projection and safe file-reading services in `src/SemanticSearch.Infrastructure/ProjectTree/ProjectTreeBuilder.cs` and `src/SemanticSearch.Infrastructure/FileSystem/ProjectFileReader.cs`
- [X] T025 [US3] Expose `GET /api/project/tree/{projectKey}` and `POST /api/file/read` in `src/SemanticSearch.WebApi/Controllers/ProjectController.cs`, `src/SemanticSearch.WebApi/Controllers/FileController.cs`, and `src/SemanticSearch.WebApi/Contracts/Files/FileReadDtos.cs`
- [X] T026 [US3] Build the explorer page, tree, and file viewer in `src/SemanticSearch.WebApi/Components/Pages/Explorer.razor`, `src/SemanticSearch.WebApi/Components/Explorer/ProjectTree.razor`, and `src/SemanticSearch.WebApi/Components/Explorer/FileViewer.razor`
- [X] T027 [US3] Connect explorer selections, file loading, and missing-file error states in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` and `src/SemanticSearch.WebApi/Components/Explorer/FileViewer.razor`

**Checkpoint**: User Story 3 is independently functional when the explorer and file reader work for an already indexed project.

---

## Phase 6: User Story 4 - Monitor Status and Keep an Index Current (Priority: P2)

**Goal**: Let operators monitor active workspaces, see indexing status, and refresh a single file without rebuilding the entire project.

**Independent Test**: View the dashboard for indexed projects, confirm status values update during indexing, submit a single-file refresh request, and verify the changed file is re-indexed without rebuilding unrelated files.

- [X] T028 [US4] Implement project-status and single-file refresh application flows in `src/SemanticSearch.Application/Status/Queries/GetProjectStatusQuery.cs`, `src/SemanticSearch.Application/Status/Queries/GetProjectStatusQueryHandler.cs`, `src/SemanticSearch.Application/Indexing/Commands/RefreshProjectFileCommand.cs`, and `src/SemanticSearch.Application/Indexing/Validators/RefreshProjectFileCommandValidator.cs`
- [X] T029 [P] [US4] Persist indexing run status, conflict handling, and single-file replacement logic in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs` and `src/SemanticSearch.Infrastructure/Indexing/IndexingWorker.cs`
- [X] T030 [US4] Expose `GET /api/project/status/{projectKey}` and `POST /api/project/index/file` in `src/SemanticSearch.WebApi/Controllers/ProjectController.cs` and `src/SemanticSearch.WebApi/Contracts/Projects/ProjectStatusDtos.cs`
- [X] T031 [US4] Build the dashboard page and status components in `src/SemanticSearch.WebApi/Components/Pages/Home.razor`, `src/SemanticSearch.WebApi/Components/Dashboard/ProjectDashboard.razor`, and `src/SemanticSearch.WebApi/Components/Dashboard/ProjectStatusCard.razor`
- [X] T032 [US4] Connect dashboard polling, in-progress indicators, and single-file refresh actions in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` and `src/SemanticSearch.WebApi/Components/Dashboard/ProjectDashboard.razor`

**Checkpoint**: User Story 4 is independently functional when dashboard status and single-file refresh work against existing indexed projects.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Finish the feature with documentation, usability, and smoke validation across all stories.

- [X] T033 [P] Update deployment and usage documentation in `README.md` and `specs/002-local-semantic-search/quickstart.md`
- [X] T034 [P] Add accessibility, empty-state, and error-id polish in `src/SemanticSearch.WebApi/Components/Pages/*.razor`, `src/SemanticSearch.WebApi/Components/**/*.razor`, and `src/SemanticSearch.WebApi/Middleware/ExceptionHandlingMiddleware.cs`
- [X] T035 Validate the OpenAPI contract, quickstart flow, and IIS smoke steps in `specs/002-local-semantic-search/contracts/local-semantic-search.openapi.yaml`, `specs/002-local-semantic-search/quickstart.md`, and `README.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- Phase 1 depends on no prior work.
- Phase 2 depends on Phase 1 and blocks all user-story phases.
- Phases 3-6 depend on Phase 2.
- Phase 7 depends on the completion of the user stories included in the release.

### User Story Dependencies

- US1 depends only on Phase 2 and is the MVP slice.
- US2 depends only on Phase 2 for implementation, but its end-to-end validation uses indexed data produced by US1 or equivalent seeded workspace data.
- US3 depends only on Phase 2 for implementation, but its end-to-end validation uses an indexed project workspace produced by US1 or equivalent seeded workspace data.
- US4 depends only on Phase 2 for implementation, but its dashboard and refresh flows are meaningful once US1 indexing exists.

### Suggested Completion Order

1. Phase 1: Setup
2. Phase 2: Foundational
3. Phase 3: US1
4. Validate MVP
5. Phase 4: US2
6. Phase 5: US3
7. Phase 6: US4
8. Phase 7: Polish

---

## Parallel Opportunities

- `T002` and `T003` can run in parallel after `T001`.
- `T005`, `T006`, `T007`, and `T010` can run in parallel after `T004`.
- In US1, `T012` can run in parallel with `T011` once the foundational interfaces are in place.
- In US2, `T018` can run in parallel with `T017`.
- In US3, `T024` can run in parallel with `T023`.
- In US4, `T029` can run in parallel with `T028`.

## Parallel Example: User Story 1

```text
Task: T011 Implement the full project indexing command flow in src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommand.cs, src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommandHandler.cs, and src/SemanticSearch.Application/Indexing/Validators/IndexProjectCommandValidator.cs
Task: T012 Update project scanning and chunk-generation rules in src/SemanticSearch.Infrastructure/FileSystem/ProjectScanner.cs and src/SemanticSearch.Infrastructure/FileSystem/FileChunker.cs
```

## Parallel Example: User Story 2

```text
Task: T017 Implement semantic search query handling in src/SemanticSearch.Application/Search/Queries/SearchSemanticQuery.cs, src/SemanticSearch.Application/Search/Queries/SearchSemanticQueryHandler.cs, and src/SemanticSearch.Application/Search/Validators/SearchSemanticQueryValidator.cs
Task: T018 Implement exact-search query handling in src/SemanticSearch.Application/Search/Queries/SearchExactQuery.cs, src/SemanticSearch.Application/Search/Queries/SearchExactQueryHandler.cs, and src/SemanticSearch.Application/Search/Validators/SearchExactQueryValidator.cs
```

## Parallel Example: User Story 3

```text
Task: T023 Implement project-tree and file-read query handling in src/SemanticSearch.Application/Projects/Queries/GetProjectTreeQuery.cs, src/SemanticSearch.Application/Files/Queries/ReadProjectFileQuery.cs, and validator files under src/SemanticSearch.Application/Projects/Validators/ and src/SemanticSearch.Application/Files/Validators/
Task: T024 Implement indexed tree projection and safe file-reading services in src/SemanticSearch.Infrastructure/ProjectTree/ProjectTreeBuilder.cs and src/SemanticSearch.Infrastructure/FileSystem/ProjectFileReader.cs
```

## Parallel Example: User Story 4

```text
Task: T028 Implement project-status and single-file refresh application flows in src/SemanticSearch.Application/Status/Queries/GetProjectStatusQuery.cs, src/SemanticSearch.Application/Status/Queries/GetProjectStatusQueryHandler.cs, src/SemanticSearch.Application/Indexing/Commands/RefreshProjectFileCommand.cs, and src/SemanticSearch.Application/Indexing/Validators/RefreshProjectFileCommandValidator.cs
Task: T029 Persist indexing run status, conflict handling, and single-file replacement logic in src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs and src/SemanticSearch.Infrastructure/Indexing/IndexingWorker.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1.
2. Complete Phase 2.
3. Complete Phase 3 (US1).
4. Validate the US1 independent test before expanding scope.

### Incremental Delivery

1. Deliver US1 to establish indexing and project-workspace creation.
2. Deliver US2 to make indexed workspaces searchable.
3. Deliver US3 to add project exploration and full-file reading.
4. Deliver US4 to add operational visibility and single-file refresh.
5. Finish with Phase 7 polish and smoke validation.

### Notes

- Every task line follows the required checklist format: checkbox, task ID, optional `[P]`, optional story label, and exact file path.
- `[P]` marks tasks that can run in parallel because they touch separate files or depend only on shared completed foundation work.
- Story phases are independently testable increments even when their end-to-end demo uses indexed data created by US1.
