# Tasks: Local AI Tech Lead Assistant

**Input**: Design documents from `/specs/004-local-ai-tech-lead/`
**Prerequisites**: [plan.md](./plan.md), [spec.md](./spec.md), [research.md](./research.md), [data-model.md](./data-model.md), [contracts/quality-ai.openapi.yaml](./contracts/quality-ai.openapi.yaml), [quickstart.md](./quickstart.md)

**Tests**: No explicit TDD or test-first requirement was requested in the feature specification, so this task list focuses on implementation work. Independent test criteria are captured for each user story and in the quickstart document.

**Organization**: Tasks are grouped by user story to enable independent implementation and validation.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Add the packages, configuration, and contract surface the assistant feature needs before runtime work begins.

- [X] T001 Add LLamaSharp and markdown package references in `src/SemanticSearch.Infrastructure/SemanticSearch.Infrastructure.csproj` and `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj`
- [X] T002 Add assistant configuration models and defaults in `src/SemanticSearch.Application/Common/SemanticSearchOptions.cs`, `src/SemanticSearch.WebApi/appsettings.json`, and `src/SemanticSearch.WebApi/appsettings.Development.json`
- [X] T003 Create assistant web contract records in `src/SemanticSearch.WebApi/Contracts/Quality/QualityAiDtos.cs`

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Build the shared assistant runtime, readiness, streaming primitives, and host wiring that every story depends on.

**CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 Create shared assistant application models in `src/SemanticSearch.Application/Quality/Assistant/Models/QualityAssistantModels.cs`
- [X] T005 [P] Create assistant application interfaces in `src/SemanticSearch.Application/Common/Interfaces/IAiAssistantModelProvider.cs`, `src/SemanticSearch.Application/Common/Interfaces/IQualityAssistantService.cs`, and `src/SemanticSearch.Application/Common/Interfaces/IQualityAssistantPromptBuilder.cs`
- [X] T006 [P] Create assistant readiness query and validator in `src/SemanticSearch.Application/Quality/Assistant/Queries/GetAssistantStatusQuery.cs`, `src/SemanticSearch.Application/Quality/Assistant/Queries/GetAssistantStatusQueryHandler.cs`, and `src/SemanticSearch.Application/Quality/Assistant/Validators/GetAssistantStatusQueryValidator.cs`
- [X] T007 Implement the singleton GGUF model loader and readiness tracker in `src/SemanticSearch.Infrastructure/Quality/Assistant/LlamaModelProvider.cs` and `src/SemanticSearch.Infrastructure/Quality/Assistant/AssistantReadinessService.cs`
- [X] T008 [P] Implement shared prompt-bounding and NDJSON stream event utilities in `src/SemanticSearch.Infrastructure/Quality/Assistant/DuplicateSnippetLimiter.cs` and `src/SemanticSearch.WebApi/Services/AiStreamEventWriter.cs`
- [X] T009 Register assistant services and startup initialization in `src/SemanticSearch.Infrastructure/DependencyInjection/InfrastructureServiceRegistration.cs` and `src/SemanticSearch.WebApi/Program.cs`
- [X] T010 Extend assistant status and streaming client support in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`

**Checkpoint**: Foundation ready. User story implementation can now proceed.

---

## Phase 3: User Story 1 - Generate Project Action Plan (Priority: P1) MVP

**Goal**: Let an engineering lead request a streamed three-point action plan from the dashboard summary they are already reviewing.

**Independent Test**: Open the quality dashboard for an analyzed project, trigger the project-level assistant, and confirm a dedicated panel expands below the summary and starts streaming a three-point plan without leaving the page.

### Implementation for User Story 1

- [X] T011 [US1] Create the project-plan query, handler, and validator in `src/SemanticSearch.Application/Quality/Assistant/Queries/StreamProjectPlanQuery.cs`, `src/SemanticSearch.Application/Quality/Assistant/Queries/StreamProjectPlanQueryHandler.cs`, and `src/SemanticSearch.Application/Quality/Assistant/Validators/StreamProjectPlanQueryValidator.cs`
- [X] T012 [US1] Implement project-summary prompt building and LLama streaming orchestration in `src/SemanticSearch.Infrastructure/Quality/Assistant/QualityAssistantPromptBuilder.cs` and `src/SemanticSearch.Infrastructure/Quality/Assistant/LlamaStreamingAssistantService.cs`
- [X] T013 [US1] Add the project-plan streaming endpoint to `src/SemanticSearch.WebApi/Controllers/QualityController.cs`
- [X] T014 [P] [US1] Create the project action-plan panel component in `src/SemanticSearch.WebApi/Components/Quality/Assistant/ProjectActionPlanPanel.razor`
- [X] T015 [US1] Integrate the project-plan trigger, status load, and panel state into `src/SemanticSearch.WebApi/Components/Quality/QualityHero.razor` and `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor`
- [X] T016 [US1] Add project-plan panel styling, loading state, and terminal-style presentation in `src/SemanticSearch.WebApi/wwwroot/css/app.css`

**Checkpoint**: User Story 1 is independently functional from the quality dashboard.

---

## Phase 4: User Story 2 - Request Duplicate-Specific Refactoring Guidance (Priority: P2)

**Goal**: Let a developer request a streamed consolidation proposal from the open duplicate comparison modal without replacing the existing comparison view.

**Independent Test**: Open any duplicate comparison with available code regions, trigger the duplicate-fix assistant, and confirm the modal keeps both regions visible while a dedicated assistant panel streams a refactoring proposal for that pair.

### Implementation for User Story 2

- [X] T017 [US2] Create the finding-fix query, handler, and validator in `src/SemanticSearch.Application/Quality/Assistant/Queries/StreamFindingFixQuery.cs`, `src/SemanticSearch.Application/Quality/Assistant/Queries/StreamFindingFixQueryHandler.cs`, and `src/SemanticSearch.Application/Quality/Assistant/Validators/StreamFindingFixQueryValidator.cs`
- [X] T018 [US2] Extend prompt building to map duplicate comparison context into bounded refactoring prompts in `src/SemanticSearch.Infrastructure/Quality/Assistant/QualityAssistantPromptBuilder.cs`
- [X] T019 [US2] Add the finding-fix streaming endpoint to `src/SemanticSearch.WebApi/Controllers/QualityController.cs`
- [X] T020 [P] [US2] Create the duplicate-fix assistant panel component in `src/SemanticSearch.WebApi/Components/Quality/Assistant/DuplicateFixPanel.razor`
- [X] T021 [US2] Integrate the duplicate-fix action bar and assistant panel into `src/SemanticSearch.WebApi/Components/Quality/DuplicateComparisonModal.razor` and `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor`
- [X] T022 [US2] Add modal assistant panel layout, floating action bar, and resize-handle behavior in `src/SemanticSearch.WebApi/wwwroot/css/app.css` and `src/SemanticSearch.WebApi/wwwroot/js/quality-dashboard.js`

**Checkpoint**: User Story 2 is independently functional from the duplicate comparison modal.

---

## Phase 5: User Story 3 - Read AI Guidance While It Streams (Priority: P3)

**Goal**: Keep streamed AI output readable and resilient while it is still arriving, including partial markdown and code blocks.

**Independent Test**: Trigger both the dashboard action plan and the modal refactoring proposal, then confirm partial responses render as formatted markdown, code blocks stay readable, and partial output remains visible after cancellation or failure.

### Implementation for User Story 3

- [X] T023 [US3] Create a shared streaming markdown renderer in `src/SemanticSearch.WebApi/Components/Quality/Assistant/StreamingMarkdown.razor` and `src/SemanticSearch.WebApi/Services/MarkdownRenderService.cs`
- [X] T024 [US3] Update NDJSON stream consumption and ordered event accumulation in `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs`
- [X] T025 [US3] Wire per-surface cancellation, restart handling, and partial-output retention into `src/SemanticSearch.WebApi/Components/Quality/QualityDashboard.razor` and `src/SemanticSearch.WebApi/Components/Quality/DuplicateComparisonModal.razor`
- [X] T026 [US3] Add shared markdown, syntax highlighting, and partial-state styles in `src/SemanticSearch.WebApi/wwwroot/css/app.css`

**Checkpoint**: Both assistant surfaces stream readable markdown and preserve partial output correctly.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize operator guidance, diagnostics, and end-to-end validation across all stories.

- [X] T027 Add structured assistant lifecycle logging and consistent failure messaging in `src/SemanticSearch.Infrastructure/Quality/Assistant/LlamaModelProvider.cs`, `src/SemanticSearch.Infrastructure/Quality/Assistant/LlamaStreamingAssistantService.cs`, and `src/SemanticSearch.WebApi/Controllers/QualityController.cs`
- [X] T028 [P] Update operator documentation for assistant setup and usage in `README.md` and `specs/004-local-ai-tech-lead/quickstart.md`
- [X] T029 Run the quickstart validation flow and reconcile any command or contract drift in `specs/004-local-ai-tech-lead/quickstart.md` and `specs/004-local-ai-tech-lead/contracts/quality-ai.openapi.yaml`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Phase 1: Setup**: Starts immediately.
- **Phase 2: Foundational**: Depends on Phase 1 and blocks all story work.
- **Phase 3: User Story 1**: Depends on Phase 2 only.
- **Phase 4: User Story 2**: Depends on Phase 2 only.
- **Phase 5: User Story 3**: Depends on Phase 3 and Phase 4 because it improves the shared streaming presentation used by both assistant surfaces.
- **Phase 6: Polish**: Depends on the stories you plan to ship.

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after the foundational runtime exists.
- **US2 (P2)**: No dependency on US1 after the foundational runtime exists; it depends only on the existing duplicate comparison flow.
- **US3 (P3)**: Depends on both US1 and US2 because it unifies the formatting and partial-output behavior across both assistant surfaces.

### Parallel Opportunities

- `T005`, `T006`, and `T008` can run in parallel after `T004`.
- `T014` can run in parallel with `T011` through `T013` once the shared runtime is in place.
- `T020` can run in parallel with `T017` through `T019` once the shared runtime is in place.
- `T028` can run in parallel with `T027`.

---

## Parallel Example: User Story 1

```text
T011 Create the project-plan query, handler, and validator in src/SemanticSearch.Application/Quality/Assistant/Queries/StreamProjectPlanQuery.cs, src/SemanticSearch.Application/Quality/Assistant/Queries/StreamProjectPlanQueryHandler.cs, and src/SemanticSearch.Application/Quality/Assistant/Validators/StreamProjectPlanQueryValidator.cs
T014 Create the project action-plan panel component in src/SemanticSearch.WebApi/Components/Quality/Assistant/ProjectActionPlanPanel.razor
```

## Parallel Example: User Story 2

```text
T017 Create the finding-fix query, handler, and validator in src/SemanticSearch.Application/Quality/Assistant/Queries/StreamFindingFixQuery.cs, src/SemanticSearch.Application/Quality/Assistant/Queries/StreamFindingFixQueryHandler.cs, and src/SemanticSearch.Application/Quality/Assistant/Validators/StreamFindingFixQueryValidator.cs
T020 Create the duplicate-fix assistant panel component in src/SemanticSearch.WebApi/Components/Quality/Assistant/DuplicateFixPanel.razor
```

## Parallel Example: User Story 3

```text
T023 Create a shared streaming markdown renderer in src/SemanticSearch.WebApi/Components/Quality/Assistant/StreamingMarkdown.razor and src/SemanticSearch.WebApi/Services/MarkdownRenderService.cs
T024 Update NDJSON stream consumption and ordered event accumulation in src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs
```

---

## Implementation Strategy

### MVP First

1. Complete Phase 1: Setup.
2. Complete Phase 2: Foundational runtime.
3. Complete Phase 3: User Story 1.
4. Validate the dashboard action-plan flow from the quickstart.
5. Stop there if you need the narrowest shippable increment.

### Incremental Delivery

1. Deliver Setup + Foundational to establish the local assistant runtime.
2. Deliver US1 for project-level action plans as the MVP.
3. Deliver US2 for modal-level refactoring guidance.
4. Deliver US3 to make both streaming surfaces resilient and readable.
5. Finish with Phase 6 logging, docs, and quickstart validation.

### Suggested MVP Scope

- Phase 1
- Phase 2
- Phase 3 (US1 only)

---

## Notes

- Every task uses the required checklist format: checkbox, task ID, optional `[P]`, required `[US#]` for story tasks, and explicit file paths.
- User stories remain independently testable after the foundational runtime is complete.
- User Story 3 intentionally comes after US1 and US2 because it refines the shared streaming experience rather than introducing a new entry point.
