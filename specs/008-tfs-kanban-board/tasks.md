# Tasks: TFS Kanban Board

**Input**: Design documents from `/specs/008-tfs-kanban-board/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Tests**: Not requested — test tasks omitted.

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2)
- Exact file paths included in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Extend domain interfaces, add new DTOs, and wire up new Application-layer folders for TFS Kanban backend

- [X] T001 Add three new domain records (TfsWorkItemComment, TfsWorkItemUpdateResult) and three new methods (UpdateWorkItemStateAsync, GetWorkItemCommentsAsync, AddWorkItemCommentAsync) to `src/SemanticSearch.Domain/Interfaces/ITfsApiClient.cs`
- [X] T002 [P] Add new sealed record DTOs (UpdateWorkItemStateRequest, UpdateWorkItemStateResponse, WorkItemCommentResponse, WorkItemCommentsResponse, AddWorkItemCommentRequest, AddWorkItemCommentResponse) to `src/SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs`
- [X] T003 [P] Create Application Tfs folder structure with empty files: `src/SemanticSearch.Application/Tfs/Commands/UpdateWorkItemState.cs` and `src/SemanticSearch.Application/Tfs/Commands/AddWorkItemComment.cs` and `src/SemanticSearch.Application/Tfs/Queries/GetWorkItemComments.cs`

**Checkpoint**: Solution builds. Domain interface extended, DTOs defined, Application folder structure ready.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement backend infrastructure that ALL user stories depend on — TFS API client methods, MediatR handlers, controller endpoints, and WorkspaceApiClient proxy methods

**⚠️ CRITICAL**: No user story UI work can begin until this phase is complete

- [X] T004 Implement UpdateWorkItemStateAsync in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs` — send PATCH `/_apis/wit/workitems/{id}` with JSON Patch body `[{"op":"replace","path":"/fields/System.State","value":"{state}"}]` using content-type `application/json-patch+json`, following the existing API version fallback strategy
- [X] T005 [P] Implement GetWorkItemCommentsAsync in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs` — send GET `/_apis/wit/workitems/{id}/comments?api-version=7.1-preview.4`, parse response into list of TfsWorkItemComment records; fall back to empty list with log warning for older TFS versions
- [X] T006 [P] Implement AddWorkItemCommentAsync in `src/SemanticSearch.Infrastructure/Tfs/TfsApiClient.cs` — send POST `/_apis/wit/workitems/{id}/comments?api-version=7.1-preview.4` with `{"text":"{text}"}` body; fall back to PATCH with System.History for older TFS versions
- [X] T007 Implement UpdateWorkItemState command, handler, and FluentValidation validator in `src/SemanticSearch.Application/Tfs/Commands/UpdateWorkItemState.cs` — validator ensures State is non-empty and one of New/Active/Resolved/Closed; handler calls ITfsApiClient.UpdateWorkItemStateAsync
- [X] T008 [P] Implement GetWorkItemComments query and handler in `src/SemanticSearch.Application/Tfs/Queries/GetWorkItemComments.cs` — handler calls ITfsApiClient.GetWorkItemCommentsAsync and returns list of TfsWorkItemComment
- [X] T009 [P] Implement AddWorkItemComment command, handler, and FluentValidation validator in `src/SemanticSearch.Application/Tfs/Commands/AddWorkItemComment.cs` — validator ensures Text is non-empty with max length 4000; handler calls ITfsApiClient.AddWorkItemCommentAsync
- [X] T010 Add three new endpoints to `src/SemanticSearch.WebApi/Controllers/TfsController.cs` — PATCH `workitems/{id}/state` (sends UpdateWorkItemState command), GET `workitems/{id}/comments` (sends GetWorkItemComments query), POST `workitems/{id}/comments` (sends AddWorkItemComment command); each ≤ 15 lines following existing thin-controller pattern
- [X] T011 Add three proxy methods to `src/SemanticSearch.WebApi/Services/WorkspaceApiClient.cs` — UpdateWorkItemStateAsync(int id, string state), GetWorkItemCommentsAsync(int id), AddWorkItemCommentAsync(int id, string text); each calls the corresponding new TFS API endpoint

**Checkpoint**: All three new API endpoints operational. `dotnet build` succeeds. Endpoints testable via HTTP client: PATCH state, GET comments, POST comment.

---

## Phase 3: User Story 1 — View Work Items on a Kanban Board (Priority: P1) 🎯 MVP

**Goal**: Replace the existing MyWork.razor with a componentized Kanban board displaying work items in four state columns (New, Active, Resolved, Closed) with colored type badges, prominent titles, and muted metadata.

**Independent Test**: Configure TFS credentials, navigate to `/my-work`, verify work items appear in correct state columns with type badges, titles, IDs, and area paths. Verify hover effects, empty columns, loading state, and error state.

### Implementation for User Story 1

- [X] T012 [P] [US1] Create KanbanBoardState class in `src/SemanticSearch.WebApi/Components/Kanban/KanbanBoardState.cs` — plain C# class with properties: Items (List\<WorkItemResponse\>), IsLoading, Error, SelectedItem, DraggedItem, DragSourceState, SyncingItemIds (HashSet\<int\>), ErrorItems (Dictionary\<int,string\>); methods: StartDrag, CancelDrag, DropOnColumn, CompleteSyncSuccess, CompleteSyncFailure, DismissError, SelectItem; Action? OnStateChanged delegate for triggering UI updates
- [X] T013 [P] [US1] Create TicketCard.razor component in `src/SemanticSearch.WebApi/Components/Kanban/TicketCard.razor` — receives WorkItemResponse Item parameter; renders colored type badge (red=Bug, blue=Task, green=User Story, neutral=other), prominent title (truncated with ellipsis), muted ID (prefixed with #) and area path; CSS hover effect with lift and shadow; draggable="true" attribute (drag logic wired in US2)
- [X] T014 [P] [US1] Create BoardColumn.razor component in `src/SemanticSearch.WebApi/Components/Kanban/BoardColumn.razor` — receives string State, string Label, IReadOnlyList\<WorkItemResponse\> Items parameters; renders column header with state name and item count badge; iterates Items rendering TicketCard for each; shows empty-state indicator when column has zero items (drop zone wired in US2)
- [X] T015 [US1] Create KanbanBoard.razor page in `src/SemanticSearch.WebApi/Components/Pages/KanbanBoard.razor` — @page "/my-work" route; injects WorkspaceApiClient; on init fetches work items via existing GetAssignedWorkItemsAsync; instantiates KanbanBoardState with fetched items; provides CascadingValue\<KanbanBoardState\>; renders loading spinner, error state with retry, empty state, or four BoardColumn components (New, Active, Resolved, Closed) filtering items by state
- [X] T016 [US1] Add kanban board CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — add .kanban-board (4-column grid layout), .kanban-column (flex column, rounded, background surface), .kanban-column-header (state name + count), .kanban-card (rounded, shadow, border, padding, cursor pointer), .kanban-card:hover (translateY lift + shadow), .kanban-card-badge (inline pill with type-specific colors), .kanban-card-title (font-weight, truncation), .kanban-card-meta (muted text for ID and area path), .kanban-empty-column (centered muted text)
- [X] T017 [US1] Delete the old MyWork.razor page at `src/SemanticSearch.WebApi/Components/Pages/MyWork.razor` and update any navigation references in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor` to point to the new KanbanBoard page (same /my-work route, so NavMenu link href stays the same — just verify)

**Checkpoint**: Board renders at `/my-work` with 4 columns, work items displayed as styled cards with type badges. Loading, error, and empty states functional. Cards not yet draggable.

---

## Phase 4: User Story 2 — Drag and Drop Work Items Between Columns (Priority: P2)

**Goal**: Enable dragging work item cards between state columns to update their TFS state, with optimistic UI updates, syncing indicators, and error rollback.

**Independent Test**: Drag a card from "New" to "Active", verify loading spinner appears on card, confirm TFS state updates. Simulate failure by disconnecting TFS and verify card reverts to original column with error message.

### Implementation for User Story 2

- [X] T018 [US2] Wire drag-and-drop events on TicketCard.razor in `src/SemanticSearch.WebApi/Components/Kanban/TicketCard.razor` — add @ondragstart handler calling boardState.StartDrag(Item), @ondragend handler calling boardState.CancelDrag(); apply .kanban-card--dragging CSS class when item equals boardState.DraggedItem; show .kanban-card--syncing spinner overlay when item ID is in boardState.SyncingItemIds; show .kanban-card--error badge with dismiss click when item ID is in boardState.ErrorItems
- [X] T019 [US2] Wire drop zone events on BoardColumn.razor in `src/SemanticSearch.WebApi/Components/Kanban/BoardColumn.razor` — add @ondragover:preventDefault to allow dropping; add @ondragenter/@ondragleave handlers to toggle .kanban-column--dragover highlight class; add @ondrop handler that calls boardState.DropOnColumn(State) and then triggers the async TFS state update via WorkspaceApiClient.UpdateWorkItemStateAsync
- [X] T020 [US2] Implement the async state sync flow in KanbanBoard.razor in `src/SemanticSearch.WebApi/Components/Pages/KanbanBoard.razor` — when DropOnColumn is called: optimistically move item in boardState.Items, add item ID to SyncingItemIds, call StateHasChanged, await WorkspaceApiClient.UpdateWorkItemStateAsync, on success call CompleteSyncSuccess, on failure call CompleteSyncFailure (reverts item state), call StateHasChanged after each
- [X] T021 [US2] Add drag-and-drop CSS classes to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — add .kanban-card--dragging (reduced opacity), .kanban-card--syncing (spinner overlay with semi-transparent backdrop), .kanban-card--error (error badge, red border accent), .kanban-column--dragover (highlighted border/background indicating valid drop zone), syncing spinner keyframe animation

**Checkpoint**: Cards draggable between columns. Optimistic move, syncing spinner, success confirmation, failure rollback with error all functional.

---

## Phase 5: User Story 3 — View Work Item Details in a Modal (Priority: P3)

**Goal**: Clicking a card opens a side-drawer modal displaying full work item details with multiple dismiss methods.

**Independent Test**: Click any card, verify modal opens with all detail fields (ID, type badge, title, state, assigned user, area path, iteration path, priority, created/changed dates, TFS link). Dismiss via close button, Escape key, and backdrop click.

### Implementation for User Story 3

- [X] T022 [US3] Create TicketDetailsModal.razor component in `src/SemanticSearch.WebApi/Components/Kanban/TicketDetailsModal.razor` — receives WorkItemResponse Item and EventCallback OnClose parameters; renders fixed-position backdrop overlay with @onclick calling OnClose; modal panel with @onclick:stopPropagation; header with title and close button (X); detail fields table showing: ID, type (colored badge), title, state, assigned user, area path, iteration path, priority, created date, changed date; "Open in TFS" external link; @onkeydown handler for Escape key dismissal
- [X] T023 [US3] Wire modal open/close in KanbanBoard.razor in `src/SemanticSearch.WebApi/Components/Pages/KanbanBoard.razor` — when boardState.SelectedItem is not null, render TicketDetailsModal with Item=boardState.SelectedItem and OnClose calling boardState.SelectItem(null); ensure click on TicketCard triggers boardState.SelectItem(item) only when not dragging
- [X] T024 [US3] Add modal CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — add .kanban-modal-backdrop (fixed inset-0, z-index 1000, semi-transparent background, flex center), .kanban-modal (max-width, max-height with overflow-y auto, rounded, shadow, background surface, padding), .kanban-modal-header (flex between, title + close button), .kanban-modal-field (label + value row styling), .kanban-modal-link (external link style)

**Checkpoint**: Clicking card opens detail modal with all fields. All three dismiss methods work. Drag-and-drop still functional (click vs drag distinguished).

---

## Phase 6: User Story 4 — View and Add Comments on Work Items (Priority: P4)

**Goal**: Add a Comments/Activity section inside the detail modal that loads existing TFS comments and allows adding new ones.

**Independent Test**: Open a work item modal, verify existing comments display with author and timestamp. Type a new comment, submit, verify it appears in the list. Verify empty state and error handling.

### Implementation for User Story 4

- [X] T025 [US4] Create CommentSection.razor component in `src/SemanticSearch.WebApi/Components/Kanban/CommentSection.razor` — receives int WorkItemId parameter; injects WorkspaceApiClient; on init calls GetWorkItemCommentsAsync to load comments; renders: loading spinner while fetching, error state with retry button on failure, "No comments yet" when empty, chronological comment list with author name, timestamp, and text (rendered as HTML via MarkupString); textarea input bound to newCommentText, submit button that calls AddWorkItemCommentAsync, clears input on success, preserves input and shows error on failure; tracks IsSubmitting state to disable button during submission
- [X] T026 [US4] Embed CommentSection in TicketDetailsModal.razor in `src/SemanticSearch.WebApi/Components/Kanban/TicketDetailsModal.razor` — add CommentSection below the detail fields table with a "Comments / Activity" heading separator; pass WorkItemId=Item.Id
- [X] T027 [US4] Add comment section CSS styles to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — add .kanban-comments (section container), .kanban-comment-item (individual comment with author, timestamp, text), .kanban-comment-author (font-weight bold), .kanban-comment-date (muted timestamp), .kanban-comment-text (body text), .kanban-comment-input (textarea + submit button layout), .kanban-comment-empty (centered muted empty state)

**Checkpoint**: Comments load in modal, new comments submittable, error and empty states handled.

---

## Phase 7: User Story 5 — Dark Mode Support (Priority: P5)

**Goal**: Add a dark mode toggle that switches the board, cards, modals, and all UI elements to a dark color scheme, with preference persisted in localStorage.

**Independent Test**: Toggle dark mode, verify all board elements (columns, cards, badges, modals, comments) adapt to dark colors with readable contrast. Refresh page, verify dark mode persists.

### Implementation for User Story 5

- [X] T028 [US5] Add dark mode CSS variable overrides to `src/SemanticSearch.WebApi/wwwroot/css/app.css` — under `.dark` selector, override root CSS variables: --bg, --surface, --surface-soft, --ink, --muted, --border, --accent, and all kanban-specific variables to dark palette values; ensure all .kanban-* classes inherit from CSS variables (not hardcoded colors)
- [X] T029 [US5] Create theme toggle JS interop in `src/SemanticSearch.WebApi/wwwroot/js/kanban.js` — expose window.themeManager.get() returning localStorage theme or 'light', window.themeManager.set(theme) setting localStorage and toggling .dark class on document.documentElement; add inline script in `src/SemanticSearch.WebApi/Components/App.razor` head section to apply stored theme before Blazor hydration (prevents flash of wrong theme)
- [X] T030 [US5] Add dark mode toggle button to the page header in `src/SemanticSearch.WebApi/Components/Pages/KanbanBoard.razor` — inject IJSRuntime, on init call themeManager.get() to set current theme state, render toggle button (sun/moon icon) in the board header that calls themeManager.set() to switch theme and updates local state

**Checkpoint**: Dark mode toggle functional, all elements adapted, preference persists across navigation and page refresh.

---

## Phase 8: Polish & Cross-Cutting Concerns

**Purpose**: Final cleanup, responsive design, and quickstart validation

- [X] T031 [P] Ensure all kanban CSS classes use CSS custom properties (not hardcoded colors) for consistent theming across light and dark modes in `src/SemanticSearch.WebApi/wwwroot/css/app.css`
- [X] T032 [P] Add responsive layout adjustments in `src/SemanticSearch.WebApi/wwwroot/css/app.css` — ensure kanban board scrolls horizontally on narrow viewports, columns have min-width, cards remain readable; columns with 50+ items become scrollable with fixed header
- [X] T033 Run quickstart.md verification checklist — build and run the application, navigate to `/my-work`, validate all 5 user story acceptance criteria per `specs/008-tfs-kanban-board/quickstart.md`

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (domain interface + DTOs must exist before handlers/controllers)
- **User Story 1 (Phase 3)**: Depends on Phase 2 (needs WorkspaceApiClient proxy methods for data fetching)
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs Board, Column, Card components to add drag-and-drop to)
- **User Story 3 (Phase 5)**: Depends on Phase 3 (needs TicketCard click handler and board state SelectItem)
- **User Story 4 (Phase 6)**: Depends on Phase 5 (needs TicketDetailsModal to embed comments in)
- **User Story 5 (Phase 7)**: Depends on Phase 3 (needs kanban CSS classes defined; can run in parallel with US3/US4)
- **Polish (Phase 8)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (P1)**: After Foundational → **no dependencies on other stories**
- **US2 (P2)**: After US1 → needs Board/Column/Card components
- **US3 (P3)**: After US1 → needs Card click + board state; **independent of US2**
- **US4 (P4)**: After US3 → needs detail modal; **independent of US2**
- **US5 (P5)**: After US1 → needs kanban CSS; **independent of US2/US3/US4**

### Within Each User Story

- State/services before UI components
- Components before page-level wiring
- CSS alongside or after components that reference it

### Parallel Opportunities

- **Phase 1**: T002 and T003 can run in parallel (DTOs and folder structure are independent)
- **Phase 2**: T005 and T006 can run in parallel (independent API methods); T008 and T009 can run in parallel (independent handlers)
- **Phase 3**: T012, T013, T014 can all run in parallel (independent files: state class, card component, column component)
- **After US1**: US3 and US5 can run in parallel (modal and dark mode touch different files)
- **Phase 8**: T031 and T032 can run in parallel

---

## Parallel Example: User Story 1

```bash
# Launch these three tasks in parallel (independent files):
Task T012: "Create KanbanBoardState.cs"
Task T013: "Create TicketCard.razor"
Task T014: "Create BoardColumn.razor"

# Then sequentially:
Task T015: "Create KanbanBoard.razor" (depends on T012, T013, T014)
Task T016: "Add kanban CSS styles" (can overlap with T015)
Task T017: "Delete old MyWork.razor" (after T015 is complete)
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup (T001–T003)
2. Complete Phase 2: Foundational (T004–T011)
3. Complete Phase 3: User Story 1 (T012–T017)
4. **STOP and VALIDATE**: Navigate to `/my-work`, verify board renders with 4 columns and styled cards
5. Deploy/demo if ready — board is a useful read-only replacement for the old MyWork page

### Incremental Delivery

1. Setup + Foundational → Backend ready (3 new API endpoints operational)
2. Add US1 → Board view with cards → **MVP!**
3. Add US2 → Drag-and-drop state management → Board becomes actionable
4. Add US3 → Detail modal → Full information without leaving board
5. Add US4 → Comments → Collaboration without leaving board
6. Add US5 → Dark mode → Developer experience polish
7. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers after Foundational is complete:

1. Developer A: US1 (board view — must complete first)
2. After US1: Developer A on US2, Developer B on US3, Developer C on US5
3. After US3: Developer B on US4
4. Final: Polish phase together

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- All new components go in `src/SemanticSearch.WebApi/Components/Kanban/` folder
- All backend changes extend existing files (ITfsApiClient, TfsApiClient, TfsController, TfsContracts, WorkspaceApiClient)
- KanbanBoardState is a plain C# class (not a DI service) — instantiated per page, passed as CascadingValue
- Dark mode CSS uses existing CSS custom property pattern — no Tailwind dark: variants
- PATCH request to TFS uses content-type `application/json-patch+json` (not regular JSON)
- Comment API uses `-preview` suffix in API version
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
