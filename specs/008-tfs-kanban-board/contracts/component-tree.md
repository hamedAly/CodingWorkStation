# Component Tree: TFS Kanban Board

**Feature**: 008-tfs-kanban-board  
**Framework**: Blazor Web App (Interactive Server)  
**Pattern**: Feature-based folder under `Components/Kanban/`

---

## Component Hierarchy

```
KanbanBoard.razor                    (Page: @page "/my-work")
│
├── <CascadingValue Value="boardState">
│   │
│   ├── BoardColumn.razor × 4        (New, Active, Resolved, Closed)
│   │   ├── Column header             (state name + item count)
│   │   ├── Drop zone                 (@ondragover, @ondrop)
│   │   └── TicketCard.razor × N      (one per work item in column)
│   │       ├── Type badge             (colored: red=Bug, blue=Task, green=Story)
│   │       ├── Title                  (prominent, truncated with ellipsis)
│   │       ├── ID + Area path         (muted)
│   │       ├── Drag source            (@ondragstart, @ondragend)
│   │       ├── Syncing indicator      (spinner overlay when updating TFS)
│   │       └── Error indicator        (error badge, click to dismiss)
│   │
│   └── TicketDetailsModal.razor      (conditional: when SelectedItem ≠ null)
│       ├── Backdrop overlay           (@onclick → close)
│       ├── Modal panel                (@onclick:stopPropagation)
│       │   ├── Header                 (title + close button)
│       │   ├── Detail fields table    (ID, type, state, assigned to, etc.)
│       │   ├── TFS link               (external link)
│       │   └── CommentSection.razor
│       │       ├── Comment list       (chronological, with author + timestamp)
│       │       ├── Empty state        ("No comments yet")
│       │       ├── Error state        (retry button)
│       │       └── Comment input      (textarea + submit button)
│       └── Escape key handler         (@onkeydown)
```

---

## Component Specifications

### KanbanBoard.razor

| Aspect | Detail |
|--------|--------|
| **Route** | `@page "/my-work"` |
| **Location** | `Components/Pages/KanbanBoard.razor` |
| **Injects** | `WorkspaceApiClient` |
| **Owns** | `KanbanBoardState` instance |
| **Provides** | `CascadingValue<KanbanBoardState>` |
| **States** | Loading, Error, Empty, Board |
| **Responsibilities** | Fetch work items, instantiate board state, render page header, error/loading/empty states |

### BoardColumn.razor

| Aspect | Detail |
|--------|--------|
| **Location** | `Components/Kanban/BoardColumn.razor` |
| **Parameters** | `string State`, `string Label`, `IReadOnlyList<WorkItemResponse> Items` |
| **Cascading** | Consumes `KanbanBoardState` |
| **Events** | `@ondragover:preventDefault` (allow drop), `@ondrop` (handle drop), `@ondragenter`/`@ondragleave` (highlight) |
| **States** | Normal, DragOver (highlighted), Empty |
| **CSS** | `.kanban-column`, `.kanban-column--dragover` |

### TicketCard.razor

| Aspect | Detail |
|--------|--------|
| **Location** | `Components/Kanban/TicketCard.razor` |
| **Parameters** | `WorkItemResponse Item` |
| **Cascading** | Consumes `KanbanBoardState` |
| **Events** | `@onclick` (select item → open modal), `@ondragstart` (begin drag), `@ondragend` (cancel drag) |
| **Attributes** | `draggable="true"` |
| **States** | Normal, Hover (CSS), Dragging (opacity), Syncing (spinner), Error (error badge) |
| **CSS** | `.kanban-card`, `.kanban-card--dragging`, `.kanban-card--syncing`, `.kanban-card--error` |

### TicketDetailsModal.razor

| Aspect | Detail |
|--------|--------|
| **Location** | `Components/Kanban/TicketDetailsModal.razor` |
| **Parameters** | `WorkItemResponse Item`, `EventCallback OnClose` |
| **Injects** | `WorkspaceApiClient` (for comments) |
| **Renders** | Detail table, `CommentSection`, close button |
| **Dismiss** | Close button click, Escape key, backdrop click |
| **CSS** | `.kanban-modal-backdrop`, `.kanban-modal` |

### CommentSection.razor

| Aspect | Detail |
|--------|--------|
| **Location** | `Components/Kanban/CommentSection.razor` |
| **Parameters** | `int WorkItemId` |
| **Injects** | `WorkspaceApiClient` |
| **States** | Loading, Loaded, Error, Submitting |
| **Contains** | Comment list, empty state, error+retry, textarea input, submit button |
| **CSS** | `.kanban-comments`, `.kanban-comment-item`, `.kanban-comment-input` |

### KanbanBoardState.cs

| Aspect | Detail |
|--------|--------|
| **Location** | `Components/Kanban/KanbanBoardState.cs` |
| **Type** | Plain C# class (not a DI service) |
| **Lifecycle** | Created per page load, passed as `CascadingValue` |
| **Responsibilities** | Track drag state, manage optimistic updates, track syncing/error items |
| **Notify** | Exposes `Action? OnStateChanged` for triggering `StateHasChanged()` |

---

## Event Flow: Drag and Drop

```
1. User starts dragging TicketCard
   TicketCard.@ondragstart → boardState.StartDrag(item)
   Card gets .kanban-card--dragging class

2. User drags over BoardColumn
   BoardColumn.@ondragenter → boardState tracks hover column
   Column gets .kanban-column--dragover class

3. User drops on BoardColumn
   BoardColumn.@ondrop → boardState.DropOnColumn(targetState)
   → Optimistic: item moves to new column immediately
   → Card gets .kanban-card--syncing class
   → API call: PATCH /api/tfs/workitems/{id}/state

4a. API Success
   → boardState.CompleteSyncSuccess(itemId)
   → Remove .kanban-card--syncing

4b. API Failure
   → boardState.CompleteSyncFailure(itemId, error)
   → Revert item to original column
   → Card gets .kanban-card--error class
   → Error notification visible on card
```

---

## Event Flow: Detail Modal

```
1. User clicks TicketCard
   TicketCard.@onclick → boardState.SelectItem(item)
   → KanbanBoard renders TicketDetailsModal

2. TicketDetailsModal loads comments
   OnInitializedAsync → GET /api/tfs/workitems/{id}/comments
   → Renders CommentSection with data

3. User submits comment
   CommentSection → POST /api/tfs/workitems/{id}/comments
   → On success: prepend to comment list, clear input
   → On failure: show error, preserve input text

4. User closes modal
   → Close button / Escape / backdrop click
   → boardState.SelectItem(null)
```
