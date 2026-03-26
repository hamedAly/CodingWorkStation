# Data Model: TFS Kanban Board

**Feature**: 008-tfs-kanban-board  
**Date**: 2026-03-25

---

## Entities

### WorkItemResponse (Existing — Extended)

Existing DTO fetched from TFS. Already defined in `TfsContracts.cs`. No changes needed for the board view.

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| Id | int | `System.Id` | TFS work item ID |
| Title | string | `System.Title` | Work item title |
| WorkItemType | string | `System.WorkItemType` | "Task", "Bug", "User Story", "Feature", "Epic" |
| State | string | `System.State` | Current state: New, Active, Resolved, Closed |
| AssignedTo | string? | `System.AssignedTo` | Display name of assigned user |
| AreaPath | string? | `System.AreaPath` | Project/area path (e.g., "Mobile\PrvPortal") |
| IterationPath | string? | `System.IterationPath` | Sprint/iteration path |
| Priority | string? | `Microsoft.VSTS.Common.Priority` | Priority level (1-4) |
| CreatedDate | DateTime? | `System.CreatedDate` | Creation timestamp |
| ChangedDate | DateTime? | `System.ChangedDate` | Last modification timestamp |
| Url | string | Computed | Direct link to work item in TFS web UI |

---

### UpdateWorkItemStateRequest (New)

Request DTO for changing a work item's state via drag-and-drop.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| State | string | Yes | NotEmpty, must be one of: New, Active, Resolved, Closed | Target state for the work item |

---

### UpdateWorkItemStateResponse (New)

Response DTO confirming a state update.

| Field | Type | Description |
|-------|------|-------------|
| Success | bool | Whether the update succeeded |
| Error | string? | Error message if failed (TFS rejection, network error) |
| NewState | string? | Confirmed new state from TFS (may differ if TFS auto-transitions) |

---

### WorkItemCommentResponse (New)

A comment on a work item, fetched from TFS Comments API.

| Field | Type | Source | Description |
|-------|------|--------|-------------|
| Id | int | `comments[].id` | Comment ID within the work item |
| Text | string | `comments[].text` | Comment body (HTML) |
| CreatedBy | string | `comments[].createdBy.displayName` | Author display name |
| CreatedDate | DateTime | `comments[].createdDate` | Comment creation timestamp |

---

### WorkItemCommentsResponse (New)

Container for a list of comments.

| Field | Type | Description |
|-------|------|-------------|
| Comments | IReadOnlyList\<WorkItemCommentResponse\> | Ordered list of comments (chronological) |
| TotalCount | int | Total number of comments on the work item |

---

### AddWorkItemCommentRequest (New)

Request DTO for adding a new comment to a work item.

| Field | Type | Required | Validation | Description |
|-------|------|----------|------------|-------------|
| Text | string | Yes | NotEmpty, MaxLength(4000) | Comment body text |

---

### AddWorkItemCommentResponse (New)

Response DTO after adding a comment.

| Field | Type | Description |
|-------|------|-------------|
| Success | bool | Whether the comment was added |
| Comment | WorkItemCommentResponse? | The created comment (on success) |
| Error | string? | Error message (on failure) |

---

## Domain Records (New — in ITfsApiClient.cs)

### TfsWorkItemComment

Domain record returned by `ITfsApiClient`. Mapped to `WorkItemCommentResponse` at the controller layer.

| Field | Type | Description |
|-------|------|-------------|
| Id | int | Comment ID |
| Text | string | Comment body (HTML from TFS) |
| CreatedBy | string | Author display name |
| CreatedDate | DateTime | Creation timestamp |

---

## Client-Side State (Blazor — no persistence)

### KanbanBoardState

In-memory state class for the board page. Not persisted; reconstructed on page load.

| Field | Type | Description |
|-------|------|-------------|
| Items | List\<WorkItemResponse\> | All work items on the board |
| DraggedItem | WorkItemResponse? | Item currently being dragged (null when idle) |
| DragSourceState | string? | Original column state of the dragged item |
| SyncingItemIds | HashSet\<int\> | IDs of items currently syncing state with TFS |
| ErrorItems | Dictionary\<int, string\> | Item ID → error message for failed operations |
| SelectedItem | WorkItemResponse? | Item whose detail modal is open |
| IsLoading | bool | Whether the board is fetching data |
| Error | string? | Board-level error message |

### Methods:
- `StartDrag(WorkItemResponse item)` — Sets DraggedItem and DragSourceState
- `CancelDrag()` — Clears drag state
- `DropOnColumn(string targetState)` — Optimistic update: moves item, triggers sync
- `CompleteSyncSuccess(int itemId)` — Removes from SyncingItemIds
- `CompleteSyncFailure(int itemId, string error)` — Reverts item, adds to ErrorItems
- `DismissError(int itemId)` — Removes from ErrorItems
- `SelectItem(WorkItemResponse? item)` — Opens/closes detail modal

---

## State Transitions

### Work Item State Flow (TFS Default)

```
New → Active → Resolved → Closed
       ↑          ↓
       └──────────┘  (Reactivated)
```

All transitions are validated by TFS server-side. The board allows dropping on any column — TFS will reject invalid transitions with a descriptive 400 error.

### Drag-and-Drop State Machine

```
Idle → Dragging → Dropped
  ↑       ↓          ↓
  └───────┘     ┌────┴────┐
  (canceled)    │         │
              Syncing   Failed
                │         │
              Success   Reverted
                │         │
                └────┬────┘
                     ↓
                   Idle
```

---

## No New Database Tables

This feature does not require any new SQLite tables. All data flows through the TFS REST API in real-time. The existing `TfsCredentials` table provides authentication context.
