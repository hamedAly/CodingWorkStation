# Tasks: Visual Architecture & Impact Analysis

**Input**: Design documents from `/specs/006-visual-architecture-analysis/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/architecture.openapi.yaml, quickstart.md

**Tests**: Not requested in the feature specification — test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization — frontend libraries and JS interop shell

- [X] T001 Download Vis.js Network standalone library to `src/SemanticSearch.WebApi/wwwroot/lib/vis-network/vis-network.min.js`
- [X] T002 [P] Download Mermaid.js library to `src/SemanticSearch.WebApi/wwwroot/lib/mermaid/mermaid.min.js`
- [X] T003 [P] Download chartjs-chart-treemap plugin to `src/SemanticSearch.WebApi/wwwroot/lib/chartjs-chart-treemap/chartjs-chart-treemap.min.js`
- [X] T004 Add `<script>` references for vis-network, mermaid, and chartjs-chart-treemap in `src/SemanticSearch.WebApi/Components/App.razor`
- [X] T005 Create JS interop shell file `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` with namespace `architectureDashboard` and stub functions: `renderDependencyGraph`, `destroyDependencyGraph`, `renderHeatmap`, `destroyHeatmap`, `renderErDiagram`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, repository interface, and SQLite schema that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T006 [P] Create `DependencyAnalysisStatus` enum (Queued, Running, Completed, Failed) in `src/SemanticSearch.Domain/ValueObjects/DependencyAnalysisStatus.cs`
- [X] T007 [P] Create `DependencyNodeKind` enum (Class, Method) in `src/SemanticSearch.Domain/ValueObjects/DependencyNodeKind.cs`
- [X] T008 [P] Create `DependencyRelationshipType` enum (Invocation, Inheritance, TypeReference, Construction) in `src/SemanticSearch.Domain/ValueObjects/DependencyRelationshipType.cs`
- [X] T009 [P] Create `DependencyAnalysisRun` entity in `src/SemanticSearch.Domain/Entities/DependencyAnalysisRun.cs` with RunId, ProjectKey, Status, RequestedUtc, StartedUtc, CompletedUtc, TotalFilesScanned, TotalNodesFound, TotalEdgesFound, FailureReason per data-model.md
- [X] T010 [P] Create `DependencyNode` entity in `src/SemanticSearch.Domain/Entities/DependencyNode.cs` with NodeId, ProjectKey, RunId, Name, FullName, Kind, Namespace, FilePath, StartLine, ParentNodeId per data-model.md
- [X] T011 [P] Create `DependencyEdge` entity in `src/SemanticSearch.Domain/Entities/DependencyEdge.cs` with EdgeId, ProjectKey, RunId, SourceNodeId, TargetNodeId, RelationshipType per data-model.md
- [X] T012 Create `IDependencyRepository` interface in `src/SemanticSearch.Domain/Interfaces/IDependencyRepository.cs` with GetLatestRunAsync, ListNodesAsync, ListEdgesAsync, ReplaceDependencyGraphAsync per quickstart.md Step 2
- [X] T013 Add DependencyAnalysisRuns, DependencyNodes, DependencyEdges CREATE TABLE + CREATE INDEX statements to `SqliteSchemaInitializer.Schema` in `src/SemanticSearch.Infrastructure/VectorStore/SqliteSchemaInitializer.cs` per data-model.md SQL definitions
- [X] T014 Implement `IDependencyRepository` methods in `src/SemanticSearch.Infrastructure/VectorStore/SqliteVectorStore.cs`: GetLatestRunAsync, ListNodesAsync, ListEdgesAsync, ReplaceDependencyGraphAsync following the existing IQualityRepository patterns (transaction, delete-then-insert, reader mapping)
- [X] T015 Create `ArchitectureController` in `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` with constructor injecting IMediator — empty action methods will be filled per user story

**Checkpoint**: Foundation ready — domain model, persistence, and controller shell in place

---

## Phase 3: User Story 1 — Explore Dependency Graph (Priority: P1) 🎯 MVP

**Goal**: Analyze C# source files with Roslyn, extract class/method call relationships, persist the graph, expose via API, and render as an interactive Vis.js network with click-to-highlight and namespace filtering.

**Independent Test**: Index a sample project → POST to run dependency analysis → GET dependency graph → verify interactive graph renders with correct nodes/edges in browser.

### Infrastructure for User Story 1

- [X] T016 [P] [US1] Create `IDependencyExtractor` interface in `src/SemanticSearch.Domain/Interfaces/IDependencyExtractor.cs` with method `Task<DependencyExtractionResult> ExtractAsync(string projectKey, CancellationToken ct)` returning a result record containing nodes, edges, and file count
- [X] T017 [US1] Implement `RoslynDependencyExtractor` in `src/SemanticSearch.Infrastructure/Architecture/RoslynDependencyExtractor.cs` — inject IProjectFileRepository and QualityFileFilter; first pass: parse all .cs files, extract ClassDeclarationSyntax and MethodDeclarationSyntax to build symbol table of DependencyNode objects; second pass: walk method bodies for InvocationExpressionSyntax, MemberAccessExpressionSyntax, ObjectCreationExpressionSyntax, BaseListSyntax and match against symbol table to produce DependencyEdge objects; use ComputeContentHash for NodeId/EdgeId generation; handle circular references via visited-set tracking

### Application Layer for User Story 1

- [X] T018 [P] [US1] Create `RunDependencyAnalysisCommand` record (ProjectKey) and `RunDependencyAnalysisCommandHandler` in `src/SemanticSearch.Application/Architecture/Commands/RunDependencyAnalysisCommand.cs` — handler calls IDependencyExtractor.ExtractAsync, creates DependencyAnalysisRun entity, persists via IDependencyRepository.ReplaceDependencyGraphAsync, returns run result
- [X] T019 [P] [US1] Create `RunDependencyAnalysisCommandValidator` in `src/SemanticSearch.Application/Architecture/Validators/RunDependencyAnalysisCommandValidator.cs` — validate ProjectKey is not empty
- [X] T020 [P] [US1] Create `GetDependencyGraphQuery` record (ProjectKey, Namespace?, FilePath?) and `GetDependencyGraphQueryHandler` in `src/SemanticSearch.Application/Architecture/Queries/GetDependencyGraphQuery.cs` — handler calls IDependencyRepository.GetLatestRunAsync, ListNodesAsync, ListEdgesAsync; apply optional namespace prefix filter and filePath filter on nodes, then filter edges to only include edges where both source and target are in the filtered node set; return null if no run exists
- [X] T021 [P] [US1] Create `GetDependencyGraphQueryValidator` in `src/SemanticSearch.Application/Architecture/Validators/GetDependencyGraphQueryValidator.cs` — validate ProjectKey is not empty

### API Contracts for User Story 1

- [X] T022 [P] [US1] Create `DependencyGraphResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/DependencyGraphResponse.cs` with ProjectKey, RunId, AnalyzedUtc, TotalNodes, TotalEdges, Nodes (list), Edges (list) per OpenAPI schema
- [X] T023 [P] [US1] Create `DependencyNodeResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/DependencyNodeResponse.cs` with NodeId, Name, FullName, Kind, Namespace, FilePath, StartLine, ParentNodeId per OpenAPI schema
- [X] T024 [P] [US1] Create `DependencyEdgeResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/DependencyEdgeResponse.cs` with EdgeId, SourceNodeId, TargetNodeId, RelationshipType, SourceNodeName, TargetNodeName per OpenAPI schema
- [X] T025 [P] [US1] Create `DependencyAnalysisRunResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/DependencyAnalysisRunResponse.cs` with RunId, ProjectKey, Status, RequestedUtc, StartedUtc, CompletedUtc, TotalFilesScanned, TotalNodesFound, TotalEdgesFound, FailureReason per OpenAPI schema

### Controller Endpoints for User Story 1

- [X] T026 [US1] Add POST `RunDependencyAnalysis` action to `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` — route `/api/architecture/{projectKey}/dependency-graph`, sends RunDependencyAnalysisCommand via MediatR, maps to DependencyAnalysisRunResponse, returns 200/404/409 per OpenAPI contract
- [X] T027 [US1] Add GET `GetDependencyGraph` action to `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` — route `/api/architecture/{projectKey}/dependency-graph` with optional query params `namespace` and `filePath`, sends GetDependencyGraphQuery via MediatR, maps to DependencyGraphResponse, returns 200/404 per OpenAPI contract

### Frontend for User Story 1

- [X] T028 [US1] Implement `renderDependencyGraph(containerId, nodes, edges)` function in `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` — create vis.DataSet from nodes/edges arrays, configure force-directed layout options, create vis.Network instance, color Class nodes differently from Method nodes, add click event handler that highlights upstream/downstream connections and dims other nodes, add double-click to reset highlight
- [X] T029 [US1] Implement `destroyDependencyGraph(containerId)` function in `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` — destroy existing vis.Network instance and clean up memory
- [X] T030 [US1] Create `DependencyGraphView.razor` component in `src/SemanticSearch.WebApi/Components/Architecture/DependencyGraphView.razor` — accepts ProjectKey parameter; on load calls GET dependency graph API via WorkspaceApiClient; displays loading spinner while fetching; renders empty state with "Run Dependency Analysis" button when no data exists; on data received calls JS interop `architectureDashboard.renderDependencyGraph`; includes namespace/class filter text input that re-fetches with filter query params; includes "Run Analysis" button that POSTs to run analysis then refreshes graph

### DI Registration for User Story 1

- [X] T031 [US1] Register `IDependencyExtractor` → `RoslynDependencyExtractor` and `IDependencyRepository` → `SqliteVectorStore` in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

**Checkpoint**: Dependency graph fully functional — users can run analysis and explore interactive graph with click-to-highlight and filtering

---

## Phase 4: User Story 2 — View Code Duplication Heatmap (Priority: P2)

**Goal**: Display a treemap of project files colored by duplication density, using existing quality analysis data. Users hover for detail tooltips and click to navigate to findings.

**Independent Test**: Run quality analysis on a project with known duplicates → open Heatmap view → verify files with duplicates appear red and clean files appear green.

### Infrastructure for User Story 2

- [X] T032 [P] [US2] Create `IHeatmapDataBuilder` interface in `src/SemanticSearch.Domain/Interfaces/IHeatmapDataBuilder.cs` with method `Task<IReadOnlyList<FileHeatmapEntry>> BuildAsync(string projectKey, CancellationToken ct)` returning per-file heatmap entries
- [X] T033 [US2] Implement `HeatmapDataBuilder` in `src/SemanticSearch.Infrastructure/Architecture/HeatmapDataBuilder.cs` — inject IProjectFileRepository and IQualityRepository; query IndexedFiles for line counts; query DuplicationFindings + CodeRegions to count structural and semantic duplicates per relative file path; compute duplicationDensity = (structural + semantic) / totalLines; return list of FileHeatmapEntry read models

### Application Layer for User Story 2

- [X] T034 [P] [US2] Create `GetFileHeatmapQuery` record (ProjectKey) and `GetFileHeatmapQueryHandler` in `src/SemanticSearch.Application/Architecture/Queries/GetFileHeatmapQuery.cs` — handler calls IHeatmapDataBuilder.BuildAsync, returns null if no quality analysis run exists for the project
- [X] T035 [P] [US2] Create `GetFileHeatmapQueryValidator` in `src/SemanticSearch.Application/Architecture/Validators/GetFileHeatmapQueryValidator.cs` — validate ProjectKey is not empty

### API Contracts for User Story 2

- [X] T036 [P] [US2] Create `FileHeatmapResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/FileHeatmapResponse.cs` with ProjectKey, TotalFiles, Entries (list of FileHeatmapEntryResponse) per OpenAPI schema
- [X] T037 [P] [US2] Create `FileHeatmapEntryResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/FileHeatmapEntryResponse.cs` with RelativeFilePath, FileName, TotalLines, StructuralDuplicateCount, SemanticDuplicateCount, DuplicationDensity per OpenAPI schema

### Controller Endpoint for User Story 2

- [X] T038 [US2] Add GET `GetFileHeatmap` action to `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` — route `/api/architecture/{projectKey}/heatmap`, sends GetFileHeatmapQuery via MediatR, maps to FileHeatmapResponse, returns 200/404 per OpenAPI contract

### Frontend for User Story 2

- [X] T039 [US2] Implement `renderHeatmap(canvasId, entries)` function in `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` — register chartjs-chart-treemap controller with Chart.js; create treemap chart on canvas; size rectangles by `totalLines`; compute backgroundColor via HSL gradient: `hsl(120 - (density * 120), 70%, 50%)` from green (0 density) to red (max density); configure tooltip to show fileName, totalLines, structuralDuplicateCount, semanticDuplicateCount; add onClick handler that returns the clicked file's relativeFilePath
- [X] T040 [US2] Implement `destroyHeatmap(canvasId)` function in `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` — destroy existing Chart.js instance for the canvas
- [X] T041 [US2] Create `FileHeatmapView.razor` component in `src/SemanticSearch.WebApi/Components/Architecture/FileHeatmapView.razor` — accepts ProjectKey parameter and an EventCallback<string> OnFileSelected; on load calls GET heatmap API via WorkspaceApiClient; displays loading spinner while fetching; renders empty state with message to run quality analysis when no data exists; on data received calls JS interop `architectureDashboard.renderHeatmap`; handles JS click callback to invoke OnFileSelected with the clicked file path for navigation to filtered findings

### DI Registration for User Story 2

- [X] T042 [US2] Register `IHeatmapDataBuilder` → `HeatmapDataBuilder` in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

**Checkpoint**: Heatmap fully functional — users see color-coded treemap, hover for details, click to navigate to findings

---

## Phase 5: User Story 3 — View Database Entity Relationship Diagram (Priority: P3)

**Goal**: Introspect SQLite schema and domain entities, generate Mermaid.js ER diagram markup, expose via API, and render as a zoomable/pannable diagram.

**Independent Test**: GET /api/architecture/er-diagram → verify Mermaid markup contains all SQLite tables (ProjectWorkspaces, IndexingRuns, IndexedFiles, SearchSegments, QualityAnalysisRuns, DuplicationFindings, CodeRegions, QualitySummarySnapshots, plus the 3 new Dependency* tables) with correct FK relationships → verify diagram renders in browser.

### Infrastructure for User Story 3

- [X] T043 [P] [US3] Create `IErDiagramGenerator` interface in `src/SemanticSearch.Domain/Interfaces/IErDiagramGenerator.cs` with method `Task<ErDiagramResult> GenerateAsync(CancellationToken ct)` returning MermaidMarkup string, EntityCount, RelationshipCount
- [X] T044 [US3] Implement `SqliteErDiagramGenerator` in `src/SemanticSearch.Infrastructure/Architecture/SqliteErDiagramGenerator.cs` — inject SqliteVectorStore connection string; execute `SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%'` to list tables; for each table execute `PRAGMA table_info(tableName)` to get columns (name, type, notnull, pk) and `PRAGMA foreign_key_list(tableName)` to get FK relationships (table, from, to); build Mermaid `erDiagram` string with entity blocks containing column attributes and relationship lines with cardinality markers (||--o{ for one-to-many based on FK direction); return ErDiagramResult

### Application Layer for User Story 3

- [X] T045 [P] [US3] Create `GetErDiagramQuery` record and `GetErDiagramQueryHandler` in `src/SemanticSearch.Application/Architecture/Queries/GetErDiagramQuery.cs` — handler calls IErDiagramGenerator.GenerateAsync, returns result directly (always succeeds since it introspects the local database)
- [X] T046 [P] [US3] Create `GetErDiagramQueryValidator` in `src/SemanticSearch.Application/Architecture/Validators/GetErDiagramQueryValidator.cs` — no-op validator (query has no parameters)

### API Contract for User Story 3

- [X] T047 [P] [US3] Create `ErDiagramResponse` record in `src/SemanticSearch.WebApi/Contracts/Architecture/ErDiagramResponse.cs` with MermaidMarkup, EntityCount, RelationshipCount per OpenAPI schema

### Controller Endpoint for User Story 3

- [X] T048 [US3] Add GET `GetErDiagram` action to `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` — route `/api/architecture/er-diagram`, sends GetErDiagramQuery via MediatR, maps to ErDiagramResponse, returns 200 per OpenAPI contract

### Frontend for User Story 3

- [X] T049 [US3] Implement `renderErDiagram(containerId, mermaidMarkup)` function in `src/SemanticSearch.WebApi/wwwroot/js/architecture.js` — call `mermaid.render('er-svg', mermaidMarkup)` to generate SVG string; inject SVG into the container element; wrap in a scrollable/zoomable container with CSS `overflow: auto` and mouse-wheel zoom via `transform: scale()`
- [X] T050 [US3] Create `ErDiagramView.razor` component in `src/SemanticSearch.WebApi/Components/Architecture/ErDiagramView.razor` — on load calls GET er-diagram API via WorkspaceApiClient; displays loading spinner while fetching; on data received calls JS interop `architectureDashboard.renderErDiagram`; displays entity and relationship counts below the diagram

### DI Registration for User Story 3

- [X] T051 [US3] Register `IErDiagramGenerator` → `SqliteErDiagramGenerator` in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs`

**Checkpoint**: ER diagram fully functional — users see auto-generated entity relationship diagram with zoom/pan

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Dashboard integration and final wiring that connects all three visualizations

- [X] T052 Add tab navigation to the existing quality dashboard in `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor` — add tabs for "Dependency Graph", "Heatmap", "Data Model" alongside existing "Summary" and "Findings" tabs; conditionally render `DependencyGraphView`, `FileHeatmapView`, `ErDiagramView` components based on active tab; wire FileHeatmapView's OnFileSelected callback to switch to Findings tab with file filter applied
- [X] T053 [P] Add Vis.js Network CSS styles (vis-network.min.css or inline container sizing) to `src/SemanticSearch.WebApi/Components/App.razor` or `src/SemanticSearch.WebApi/wwwroot/css/app.css` to ensure the graph container has explicit height and proper layout
- [X] T054 Run `quickstart.md` verification: index a sample project, run dependency analysis, verify graph renders; run quality analysis, verify heatmap renders; verify ER diagram renders — confirm all three visualizations are accessible from dashboard tabs

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: T004–T005 depend on Phase 1 (JS shell); T006–T015 can start immediately (different files)
- **User Stories (Phase 3–5)**: All depend on Phase 2 completion (entities, repository, schema, controller shell)
  - US1, US2, US3 can proceed in parallel (different files, independent concerns)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all three user stories being complete (dashboard integration)

### User Story Dependencies

- **US1 (P1) Dependency Graph**: Depends on Phase 2 only — fully independent of US2 and US3
- **US2 (P2) Heatmap**: Depends on Phase 2 only — uses existing quality data, independent of US1 and US3
- **US3 (P3) ER Diagram**: Depends on Phase 2 only — independent of US1 and US2

### Within Each User Story

- Infrastructure (interface → implementation) before Application layer
- Application layer (handler, validator) before Controller endpoints
- API contracts can be parallel with handler implementation
- Controller endpoints before Frontend components (component calls API)
- Frontend JS functions before Blazor components (component calls JS interop)
- DI registration after implementation complete

### Parallel Opportunities

**Phase 1** — All library downloads (T001–T003) can run in parallel

**Phase 2** — All enums (T006–T008) and all entities (T009–T011) can run in parallel; interface (T012), schema (T013), and repository (T014) are sequential

**Phase 3 (US1)** — T018–T021 (command/query handlers and validators) can all run in parallel; T022–T025 (API contracts) can all run in parallel; T028–T029 (JS functions) are sequential but parallel with backend tasks

**Phase 4 (US2)** — T034–T037 (handler, validator, contracts) can all run in parallel; T039–T040 (JS functions) are sequential but parallel with backend tasks

**Phase 5 (US3)** — T045–T047 (handler, validator, contract) can all run in parallel

---

## Parallel Example: User Story 1

```bash
# After Phase 2 is complete, launch in parallel:

# Backend (parallel batch 1):
Task T016: Create IDependencyExtractor interface
Task T019: Create RunDependencyAnalysisCommandValidator
Task T021: Create GetDependencyGraphQueryValidator
Task T022: Create DependencyGraphResponse contract
Task T023: Create DependencyNodeResponse contract
Task T024: Create DependencyEdgeResponse contract
Task T025: Create DependencyAnalysisRunResponse contract

# Then sequential:
Task T017: Implement RoslynDependencyExtractor (depends on T016)
Task T018: Create RunDependencyAnalysisCommandHandler (depends on T017)
Task T020: Create GetDependencyGraphQueryHandler (depends on T014)

# Controller (depends on handlers + contracts):
Task T026: Add POST RunDependencyAnalysis action
Task T027: Add GET GetDependencyGraph action

# Frontend (parallel with backend after T005):
Task T028: Implement renderDependencyGraph JS function
Task T029: Implement destroyDependencyGraph JS function
Task T030: Create DependencyGraphView.razor (depends on T027 + T028)

# DI Registration (after T017):
Task T031: Register IDependencyExtractor and IDependencyRepository
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (library downloads + JS shell)
2. Complete Phase 2: Foundational (entities, schema, repository, controller shell)
3. Complete Phase 3: User Story 1 — Dependency Graph
4. **STOP and VALIDATE**: Index sample project → run analysis → verify interactive graph
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test dependency graph independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test heatmap independently → Deploy/Demo
4. Add User Story 3 → Test ER diagram independently → Deploy/Demo
5. Complete Polish → Dashboard tabs integrate all three views
6. Each story adds value without breaking previous stories

---

## Notes

- [P] tasks = different files, no dependencies on incomplete tasks
- [US1/US2/US3] label maps task to specific user story for traceability
- Each user story is independently completable and testable
- The ArchitectureController is created once in Phase 2 (T015) and extended per story (T026–T027, T038, T048)
- The architecture.js file is created once in Phase 1 (T005) and extended per story (T028–T029, T039–T040, T049)
- DI registrations are accumulated per story (T031, T042, T051) — each adds to InfrastructureServiceRegistration.cs
- Commit after each task or logical group
