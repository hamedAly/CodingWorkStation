# Data Model: Premium Enterprise App Shell & UI Architecture

**Feature**: `005-enterprise-app-shell`  
**Date**: 2026-03-17  
**Status**: Complete

## Overview

This feature is UI-only — it has no database tables or persistent storage. The data model defines the in-memory types used by the shell's Blazor components to drive sidebar navigation, breadcrumb trails, and sidebar state.

---

## Entities

### NavigationItem

Represents a single link in the sidebar navigation.

| Field | Type | Description |
|-------|------|-------------|
| Label | `string` | Display text for the navigation item (e.g., "Dashboard") |
| Icon | `MarkupString` | Inline SVG content from the `Icons` static class |
| Route | `string?` | URL path for active items (e.g., `"/quality"`). `null` for Coming Soon items. |
| IsActive | `bool` | Whether this item links to an implemented page (`true`) or is a Coming Soon placeholder (`false`) |

**Validation rules**:
- `Label` must be non-empty
- `Icon` must contain valid SVG markup
- `Route` must be non-null when `IsActive` is `true`
- `Route` must be `null` when `IsActive` is `false`

**Implementation note**: Defined as a C# `record` for immutability and value equality.

---

### PhaseGroup

A logical grouping of navigation items under a phase heading.

| Field | Type | Description |
|-------|------|-------------|
| Name | `string` | Phase heading label (e.g., "Code Quality") |
| PhaseNumber | `int` | Phase number (1–5) for ordering |
| Status | `PhaseStatus` | Whether the phase is Active or ComingSoon |
| Items | `IReadOnlyList<NavigationItem>` | Ordered list of navigation items in this phase |

**Relationships**:
- A `PhaseGroup` contains 1..N `NavigationItem` instances
- All items within a Coming Soon phase must have `IsActive = false`
- All items within an Active phase must have `IsActive = true`

---

### PhaseStatus (Enum)

| Value | Description |
|-------|-------------|
| `Active` | Phase is fully implemented; items are navigable |
| `ComingSoon` | Phase is planned; items show a badge and are non-navigable |

---

### BreadcrumbSegment

Represents one step in the breadcrumb trail.

| Field | Type | Description |
|-------|------|-------------|
| Label | `string` | Display text (e.g., "Quality") |
| Href | `string?` | Navigation URL. `null` for the last (current) segment. |
| IsCurrent | `bool` | Whether this is the final segment (rendered as text, not a link) |

**Validation rules**:
- `Label` must be non-empty
- The last segment in any breadcrumb trail must have `IsCurrent = true` and `Href = null`
- The first segment must always be "Home" with `Href = "/"` (unless it's the only segment)

---

### SidebarState

The collapse/expand state of the sidebar. Not a standalone entity — it's a `bool` field in the `MainLayout` component.

| Field | Type | Description |
|-------|------|-------------|
| IsCollapsed | `bool` | `true` = icon-only rail; `false` = full expanded sidebar |

**State transitions**:
- Default: `false` (expanded)
- Toggle: `false` → `true` or `true` → `false` on user click
- Persist: Survives across page navigations within the same Blazor circuit (browser session)
- Reset: Returns to `false` on full page reload (F5)

---

## Static Data: Navigation Configuration

The full navigation tree is defined as static data in the `NavMenu` code-behind:

```
PhaseGroup("Code Quality", 1, Active)
├── NavigationItem("Dashboard", Icons.Dashboard, "/", true)
├── NavigationItem("Quality Analysis", Icons.Quality, "/quality", true)
├── NavigationItem("Indexing", Icons.Indexing, "/indexing", true)
├── NavigationItem("Search", Icons.Search, "/search", true)
└── NavigationItem("Explorer", Icons.Explorer, "/explorer", true)

PhaseGroup("AI Tech Lead", 2, Active)
└── NavigationItem("AI Assistant", Icons.AiAssistant, "/assistant", true)

PhaseGroup("Visual Architecture", 3, ComingSoon)
├── NavigationItem("Architecture Map", Icons.Architecture, null, false)
└── NavigationItem("Dependency Graph", Icons.Dependency, null, false)

PhaseGroup("TFS & Automation", 4, ComingSoon)
├── NavigationItem("Automation Hub", Icons.Automation, null, false)
└── NavigationItem("Pipeline Status", Icons.Pipeline, null, false)

PhaseGroup("Guardrails & Safety", 5, ComingSoon)
├── NavigationItem("Code Guardrails", Icons.Guardrails, null, false)
└── NavigationItem("Safety Dashboard", Icons.Safety, null, false)
```

## Static Data: Breadcrumb Map

Route-to-label mapping:

| Route | Label |
|-------|-------|
| `/` | Home |
| `/dashboard` | Dashboard |
| `/quality` | Quality Analysis |
| `/indexing` | Indexing |
| `/search` | Search |
| `/explorer` | Explorer |
| `/assistant` | AI Assistant |

---

## Component Hierarchy

```
MainLayout.razor
├── NavMenu.razor (sidebar)
│   ├── Branding area (app name + subtitle)
│   ├── Collapse toggle button
│   └── For each PhaseGroup:
│       ├── Phase heading + status badge
│       └── For each NavigationItem:
│           ├── Icon (MarkupString)
│           ├── Label (hidden when collapsed)
│           ├── Tooltip (visible when collapsed, on hover)
│           └── "Coming Soon" badge (when !IsActive)
├── TopBar.razor (header)
│   ├── Breadcrumb trail (from BreadcrumbSegment[])
│   ├── Command palette placeholder (search input)
│   └── Profile/settings placeholder (avatar icon)
└── @Body (main content area)
```
