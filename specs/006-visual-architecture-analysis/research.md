# Research: Visual Architecture & Impact Analysis

**Feature**: 006-visual-architecture-analysis  
**Date**: 2026-03-18  
**Status**: Complete

## R1: Roslyn Approach for Dependency Graph Extraction

**Question**: How should we extract class/method call relationships — full CSharpCompilation with SemanticModel, syntax-only heuristics, or partial compilation?

**Decision**: Syntax-only heuristic approach

**Rationale**:
- The existing codebase uses `CSharpSyntaxTree.ParseText()` exclusively (see `RoslynStructuralCloneAnalyzer`). No `CSharpCompilation` or `SemanticModel` is used anywhere.
- Full compilation requires resolving all NuGet package references and framework assemblies for the target project, adding significant complexity and build-time dependencies.
- Syntax-only analysis can extract: class declarations (`ClassDeclarationSyntax`), method declarations (`MethodDeclarationSyntax`), invocation expressions (`InvocationExpressionSyntax`, `MemberAccessExpressionSyntax`), type references in inheritance (`BaseListSyntax`), constructor calls (`ObjectCreationExpressionSyntax`), and field/property/parameter type references.
- Within a single project boundary, syntax-only analysis captures the vast majority of intra-project relationships by matching identifier names from `using` directives and type syntax.

**Alternatives considered**:
- **CSharpCompilation + SemanticModel**: Most accurate symbol resolution but requires resolving all package references — impractical for arbitrary user projects without their `.csproj` build graph.
- **MSBuildWorkspace**: Would need `Microsoft.Build.Locator` and a compatible MSBuild installation — heavy external dependency.

**Extraction strategy**:
1. Parse all `.cs` files in the project into syntax trees
2. First pass: Build a symbol table of all declared classes/methods (name → file path, line, namespace)
3. Second pass: Walk method bodies to find invocation expressions, member accesses, object creations, base types
4. Match referenced identifiers against the symbol table to create edges
5. Unresolved references (external types) are ignored — graph covers intra-project relationships only

---

## R2: ER Diagram Data Source (Resolves FR-009)

**Question**: The spec asks for an "EF Core DbContext" scan, but the project uses raw SQLite with `Microsoft.Data.Sqlite`. What should generate the ER diagram?

**Decision**: Dual-source approach — SQLite schema via PRAGMA queries plus domain entity class reflection

**Rationale**:
- The project defines its schema in `SqliteSchemaInitializer.Schema` and uses raw ADO.NET-style queries. There is no `DbContext`.
- `PRAGMA table_info(table_name)` returns column name, type, nullable, default, PK flag for each table.
- `PRAGMA foreign_key_list(table_name)` returns FK relationships (from table, to table, from column, to column).
- Domain entity classes under `SemanticSearch.Domain/Entities/` provide meaningful C# property names and types.
- Combining both sources gives us: actual DB schema structure (tables, columns, FKs) enriched with C# type information.

**Alternatives considered**:
- **EF Core DbContext introduction**: Would require adding `Microsoft.EntityFrameworkCore.Sqlite` as a dependency and mapping all existing entities — scope creep for a visualization feature.
- **Domain class reflection only**: Would miss actual DB relationships (FK constraints) and non-entity tables.
- **SQLite schema only**: Would lack C# type names and produce SQL-oriented (TEXT/INTEGER) column types instead of domain types.

**Output format**: Mermaid.js `erDiagram` syntax string, generated server-side and returned via API.

---

## R3: Interactive Graph Library — Vis.js vs D3.js

**Question**: Which library should render the interactive dependency graph?

**Decision**: Vis.js Network (vis-network standalone package)

**Rationale**:
- **Vis.js Network** provides a purpose-built graph/network visualization with built-in: physics-based force-directed layout, click/hover/select events, node dragging, zoom/pan, clustering for large graphs, and configurable node/edge styling.
- D3.js is a general-purpose data visualization library — building an interactive graph requires significantly more code (force simulation, SVG rendering, event handling, zoom behavior all manual).
- Vis.js Network is approximately 300 KB minified (standalone). Loaded only on the Architecture view page, not globally.
- The Blazor JS interop pattern is simpler: pass nodes/edges JSON arrays → `new vis.Network(container, data, options)`.

**Alternatives considered**:
- **D3.js force-directed**: More control over rendering but 3-5x more JS code for equivalent functionality. Better suited for custom/artistic visualizations, not standard graph exploration.
- **Cytoscape.js**: Strong graph library but heavier (500+ KB) and higher learning curve than Vis.js for this use case.

**Integration**:
- Download `vis-network.min.js` to `wwwroot/lib/vis-network/`
- Expose `architectureDashboard.renderDependencyGraph(containerId, nodes, edges, options)` via JS interop
- Blazor component calls `IJSRuntime.InvokeVoidAsync()` to initialize and update

---

## R4: Mermaid.js for ER Diagram Rendering

**Question**: How should the ER diagram be rendered in the Blazor dashboard?

**Decision**: Mermaid.js loaded on-demand with JS interop

**Rationale**:
- Mermaid.js natively supports `erDiagram` syntax with entities, attributes, and relationship cardinality (||--o{, }|--|{, etc.).
- The server generates the Mermaid markup string; the client renders it — clean separation.
- Mermaid.js is ~1.5 MB minified but can be loaded lazily only when the Data Model tab is selected.
- Mermaid supports SVG output, which provides natural zoom/pan via browser controls.

**Integration**:
- Download `mermaid.min.js` to `wwwroot/lib/mermaid/`
- Server returns Mermaid markup string via API
- JS interop: `mermaid.render('er-diagram', markup)` → inject SVG into container div
- Wrap SVG in a pannable/zoomable container (CSS `overflow: auto` + `transform: scale()`)

---

## R5: Chart.js Treemap Plugin for Heatmap

**Question**: How should the duplication heatmap be implemented?

**Decision**: `chartjs-chart-treemap` plugin for Chart.js

**Rationale**:
- Chart.js is already loaded globally (`lib/chart.js/chart.umd.js`) and the JS interop pattern is established in `QualityBreakdownChart.razor`.
- `chartjs-chart-treemap` adds a `treemap` chart type that integrates natively with Chart.js — no additional charting library needed.
- The plugin supports: custom rectangle sizing (by value), custom coloring (via `backgroundColor` callback), tooltips, and click events — all required for the heatmap spec.
- Plugin size is ~30 KB minified.

**Alternatives considered**:
- **D3.js treemap**: Would require a second charting library alongside Chart.js.
- **Standalone treemap libraries**: Fragmentation — better to extend existing Chart.js setup.

**Color mapping strategy**:
- Compute `duplicationDensity = (structuralCount + semanticCount) / totalLines` per file
- Map to HSL color: `hsl(120 - (density * 120), 70%, 50%)` — green (120°) for 0 density, red (0°) for max density
- Size each rectangle by `totalLines` (lines of code)

---

## R6: Dependency Graph Persistence Strategy

**Question**: Should the dependency graph be persisted or computed on-the-fly?

**Decision**: Persist in SQLite (new tables), following the existing quality snapshot pattern

**Rationale**:
- The quality dashboard already persists analysis results (`DuplicationFindings`, `CodeRegions`, `QualityAnalysisRuns`) and replaces them atomically on re-analysis.
- Dependency graphs for 200+ files will take seconds to compute; caching avoids re-analysis on every page load.
- The `ReplaceSnapshotAsync` transactional pattern in `SqliteVectorStore` provides a proven template.
- Following the existing pattern: `DependencyAnalysisRun` tracks metadata, `DependencyNodes` and `DependencyEdges` store the graph.

**New tables**:
- `DependencyAnalysisRuns` — mirrors `QualityAnalysisRuns` structure
- `DependencyNodes` — NodeId, ProjectKey, RunId, Name, Kind, Namespace, FilePath, StartLine
- `DependencyEdges` — EdgeId, ProjectKey, RunId, SourceNodeId, TargetNodeId, RelationshipType

---

## R7: Large Graph Handling

**Question**: How to handle projects with 500+ classes without degrading performance?

**Decision**: Server-side filtering with namespace grouping fallback

**Rationale**:
- Vis.js Network handles up to ~300 nodes smoothly with physics simulation.
- For larger graphs, the API supports optional `namespace` and `filePath` filter parameters to scope the returned subgraph.
- The Blazor component defaults to namespace-level view (classes grouped by namespace as cluster nodes). Users expand clusters to see individual classes.
- This approach is simpler than pagination for graph data and maintains visual coherence.

**Thresholds**:
- ≤ 300 nodes: Full graph rendered with physics
- \> 300 nodes: Automatic namespace-level grouping; user can expand individual namespaces
