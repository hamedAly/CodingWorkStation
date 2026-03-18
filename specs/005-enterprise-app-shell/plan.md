# Implementation Plan: Premium Enterprise App Shell & UI Architecture

**Branch**: `005-enterprise-app-shell` | **Date**: 2026-03-17 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/005-enterprise-app-shell/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/commands/plan.md` for the execution workflow.

## Summary

Replace the existing flat `MainLayout.razor` and `NavMenu.razor` with a premium enterprise application shell that organizes navigation into five phase groups, adds a collapsible sidebar with icons and badges, introduces a top bar with breadcrumbs and a command palette placeholder, and applies a Tailwind CSS–driven design system with a professional slate/teal color palette. The existing custom CSS is preserved for page-level components; the shell uses Tailwind CSS standalone CLI for its own styling.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), Tailwind CSS 4.x (standalone CLI)  
**Storage**: N/A (UI-only feature; no database changes)  
**Testing**: Manual visual verification; no Blazor component test framework currently configured  
**Target Platform**: Local developer tool, Windows desktop browsers (Chrome/Edge), viewports 1024px–2560px  
**Project Type**: Web (Blazor Server-Side Rendered with Interactive Server components)  
**Performance Goals**: Shell renders without layout shifts; sidebar toggle < 200ms transition  
**Constraints**: Must coexist with existing custom CSS (`app.css`); no breaking changes to existing page components  
**Scale/Scope**: 11 navigation items across 5 phase groups; 6 active routes, 5 disabled "Coming Soon" placeholders

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality & Maintainability | ✅ PASS | Components well under 400 lines; methods under 40 lines |
| II. Code Organization & Readability | ✅ PASS | Feature-based structure in `Components/Layout/` |
| III. Comments & Documentation | ✅ PASS | Self-documenting component and parameter names |
| IV. DRY & Smart Duplication | ✅ PASS | Navigation items defined once as typed data, rendered by loop |
| V. Type System & Safety | ✅ PASS | Strongly-typed `NavigationItem` and `PhaseGroup` models |
| VI. Testing Standards | ⚠️ DEVIATION | No Blazor component test infrastructure exists in the project. Validated via manual testing and visual inspection. Acceptable for a local dev tool. |
| VII. UX & UI Consistency | ⚠️ DEVIATION | Constitution requires i18n for all user-facing text. This is a single-user local dev tool—hardcoded English strings are acceptable. All interactive elements will have proper disabled/hover/focus states and accessibility attributes. |
| VIII. Performance Budget | ✅ PASS | Tailwind CSS with purging produces < 15 KB CSS. No JS bundle impact. |
| IX. Git Hygiene | ✅ PASS | Feature branch `005-enterprise-app-shell` |
| X. Operations & Observability | N/A | No backend changes |
| XI. Security Baseline | ✅ PASS | No user input, no API changes |
| XII. Controller & API Layer | N/A | No API endpoints |
| XIII. Validation Strategy | N/A | No input validation |
| XIV. Separation of Concerns | ✅ PASS | Navigation data model separate from rendering components |

**Gate Result**: PASS (2 documented deviations, both justified for local dev tool context)

## Project Structure

### Documentation (this feature)

```text
specs/005-enterprise-app-shell/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (minimal — UI-only feature)
└── tasks.md             # Phase 2 output (NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
src/SemanticSearch.WebApi/
├── Components/
│   └── Layout/
│       ├── MainLayout.razor          # Rewritten: shell grid with sidebar + topbar + content
│       ├── MainLayout.razor.css      # Scoped Tailwind-based styles (if needed for overrides)
│       ├── NavMenu.razor             # Rewritten: phase-grouped sidebar with icons, badges, collapse
│       ├── NavMenu.razor.cs          # Code-behind with navigation data model and collapse state
│       ├── TopBar.razor              # New: breadcrumbs + command palette placeholder + profile area
│       └── TopBar.razor.cs           # New: breadcrumb generation from current route
├── Models/
│   └── Navigation/
│       ├── NavigationItem.cs         # Record: Label, Icon, Route, PhaseGroup, IsActive
│       └── PhaseGroup.cs             # Record: Name, PhaseNumber, Status (Active/ComingSoon), Items
├── wwwroot/
│   └── css/
│       ├── app.css                   # Existing — preserved, no changes
│       └── shell.css                 # New: Tailwind CLI output for shell components
└── tailwind.config.js                # New: Tailwind config scoped to Layout components
```

**Structure Decision**: The existing `Components/Layout/` folder is extended with the new `TopBar` component and code-behind files. Navigation data models go into a new `Models/Navigation/` folder to keep the data structure separate from rendering. The Tailwind CSS output is a separate `shell.css` file that coexists with the existing `app.css`. A `tailwind.config.js` at the WebApi project root enables the Tailwind standalone CLI to scan `.razor` files.

## Complexity Tracking

| Deviation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| No Blazor component tests | No test infrastructure for Blazor components exists in the project | Setting up bUnit would be a separate feature; manual verification is sufficient for layout components |
| Hardcoded English strings | Local single-user dev tool | Adding i18n infrastructure would be over-engineering for this context |
