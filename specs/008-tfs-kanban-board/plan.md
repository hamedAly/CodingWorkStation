# Implementation Plan: TFS Kanban Board

**Branch**: `008-tfs-kanban-board` | **Date**: 2026-03-25 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/008-tfs-kanban-board/spec.md`

## Summary

Build an interactive Jira-like Kanban Board for TFS work items using the existing Blazor Web App (Interactive Server) architecture. The board replaces the current read-only `MyWork.razor` page with a fully componentized board featuring drag-and-drop state management via the HTML5 Drag and Drop API, a slide-over detail modal with comments, and dark mode support. Backend extensions add work item state update (`PATCH`) and comment CRUD endpoints to the existing TFS API integration.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, Tailwind CSS 4.x (standalone CLI), HTML5 Drag and Drop API (no external JS library)  
**Storage**: Existing SQLite file database for credentials; no new tables required  
**Testing**: xUnit (unit tests for handlers, validators; integration tests for API endpoints)  
**Target Platform**: Web (server-side Blazor, all modern browsers)  
**Project Type**: Web application (monolithic — single ASP.NET Core project with Blazor SSR)  
**Performance Goals**: Board render < 3s, modal open < 500ms, drag-and-drop UI response < 2s (excluding TFS latency)  
**Constraints**: Must integrate with existing TFS REST API fallback strategy (API versions 7.1 → 6.0 → 5.1); must follow existing CQRS/MediatR patterns  
**Scale/Scope**: Single-user dashboard; up to 200 work items per board (existing WIQL limit)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Rule | Status | Notes |
|------|------|--------|-------|
| III — Thin Controllers | ≤ 15 lines per action | ✅ PASS | New endpoints follow existing pattern: receive request → send command/query → map response |
| XII — CQRS | One action → one command/query | ✅ PASS | UpdateWorkItemState → command, GetWorkItemComments → query, AddWorkItemComment → command |
| XIII — FluentValidation | Automatic pipeline | ✅ PASS | New commands get FluentValidation validators registered via assembly scanning |
| XIV — Separation of Concerns | Strict layer separation | ✅ PASS | Domain (interfaces/records) → Application (handlers) → Infrastructure (TfsApiClient) → Web (controllers + Blazor) |
| I.3 — Method length | ≤ 40 lines soft, ≤ 60 hard | ✅ PASS | UI components decomposed into small sub-components (Card, Column, Modal, CommentSection) |
| I.4 — File length | ≤ 400 lines | ✅ PASS | Board decomposed into 6+ component files; no monolithic page |
| II.3 — Feature-based folders | Feature-based structure | ✅ PASS | New components under `Components/Kanban/`; follows existing pattern (Quality/, Architecture/, etc.) |
| V — Type Safety | Strict types, no `any` | ✅ PASS | All DTOs are sealed records; no dynamic/object usage |
| VII.3 — Interactive states | Loading / error / disabled | ✅ PASS | Board has loading, error, empty states; cards have syncing/error states |
| FR-021 — Component isolation | Reusable isolated components | ✅ PASS | Board, Column, Card, DetailModal, CommentSection are independent |
| FR-022 — Separated DnD logic | DnD state separate from UI | ✅ PASS | `KanbanBoardState` service manages drag state; components consume via cascading value |

**No violations. All gates pass.**

## Project Structure

### Documentation (this feature)

```text
specs/008-tfs-kanban-board/
├── plan.md              # This file
├── research.md          # Phase 0: TFS API research (PATCH, comments)
├── data-model.md        # Phase 1: Entity definitions
├── quickstart.md        # Phase 1: Getting started guide
├── contracts/           # Phase 1: API contracts
│   ├── kanban-api.md    # Work item state update + comment endpoints
│   └── component-tree.md # Blazor component hierarchy
└── tasks.md             # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/
│   └── Interfaces/
│       └── ITfsApiClient.cs              # +3 methods: UpdateWorkItemStateAsync, GetWorkItemCommentsAsync, AddWorkItemCommentAsync
│
├── SemanticSearch.Application/
│   └── Tfs/
│       ├── Commands/
│       │   ├── UpdateWorkItemState.cs    # NEW: Command + handler + validator
│       │   └── AddWorkItemComment.cs     # NEW: Command + handler + validator
│       └── Queries/
│           └── GetWorkItemComments.cs    # NEW: Query + handler
│
├── SemanticSearch.Infrastructure/
│   └── Tfs/
│       └── TfsApiClient.cs              # +3 method implementations (PATCH state, GET/POST comments)
│
└── SemanticSearch.WebApi/
    ├── Contracts/Tfs/
    │   └── TfsContracts.cs              # +4 DTOs: UpdateWorkItemStateRequest, WorkItemCommentResponse, etc.
    ├── Controllers/
    │   └── TfsController.cs             # +3 endpoints: PATCH workitems/{id}/state, GET/POST workitems/{id}/comments
    ├── Services/
    │   └── WorkspaceApiClient.cs        # +3 methods for new endpoints
    ├── Components/
    │   ├── Pages/
    │   │   └── KanbanBoard.razor        # NEW: Top-level page (replaces /my-work route)
    │   └── Kanban/                      # NEW: Feature component folder
    │       ├── BoardColumn.razor         # Column container with drop zone
    │       ├── TicketCard.razor          # Individual card with drag source
    │       ├── TicketDetailsModal.razor  # Sliding modal/side-drawer
    │       ├── CommentSection.razor      # Activity feed + comment input
    │       └── KanbanBoardState.cs       # Board state management service (drag state, optimistic updates)
    ├── wwwroot/
    │   ├── css/app.css                  # +kanban v2 styles, dark mode theme, modal styles
    │   └── js/kanban.js                 # NEW: Minimal JS interop for drag-and-drop events (if needed)
    └── Styles/
        └── shell.input.css             # Unchanged (Tailwind utilities already available)
```

**Structure Decision**: Follow the existing feature-based folder pattern. New Blazor components go under `Components/Kanban/` (matching the existing `Quality/`, `Architecture/`, `Explorer/` pattern). The page-level component stays in `Components/Pages/`. Backend changes extend existing files (ITfsApiClient, TfsApiClient, TfsController, TfsContracts) with new commands/queries in the Application layer.

## Complexity Tracking

No constitution violations to justify.
