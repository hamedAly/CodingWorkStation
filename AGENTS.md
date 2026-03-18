# Indexing Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-03-17

## Active Technologies
- C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, Microsoft.CodeAnalysis.CSharp, Microsoft.Data.Sqlite, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Chart.js (003-duplication-dashboard-foundation)
- SQLite file database under the application content root for indexed source metadata, embeddings, quality analysis runs, summary snapshots, and duplicate findings; source files and model assets remain on the local filesystem (003-duplication-dashboard-foundation)
- C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, LLamaSharp, LLamaSharp.Backend.Cpu, Markdig, existing quality dashboard contracts/components (004-local-ai-tech-lead)
- Existing SQLite quality data plus local filesystem model assets; no new persistent database tables are required for streaming assistant sessions (004-local-ai-tech-lead)
- C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), Tailwind CSS 4.x (standalone CLI) (005-enterprise-app-shell)
- N/A (UI-only feature; no database changes) (005-enterprise-app-shell)

- C# 13 on .NET 10 + ASP.NET Core, Blazor Web App, MediatR, FluentValidation, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Microsoft.Data.Sqlite (002-local-semantic-search)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# 13 on .NET 10

## Code Style

C# 13 on .NET 10: Follow standard conventions

## Recent Changes
- 005-enterprise-app-shell: Added C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), Tailwind CSS 4.x (standalone CLI)
- 004-local-ai-tech-lead: Added C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, LLamaSharp, LLamaSharp.Backend.Cpu, Markdig, existing quality dashboard contracts/components
- 003-duplication-dashboard-foundation: Added C# 13 on .NET 10 + ASP.NET Core, Blazor Web App (Interactive Server), MediatR, FluentValidation, Microsoft.CodeAnalysis.CSharp, Microsoft.Data.Sqlite, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Chart.js

- 002-local-semantic-search: Added C# 13 on .NET 10 + ASP.NET Core, Blazor Web App, MediatR, FluentValidation, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, Microsoft.Data.Sqlite

<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
