# Implementation Plan: Local AI Tech Lead Assistant

**Branch**: `004-local-ai-tech-lead` | **Date**: 2026-03-16 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `D:\Indexing\specs\004-local-ai-tech-lead\spec.md`

## Summary

Add a local offline AI recommendation slice to the existing quality dashboard by loading one GGUF model into the ASP.NET Core host at startup, exposing streaming quality-assistant endpoints over the current quality API surface, and rendering progressively formatted recommendations inside the dashboard hero and duplicate comparison modal without replacing existing quality evidence.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, LLamaSharp, LLamaSharp.Backend.Cpu, Markdig, existing quality dashboard contracts/components  
**Storage**: Existing SQLite quality data plus local filesystem model assets; no new persistent database tables are required for streaming assistant sessions  
**Testing**: xUnit, FluentAssertions, ASP.NET Core integration tests, bUnit component tests, streaming contract validation, markdown rendering snapshot tests  
**Target Platform**: Windows development workstations and Windows Server with IIS, running fully offline with CPU-backed local inference  
**Project Type**: Web application with one ASP.NET Core host serving REST APIs and the Blazor UI over existing Clean Architecture layers  
**Performance Goals**: First visible project-plan content within 5 seconds for 90% of requests, duplicate-fix response start within 15 seconds for 90% of requests, and zero repeated model-weight reloads between requests during one app run  
**Constraints**: 100% offline operation, load GGUF weights only once at startup, keep one active stream per assistant surface, preserve dashboard and modal context while streaming, thin controllers, application-layer validation, accessible loading/error/cancel states, bounded prompt size for duplicate code samples  
**Scale/Scope**: Single local operator, one selected project at a time, up to 5000 indexed files per project, and duplicate-fix prompts built from one selected finding containing two bounded code regions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Review

- `PASS`: The feature extends the existing `Quality` slice and can keep one-way dependencies across WebApi, Application, Infrastructure, and Domain-aligned read models.
- `PASS`: Controllers can remain thin by delegating readiness checks and streaming requests to one query/service path per endpoint, with validation still handled in the application layer.
- `PASS`: Offline execution, structured logging, and explicit unavailable/error states are compatible with the existing local-host architecture.
- `PASS`: The feature improves duplication remediation while preserving existing user flows in the quality dashboard and duplicate comparison modal.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The constitution appendix mandates `.NET 8`, `React`, `SQL Server`, `JWT/RBAC`, and Docker-first deployment, but this repository and feature remain explicitly `.NET 10`, Blazor-hosted, SQLite-backed, trusted-local, and IIS-oriented. The plan preserves the constitution's architectural rules while honoring the repository's actual runtime contract.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The constitution expects full i18n for user-facing text, while the current local operator UI is English-only and has no translation layer. This feature stays aligned with that existing bounded local-only baseline rather than introducing a one-off localization subsystem.

### Post-Phase 1 Design Re-check

- `PASS`: The design adds a focused quality-assistant slice without introducing cross-layer coupling or bypassing current infrastructure registration patterns.
- `PASS`: The planned streaming endpoints keep controllers orchestration-only and preserve FluentValidation at external boundaries.
- `PASS`: Model configuration and file paths stay local to the application content root, which preserves the offline requirement and matches the existing content-root-based options model.
- `PASS`: The UI design adds dedicated loading, error, cancellation, and partial-output states while preserving the existing dashboard hero and duplicate comparison interactions.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The same stack, auth, deployment, and localization exceptions remain necessary and bounded to the repository's established product contract.

## Project Structure

### Documentation (this feature)

```text
specs/004-local-ai-tech-lead/
|-- plan.md
|-- research.md
|-- data-model.md
|-- quickstart.md
|-- contracts/
|   `-- quality-ai.openapi.yaml
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
|   |-- Quality/
|   |   |-- Assistant/           # planned
|   |   |-- Commands/
|   |   |-- Models/
|   |   `-- Queries/
|   |-- Search/
|   `-- Status/
|-- SemanticSearch.Infrastructure/
|   |-- DependencyInjection/
|   |-- Quality/
|   |   `-- Assistant/           # planned
|   |-- Embedding/
|   `-- VectorStore/
`-- SemanticSearch.WebApi/
    |-- Components/
    |   |-- Quality/
    |   |   `-- Assistant/       # planned
    |   `-- Pages/
    |-- Contracts/
    |   `-- Quality/
    |-- Controllers/
    |-- Services/
    `-- wwwroot/

tests/
|-- SemanticSearch.Application.Tests/       # planned
|-- SemanticSearch.WebApi.ComponentTests/   # planned
`-- SemanticSearch.WebApi.IntegrationTests/ # planned
```

**Structure Decision**: Preserve the current four-project Clean Architecture solution and extend the existing `Quality` slice with assistant-specific commands, queries, infrastructure services, contracts, and Blazor components. Keep the current API-client pattern between Blazor and controllers instead of introducing a second frontend runtime or bypassing the existing host boundary.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `.NET 10` instead of constitution `.NET 8` | The repository already targets `net10.0` and the feature request assumes `.NET 10` | Downgrading the solution adds churn without product value and conflicts with the current codebase |
| `Blazor Web App` instead of `React 18 + TypeScript + Vite` | The current product already renders the dashboard from one ASP.NET Core host | A separate SPA adds an unnecessary runtime, toolchain, and deployment artifact |
| `SQLite + local files` instead of `SQL Server` | The assistant must remain portable and fully offline beside the existing workspace data | SQL Server adds operational overhead and breaks the local-file deployment goal |
| `No JWT/RBAC in scope` | The workspace remains bounded to trusted local environments and existing flows | Adding identity expands scope beyond the feature's recommendation workflow |
| `English-only operator text for this phase` | The existing UI has no translation infrastructure and this feature extends that same operator surface | Introducing full localization only here creates an inconsistent one-off subsystem |
