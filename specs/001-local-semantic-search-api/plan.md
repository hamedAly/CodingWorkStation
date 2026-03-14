# Implementation Plan: Local Semantic Search Web API

**Branch**: `001-local-semantic-search-api` | **Date**: 2026-03-14 | **Spec**: [spec.md](spec.md)
**Input**: Feature specification from `/specs/001-local-semantic-search-api/spec.md`

## Summary

Build a 100% local Semantic Search Web API using .NET 10, hosted on IIS. The system indexes local codebases by traversing directories, chunking source files, generating embeddings via a local ONNX model (all-MiniLM-L6-v2), and storing vectors in a SQLite-based vector database with project-level partitioning. Exposes three endpoints: indexing, semantic search, and status. Consumed exclusively by an AI Agent — no human UI.

## Technical Context

**Language/Version**: C# / .NET 10  
**Primary Dependencies**: ASP.NET Core 10, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, SQLite (Microsoft.Data.Sqlite), MediatR, FluentValidation  
**Storage**: SQLite with vector storage (file-based, absolute paths under IIS ContentRootPath)  
**Testing**: xUnit, FluentAssertions, NSubstitute  
**Target Platform**: Windows Server / IIS (also runnable via Kestrel for development)  
**Project Type**: Web API (backend only, no frontend)  
**Performance Goals**: Search query response < 2s (P95), Index 500+ files < 5 min  
**Constraints**: 100% offline — zero external network calls, absolute paths for IIS compatibility  
**Scale/Scope**: Multiple codebases indexed concurrently, each identified by ProjectKey; single-server deployment

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Constitution Rule | Status | Notes |
|-------------------|--------|-------|
| I. Code Quality (40-line methods, 400-line files) | PASS | Clean Architecture enforces small, focused classes |
| II. Code Organization (feature/domain-based folders) | PASS | Using Clean Architecture layers + vertical slices per feature |
| III. Comments & Documentation | PASS | Public API documented; README with setup/deployment instructions |
| IV. DRY | PASS | Shared abstractions for embedding generation, chunking, vector storage |
| V. Type System & Safety | PASS | Strong typing in C#; FluentValidation on all external boundaries |
| VI. Testing Standards (92%+ unit, 75%+ integration) | PASS | xUnit tests planned for all layers |
| VII. User Experience & UI | N/A | No human-facing UI — API only, consumed by AI Agent |
| VIII. Performance Budget (page load) | N/A | No frontend; API performance goals defined separately |
| IX. Git Hygiene | PASS | Conventional commits, feature branch workflow |
| X. Operations & Observability | PASS | Structured logging via ILogger; traceable errors |
| XI. Security Baseline | PASS | Input validation everywhere; no secrets needed (local-only); OWASP-aware |
| XII. Thin Controllers | PASS | Controllers delegate to MediatR handlers; 5-15 lines per action |
| XIII. FluentValidation Pipeline | PASS | Automatic validation via MediatR pipeline behavior |
| XIV. Separation of Concerns | PASS | Strict Clean Architecture layers: Domain → Application → Infrastructure → Web |

**Constitution technology stack deviation**: Constitution specifies ".NET 8" but spec requires .NET 10. This is an upgrade, not a violation — .NET 10 is backward-compatible with all listed patterns. Constitution specifies React/frontend tooling — N/A for this backend-only API.

## Project Structure

### Documentation (this feature)

```text
specs/001-local-semantic-search-api/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output (OpenAPI spec)
└── tasks.md             # Phase 2 output (created by /speckit.tasks)
```

### Source Code (repository root)

```text
src/
├── SemanticSearch.Domain/            # Core domain: entities, value objects, interfaces
│   ├── Entities/
│   ├── ValueObjects/
│   └── Interfaces/
├── SemanticSearch.Application/       # Use cases: commands, queries, handlers, validators
│   ├── Common/
│   │   ├── Behaviors/                # MediatR pipeline behaviors (validation, logging)
│   │   └── Interfaces/              # Application-level abstractions
│   ├── Indexing/
│   │   ├── Commands/
│   │   └── Validators/
│   ├── Search/
│   │   ├── Queries/
│   │   └── Validators/
│   └── Status/
│       └── Queries/
├── SemanticSearch.Infrastructure/    # Implementations: vector store, embedding, file system
│   ├── Embedding/                   # ONNX model loading, tokenization, inference
│   ├── VectorStore/                 # SQLite-based vector storage and retrieval
│   ├── FileSystem/                  # Directory traversal, file reading, chunking
│   └── DependencyInjection/        # Service registration
└── SemanticSearch.WebApi/            # ASP.NET Core host: controllers, middleware, config
    ├── Controllers/
    ├── Middleware/
    └── Configuration/

tests/
├── SemanticSearch.Domain.Tests/
├── SemanticSearch.Application.Tests/
├── SemanticSearch.Infrastructure.Tests/
└── SemanticSearch.WebApi.Tests/

models/                               # Pre-downloaded ONNX model files (not in git)
└── all-MiniLM-L6-v2/
    ├── model.onnx
    └── tokenizer.json
```

**Structure Decision**: Clean Architecture with 4 projects (Domain, Application, Infrastructure, WebApi) matching Constitution rules XII (thin controllers), XIII (FluentValidation pipeline), and XIV (strict layer separation). This is the standard structure for .NET CQRS/MediatR applications. Test projects mirror source projects.

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|-------------------------------------|
| 4 source projects (Constitution prefers simplicity) | Clean Architecture requires separate Domain, Application, Infrastructure, Presentation layers per Constitution rule XIV | A single project would violate Separation of Concerns (XIV) and make Dependency Rule enforcement impossible |
| SQLite as vector DB (not traditional EF Core) | Need efficient cosine similarity search on embeddings; EF Core doesn't support vector operations natively | Full SQL Server would require server setup, violating "file-based, local" requirement |
