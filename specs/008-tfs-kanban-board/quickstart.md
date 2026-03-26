# Quickstart: TFS Kanban Board

**Feature**: 008-tfs-kanban-board  
**Date**: 2026-03-25

---

## Prerequisites

1. **.NET 10 SDK** installed
2. **TFS/Azure DevOps credentials** already configured via the Integration Settings page (`/integration`)
3. **Tailwind CSS CLI** available at `tools/tailwindcss.exe` (already in repo)

## Build & Run

```bash
cd src/SemanticSearch.WebApi
dotnet run
```

Navigate to `https://localhost:5001/my-work` (or the configured port).

## What to Verify

### Board View (US1 — P1)
- [ ] Board displays 4 columns: New, Active, Resolved, Closed
- [ ] Each column shows a count of items
- [ ] Cards display: type badge (colored), title (prominent), ID (muted), area path (muted)
- [ ] Bug cards have a red badge, Task cards blue, User Story cards green
- [ ] Hovering a card shows a lift + shadow effect
- [ ] Empty columns show an empty-state indicator
- [ ] Loading state appears while fetching
- [ ] Error state appears if TFS connection fails

### Drag and Drop (US2 — P2)
- [ ] Cards are draggable (cursor changes on hover)
- [ ] Dragging over a column highlights it as a drop zone
- [ ] Dropping a card in a new column moves it immediately (optimistic)
- [ ] A syncing spinner appears on the card during TFS update
- [ ] Successful sync removes the spinner
- [ ] Failed sync reverts the card to the original column + shows error

### Detail Modal (US3 — P3)
- [ ] Clicking a card opens a modal/side-drawer
- [ ] Modal displays all work item fields (ID, type, title, state, assigned to, area, iteration, priority, dates)
- [ ] "Open in TFS" link works
- [ ] Modal closes via close button, Escape key, or backdrop click

### Comments (US4 — P4)
- [ ] Comments section appears in the modal
- [ ] Existing comments load with author name and timestamp
- [ ] New comment can be typed and submitted
- [ ] Submitted comment appears in the list
- [ ] Empty state shows "No comments yet"

### Dark Mode (US5 — P5)
- [ ] Dark mode toggle exists (TopBar or sidebar)
- [ ] Toggling switches all board elements to dark theme
- [ ] Preference persists across page navigations
- [ ] All text is readable in dark mode

## API Endpoints to Test

```bash
# Get work items (existing)
GET /api/tfs/workitems

# Update work item state (new)
PATCH /api/tfs/workitems/26690/state
Content-Type: application/json
{ "state": "Active" }

# Get work item comments (new)
GET /api/tfs/workitems/26690/comments

# Add work item comment (new)
POST /api/tfs/workitems/26690/comments
Content-Type: application/json
{ "text": "Started working on this." }
```

## Key Files

| File | Purpose |
|------|---------|
| `Components/Pages/KanbanBoard.razor` | Top-level page (replaces MyWork.razor) |
| `Components/Kanban/BoardColumn.razor` | Column with drop zone |
| `Components/Kanban/TicketCard.razor` | Card with drag source |
| `Components/Kanban/TicketDetailsModal.razor` | Detail modal with comments |
| `Components/Kanban/CommentSection.razor` | Comment activity feed |
| `Components/Kanban/KanbanBoardState.cs` | Board state management |
| `Controllers/TfsController.cs` | +3 new endpoints |
| `Contracts/Tfs/TfsContracts.cs` | +6 new DTOs |
| `Application/Tfs/Commands/UpdateWorkItemState.cs` | State update command |
| `Application/Tfs/Commands/AddWorkItemComment.cs` | Comment add command |
| `Application/Tfs/Queries/GetWorkItemComments.cs` | Comment fetch query |
| `Infrastructure/Tfs/TfsApiClient.cs` | +3 TFS API method implementations |
| `wwwroot/css/app.css` | Extended kanban + dark mode styles |
