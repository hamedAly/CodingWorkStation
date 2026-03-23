# Quickstart: Visual Architecture & Impact Analysis

**Feature**: 006-visual-architecture-analysis  
**Date**: 2026-03-18

## Prerequisites

- .NET 10 SDK installed
- Existing SemanticSearch solution builds and runs
- At least one project indexed (via existing indexing workflow)
- Quality analysis run completed (for heatmap visualization)

## New Dependencies

### NuGet Packages

No new NuGet packages required. The existing `Microsoft.CodeAnalysis.CSharp 4.11.0` in the Infrastructure project provides all necessary Roslyn APIs.

### Frontend Libraries (local copies under wwwroot/lib/)

| Library | Version | File | Purpose |
|---------|---------|------|---------|
| vis-network | latest stable | `wwwroot/lib/vis-network/vis-network.min.js` | Interactive dependency graph |
| Mermaid.js | latest stable | `wwwroot/lib/mermaid/mermaid.min.js` | ER diagram rendering |
| chartjs-chart-treemap | latest stable | `wwwroot/lib/chartjs-chart-treemap/chartjs-chart-treemap.min.js` | Heatmap treemap chart type |

Download via CDN and place in `src/SemanticSearch.WebApi/wwwroot/lib/`.

## Implementation Order

### Step 1: Domain Entities & Enums

Add to `src/SemanticSearch.Domain/`:

```
Entities/
├── DependencyAnalysisRun.cs
├── DependencyNode.cs
└── DependencyEdge.cs

ValueObjects/
├── DependencyAnalysisStatus.cs    (enum: Queued, Running, Completed, Failed)
├── DependencyNodeKind.cs          (enum: Class, Method)
└── DependencyRelationshipType.cs  (enum: Invocation, Inheritance, TypeReference, Construction)
```

### Step 2: Domain Interfaces

Add to `src/SemanticSearch.Domain/Interfaces/`:

```csharp
public interface IDependencyRepository
{
    Task<DependencyAnalysisRun?> GetLatestRunAsync(string projectKey, CancellationToken ct);
    Task<IReadOnlyList<DependencyNode>> ListNodesAsync(string projectKey, CancellationToken ct);
    Task<IReadOnlyList<DependencyEdge>> ListEdgesAsync(string projectKey, CancellationToken ct);
    Task ReplaceDependencyGraphAsync(
        DependencyAnalysisRun run,
        IReadOnlyList<DependencyNode> nodes,
        IReadOnlyList<DependencyEdge> edges,
        CancellationToken ct);
}
```

### Step 3: SQLite Schema & Repository

Extend `SqliteSchemaInitializer.Schema` with 3 new CREATE TABLE statements (see data-model.md).

Implement `IDependencyRepository` in `SqliteVectorStore` following the existing `IQualityRepository` pattern.

### Step 4: Infrastructure Services

Add to `src/SemanticSearch.Infrastructure/Architecture/`:

```
RoslynDependencyExtractor.cs    — IDependencyExtractor implementation
SqliteErDiagramGenerator.cs     — IErDiagramGenerator implementation
HeatmapDataBuilder.cs           — IHeatmapDataBuilder implementation
```

**RoslynDependencyExtractor**: Two-pass syntax analysis:
1. Build symbol table: parse all files, extract `ClassDeclarationSyntax` and `MethodDeclarationSyntax` nodes
2. Extract edges: walk method bodies for `InvocationExpressionSyntax`, `MemberAccessExpressionSyntax`, `ObjectCreationExpressionSyntax`, `BaseListSyntax`

**SqliteErDiagramGenerator**: Execute `PRAGMA table_list`, `PRAGMA table_info(T)`, `PRAGMA foreign_key_list(T)` for each table. Build Mermaid `erDiagram` string.

**HeatmapDataBuilder**: Query `IndexedFiles` for line counts, aggregate `DuplicationFindings` + `CodeRegions` per file path.

### Step 5: Application Layer (MediatR Handlers)

Add to `src/SemanticSearch.Application/Architecture/`:

```
Commands/
├── RunDependencyAnalysisCommand.cs
└── RunDependencyAnalysisCommandHandler.cs

Queries/
├── GetDependencyGraphQuery.cs
├── GetDependencyGraphQueryHandler.cs
├── GetFileHeatmapQuery.cs
├── GetFileHeatmapQueryHandler.cs
├── GetErDiagramQuery.cs
└── GetErDiagramQueryHandler.cs

Validators/
├── RunDependencyAnalysisCommandValidator.cs
├── GetDependencyGraphQueryValidator.cs
├── GetFileHeatmapQueryValidator.cs
└── GetErDiagramQueryValidator.cs
```

### Step 6: API Contracts & Controller

Add to `src/SemanticSearch.WebApi/Contracts/Architecture/`:

```
DependencyGraphResponse.cs
DependencyNodeResponse.cs
DependencyEdgeResponse.cs
DependencyAnalysisRunResponse.cs
FileHeatmapResponse.cs
FileHeatmapEntryResponse.cs
ErDiagramResponse.cs
```

Add `src/SemanticSearch.WebApi/Controllers/ArchitectureController.cs` — thin controller with MediatR Send.

### Step 7: Frontend Libraries

1. Download Vis.js Network, Mermaid.js, and chartjs-chart-treemap to `wwwroot/lib/`
2. Add `<script>` references in `App.razor` (lazy-load or conditional)
3. Create `wwwroot/js/architecture.js` with JS interop functions:
   - `architectureDashboard.renderDependencyGraph(containerId, nodes, edges)`
   - `architectureDashboard.renderErDiagram(containerId, mermaidMarkup)`
   - `architectureDashboard.renderHeatmap(canvasId, entries)`

### Step 8: Blazor Components

Add to `src/SemanticSearch.WebApi/Components/Architecture/`:

```
DependencyGraphView.razor       — Vis.js network rendering
FileHeatmapView.razor           — Chart.js treemap rendering
ErDiagramView.razor             — Mermaid.js ER diagram rendering
```

### Step 9: Dashboard Integration

Extend the existing `QualityDashboard.razor` (or create a parent `ArchitectureDashboard.razor`) to include tab navigation between:
- Summary (existing)
- Findings (existing)
- Dependency Graph (new)
- Heatmap (new)
- Data Model (new)

### Step 10: DI Registration

Register new services in `InfrastructureServiceRegistration.cs`:

```csharp
services.AddSingleton<IDependencyExtractor, RoslynDependencyExtractor>();
services.AddSingleton<IErDiagramGenerator, SqliteErDiagramGenerator>();
services.AddSingleton<IHeatmapDataBuilder, HeatmapDataBuilder>();
services.AddSingleton<IDependencyRepository>(sp => sp.GetRequiredService<SqliteVectorStore>());
```

## Verification

1. **Dependency Graph**: Index a sample project → POST to run analysis → GET graph → verify nodes and edges in browser
2. **Heatmap**: Run quality analysis on a project → GET heatmap → verify treemap renders with correct colors
3. **ER Diagram**: GET /api/architecture/er-diagram → verify Mermaid markup contains all 8 existing SQLite tables with correct FK relationships
