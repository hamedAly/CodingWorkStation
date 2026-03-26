# Feature Specification: TFS Kanban Board

**Feature Branch**: `008-tfs-kanban-board`  
**Created**: 2026-03-25  
**Status**: Draft  
**Input**: User description: "Build a highly interactive, modern, Jira-like Kanban Board to visualize and manage TFS work items with drag-and-drop, detailed modals with comments, clean componentized architecture, and dark mode support."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View Work Items on a Kanban Board (Priority: P1)

As a developer, I open the Kanban Board page and see my TFS-assigned work items organized into columns by state (New, Active, Resolved, Closed). Each card displays the work item type as a colored badge, the title prominently, and the ID and project path in a muted style. The board has a clean, modern aesthetic with a subtle background. I can quickly scan my workload at a glance.

**Why this priority**: This is the foundational view — without a well-designed board and cards, no other interactions (drag-and-drop, modals, comments) have value. It replaces the need to switch to the TFS web UI for workload visibility.

**Independent Test**: Can be fully tested by configuring TFS credentials, navigating to the Kanban Board, and verifying work items appear in the correct state columns with proper type badges, titles, IDs, and project paths.

**Acceptance Scenarios**:

1. **Given** valid TFS credentials are stored and work items exist, **When** the user navigates to the Kanban Board, **Then** work items are displayed in columns corresponding to their state (New, Active, Resolved, Closed).
2. **Given** a work item is of type "Bug," **When** it is rendered on the board, **Then** it displays a red badge labeled "Bug."
3. **Given** a work item is of type "Task," **When** it is rendered on the board, **Then** it displays a blue badge labeled "Task."
4. **Given** a work item is of type "User Story," **When** it is rendered on the board, **Then** it displays a green badge labeled "User Story."
5. **Given** each card is displayed, **Then** the title is prominent, and the ID (e.g., #26690) and project path (e.g., Mobile\PrvPortal) are visible but visually muted.
6. **Given** the user hovers over a card, **Then** the card shows a subtle lift and shadow effect.
7. **Given** a column has zero work items, **Then** the column still displays with an empty-state indicator.

---

### User Story 2 - Drag and Drop Work Items Between Columns (Priority: P2)

As a developer, I drag a work item card from one column and drop it into another column to change its state. When I drop the card, the board shows a loading indicator on that card while the background syncs with TFS. Once the sync completes, the card settles into its new column. If the sync fails, the card returns to its original column with an error message.

**Why this priority**: Drag-and-drop is the key interaction that elevates this board from a read-only display to an actionable management tool. Without it, users must still go to TFS to update states.

**Independent Test**: Can be tested by dragging a card from "New" to "Active," verifying the loading state appears, and confirming the work item state is updated in TFS. Test the failure case by simulating a network error and verifying the card returns to the original column.

**Acceptance Scenarios**:

1. **Given** a work item card is in the "New" column, **When** the user drags it to the "Active" column and drops it, **Then** the card appears in the "Active" column and a loading indicator is shown on the card.
2. **Given** a card has been dropped in a new column, **When** the background sync completes successfully, **Then** the loading indicator is removed and the card remains in the new column.
3. **Given** a card has been dropped in a new column, **When** the background sync fails, **Then** the card animates back to its original column and an error notification is displayed.
4. **Given** the user is dragging a card, **Then** the target column visually highlights as a valid drop zone.
5. **Given** a card is being dragged, **Then** the original card position shows a placeholder ghost element.

---

### User Story 3 - View Work Item Details in a Modal (Priority: P3)

As a developer, I click on a work item card to open a detailed view in a centered modal or sliding side-drawer. The modal displays the full ticket details (ID, type, title, state, assigned user, area path, iteration path, priority, created date, changed date) and provides a link to open the item in TFS. The modal can be dismissed by clicking outside it, pressing Escape, or clicking a close button.

**Why this priority**: Detailed views complete the information loop — users can see all metadata without leaving the board. This builds on the board view (US1) and provides the container for comments (US4).

**Independent Test**: Can be tested by clicking any card on the board and verifying the modal opens with complete work item details, then dismissing via each method (close button, Escape key, backdrop click).

**Acceptance Scenarios**:

1. **Given** a work item card is displayed on the board, **When** the user clicks the card, **Then** a modal or side-drawer opens displaying full work item details.
2. **Given** the detail modal is open, **Then** it displays: ID, type (with colored badge), title, state, assigned user, area path, iteration path, priority, created date, changed date, and a link to open in TFS.
3. **Given** the detail modal is open, **When** the user clicks the close button, **Then** the modal closes and the board is visible again.
4. **Given** the detail modal is open, **When** the user presses the Escape key, **Then** the modal closes.
5. **Given** the detail modal is open, **When** the user clicks outside the modal on the backdrop, **Then** the modal closes.

---

### User Story 4 - View and Add Comments on Work Items (Priority: P4)

As a developer, while viewing a work item in the detail modal, I scroll down to see a "Comments/Activity" section that shows existing comments. I can type a new comment in a text input area and submit it. The new comment appears in the list after successful submission.

**Why this priority**: Comments add collaboration capability directly within the board. This is a natural extension of the detail modal (US3) but is not essential for the core board experience.

**Independent Test**: Can be tested by opening a work item modal, viewing existing comments (if any), typing a new comment, submitting it, and verifying it appears in the comment list.

**Acceptance Scenarios**:

1. **Given** a work item detail modal is open, **Then** a "Comments/Activity" section is visible below the work item details.
2. **Given** the work item has existing comments in TFS, **When** the modal opens, **Then** existing comments are displayed in chronological order with author and timestamp.
3. **Given** the user types a comment and clicks submit, **When** the submission succeeds, **Then** the new comment appears in the list and the input area is cleared.
4. **Given** the user types a comment and clicks submit, **When** the submission fails, **Then** an error message is displayed and the typed comment is preserved in the input area.
5. **Given** no comments exist for the work item, **Then** the section shows an empty state message (e.g., "No comments yet").

---

### User Story 5 - Dark Mode Support (Priority: P5)

As a developer, I can toggle between light and dark modes for the Kanban Board. The board, cards, modals, and all UI elements adapt their color scheme accordingly, maintaining readability and visual harmony.

**Why this priority**: Dark mode is a polish feature that enhances user experience, especially during long sessions. It does not add functional capability but is highly requested for developer tools.

**Independent Test**: Can be tested by toggling dark mode and verifying all board elements (columns, cards, badges, modals, backgrounds) adapt to the dark color scheme with proper contrast.

**Acceptance Scenarios**:

1. **Given** the user is in light mode, **When** they toggle to dark mode, **Then** the board background, columns, cards, and modals switch to a dark color scheme.
2. **Given** the user is in dark mode, **Then** all text, badges, and interactive elements remain readable with sufficient contrast.
3. **Given** the user toggles dark mode, **Then** the preference persists across page navigations within the session.

---

### Edge Cases

- What happens when the user has zero assigned work items? The board displays all four state columns with empty-state indicators and a message suggesting to check TFS credentials configuration.
- What happens when a drag-and-drop state update fails due to a network error? The card animates back to its original column and a non-blocking error notification is shown.
- What happens when the TFS API rejects a state transition (e.g., "New" directly to "Closed")? The card returns to its original column and the error message from TFS is displayed to the user.
- What happens when a work item type is not one of the known types (Task, Bug, User Story)? The card displays a neutral/default badge color with the type name as-is.
- What happens when the user clicks a card while another modal is already open? The current modal closes and the new one opens.
- What happens when comments fail to load? The comments section shows an error message with a retry option; the rest of the work item details remain visible.
- What happens when the work item title is extremely long? The title is truncated with an ellipsis on the card, and shown in full in the detail modal.
- What happens when a column has many items (50+)? The column becomes scrollable while the board header remains fixed.

## Requirements *(mandatory)*

### Functional Requirements

**Board Display**

- **FR-001**: System MUST display a board with four columns mapped to TFS work item states: New, Active, Resolved, and Closed.
- **FR-002**: Each column MUST display a header with the state name and a count of work items in that column.
- **FR-003**: The board MUST fetch and display work items assigned to the authenticated user from the existing TFS integration.
- **FR-004**: The board MUST support a loading state while fetching work items, an error state when fetching fails, and an empty state when no items are assigned.

**Card Design**

- **FR-005**: Each card MUST display the work item type as a colored badge — red for Bug, blue for Task, green for User Story, and a neutral color for any other type.
- **FR-006**: Each card MUST display the title prominently, with the work item ID (prefixed with #) and project path (area path) in a muted style.
- **FR-007**: Cards MUST have a hover effect that provides a subtle lift and shadow to indicate interactivity.

**Drag and Drop**

- **FR-008**: Users MUST be able to drag a work item card from one state column and drop it into another state column.
- **FR-009**: When a card is dropped in a new column, the system MUST display a loading indicator on the card while the state update is sent to TFS.
- **FR-010**: System MUST update the work item state in TFS when a card is dropped into a new column.
- **FR-011**: If the state update fails, the card MUST animate back to its original column and a user-visible error notification MUST be shown.
- **FR-012**: The target column MUST visually highlight as a valid drop zone while a card is being dragged over it.

**Detail Modal**

- **FR-013**: Clicking a card MUST open a modal or side-drawer displaying the full work item details: ID, type, title, state, assigned user, area path, iteration path, priority, created date, changed date, and a link to open in TFS.
- **FR-014**: The modal MUST be dismissible by clicking a close button, pressing the Escape key, or clicking outside the modal on the backdrop.

**Comments**

- **FR-015**: The detail modal MUST include a "Comments/Activity" section below the work item details.
- **FR-016**: Existing comments from TFS MUST be displayed in chronological order with author name and timestamp.
- **FR-017**: Users MUST be able to type a new comment in a text input area and submit it to TFS.
- **FR-018**: After successful comment submission, the new comment MUST appear in the list and the input area MUST be cleared.

**Dark Mode**

- **FR-019**: The board MUST support dark mode, with all elements (columns, cards, badges, modals, backgrounds) adapting to a dark color scheme.
- **FR-020**: The dark mode preference MUST persist across page navigations within the session.

**Component Architecture**

- **FR-021**: The board UI MUST be composed of reusable, isolated components: Board (layout), Column (per-state container), Card (individual ticket), Detail Modal (full details view), and Comment Section (activity feed).
- **FR-022**: Drag-and-drop logic and board state management MUST be separated from UI components so that the data sync layer can be modified independently.

### Key Entities

- **Work Item**: A task, bug, or user story fetched from TFS. Key attributes: ID, title, type, state, assigned user, area path, iteration path, priority, created date, changed date, URL.
- **Board Column**: A visual container representing a work item state (New, Active, Resolved, Closed). Contains zero or more work item cards.
- **Work Item Comment**: A comment associated with a work item. Key attributes: author, timestamp, body text.
- **Board State**: The current client-side representation of all work items and their column assignments, including any in-flight drag operations and optimistic updates.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view all their assigned TFS work items organized by state within 3 seconds of page load.
- **SC-002**: Users can change a work item's state by drag-and-drop in under 2 seconds (excluding TFS API latency).
- **SC-003**: Users can view complete work item details via one click, with the modal appearing within 500 milliseconds.
- **SC-004**: Users can add a comment to a work item without leaving the board.
- **SC-005**: 100% of board components are reusable in isolation — each can be rendered independently with mock data.
- **SC-006**: A failed drag-and-drop operation visually reverts within 1 second, with a clear error message visible to the user.
- **SC-007**: The board is fully functional in both light and dark modes with all text meeting WCAG AA contrast requirements.

## Assumptions

- TFS credentials are already configured via the existing Integration Settings page. This feature does not implement credential management.
- The existing TFS REST API integration (work item fetch, authentication) is functional and will be extended — not rebuilt — for state updates and comments.
- Work item state transitions follow the standard TFS workflow: New → Active → Resolved → Closed, with reactivation (Resolved → Active) allowed. Invalid transitions will be rejected by TFS and surfaced as errors.
- The TFS REST API supports work item updates (`PATCH /_apis/wit/workitems/{id}`) and comment retrieval/creation (`GET/POST /_apis/wit/workitems/{id}/comments`). API version compatibility follows the existing fallback strategy.
- The four-column layout (New, Active, Resolved, Closed) is sufficient for the user's workflow. Custom columns or additional states are out of scope.
- The board displays only work items assigned to the current user (consistent with the existing "My Work" page behavior).
- Dark mode follows the application-wide theme system if one exists; otherwise, a board-specific toggle is provided.
