# Tasks: Premium Enterprise App Shell & UI Architecture

**Input**: Design documents from `/specs/005-enterprise-app-shell/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not requested. Manual visual verification per quickstart.md.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Web app (single solution)**: `src/SemanticSearch.WebApi/` at repository root
- Generated CSS output: `src/SemanticSearch.WebApi/wwwroot/css/shell.css` (build artifact, not hand-edited)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Tailwind CSS toolchain setup, navigation data models, and icon library — required by all user stories

- [X] T001 Download Tailwind CSS v4 standalone binary to `tools/tailwindcss.exe` and add `tools/` to `.gitignore`
- [X] T002 Create Tailwind CSS input file at `src/SemanticSearch.WebApi/Styles/shell.input.css` with theme and utilities layers (no Preflight) per research.md R-001 and R-002
- [X] T003 Add `TailwindBuild` MSBuild target to `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj` that runs the Tailwind CLI before build
- [X] T004 Add `<link rel="stylesheet" href="css/shell.css" />` to `src/SemanticSearch.WebApi/Components/App.razor` before the existing `app.css` link
- [X] T005 [P] Create `PhaseStatus` enum in `src/SemanticSearch.WebApi/Models/Navigation/PhaseStatus.cs` with `Active` and `ComingSoon` values
- [X] T006 [P] Create `NavigationItem` record in `src/SemanticSearch.WebApi/Models/Navigation/NavigationItem.cs` with Label, Icon (MarkupString), Route (string?), IsActive (bool)
- [X] T007 [P] Create `PhaseGroup` record in `src/SemanticSearch.WebApi/Models/Navigation/PhaseGroup.cs` with Name, PhaseNumber, Status (PhaseStatus), Items (IReadOnlyList<NavigationItem>)
- [X] T008 [P] Create `BreadcrumbSegment` record in `src/SemanticSearch.WebApi/Models/Navigation/BreadcrumbSegment.cs` with Label, Href (string?), IsCurrent (bool)
- [X] T009 Create `Icons` static class in `src/SemanticSearch.WebApi/Components/Layout/Icons.cs` with all 16 Heroicon SVG MarkupString constants (Dashboard, Quality, Indexing, Search, Explorer, AiAssistant, Architecture, Dependency, Automation, Pipeline, Guardrails, Safety, ChevronRight, Bars3, XMark, UserCircle) per contracts/component-contracts.md

**Checkpoint**: Tailwind CLI generates `shell.css`, data models compile, icons are available. Build succeeds with `dotnet build`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core shell layout structure that MUST be complete before individual user stories can layer their features on top

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T010 Rewrite `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor` with the new shell grid layout: sidebar (`<aside>`) + top bar area + main content area with `@Body`. Add `bool _collapsed` field and `ToggleSidebar()` method. Pass `Collapsed` and `OnToggleCollapse` to NavMenu, pass `Collapsed` to TopBar. Use Tailwind classes for the grid (`flex`, `min-h-screen`), sidebar width (`w-64` / `w-16` based on collapsed state), content background (`bg-slate-50`), and `transition-all duration-200` for animated transitions
- [X] T011 Create skeleton `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor` with a `[Parameter] bool Collapsed` property and a placeholder `<header>` element with Tailwind classes. Breadcrumb and command palette content will be added in US2
- [X] T012 Create skeleton `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor` with `[Parameter] bool Collapsed` and `[Parameter] EventCallback OnToggleCollapse` properties. Render a basic `<nav>` with the sidebar background gradient using Tailwind classes. Full navigation items will be added in US1

**Checkpoint**: App renders the new three-panel shell layout (sidebar + topbar + content). Existing pages load correctly inside the content area. No navigation items yet.

---

## Phase 3: User Story 1 — Sidebar Navigation (Priority: P1) 🎯 MVP

**Goal**: Fully functional sidebar with five phase groups, icons, active/coming-soon badges, and collapse toggle

**Independent Test**: Launch the app, verify all 5 phase groups render with headings, all 6 active items navigate correctly, all 5 coming-soon items show badges and don't navigate, collapse toggle works

### Implementation for User Story 1

- [X] T013 Create `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor.cs` code-behind with the static `IReadOnlyList<PhaseGroup>` navigation data containing all 5 phase groups and 11 navigation items per data-model.md
- [X] T014 [US1] Implement branding area in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: render app name ("Developer Command Center") and subtitle ("Local Workspace") when expanded, show abbreviated icon when collapsed. Use Tailwind classes for typography (`text-xs`, `uppercase`, `tracking-wider`, `text-slate-400`), spacing, and conditional visibility
- [X] T015 [US1] Implement phase group rendering in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: loop over PhaseGroups, render phase heading with name and status badge ("Active" pill for Active phases, "Coming Soon" pill for ComingSoon phases). Hide headings when collapsed. Use Tailwind classes for badge styling (`text-xs`, `rounded-full`, `px-2`, `py-0.5`)
- [X] T016 [US1] Implement navigation item rendering in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: for each NavigationItem in a phase group, render icon + label. Active items use `<NavLink>` with `href` and `Match="NavLinkMatch.All"` for the dashboard or `NavLinkMatch.Prefix` for others. Coming Soon items render as disabled `<span>` with `cursor-not-allowed` and `opacity-50`. Use Tailwind hover/focus states (`hover:bg-white/10`, `focus:ring-2`, `focus:ring-teal-400`)
- [X] T017 [US1] Implement active state highlighting in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: the currently active NavLink gets visual distinction via Tailwind classes (`bg-white/15`, `text-white`, `font-semibold`) applied through Blazor's `ActiveClass` parameter on `<NavLink>`
- [X] T018 [US1] Implement collapse toggle button in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: render a button at the bottom of the sidebar that shows `Icons.XMark` when expanded and `Icons.Bars3` when collapsed. Wire `@onclick` to `OnToggleCollapse`. Use Tailwind transition classes for smooth icon swap
- [X] T019 [US1] Implement tooltip behavior for collapsed sidebar in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: when `Collapsed = true`, each navigation item wraps its icon in a container with a CSS-only tooltip (Tailwind `group`, `group-hover:visible`, `group-hover:opacity-100` pattern) showing the item label. Coming Soon items append " (Coming Soon)" to the tooltip text

**Checkpoint**: Sidebar fully functional — all 5 phase groups visible, 6 active links navigate, 5 coming-soon items disabled with badges, collapse toggle works, tooltips show on collapsed icons. This is the MVP.

---

## Phase 4: User Story 2 — Top Bar and Breadcrumbs (Priority: P2)

**Goal**: Top bar with route-aware breadcrumbs, command palette search placeholder, and profile area

**Independent Test**: Navigate to each active page, verify breadcrumbs show correct hierarchy, command palette placeholder shows "Ctrl+K", profile icon renders on the right

### Implementation for User Story 2

- [X] T020 [P] [US2] Create `BreadcrumbMap` static class in `src/SemanticSearch.WebApi/Components/Layout/BreadcrumbMap.cs` with route-to-label dictionary (7 routes per data-model.md) and `GetBreadcrumbs(string path)` method that returns `IReadOnlyList<BreadcrumbSegment>`
- [X] T021 [US2] Create `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor.cs` code-behind that injects `NavigationManager`, subscribes to `LocationChanged` in `OnInitialized`, calls `BreadcrumbMap.GetBreadcrumbs(...)` to produce the breadcrumb trail, and implements `IDisposable` to unsubscribe
- [X] T022 [US2] Implement breadcrumb trail rendering in `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor`: render `<nav aria-label="Breadcrumb">` with an ordered list of segments. Non-current segments render as `<a>` links with `href`. The last segment renders as `<span>` with `text-slate-500`. Segments separated by `@Icons.ChevronRight` SVG. Use Tailwind classes for breadcrumb styling (`flex`, `items-center`, `gap-1`, `text-sm`, `text-slate-600`)
- [X] T023 [US2] Implement command palette placeholder in `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor`: render a read-only search input with placeholder text "Search..." and a `<kbd>` badge showing "Ctrl+K". Use `@Icons.Search` as the leading icon. Style with Tailwind (`bg-slate-100`, `rounded-lg`, `px-3`, `py-1.5`, `text-sm`, `border border-slate-200`, glassmorphism effect with `backdrop-blur-sm`)
- [X] T024 [US2] Implement profile placeholder in `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor`: render `@Icons.UserCircle` in a circular container on the right side of the top bar. Add Tailwind hover ring (`hover:ring-2`, `hover:ring-teal-400`, `transition-all`, `rounded-full`)

**Checkpoint**: Top bar fully functional — breadcrumbs update on every navigation, command palette placeholder visible, profile icon renders. All US1 features still work.

---

## Phase 5: User Story 3 — Premium Visual Design (Priority: P2)

**Goal**: Apply premium visual polish across the entire shell — color palette, shadows, transitions, glassmorphism, and spacing consistency

**Independent Test**: Load any page, verify slate/gray backgrounds, teal accents, soft shadows on the sidebar and top bar, smooth hover transitions on all interactive elements, consistent spacing

### Implementation for User Story 3

- [X] T025 [US3] Apply premium sidebar styling in `src/SemanticSearch.WebApi/Components/Layout/NavMenu.razor`: set sidebar background to a dark gradient (`bg-gradient-to-b from-slate-800 to-slate-900`), text color to slate-200, soft inner border/glow effect, and consistent padding/spacing. Apply `shadow-xl` to the expanded sidebar. Ensure phase headings use `text-[10px]`, `uppercase`, `tracking-widest`, `text-slate-500` for the premium small-caps look
- [X] T026 [US3] Apply premium top bar styling in `src/SemanticSearch.WebApi/Components/Layout/TopBar.razor`: set background to `bg-white/80 backdrop-blur-md` for frosted glass effect, add bottom border `border-b border-slate-200`, consistent height (`h-14`), and horizontal padding. Apply `shadow-sm` for subtle elevation
- [X] T027 [US3] Apply premium main content area styling in `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor`: set content area background to `bg-slate-50`, add inner padding (`p-6`), and ensure existing page cards render cleanly within the new background tone
- [X] T028 [US3] Add transition and hover utilities across all interactive elements: ensure all sidebar nav items have `transition-colors duration-150`, buttons have `transition-all duration-200`, and focus states use `focus:outline-none focus:ring-2 focus:ring-teal-500 focus:ring-offset-2` ring pattern. These should already be partially applied from US1 tasks — this task does a consistency pass
- [X] T029 [US3] Add the "Coming Soon" badge styling refinement: badges should use `bg-amber-400/10 text-amber-500 text-[10px] font-medium rounded-full px-2 py-0.5` for a subtle premium look that contrasts with the sidebar's dark background

**Checkpoint**: The entire shell looks premium and consistent. Color palette, shadows, transitions, and spacing match across sidebar, top bar, and content area.

---

## Phase 6: User Story 4 — Sidebar Collapse Persistence & Responsiveness (Priority: P3)

**Goal**: Sidebar collapse state persists across page navigations (circuit-scoped) and the layout adapts to narrower viewports

**Independent Test**: Collapse sidebar → navigate to another page → sidebar remains collapsed. Resize browser below 1024px → sidebar auto-collapses or becomes an overlay

### Implementation for User Story 4

- [X] T030 [US4] Verify circuit-scoped persistence of `_collapsed` state in `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor`: confirm that the `bool _collapsed` field (already added in T010) naturally persists across Blazor page navigations within the same SignalR circuit. No additional code is needed — this task verifies the behavior works correctly by testing navigation while collapsed
- [X] T031 [US4] Add responsive breakpoint behavior in `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor`: use Tailwind responsive utilities to auto-collapse the sidebar below `lg:` breakpoint (1024px). At `< lg`, hide the sidebar off-screen and show a hamburger menu button in the top bar. Use `lg:w-64` for expanded, `lg:w-16` for collapsed, and `hidden lg:flex` patterns to conditionally show/hide the sidebar
- [X] T032 [US4] Implement mobile overlay behavior in `src/SemanticSearch.WebApi/Components/Layout/MainLayout.razor`: below `lg` breakpoint, when the hamburger button is clicked, show the sidebar as a fixed overlay with a semi-transparent backdrop (`fixed inset-0 z-40 bg-black/50`). Clicking the backdrop or a navigation link closes the overlay. Add `transition-transform duration-200` for slide-in/slide-out animation

**Checkpoint**: Sidebar collapse state persists across navigation. Below 1024px, sidebar adapts with overlay behavior. All previous stories still work.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Final verification, cleanup, and documentation validation

- [X] T033 Verify all existing pages render correctly within the new shell: navigate to each of the 6 active pages (Dashboard `/`, Quality `/quality`, Indexing `/indexing`, Search `/search`, Explorer `/explorer`, AI Assistant `/assistant`) and confirm no visual regressions in existing page components
- [X] T034 [P] Verify the build succeeds cleanly with `dotnet build` from repository root — no warnings related to shell components
- [X] T035 [P] Run quickstart.md validation: follow all steps in `specs/005-enterprise-app-shell/quickstart.md` to confirm the setup instructions are accurate and the verification checklist passes
- [X] T036 Add `@using SemanticSearch.WebApi.Models.Navigation` to `src/SemanticSearch.WebApi/Components/_Imports.razor` if not already included

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on T001–T009 (Setup) — BLOCKS all user stories
- **US1 Sidebar (Phase 3)**: Depends on Phase 2 completion
- **US2 Top Bar (Phase 4)**: Depends on Phase 2 completion; can run in parallel with US1
- **US3 Visual Polish (Phase 5)**: Depends on US1 (Phase 3) and US2 (Phase 4) — applies refinements to existing components
- **US4 Responsiveness (Phase 6)**: Depends on US1 (Phase 3) — modifies sidebar layout
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Phase 2 — **no dependencies on other stories** — delivers the MVP
- **User Story 2 (P2)**: Can start after Phase 2 — **no dependencies on US1** — TopBar is independent
- **User Story 3 (P2)**: Depends on US1 and US2 — **applies visual refinements to completed components**
- **User Story 4 (P3)**: Depends on US1 — **modifies sidebar layout for responsiveness**

### Within Each User Story

- Models / data classes before component rendering
- Component code-behind before razor markup
- Core rendering before visual polish
- Story complete before moving to next priority

### Parallel Opportunities

- T005, T006, T007, T008 can all run in parallel (independent model files)
- T020 (BreadcrumbMap) can run in parallel with US1 tasks (separate file)
- T034, T035 can run in parallel (verification tasks)
- US1 and US2 can run in parallel after Phase 2 (independent components)

---

## Parallel Examples

### Setup Phase — Model files:
```
T005: Create PhaseStatus.cs          ─┐
T006: Create NavigationItem.cs       ─┤─ All parallel (independent files)
T007: Create PhaseGroup.cs           ─┤
T008: Create BreadcrumbSegment.cs    ─┘
```

### User Stories 1 & 2 — After Phase 2:
```
US1 (Phase 3): NavMenu implementation ─┐
                                       ├─ Can run in parallel
US2 (Phase 4): TopBar implementation  ─┘
```

### Polish — Verification:
```
T034: dotnet build verification      ─┐
T035: quickstart.md validation       ─┤─ Both parallel
```

---

## Implementation Strategy

### MVP Scope (Recommended First Delivery)

**Phase 1 + Phase 2 + Phase 3 (User Story 1)** = Fully functional sidebar with all navigation. This is the core value delivery.

### Incremental Additions

1. **Add US2 (Phase 4)**: Top bar with breadcrumbs — enhances wayfinding
2. **Add US3 (Phase 5)**: Visual polish pass — elevates perceived quality
3. **Add US4 (Phase 6)**: Responsive behavior — improves usability on different viewports
4. **Phase 7**: Final verification and cleanup
