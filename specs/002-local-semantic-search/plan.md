# Implementation Plan: Local Semantic Search Workspace

**Branch**: `002-local-semantic-search` | **Date**: 2026-03-14 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/002-local-semantic-search/spec.md`

## Summary

Extend the existing .NET 10 Clean Architecture solution into a single IIS-hosted local search workspace that serves both REST APIs and a Blazor-based web UI. The design keeps indexing, search, and file-browsing fully offline by using an ONNX embedding model, a local SQLite-backed vector store, background indexing workers, and absolute content-root-based storage paths for all runtime assets.

## Technical Context

**Language/Version**: C# 13 on .NET 10  
**Primary Dependencies**: ASP.NET Core, Blazor Web App, MediatR, FluentValidation, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Microsoft.Data.Sqlite  
**Storage**: SQLite file database for projects/files/segments/indexing state, local model files on disk, local data/log directories under content root  
**Testing**: xUnit, FluentAssertions, ASP.NET Core integration tests, bUnit component tests, contract snapshot tests  
**Target Platform**: Windows development workstations and Windows Server with IIS for production hosting  
**Project Type**: Web application with a single ASP.NET Core host serving REST APIs and the Blazor UI over the existing Clean Architecture layers  
**Performance Goals**: Full indexing of a 500-file project in under 10 minutes, 95% of interactive searches in under 3 seconds, single-file refresh in under 1 minute for files up to 1 MB  
**Constraints**: 100% offline operation, absolute path resolution from host content root, no external paid services, per-project isolation, thin controllers, accessible loading/error states, background-safe IIS hosting  
**Scale/Scope**: 10-20 active project keys on a single machine, 500-5000 files per project, up to roughly 1 million lines of code per indexed project

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

### Pre-Phase 0 Gate Review

- `PASS`: The existing solution already aligns with Clean Architecture, CQRS-style application handlers, FluentValidation, and thin-controller expectations.
- `PASS`: The planned design keeps validation at external boundaries, preserves one-way layer dependencies, and keeps infrastructure concerns out of controllers.
- `PASS`: Structured logging, problem-details style error handling, and measurable latency/indexing goals will remain first-class concerns in the design.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The constitution's stack appendix requires .NET 8, React, SQL Server, JWT, and Docker. This feature explicitly requires .NET 10, a single-host local UI, a file-based local vector store, trusted-local deployment, and IIS hosting. The plan adopts .NET-native equivalents that satisfy the product contract without violating the core engineering principles.

### Post-Phase 1 Design Re-check

- `PASS`: The design keeps controllers thin by routing each external action to one command/query handler or one focused file-read/query flow.
- `PASS`: Validation remains in the application layer via FluentValidation, with consistent problem responses from the web layer.
- `PASS`: The source structure stays feature- and domain-oriented, with UI, contracts, and infrastructure additions fitting the existing repository instead of creating a parallel stack.
- `PASS WITH JUSTIFIED EXCEPTIONS`: The same stack deviations remain necessary and bounded:
  - `.NET 10` instead of `.NET 8` because the repository and feature requirement target `net10.0`.
  - `Blazor Web App` instead of `React/Vite/Tailwind` because the feature requires one IIS-hosted local application with minimal deployment friction.
  - `SQLite-backed local vector store` instead of `SQL Server` because the feature requires a portable file-based offline index.
  - `No JWT/RBAC in initial scope` because the feature is explicitly limited to trusted local environments; network exposure remains an operational responsibility outside this feature.
  - `No Docker-first deployment requirement` because IIS is the primary runtime target for this feature.

## Project Structure

### Documentation (this feature)

```text
specs/002-local-semantic-search/
├── plan.md
├── research.md
├── data-model.md
├── quickstart.md
├── contracts/
│   └── local-semantic-search.openapi.yaml
└── tasks.md
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/
│   ├── Entities/
│   ├── Interfaces/
│   └── ValueObjects/
├── SemanticSearch.Application/
│   ├── Common/
│   ├── Indexing/
│   ├── Search/
│   ├── Status/
│   ├── Files/                 # planned
│   └── Projects/              # planned
├── SemanticSearch.Infrastructure/
│   ├── DependencyInjection/
│   ├── Embedding/
│   ├── FileSystem/
│   ├── Indexing/
│   ├── ProjectTree/           # planned
│   └── VectorStore/
├── SemanticSearch.WebApi/
│   ├── Components/            # planned Blazor UI
│   ├── Controllers/
│   ├── Middleware/
│   ├── Pages/                 # planned routing/pages
│   ├── Services/              # planned UI-facing HTTP/query helpers
│   └── wwwroot/               # planned local assets/styles
tests/
├── SemanticSearch.Application.Tests/      # planned
├── SemanticSearch.Infrastructure.Tests/   # planned
├── SemanticSearch.WebApi.IntegrationTests/# planned
└── SemanticSearch.WebApi.ComponentTests/  # planned
```

**Structure Decision**: Preserve the current four-project Clean Architecture layout and evolve `src/SemanticSearch.WebApi` into the single presentation host for both API controllers and the Blazor UI. Add test projects rather than introducing a second frontend runtime or a second deployment unit.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| `.NET 10` instead of constitution `.NET 8` | The repository already targets `net10.0` and the feature explicitly requires `.NET 10` | Downgrading the solution would add churn without product value and conflicts with the requested deliverable |
| `Blazor Web App` instead of `React 18 + TypeScript + Vite` | One ASP.NET Core host keeps IIS deployment and local-only operation simple | A separate SPA would add a second toolchain, second deployment artifact, and unnecessary host coordination |
| `SQLite-backed local vector store` instead of `SQL Server` | The feature requires a file-based offline index portable with the app | SQL Server increases operational overhead and breaks the stated local-file storage goal |
| `No JWT/RBAC in initial scope` | The feature is intentionally constrained to trusted local environments and same-machine/local-network use | Adding full identity flows would expand scope beyond the user stories and slow delivery of the core local-search capability |
