# Research: Premium Enterprise App Shell & UI Architecture

**Feature**: `005-enterprise-app-shell`  
**Date**: 2026-03-17  
**Status**: Complete

## R-001: Tailwind CSS Integration Strategy for Blazor .NET 10

**Decision**: Use Tailwind CSS v4 standalone CLI binary with CSS-based configuration (no npm, no Node.js, no `tailwind.config.js`).

**Rationale**: Tailwind v4 replaced `tailwind.config.js` with CSS-based configuration. The standalone binary (`tailwindcss-windows-x64.exe`) can scan `.razor` files automatically — it treats all non-binary, non-gitignored files as plain text and extracts class tokens. This requires zero JavaScript toolchain setup. The binary is downloaded to a `tools/` folder and invoked via an MSBuild `<Target>` before the build step.

**Alternatives considered**:
- **npm + Tailwind**: Requires Node.js installation, `package.json`, and more tooling complexity. Rejected — overkill for a local dev tool with no other JS build pipeline.
- **Tailwind CDN**: Easy setup but ships the entire Tailwind CSS framework (~300KB), cannot be purged, and requires an internet connection. Rejected for production use.
- **CSS-only hand-rolled utilities**: Manual effort to replicate Tailwind's utility classes. Rejected — defeats the purpose of using Tailwind.

**Configuration approach**:
```css
/* src/SemanticSearch.WebApi/Styles/shell.input.css */
@layer theme, base, components, utilities;
@import "tailwindcss/theme.css" layer(theme);
@import "tailwindcss/utilities.css" layer(utilities);
/* Preflight omitted intentionally — see R-002 */
```

**Build integration** (MSBuild target in `.csproj`):
```xml
<Target Name="TailwindBuild" BeforeTargets="Build">
  <Exec Command="tools\tailwindcss.exe -i Styles\shell.input.css -o wwwroot\css\shell.css --minify"
        WorkingDirectory="$(MSBuildProjectDirectory)" />
</Target>
```

**Dev workflow**: Run `tools\tailwindcss.exe -i Styles\shell.input.css -o wwwroot\css\shell.css --watch` in a separate terminal during development.

---

## R-002: Coexistence with Existing Custom CSS

**Decision**: Output Tailwind to a separate `wwwroot/css/shell.css` file that coexists with the existing `wwwroot/css/app.css`. Disable Tailwind Preflight to prevent style conflicts.

**Rationale**: The existing `app.css` defines CSS custom properties (`--bg`, `--accent`, `--ink`, etc.) and class-based styles for all existing page components (cards, tables, status pills, progress bars, etc.). These must continue to work unchanged. By omitting Tailwind's Preflight reset layer, we avoid conflicts with existing heading sizes, margins, list styles, and box-sizing rules.

**Alternatives considered**:
- **Full migration to Tailwind**: Would require rewriting all existing component styles. Rejected — too large a scope for a shell redesign feature.
- **Single combined CSS file**: Would create ordering and specificity conflicts. Rejected.
- **Scoped CSS per component (`.razor.css`)**: Blazor scoped CSS is limited and doesn't support Tailwind utilities well. Rejected.

**Reference order in `App.razor`**:
```html
<link rel="stylesheet" href="css/shell.css" />
<link rel="stylesheet" href="css/app.css" />
```

The existing `app.css` loads second to ensure its rules take precedence for existing components. New shell components exclusively use Tailwind classes from `shell.css`.

---

## R-003: Breadcrumb Implementation Pattern

**Decision**: Static route-to-label dictionary mapping combined with `NavigationManager.Uri` path parsing. Implemented in a static helper class (`BreadcrumbMap`).

**Rationale**: The application has a known, finite set of routes (currently 6 active pages). A simple dictionary maps route paths to display labels (e.g., `"/quality"` → `"Quality"`). The current URL is parsed into segments, each mapped to its label. This approach is predictable, testable, and requires no external libraries.

**Alternatives considered**:
- **Automatic URI segment parsing**: Would produce raw path segments as labels (e.g., `"quality"` instead of `"Quality"`). Rejected — insufficient control over display names.
- **CascadingValue from layout**: Each page component would push breadcrumb data up to the layout. Rejected — couples every page component to the layout and requires parameter boilerplate on every page.
- **Third-party Blazor breadcrumb library**: Adds a dependency for a simple feature. Rejected — overkill for the scope.

**Implementation**: A `BreadcrumbMap` static class with a `GetBreadcrumbs(string path)` method that returns an ordered list of `(string Href, string Label)` tuples. The `TopBar.razor` component injects `NavigationManager`, subscribes to `LocationChanged`, and calls `BreadcrumbMap.GetBreadcrumbs(...)` on each navigation.

---

## R-004: Sidebar Collapse State Persistence

**Decision**: Simple `bool _collapsed` field in `MainLayout.razor`, passed to child components via `CascadingValue`. Circuit-scoped (survives page navigations, resets on full page reload).

**Rationale**: In Blazor Interactive Server mode, the SignalR circuit maintains component state across all page navigations within the session. A `bool` field in `MainLayout` persists naturally as long as the browser tab is open. For a single-user local dev tool, resetting to expanded on F5 is acceptable and provides a clean-start behavior.

**Alternatives considered**:
- **JavaScript `localStorage` via `IJSRuntime`**: Persists across page reloads but introduces async complexity and prerendering guards (`OnAfterRenderAsync` with `firstRender` checks). Rejected — unnecessary complexity for a dev tool.
- **`ProtectedBrowserStorage`**: Encrypted browser storage. Rejected — designed for sensitive data, adds async/prerender complexity.
- **Singleton service**: Would require DI registration and survive across circuits. Rejected — over-engineering.

**Implementation**: `MainLayout.razor` declares `bool _collapsed` with a `ToggleSidebar()` method. The collapsed state is passed down to `NavMenu` and `TopBar` via parameters or a wrapping `CascadingValue`.

---

## R-005: Heroicon SVG Integration

**Decision**: Static `MarkupString` constants in a centralized helper class (`Icons.cs`), one constant per icon.

**Rationale**: Heroicons are designed for inline SVG usage (the official site provides copy-paste SVG markup). A static class with `MarkupString` constants provides single-source-of-truth reuse across all shell components without the overhead of individual `.razor` component files. The SVGs use `stroke="currentColor"` / `fill="currentColor"`, meaning they automatically inherit the parent element's text color — this works seamlessly with both existing CSS custom properties and Tailwind's `text-*` utilities.

**Alternatives considered**:
- **Inline SVG directly in `.razor` markup**: Works but leads to verbose, hard-to-maintain markup when the same icon is used in multiple places. Rejected for reusability reasons.
- **Individual Blazor components per icon**: Creates ~15 additional `.razor` files. Rejected — over-engineering for a dev tool.
- **Icon font (Font Awesome, etc.)**: Adds an external dependency and doesn't integrate as cleanly with Tailwind sizing utilities. Rejected.

**Implementation**: A `Components/Layout/Icons.cs` static class with approximately 15 `MarkupString` constants (one per navigation item icon, plus breadcrumb chevron, collapse toggle, search, profile). Usage in Razor: `@Icons.Dashboard`.

**Icons needed** (Heroicon outline style, 24x24):
| Icon | Heroicon Name | Usage |
|------|---------------|-------|
| Dashboard | `squares-2x2` | Phase 1 — Dashboard |
| Quality | `shield-check` | Phase 1 — Quality Analysis |
| Indexing | `arrow-path` | Phase 1 — Indexing |
| Search | `magnifying-glass` | Phase 1 — Search |
| Explorer | `folder-open` | Phase 1 — Explorer |
| AI Assistant | `cpu-chip` | Phase 2 — AI Assistant |
| Architecture | `cube-transparent` | Phase 3 — Architecture Map |
| Dependency | `arrows-right-left` | Phase 3 — Dependency Graph |
| Automation | `cog-6-tooth` | Phase 4 — Automation Hub |
| Pipeline | `play-circle` | Phase 4 — Pipeline Status |
| Guardrails | `shield-exclamation` | Phase 5 — Code Guardrails |
| Safety | `check-badge` | Phase 5 — Safety Dashboard |
| Chevron Right | `chevron-right` | Breadcrumb separator |
| Sidebar Toggle | `bars-3` / `x-mark` | Collapse/expand toggle |
| Command Search | `magnifying-glass` | Top bar command palette |
| Profile | `user-circle` | Top bar profile placeholder |
