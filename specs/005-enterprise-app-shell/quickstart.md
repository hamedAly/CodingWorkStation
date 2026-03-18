# Quickstart: Enterprise App Shell

**Feature**: `005-enterprise-app-shell`  
**Date**: 2026-03-17

## Prerequisites

- .NET 10 SDK installed
- Tailwind CSS v4 standalone binary (see setup below)
- Existing project builds and runs (`dotnet run` from `src/SemanticSearch.WebApi/`)

## One-Time Setup: Tailwind CSS Standalone CLI

1. Download the Tailwind CSS v4 standalone binary for Windows:
   ```powershell
   # From repository root
   New-Item -ItemType Directory -Force -Path tools
   Invoke-WebRequest -Uri "https://github.com/tailwindlabs/tailwindcss/releases/latest/download/tailwindcss-windows-x64.exe" -OutFile "tools/tailwindcss.exe"
   ```

2. Create the Tailwind input CSS file at `src/SemanticSearch.WebApi/Styles/shell.input.css`:
   ```css
   @layer theme, base, components, utilities;
   @import "tailwindcss/theme.css" layer(theme);
   @import "tailwindcss/utilities.css" layer(utilities);
   /* Preflight intentionally omitted to preserve existing app.css */
   ```

3. Add the MSBuild target to `src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj`:
   ```xml
   <Target Name="TailwindBuild" BeforeTargets="Build" Condition="Exists('..\..\tools\tailwindcss.exe')">
     <Exec Command="..\..\tools\tailwindcss.exe -i Styles\shell.input.css -o wwwroot\css\shell.css --minify"
           WorkingDirectory="$(MSBuildProjectDirectory)" />
   </Target>
   ```

4. Reference `shell.css` in `Components/App.razor`:
   ```html
   <link rel="stylesheet" href="css/shell.css" />
   <link rel="stylesheet" href="css/app.css" />
   ```

## Build & Run

```powershell
# From repository root
cd src/SemanticSearch.WebApi
dotnet run
```

The app launches on `https://localhost:5001` (or the configured port). The new shell should be immediately visible.

## Dev Mode (CSS Hot Reload)

Run the Tailwind watcher in a separate terminal for live CSS updates:
```powershell
# From repository root
.\tools\tailwindcss.exe -i src\SemanticSearch.WebApi\Styles\shell.input.css -o src\SemanticSearch.WebApi\wwwroot\css\shell.css --watch
```

Then run the Blazor app as usual in another terminal:
```powershell
cd src/SemanticSearch.WebApi
dotnet watch run
```

## Verify Success

After launching the app, verify:

1. **Sidebar** — Five phase groups visible with headings: "Code Quality", "AI Tech Lead", "Visual Architecture", "TFS & Automation", "Guardrails & Safety"
2. **Active items** — Dashboard, Quality Analysis, Indexing, Search, Explorer, AI Assistant all navigate correctly
3. **Coming Soon badges** — Architecture Map, Dependency Graph, Automation Hub, Pipeline Status, Code Guardrails, Safety Dashboard all show "Coming Soon" badges and do not navigate
4. **Icons** — Each navigation item shows an SVG icon
5. **Collapse** — Click the toggle button to collapse sidebar to icon-only rail; navigate between pages; sidebar stays collapsed
6. **Top bar** — Breadcrumbs show correct hierarchy; command palette placeholder visible with "Ctrl+K" hint; profile icon visible on the right
7. **Styling** — Slate/gray backgrounds, teal accents, soft shadows, smooth hover transitions on all interactive elements

## File Inventory

| File | Action | Description |
|------|--------|-------------|
| `tools/tailwindcss.exe` | New | Tailwind CSS standalone binary |
| `src/.../Styles/shell.input.css` | New | Tailwind CSS input configuration |
| `src/.../wwwroot/css/shell.css` | Generated | Tailwind CSS output (build artifact) |
| `src/.../wwwroot/css/app.css` | Unchanged | Existing custom CSS |
| `src/.../Components/App.razor` | Modified | Add `shell.css` link |
| `src/.../Components/Layout/MainLayout.razor` | Rewritten | New shell grid with sidebar + topbar + content |
| `src/.../Components/Layout/NavMenu.razor` | Rewritten | Phase-grouped sidebar with icons, badges, collapse |
| `src/.../Components/Layout/NavMenu.razor.cs` | New | Code-behind with navigation data and collapse handler |
| `src/.../Components/Layout/TopBar.razor` | New | Breadcrumbs, command palette placeholder, profile |
| `src/.../Components/Layout/TopBar.razor.cs` | New | Breadcrumb generation from route |
| `src/.../Components/Layout/Icons.cs` | New | Static MarkupString constants for Heroicon SVGs |
| `src/.../Components/Layout/BreadcrumbMap.cs` | New | Route-to-label mapping for breadcrumbs |
| `src/.../Models/Navigation/NavigationItem.cs` | New | Navigation item record |
| `src/.../Models/Navigation/PhaseGroup.cs` | New | Phase group record with status |
| `src/.../Models/Navigation/PhaseStatus.cs` | New | Active/ComingSoon enum |
| `src/.../Models/Navigation/BreadcrumbSegment.cs` | New | Breadcrumb segment record |
| `src/.../SemanticSearch.WebApi.csproj` | Modified | Add TailwindBuild MSBuild target |
