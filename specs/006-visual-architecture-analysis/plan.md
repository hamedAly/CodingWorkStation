# Implementation Plan: Visual Architecture & Impact Analysis

**Branch**: `006-visual-architecture-analysis` | **Date**: 2026-03-18 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/006-visual-architecture-analysis/spec.md`

## Summary

Build backend services and frontend components to visualize code architecture and project health. A Roslyn-based dependency extractor produces a directed graph of class/method relationships (nodes + edges), delivered via API and rendered as an interactive network using Vis.js. A treemap heatmap built with the Chart.js Treemap plugin surfaces per-file duplication density using existing quality analysis data. An ER diagram generator introspects the SQLite schema (via PRAGMA) and domain entity classes to produce a Mermaid.js-compatible definition rendered in the dashboard.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core (Blazor Interactive Server), MediatR 14.1.0, FluentValidation 12.1.1, Microsoft.CodeAnalysis.CSharp 4.11.0, Microsoft.Data.Sqlite, Vis.js (new — interactive graph), Mermaid.js (new — ER diagram), chartjs-chart-treemap (new — heatmap plugin)  
**Storage**: SQLite file database (existing) — new tables for dependency graph persistence  
**Testing**: No test infrastructure currently exists (pre-existing gap)  
**Target Platform**: Windows/Linux server, Blazor Interactive Server (browser rendering)  
**Project Type**: Web application (Blazor Server + REST API)  
**Performance Goals**: Dependency graph analysis + render ≤ 15s for ≤ 200 files; Heatmap render ≤ 5s for ≤ 500 files  
**Constraints**: Interactive graph must remain responsive with ≤ 300 nodes; treemap tooltip latency < 100ms  
**Scale/Scope**: Projects up to 500 C# source files, up to 300 classes, existing codebase with 4 solution projects

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Code Quality (40-line methods, 400-line classes) | **PASS** | All new services will follow limits |
| II. Code Organization (feature-based) | **PASS** | New code organized under existing feature folders (Quality/, Architecture/) |
| III. Comments (WHY not WHAT) | **PASS** | Public APIs documented; implementation self-documenting |
| IV. DRY (Rule of Three) | **PASS** | Reuse existing MediatR/FluentValidation/SqliteVectorStore patterns |
| V. Type Safety | **PASS** | Strongly-typed DTOs, domain entities, no `object`/`dynamic` |
| VI. Testing (92%+ coverage) | **PRE-EXISTING GAP** | No test projects exist in workspace. This feature does not introduce the gap. |
| VII. UX (loading/error/empty states) | **PASS** | FR-016 mandates empty states; loading indicators in Blazor components |
| VIII. Performance Budget | **PASS** | SC-001/SC-004 define budgets; server-side rendering avoids bundle concerns |
| IX. Git Hygiene | **PASS** | Feature branch created; conventional commits |
| X. Observability | **PASS** | Structured logging in analysis services |
| XI. Security | **PASS** | Input validation via FluentValidation; no external data ingestion |
| XII. Thin Controllers | **PASS** | Existing pattern: Controller → MediatR Send → Handler |
| XIII. FluentValidation Pipeline | **PASS** | Validators registered via assembly scanning |
| XIV. Separation of Concerns | **PASS** | Domain: entities. Application: handlers. Infrastructure: Roslyn/SQLite. WebApi: Blazor/controllers |

> **Gate result: PASS** — No new violations introduced. Testing gap (VI) is pre-existing.

## Project Structure

### Documentation (this feature)

```text
specs/006-visual-architecture-analysis/
├── plan.md              # This file
├── research.md          # Phase 0: technology decisions
├── data-model.md        # Phase 1: entity design
├── quickstart.md        # Phase 1: implementation guide
├── contracts/           # Phase 1: OpenAPI contract
│   └── architecture.openapi.yaml
└── tasks.md             # Phase 2 output (/speckit.tasks command)
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/
│   └── Entities/
│       ├── DependencyNode.cs          # NEW — graph node entity
│       ├── DependencyEdge.cs          # NEW — graph edge entity
│       └── DependencyAnalysisRun.cs   # NEW — analysis run tracking
│
├── SemanticSearch.Application/
│   └── Architecture/
│       ├── Commands/
│       │   └── RunDependencyAnalysisCommand.cs        # NEW
│       ├── Queries/
│       │   ├── GetDependencyGraphQuery.cs             # NEW
│       │   ├── GetFileHeatmapQuery.cs                 # NEW
│       │   └── GetErDiagramQuery.cs                   # NEW
│       └── Validators/
│           ├── RunDependencyAnalysisCommandValidator.cs
│           ├── GetDependencyGraphQueryValidator.cs
│           ├── GetFileHeatmapQueryValidator.cs
│           └── GetErDiagramQueryValidator.cs
│
├── SemanticSearch.Infrastructure/
│   └── Architecture/
│       ├── RoslynDependencyExtractor.cs               # NEW — Roslyn semantic analysis
│       ├── SqliteErDiagramGenerator.cs                # NEW — SQLite PRAGMA + reflection
│       └── HeatmapDataBuilder.cs                     # NEW — aggregates existing quality data
│
└── SemanticSearch.WebApi/
    ├── Controllers/
    │   └── ArchitectureController.cs                  # NEW — thin controller
    ├── Contracts/
    │   └── Architecture/
    │       ├── DependencyGraphResponse.cs             # NEW
    │       ├── FileHeatmapResponse.cs                 # NEW
    │       └── ErDiagramResponse.cs                   # NEW
    ├── Components/
    │   └── Architecture/
    │       ├── DependencyGraphView.razor              # NEW — Vis.js integration
    │       ├── FileHeatmapView.razor                  # NEW — Chart.js Treemap
    │       └── ErDiagramView.razor                    # NEW — Mermaid.js rendering
    └── wwwroot/
        ├── js/
        │   └── architecture.js                        # NEW — Vis.js, Mermaid.js, Treemap interop
        └── lib/
            ├── vis-network/                           # NEW — Vis.js Network library
            ├── mermaid/                               # NEW — Mermaid.js library
            └── chartjs-chart-treemap/                 # NEW — Chart.js Treemap plugin
```

**Structure Decision**: Extends the existing Clean Architecture layout. New `Architecture/` feature folders mirror the existing `Quality/` organization pattern. Frontend libraries added as local copies under `wwwroot/lib/` following the Chart.js precedent.

## Complexity Tracking

> No constitution violations to justify — all gates pass.
