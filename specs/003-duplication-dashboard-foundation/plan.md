# Implementation Plan: Code Quality Dashboard Foundation

**Branch**: `003-duplication-dashboard-foundation` | **Date**: 2026-03-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `D:\Indexing\specs\003-duplication-dashboard-foundation\spec.md`

## Summary

Add project-scoped duplication analysis to the existing .NET 10 local search workspace by persisting repeatable structural and semantic finding snapshots in SQLite, exposing quality-analysis APIs, and extending the Blazor dashboard with hero metrics, a duplication breakdown chart, a findings table, and a side-by-side comparison modal.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, Microsoft.CodeAnalysis.CSharp, Microsoft.Data.Sqlite, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Chart.js  
**Storage**: SQLite file database under the application content root for indexed source metadata, embeddings, quality analysis runs, summary snapshots, and duplicate findings; source files and model assets remain on the local filesystem  
**Testing**: xUnit, FluentAssertions, ASP.NET Core integration tests, bUnit component tests, contract snapshot validation  
**Target Platform**: Windows development workstations and Windows Server with IIS for production hosting  
**Project Type**: Web application with a single ASP.NET Core host serving REST APIs and the Blazor UI over the existing Clean Architecture layers  
**Performance Goals**: Generate structural and semantic duplication results for a 500-file indexed project in under 2 minutes, load the quality dashboard summary in under 10 seconds, and open a duplicate comparison view in under 2 seconds for 95% of attempts  
**Constraints**: 100% offline operation, one selected project per analysis request, reuse indexed file and embedding data where available, thin controllers, application-layer validation, accessible loading/error/empty states, no duplicate double-counting across categories  
**Scale/Scope**: 10-20 active project keys on one machine, 500-5000 files per project, up to roughly 1 million lines of code per indexed project, and up to several hundred surfaced duplicate findings per project

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Review

- `PASS`: The repository already uses Clean Architecture, MediatR handlers, FluentValidation behaviors, and thin controllers, so the feature can fit the current layering model.
- `PASS`: The planned API surface can remain controller-thin by routing each quality action to one command/query handler with validation in the application layer.
- `PASS`: The design keeps structured logging, problem-details-style failures, and measurable performance goals explicit for all quality-analysis endpoints.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The constitution appendix mandates `.NET 8`, `React`, `SQL Server`, `JWT/RBAC`, and Docker-first deployment, but this repository and feature are explicitly `.NET 10`, Blazor-hosted, SQLite-backed, trusted-local, and IIS-oriented. The plan keeps the constitution's architectural principles while preserving the repository's actual product contract.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The constitution expects full i18n for user-facing text, while the existing local operator UI is English-only and has no translation layer. Phase 1 stays consistent with that bounded local-only baseline rather than introducing a parallel localization system inside this feature.

### Post-Phase 1 Design Re-check

- `PASS`: The design adds one quality-focused vertical slice across Domain, Application, Infrastructure, and WebApi without breaking one-way dependencies.
- `PASS`: Controllers remain orchestration-only, with each endpoint delegating to one command/query handler and keeping all validation in FluentValidation.
- `PASS`: Persisted quality summaries and findings stay project-scoped in SQLite, which preserves offline operation and aligns with the existing storage model.
- `PASS`: The dashboard additions fit the existing Blazor component model and preserve accessible loading, empty, and error states.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The same stack and localization exceptions remain necessary and bounded to the existing repository/runtime contract.

## Project Structure

### Documentation (this feature)

```text
specs/003-duplication-dashboard-foundation/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- code-quality.openapi.yaml
`-- tasks.md
```

### Source Code (repository root)

```text
src/
|-- SemanticSearch.Domain/
|   |-- Entities/
|   |-- Interfaces/
|   `-- ValueObjects/
|-- SemanticSearch.Application/
|   |-- Common/
|   |-- Files/
|   |-- Indexing/
|   |-- Projects/
|   |-- Quality/                 # planned
|   |-- Search/
|   `-- Status/
|-- SemanticSearch.Infrastructure/
|   |-- DependencyInjection/
|   |-- Embedding/
|   |-- FileSystem/
|   |-- Indexing/
|   |-- Quality/                 # planned
|   |-- ProjectTree/
|   |-- Search/
|   `-- VectorStore/
`-- SemanticSearch.WebApi/
    |-- Components/
    |   |-- Dashboard/
    |   `-- Quality/             # planned
    |-- Contracts/
    |   |-- Projects/
    |   `-- Quality/             # planned
    |-- Controllers/
    |-- Middleware/
    |-- Services/
    `-- wwwroot/

tests/
|-- SemanticSearch.Application.Tests/       # planned
|-- SemanticSearch.Infrastructure.Tests/    # planned
|-- SemanticSearch.WebApi.ComponentTests/   # planned
`-- SemanticSearch.WebApi.IntegrationTests/ # planned
```

**Structure Decision**: Preserve the current four-project Clean Architecture solution and add a focused `Quality` slice to the existing application, infrastructure, contracts, and Blazor component areas. Reuse `SemanticSearch.WebApi` as the single presentation host instead of introducing a second frontend runtime or a second deployment unit.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `.NET 10` instead of constitution `.NET 8` | The repository already targets `net10.0` and the feature request explicitly assumes `.NET 10` | Downgrading the solution adds churn without product value and conflicts with the current codebase |
| `Blazor Web App` instead of `React 18 + TypeScript + Vite` | One ASP.NET Core host keeps local deployment and IIS hosting simple | A separate SPA adds another toolchain, build pipeline, and deployment artifact |
| `SQLite` instead of `SQL Server` | The feature must remain portable, offline, and file-based on the local machine | SQL Server adds operational overhead and breaks the local-file storage goal |
| `No JWT/RBAC in initial scope` | The feature is bounded to trusted local environments and existing workspace behavior | Adding identity flows expands scope beyond the Phase 1 user stories |
| `English-only operator text for this phase` | The existing UI has no translation infrastructure and the feature extends that same local-only operator surface | Introducing full localization only for this slice would create an inconsistent one-off subsystem |
