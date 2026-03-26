# Research: TFS Kanban Board

**Feature**: 008-tfs-kanban-board  
**Date**: 2026-03-25

---

## R-001: TFS REST API — Update Work Item State (PATCH)

**Decision**: Use `PATCH /_apis/wit/workitems/{id}?api-version={ver}` with JSON Patch operations to update the `System.State` field.

**Rationale**: This is the standard TFS/Azure DevOps API for updating work item fields. JSON Patch is the only supported format. The existing codebase already handles API version fallback (7.1 → 6.0 → 5.1), so the same pattern applies.

**Alternatives considered**:
- Bulk update endpoint (`POST /_apis/wit/workitemsbatch`): Only supports reads (batch GET), not updates.
- Work item template-based updates: Overkill for single-field state changes.

**Key details**:
- HTTP method: `PATCH`
- Content-Type: `application/json-patch+json`
- Request body:
  ```json
  [
    {
      "op": "replace",
      "path": "/fields/System.State",
      "value": "Active"
    }
  ]
  ```
- Response: Full work item JSON (same as GET response)
- Error cases: 400 (invalid state transition), 401 (auth), 404 (not found)
- State transitions are validated by TFS based on the work item type's workflow rules. Invalid transitions (e.g., "New" → "Closed" for a Task) return 400 with a descriptive error message.
- The API version fallback strategy from `GetAssignedWorkItemsAsync` should be reused.

---

## R-002: TFS REST API — Work Item Comments

**Decision**: Use the Work Item Comments API (`/_apis/wit/workitems/{id}/comments`) for both retrieving and adding comments.

**Rationale**: The Comments API is a dedicated resource introduced in API v3.0+. It provides structured comment data (author, timestamp, text) rather than requiring parsing from the work item History field.

**Alternatives considered**:
- Work Item History field (`System.History`): Older API pattern, writes to the discussion/history field via PATCH. Retrieval requires extracting from the full revision history — complex parsing.
- Work Item Updates endpoint (`/_apis/wit/workitems/{id}/updates`): Returns all field changes, not just comments. Overly broad and requires filtering.

**Key details**:

### GET Comments
- Endpoint: `GET /_apis/wit/workitems/{id}/comments?api-version=7.1-preview.4`
- Response:
  ```json
  {
    "totalCount": 2,
    "count": 2,
    "comments": [
      {
        "id": 1,
        "text": "<div>Comment HTML text</div>",
        "createdBy": { "displayName": "User Name", "id": "..." },
        "createdDate": "2026-03-20T10:30:00Z",
        "modifiedDate": "2026-03-20T10:30:00Z"
      }
    ]
  }
  ```
- The `text` field contains HTML. For display purposes, render as-is or strip tags for plain text.
- Uses `$top` and `$skip` for pagination. Default returns up to 200 comments.
- The `-preview` suffix is required for comment APIs even in stable API versions.

### POST Comment
- Endpoint: `POST /_apis/wit/workitems/{id}/comments?api-version=7.1-preview.4`
- Content-Type: `application/json`
- Request body:
  ```json
  {
    "text": "Comment text (supports HTML)"
  }
  ```
- Response: The created comment object (same structure as GET items)

### Fallback for older TFS versions
- If the Comments API is unavailable (pre-2019 TFS), fall back to `System.History`:
  - Add comment: `PATCH` with `{ "op": "add", "path": "/fields/System.History", "value": "text" }`
  - Read comments: Not feasible via History field — show "Comments unavailable for this server version"

---

## R-003: Drag-and-Drop in Blazor Server (HTML5 API)

**Decision**: Use the native HTML5 Drag and Drop API via Blazor's built-in event binding (`@ondragstart`, `@ondragover`, `@ondrop`, etc.) without any external JavaScript library.

**Rationale**: Blazor Server supports all HTML5 drag events natively. The board has a simple structure (4 columns, cards move between them) that doesn't require sortable reordering within columns. No external library dependency is needed, keeping the bundle size minimal and avoiding JavaScript interop complexity.

**Alternatives considered**:
- SortableJS via JS interop: Adds ~10KB JS, requires IJSRuntime bridging for every drag event. Overkill for column-to-column moves without intra-column reordering.
- Blazor community libraries (e.g., MudBlazor DragDrop): Would add a UI framework dependency that conflicts with the existing custom design system.
- Pure JS implementation with callbacks: Adds JS interop complexity without benefit over native Blazor events.

**Key details**:

### Blazor event bindings used:
- `@ondragstart` — on `TicketCard`: Set dragged item in board state
- `@ondragend` — on `TicketCard`: Clear drag state if not dropped
- `@ondragover` — on `BoardColumn`: Prevent default to allow drop; add visual highlight
- `@ondragleave` — on `BoardColumn`: Remove visual highlight
- `@ondrop` — on `BoardColumn`: Execute state change; trigger optimistic update

### Board state management:
- `KanbanBoardState` class tracks:
  - `DraggedItem`: The work item being dragged (null when idle)
  - `Items`: The full list with current state assignments
  - `SyncingItems`: Set of item IDs currently syncing with TFS
  - `ErrorItems`: Dictionary of item IDs → error messages
- Optimistic update pattern:
  1. On drop: Immediately move card to new column in local state
  2. Fire-and-forget API call to update TFS
  3. On success: Remove from `SyncingItems`
  4. On failure: Revert card to original column, add to `ErrorItems`, show notification

### Blazor Server considerations:
- All drag events are handled server-side (SignalR round-trip per event)
- `@ondragover` fires frequently — use `@ondragover:preventDefault` attribute (no handler needed for allow-drop)
- Use `@ondragover="@(() => {})"` only if column highlighting is needed
- `DataTransfer` is not accessible in Blazor Server — use C# state object instead

---

## R-004: Dark Mode Implementation Strategy

**Decision**: Implement dark mode using CSS custom properties with a `.dark` class on the `<html>` element, toggled via a small JS interop call. Persist the preference in `localStorage`.

**Rationale**: The existing `app.css` already defines unused dark-mode variables (`--surface-dark`, `--surface-dark-soft`). The most compatible approach is a CSS-variable-based theme system that overrides the existing `:root` variables when `.dark` is applied. This integrates with both the custom design system and Tailwind CSS 4.x.

**Alternatives considered**:
- `prefers-color-scheme` media query only: No user toggle; depends on OS setting which users may not control.
- Blazor cascading value for theme: Would require re-rendering the entire component tree on toggle; CSS-only toggle is more efficient.
- Tailwind `dark:` variant: Would require rewriting all component classes to use Tailwind dark variants — breaks the existing custom CSS approach.

**Key details**:
- Add dark-mode variable overrides in `app.css` under `.dark` selector:
  ```css
  .dark {
    --bg: #0b1120;
    --surface: #1a2332;
    --ink: #e2e8f0;
    --muted: #94a3b8;
    /* etc. */
  }
  ```
- Toggle button in the TopBar component or sidebar
- JS interop for `localStorage` persistence:
  ```javascript
  window.themeManager = {
    get: () => localStorage.getItem('theme') || 'light',
    set: (theme) => {
      localStorage.setItem('theme', theme);
      document.documentElement.classList.toggle('dark', theme === 'dark');
    }
  };
  ```
- On page load, apply theme from `localStorage` before Blazor hydration (inline `<script>` in `App.razor`)

---

## R-005: Component Architecture & Reusability

**Decision**: Decompose the board into 5 Blazor components under a `Components/Kanban/` feature folder, plus a `KanbanBoardState` class for state management passed via cascading value.

**Rationale**: Following the existing pattern where each feature has its own component folder (Quality/, Architecture/, Explorer/). The component tree maps directly to the spec's FR-021 requirement.

**Component hierarchy**:
```
KanbanBoard.razor (Page: /my-work)
├── CascadingValue<KanbanBoardState>
│   ├── BoardColumn.razor × 4 (one per state)
│   │   └── TicketCard.razor × N (one per work item)
│   └── TicketDetailsModal.razor (conditional, when item selected)
│       └── CommentSection.razor (activity feed + input)
```

**Key design decisions**:
- `KanbanBoardState` is a plain C# class (not a service) — instantiated per-page, passed as `CascadingValue`. This avoids Scoped service lifecycle issues with Blazor Server.
- Components communicate via:
  - `[Parameter]` for data flow down (items, column state)
  - `EventCallback` for events up (card clicked, card dropped)
  - `KanbanBoardState` cascading value for cross-cutting state (which item is being dragged)
- Modal uses the existing backdrop pattern from `DuplicateComparisonModal.razor` — fixed overlay, `@onclick:stopPropagation`, Escape key handling.

---

## R-006: Existing MyWork.razor Migration Strategy

**Decision**: Replace the existing `MyWork.razor` monolithic page with the new `KanbanBoard.razor` page at the same `/my-work` route. Delete the old page.

**Rationale**: The existing `MyWork.razor` is a single-file, non-componentized implementation (~140 lines) with basic cards, no drag-and-drop, and an inline detail panel. The new implementation is a complete rewrite with a different architecture. Preserving the old file adds no value.

**Migration notes**:
- The `/my-work` route is preserved (navigation links in `NavMenu.razor` continue to work)
- The `WorkspaceApiClient` methods for `GetWorkItemsAsync` are reused
- The `WorkItemResponse` DTO is reused; new DTOs are added alongside it
- CSS classes `kanban-board`, `kanban-column`, `kanban-card` are redefined/extended in `app.css`
