# Component Contracts: Enterprise App Shell

**Feature**: `005-enterprise-app-shell`  
**Date**: 2026-03-17

> This feature adds no API endpoints. The contracts below define the Blazor component interfaces (parameters, events, and render contracts) that form the public surface of the shell components.

---

## MainLayout.razor

**Type**: Layout Component (inherits `LayoutComponentBase`)  
**Renders**: Shell grid containing sidebar, top bar, and routed content.

### Internal State

| Field | Type | Default | Description |
|-------|------|---------|-------------|
| `_collapsed` | `bool` | `false` | Sidebar collapse state |

### Rendered Structure

```
┌─────────────────────────────────────────────────────┐
│ TopBar                                              │
├──────────┬──────────────────────────────────────────┤
│          │                                          │
│ NavMenu  │  @Body (routed page content)             │
│ (sidebar)│                                          │
│          │                                          │
│          │                                          │
└──────────┴──────────────────────────────────────────┘
```

### CSS Classes (Tailwind)

- Expanded sidebar: `w-64`
- Collapsed sidebar: `w-16`
- Main content background: `bg-slate-50`
- Transition: `transition-all duration-200`

---

## NavMenu.razor

**Type**: Regular Component  
**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Collapsed` | `bool` | Yes | Whether the sidebar is in collapsed (icon-only) mode |
| `OnToggleCollapse` | `EventCallback` | Yes | Event fired when the user clicks the collapse toggle |

### Render Contract

When `Collapsed = false` (expanded):
- Branding area shows app name ("Developer Command Center") and subtitle ("Local Workspace")
- Each phase group renders a heading with phase name and status badge
- Each navigation item renders icon + label
- Active items render as `<NavLink>` with `href`
- Coming Soon items render as `<span>` with badge and `cursor-not-allowed`
- A collapse toggle button is visible at the bottom or top of the sidebar

When `Collapsed = true`:
- Branding area shows only an abbreviated logo/icon
- Phase group headings are hidden
- Each navigation item renders icon only
- Hovering over an item shows a tooltip with the full label
- Coming Soon tooltip includes " (Coming Soon)" suffix
- A toggle button to expand is visible

---

## TopBar.razor

**Type**: Regular Component  
**Parameters**:

| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `Collapsed` | `bool` | Yes | Sidebar state (affects left padding/margin) |

### Render Contract

Left section:
- Breadcrumb trail: `Home > [Section] > [Page]`
- Each segment except the last is a clickable `<a>`
- Segments separated by chevron-right SVG icons
- Last segment is plain text (current page)

Center section:
- Command palette search input placeholder
- Shows keyboard shortcut badge: `Ctrl+K`
- Input is `readonly` / non-functional (placeholder only)

Right section:
- Profile avatar placeholder (circle with user icon)
- On hover: subtle ring highlight

---

## Icons.cs (Static Helper)

**Type**: Static class  
**Namespace**: `SemanticSearch.WebApi.Components.Layout`

Provides `MarkupString` constants for all icons used in the shell. Each icon is a 24x24 Heroicon outline SVG with `stroke="currentColor"` for color inheritance.

### Public Members

| Constant | Heroicon | Usage |
|----------|----------|-------|
| `Dashboard` | `squares-2x2` | Phase 1 nav item |
| `Quality` | `shield-check` | Phase 1 nav item |
| `Indexing` | `arrow-path` | Phase 1 nav item |
| `Search` | `magnifying-glass` | Phase 1 nav item + command palette |
| `Explorer` | `folder-open` | Phase 1 nav item |
| `AiAssistant` | `cpu-chip` | Phase 2 nav item |
| `Architecture` | `cube-transparent` | Phase 3 nav item |
| `Dependency` | `arrows-right-left` | Phase 3 nav item |
| `Automation` | `cog-6-tooth` | Phase 4 nav item |
| `Pipeline` | `play-circle` | Phase 4 nav item |
| `Guardrails` | `shield-exclamation` | Phase 5 nav item |
| `Safety` | `check-badge` | Phase 5 nav item |
| `ChevronRight` | `chevron-right` | Breadcrumb separator |
| `Bars3` | `bars-3` | Sidebar expand toggle |
| `XMark` | `x-mark` | Sidebar collapse toggle |
| `UserCircle` | `user-circle` | Profile placeholder |

---

## BreadcrumbMap.cs (Static Helper)

**Type**: Static class  
**Namespace**: `SemanticSearch.WebApi.Components.Layout`

### Public Methods

| Method | Signature | Description |
|--------|-----------|-------------|
| `GetBreadcrumbs` | `static IReadOnlyList<BreadcrumbSegment> GetBreadcrumbs(string path)` | Returns ordered breadcrumb segments for the given route path |

### Behavior

- Input: absolute path (e.g., `"/quality"`)
- Output: list of `BreadcrumbSegment` records
- Root path `"/"` returns `[("Home", null, true)]`
- Known routes return proper labels from internal dictionary
- Unknown segments fall back to title-cased segment name

---

## Navigation Data Records

### NavigationItem

```csharp
public record NavigationItem(
    string Label,
    MarkupString Icon,
    string? Route,
    bool IsActive);
```

### PhaseGroup

```csharp
public record PhaseGroup(
    string Name,
    int PhaseNumber,
    PhaseStatus Status,
    IReadOnlyList<NavigationItem> Items);
```

### PhaseStatus

```csharp
public enum PhaseStatus { Active, ComingSoon }
```

### BreadcrumbSegment

```csharp
public record BreadcrumbSegment(
    string Label,
    string? Href,
    bool IsCurrent);
```
