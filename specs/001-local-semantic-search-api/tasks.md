# Tasks: Local Semantic Search Web API

**Input**: Design documents from `/specs/001-local-semantic-search-api/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/openapi.yaml, quickstart.md

**Tests**: Not explicitly requested in the feature specification — test tasks omitted.

**Organization**: Tasks grouped by user story for independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Multi-project .NET**: `src/SemanticSearch.{Layer}/` at repository root
- **Tests**: `tests/SemanticSearch.{Layer}.Tests/`
- Based on Clean Architecture structure from plan.md

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create solution structure, projects, and NuGet dependencies

- [X] T001 Create .NET solution file and all project scaffolds: `SemanticSearch.sln`, `src/SemanticSearch.Domain/SemanticSearch.Domain.csproj`, `src/SemanticSearch.Application/SemanticSearch.Application.csproj`, `src/SemanticSearch.Infrastructure/SemanticSearch.Infrastructure.csproj`, `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj`
- [X] T002 Configure project references enforcing Clean Architecture dependency rule: Domain←Application←Infrastructure, Domain←Application←WebApi, Infrastructure←WebApi in each `.csproj`
- [X] T003 [P] Add NuGet packages to SemanticSearch.Application: MediatR 14.1.0, FluentValidation 12.1.1, FluentValidation.DependencyInjectionExtensions 12.1.1 in `src/SemanticSearch.Application/SemanticSearch.Application.csproj`
- [X] T004 [P] Add NuGet packages to SemanticSearch.Infrastructure: Microsoft.ML.OnnxRuntime 1.24.3, Microsoft.ML.Tokenizers 2.0.0, Microsoft.Data.Sqlite 10.0.5 in `src/SemanticSearch.Infrastructure/SemanticSearch.Infrastructure.csproj`
- [X] T005 [P] Create `.gitignore` with entries for bin/, obj/, models/, data/, *.db, .vs/ at repository root `.gitignore`
- [X] T006 [P] Create `appsettings.json` with SemanticSearch configuration section (ModelPath, DatabasePath, Chunking, Indexing settings) in `src/SemanticSearch.WebApi/appsettings.json`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain types, application abstractions, MediatR pipeline, DI wiring, and embedding infrastructure that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T007 [P] Create domain entity `Chunk` with all fields (Id, ProjectKey, FilePath, StartLine, EndLine, Content, Embedding, CreatedAt) in `src/SemanticSearch.Domain/Entities/Chunk.cs`
- [X] T008 [P] Create domain entity `ProjectMetadata` with all fields (ProjectKey, TotalFiles, TotalChunks, LastUpdated) in `src/SemanticSearch.Domain/Entities/ProjectMetadata.cs`
- [X] T009 [P] Create value object `ChunkInfo` (FilePath, Content, StartLine, EndLine) in `src/SemanticSearch.Domain/ValueObjects/ChunkInfo.cs`
- [X] T010 [P] Create value object `EmbeddingVector` wrapping float[384] with equality semantics in `src/SemanticSearch.Domain/ValueObjects/EmbeddingVector.cs`
- [X] T011 [P] Create value object `SearchResult` (FilePath, RelevanceScore, Snippet, StartLine, EndLine) in `src/SemanticSearch.Domain/ValueObjects/SearchResult.cs`
- [X] T012 [P] Create value object `IndexingStatus` (IsIndexed, TotalFiles, TotalChunks, LastUpdated) in `src/SemanticSearch.Domain/ValueObjects/IndexingStatus.cs`
- [X] T013 [P] Define interface `IEmbeddingService` with method `Task<EmbeddingVector> GenerateEmbeddingAsync(string text)` in `src/SemanticSearch.Domain/Interfaces/IEmbeddingService.cs`
- [X] T014 [P] Define interface `IVectorStore` with methods for upserting chunks, searching by project key with cosine similarity, getting project metadata, and deleting stale chunks in `src/SemanticSearch.Domain/Interfaces/IVectorStore.cs`
- [X] T015 [P] Define interface `IFileChunker` with method `IReadOnlyList<ChunkInfo> ChunkFile(string filePath, int chunkSize, int overlap)` in `src/SemanticSearch.Domain/Interfaces/IFileChunker.cs`
- [X] T016 [P] Define interface `IProjectScanner` with method `IReadOnlyList<string> ScanProject(string projectPath, IReadOnlySet<string> excludedDirs, IReadOnlySet<string> allowedExtensions)` in `src/SemanticSearch.Domain/Interfaces/IProjectScanner.cs`
- [X] T017 [P] Create `SemanticSearchOptions` configuration POCO mapping to appsettings.json section in `src/SemanticSearch.Application/Common/SemanticSearchOptions.cs`
- [X] T018 [P] Implement `ValidationBehavior<TRequest, TResponse>` MediatR pipeline behavior that runs FluentValidation validators and throws `ValidationException` on failure in `src/SemanticSearch.Application/Common/Behaviors/ValidationBehavior.cs`
- [X] T019 [P] Implement `LoggingBehavior<TRequest, TResponse>` MediatR pipeline behavior that logs request name and duration via ILogger in `src/SemanticSearch.Application/Common/Behaviors/LoggingBehavior.cs`
- [X] T020 Implement `OnnxEmbeddingService : IEmbeddingService` — load singleton InferenceSession from ContentRootPath, tokenize with WordPieceTokenizer, run ONNX inference, mean-pool, L2-normalize to 384-dim vector in `src/SemanticSearch.Infrastructure/Embedding/OnnxEmbeddingService.cs`
- [X] T021 Implement `SqliteVectorStore : IVectorStore` — initialize SQLite database at absolute path, create Chunks and ProjectMetadata tables, implement upsert/search/status/delete operations using Microsoft.Data.Sqlite in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs`
- [X] T022 Implement `FileChunker : IFileChunker` — line-based sliding window (default 200 lines, 40-line overlap), binary file detection (null bytes in first 8KB), return ChunkInfo list with 1-based line numbers in `src/SemanticSearch.Infrastructure/FileSystem/FileChunker.cs`
- [X] T023 Implement `ProjectScanner : IProjectScanner` — recursive directory traversal filtering by excluded directories and allowed file extensions, skip locked/unreadable files with warning logging in `src/SemanticSearch.Infrastructure/FileSystem/ProjectScanner.cs`
- [X] T024 Create `InfrastructureServiceRegistration` extension method registering all infrastructure services (IEmbeddingService, IVectorStore, IFileChunker, IProjectScanner) with DI container using absolute paths from IWebHostEnvironment in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`
- [X] T025 Implement global exception handling middleware that maps `ValidationException` to 400 ValidationProblemDetails and unhandled exceptions to 500 ProblemDetails in `src/SemanticSearch.WebApi/Middleware/ExceptionHandlingMiddleware.cs`
- [X] T026 Configure `Program.cs` — register MediatR with pipeline behaviors, FluentValidation validators via assembly scanning, Infrastructure services, SemanticSearchOptions binding, exception middleware, and controller routing in `src/SemanticSearch.WebApi/Program.cs`

**Checkpoint**: Foundation ready — MediatR pipeline works, embedding service can generate vectors, SQLite vector store initializes, file system traversal and chunking work

---

## Phase 3: User Story 1 — Index a Local Codebase (Priority: P1) 🎯 MVP

**Goal**: AI Agent can POST to `/api/search/index` with a project path and key; system queues background indexing that traverses, chunks, embeds, and stores vectors.

**Independent Test**: Send POST with valid project path → status endpoint shows IsIndexed=true with correct file/chunk counts.

### Implementation for User Story 1

- [X] T027 [P] [US1] Create `IndexProjectCommand` MediatR request record (ProjectPath, ProjectKey) and `IndexProjectResponse` record (ProjectKey, Status, Message) in `src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommand.cs`
- [X] T028 [P] [US1] Create `IndexProjectCommandValidator` using FluentValidation — validate ProjectKey not empty and max 128 chars, ProjectPath not empty and directory must exist in `src/SemanticSearch.Application/Indexing/Validators/IndexProjectCommandValidator.cs`
- [X] T029 [P] [US1] Create `IndexingChannel` wrapper around `Channel<IndexProjectCommand>` for typed DI registration (unbounded, single consumer) in `src/SemanticSearch.Infrastructure/Indexing/IndexingChannel.cs`
- [X] T030 [US1] Implement `IndexProjectCommandHandler : IRequestHandler<IndexProjectCommand, IndexProjectResponse>` — validate and enqueue command to IndexingChannel, return 202 response in `src/SemanticSearch.Application/Indexing/Commands/IndexProjectCommandHandler.cs`
- [X] T031 [US1] Implement `IndexingWorker : BackgroundService` — reads from IndexingChannel, for each command: scan project → chunk files → generate embeddings → upsert to vector store → update ProjectMetadata → log completion/errors in `src/SemanticSearch.Infrastructure/Indexing/IndexingWorker.cs`
- [X] T032 [US1] Register IndexingChannel as singleton and IndexingWorker as hosted service in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`
- [X] T033 [US1] Create `SearchController` with POST action for `/api/search/index` — receive IndexRequest DTO, map to IndexProjectCommand, send via MediatR, return 202 Accepted with IndexResponse DTO in `src/SemanticSearch.WebApi/Controllers/SearchController.cs`

**Checkpoint**: POST /api/search/index accepts requests → background worker indexes the project → chunks stored in SQLite with embeddings

---

## Phase 4: User Story 2 — Search an Indexed Codebase (Priority: P1)

**Goal**: AI Agent can POST to `/api/search/query` with a query and project key; system returns semantically relevant code snippets ranked by cosine similarity.

**Independent Test**: After indexing a project, search for a known code pattern → verify correct file appears in top results with all metadata fields.

### Implementation for User Story 2

- [X] T034 [P] [US2] Create `SearchProjectQuery` MediatR request record (Query, ProjectKey, TopK) and `SearchProjectResponse` record (Results list of SearchResult) in `src/SemanticSearch.Application/Search/Queries/SearchProjectQuery.cs`
- [X] T035 [P] [US2] Create `SearchProjectQueryValidator` using FluentValidation — validate Query not empty, ProjectKey not empty and max 128 chars, TopK between 1 and 100 in `src/SemanticSearch.Application/Search/Validators/SearchProjectQueryValidator.cs`
- [X] T036 [US2] Implement `SearchProjectQueryHandler : IRequestHandler<SearchProjectQuery, SearchProjectResponse>` — generate query embedding via IEmbeddingService, retrieve chunks from IVectorStore filtered by ProjectKey, compute cosine similarity (dot product of normalized vectors), sort descending, take topK, map to SearchResult in `src/SemanticSearch.Application/Search/Queries/SearchProjectQueryHandler.cs`
- [X] T037 [US2] Add POST action for `/api/search/query` to SearchController — receive SearchRequest DTO, map to SearchProjectQuery, send via MediatR, return 200 OK with SearchResponse DTO in `src/SemanticSearch.WebApi/Controllers/SearchController.cs`

**Checkpoint**: POST /api/search/query returns ranked results filtered by project key with FilePath, RelevanceScore, Snippet, StartLine, EndLine

---

## Phase 5: User Story 3 — Check Indexing Status (Priority: P2)

**Goal**: AI Agent can GET `/api/search/status/{projectKey}` to see indexing statistics (IsIndexed, TotalFiles, TotalChunks, LastUpdated).

**Independent Test**: Index a project → GET status → verify counts match expected files/chunks.

### Implementation for User Story 3

- [X] T038 [P] [US3] Create `GetProjectStatusQuery` MediatR request record (ProjectKey) and `GetProjectStatusResponse` record (IsIndexed, TotalFiles, TotalChunks, LastUpdated) in `src/SemanticSearch.Application/Status/Queries/GetProjectStatusQuery.cs`
- [X] T039 [P] [US3] Create `GetProjectStatusQueryValidator` using FluentValidation — validate ProjectKey not empty and max 128 chars in `src/SemanticSearch.Application/Status/Queries/GetProjectStatusQueryValidator.cs`
- [X] T040 [US3] Implement `GetProjectStatusQueryHandler : IRequestHandler<GetProjectStatusQuery, GetProjectStatusResponse>` — query IVectorStore for ProjectMetadata by key, map to response (return IsIndexed=false if not found) in `src/SemanticSearch.Application/Status/Queries/GetProjectStatusQueryHandler.cs`
- [X] T041 [US3] Add GET action for `/api/search/status/{projectKey}` to SearchController — extract projectKey from route, create GetProjectStatusQuery, send via MediatR, return 200 OK with StatusResponse DTO in `src/SemanticSearch.WebApi/Controllers/SearchController.cs`

**Checkpoint**: All three API endpoints functional — index, search, and status

---

## Phase 6: User Story 4 — IIS Hosting & Deployment (Priority: P2)

**Goal**: API runs stably on IIS with absolute paths, background service survives app pool recycling, all configuration documented.

**Independent Test**: Deploy to IIS → run index + search workflow → verify data persists across app pool recycle.

### Implementation for User Story 4

- [X] T042 [P] [US4] Create `web.config` with IIS hosting settings (ASP.NET Core Module, stdout logging, process path) in `src/SemanticSearch.WebApi/web.config`
- [X] T043 [P] [US4] Create IIS deployment documentation with app pool settings (AlwaysRunning, idleTimeout=0, periodicRestart=0, preloadEnabled=true) in `README.md`
- [X] T044 [US4] Verify all path resolution uses `IWebHostEnvironment.ContentRootPath` — audit `OnnxEmbeddingService`, `SqliteVectorStore`, and `InfrastructureServiceRegistration` for any relative path usage in `src/SemanticSearch.Infrastructure/`

**Checkpoint**: API deployable to IIS with documented configuration; absolute paths verified

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, configuration validation, and final hardening

- [X] T045 [P] Create complete `README.md` with project overview, prerequisites, build instructions, IIS deployment notes, NuGet package list, configuration reference, and API usage examples at repository root `README.md`
- [X] T046 [P] Add startup validation that checks ONNX model files exist at configured path and logs clear error on missing files in `src/SemanticSearch.WebApi/Program.cs`
- [X] T047 [P] Add startup validation that ensures SQLite data directory exists (create if missing) in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs`
- [X] T048 Run quickstart.md validation — follow all steps in `specs/001-local-semantic-search-api/quickstart.md` and verify endpoints return expected responses

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS all user stories
- **US1: Indexing (Phase 3)**: Depends on Phase 2
- **US2: Search (Phase 4)**: Depends on Phase 2 (and needs indexed data from US1 to verify, but code is independent)
- **US3: Status (Phase 5)**: Depends on Phase 2
- **US4: IIS (Phase 6)**: Can start after Phase 2, but best after Phases 3–5 are complete
- **Polish (Phase 7)**: Depends on all previous phases

### User Story Dependencies

- **US1 (Index)**: No dependencies on other stories — can start immediately after Phase 2
- **US2 (Search)**: Code independent of US1, but functional testing requires US1 to create indexed data
- **US3 (Status)**: Code independent of US1/US2 — reads ProjectMetadata written by US1's background worker
- **US4 (IIS)**: Cross-cutting — best done after core endpoints work
- **US5 (Offline)**: No separate tasks — validated by US1+US2 running without network; all dependencies (ONNX, SQLite) are local by design

### Within Each User Story

- Command/Query definition before handler
- Validator before handler (validators referenced by pipeline)
- Handler before controller action
- Infrastructure support (channels, workers) before handlers that use them

### Parallel Opportunities

- **Phase 1**: T003, T004, T005, T006 can all run in parallel
- **Phase 2**: T007–T016 (domain types + interfaces) all in parallel, then T017–T019 (application plumbing) in parallel, then T020–T023 (infrastructure impls) partially parallel, then T024–T026 (wiring)
- **Phase 3–5**: Once Phase 2 is done, US1/US2/US3 can start in parallel (different files)
- **Phase 6**: T042, T043 in parallel
- **Phase 7**: T045, T046, T047 in parallel

---

## Parallel Example: Phase 2 (Foundational)

```bash
# Wave 1 — All domain types and interfaces (all different files):
T007: Chunk entity
T008: ProjectMetadata entity
T009: ChunkInfo value object
T010: EmbeddingVector value object
T011: SearchResult value object
T012: IndexingStatus value object
T013: IEmbeddingService interface
T014: IVectorStore interface
T015: IFileChunker interface
T016: IProjectScanner interface

# Wave 2 — Application plumbing (all different files):
T017: SemanticSearchOptions
T018: ValidationBehavior
T019: LoggingBehavior

# Wave 3 — Infrastructure implementations (different files):
T020: OnnxEmbeddingService
T021: SqliteVectorStore
T022: FileChunker
T023: ProjectScanner

# Wave 4 — Wiring (sequential):
T024: InfrastructureServiceRegistration
T025: ExceptionHandlingMiddleware
T026: Program.cs
```

## Parallel Example: Phases 3–5 (User Stories)

```bash
# After Phase 2 completes, all three user stories can start in parallel:

# Stream A (US1 — Indexing):
T027 → T028 → T029 → T030 → T031 → T032 → T033

# Stream B (US2 — Search):
T034 → T035 → T036 → T037

# Stream C (US3 — Status):
T038 → T039 → T040 → T041
```

---

## Implementation Strategy

### MVP First (User Story 1 + 2 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks everything)
3. Complete Phase 3: US1 — Indexing
4. Complete Phase 4: US2 — Search
5. **STOP and VALIDATE**: Index a real project, search it, verify results
6. This is the **MVP** — a working index + search system

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. Add US1 (Indexing) → Can queue and process indexing jobs (MVP start)
3. Add US2 (Search) → Full index + search workflow (MVP complete!)
4. Add US3 (Status) → Workflow automation support
5. Add US4 (IIS) → Production deployment ready
6. Polish → Documentation and hardening

### Suggested MVP Scope

**US1 (Index) + US2 (Search)** = minimal viable product. An AI Agent can index a codebase and search it semantically. US3 (Status) and US4 (IIS) add operational maturity but aren't needed for the core value proposition.

---

## Notes

- User Story 5 (Offline) has no separate tasks — it's a constraint validated by US1+US2 using all-local dependencies (ONNX Runtime, SQLite)
- The SearchController handles all three endpoints in a single controller file (updated incrementally in T033, T037, T041)
- T032 updates the DI registration file created in T024 — these touch the same file but at different times
- The ONNX model files must be downloaded manually before running (see quickstart.md)

