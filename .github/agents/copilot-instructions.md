# WebReport Development Guidelines

Auto-generated from all feature plans. Last updated: 2026-02-15

## Active Technologies
- C# / .NET 8 with ASP.NET Core + MediatR, Entity Framework Core, FluentValidation, xUnit (014-prior-reports-comparison)
- SQL Server (existing WebReport database) (014-prior-reports-comparison)
- C# / .NET 10.0 (backend), TypeScript 5.7.2 / React 18 (frontend) + ASP.NET Core, EF Core 9.0, MediatR 13.0, FluentValidation 11.11, Zustand 5.0, TanStack React Query 5.62, React Router DOM 7.1 (017-report-status-workflow)
- SQL Server (PXMain database, PXReports schema), EF Core Code-First + Dapper (017-report-status-workflow)
- C# 12 / .NET 8 (ASP.NET Core); TypeScript 5.x / React 18.3+ + MediatR 12, EF Core 8, FluentValidation 11, PuppeteerSharp 20 (already wired), TanStack Query v5, Vite 5, Tailwind CSS 3, i18next (018-report-viewer-access)
- SQL Server — `PXReports` schema for new tables; PDF binary stored in `PXReports.FinalizedReportPdfs` VARBINARY(MAX) (see research.md Decision 2 for rationale) (018-report-viewer-access)
- .NET 10.0 (backend), TypeScript 5.7.2 (frontend) + ASP.NET Core, MediatR 13.0.0, FluentValidation 11.11.0, EF Core 9.0.0 + Dapper 2.1.35, PuppeteerSharp 21.x (PDF), React 18.3.1, Vite 6.0.3, Zustand 5.0.2, TanStack React Query 5.62.11, HugeRTE 1.0.9 (rich text editor), Zod 3.24.1 (019-report-workflow-enhancements)
- SQL Server (PXMain DB, `PXReports` schema), IMemoryCache (write locks, transition rules) (019-report-workflow-enhancements)
- .NET 8+ backend (ASP.NET Core, MediatR, FluentValidation), TypeScript 5.x frontend (React 18+) + ASP.NET Core Web API, MediatR, FluentValidation, EF Core (SQL Server), React Query, i18n framework already used in reporting app (020-report-status-amendments)
- SQL Server (`PXReports`/existing reporting schema), existing audit/history tables and amendment/version tables (020-report-status-amendments)
- C# (.NET 10.0), TypeScript 5.7.2 + ASP.NET Core, MediatR, FluentValidation, EF Core, React 18.3.1, TanStack React Query 5.62.11, Zustand 5.0.2, Zod 3.24.1 (018-report-status-workflow)
- SQL Server (`PXReports` + legacy `dbo.Report`/`dbo.Studies`) (018-report-status-workflow)
- .NET 10.0 (backend), TypeScript 5.7.2 (frontend) + ASP.NET Core, MediatR 13.0.0, FluentValidation 11.11.0, EF Core 9.0.0 + Dapper 2.1.35, React 18.3.1, Vite 6.0.3, Zustand 5.0.2, TanStack React Query 5.62.11, Zod 3.24.1 (020-print-status)
- SQL Server (PXMain DB, `PXReports` schema), existing `RadiologyReport` entity (020-print-status)
- C# / .NET 8, TypeScript 5.x / React 18+ + ASP.NET Core, MediatR (CQRS), FluentValidation, Entity Framework Core, React Query, Axios, Tailwind CSS (021-study-locking)
- SQL Server — schema `PXReports`, table `ReportWriteLocks` (existing), new `WriteLockAuditEvents` table (021-study-locking)
- .NET 8 (C# 12) + TypeScript 5.x (React 18) + MediatR (CQRS), FluentValidation, Entity Framework Core, PuppeteerSharp (PDF), React Query, Tailwind CSS (023-report-versioning)
- SQL Server — schema `PXReports`, existing table `ReportVersions` with indexes on `ReportId` and `CreatedAt` (023-report-versioning)
- [e.g., Python 3.11, Swift 5.9, Rust 1.75 or NEEDS CLARIFICATION] + [e.g., FastAPI, UIKit, LLVM or NEEDS CLARIFICATION] (025-dictated-reports)
- [if applicable, e.g., PostgreSQL, CoreData, files or N/A] (025-dictated-reports)
- C# / .NET 8 (backend), TypeScript 5.x (frontend) + ASP.NET Core, MediatR, FluentValidation, Entity Framework Core (backend); React 18, TanStack React Query, Tailwind CSS, Vite (frontend) (025-dictated-reports)
- SQL Server — new `PXReports.DictatedReports` and `PXReports.DictatedReportAuditLogs` tables; audio stored as `VARBINARY(MAX)` (025-dictated-reports)
- C# / .NET 8, TypeScript 5.x + ASP.NET Core, MediatR, FluentValidation, Entity Framework Core, PuppeteerSharp, HtmlAgilityPack, Mammoth, RtfPipe, React 18, Vite, Axios, Zustand, TanStack React Query, Tailwind CSS (026-report-export-import)
- SQL Server (multi-tenant schema via ReportDbContext) (026-report-export-import)
- C# on .NET 10 (ASP.NET Core Web API) + TypeScript 5.7 (React 18) + ASP.NET Core, MediatR 13, FluentValidation 11, AesEncryptionService (existing), React 18, React Query, Zustand, i18next, Tailwind CSS (027-diagnosis-popup)
- SQL Server (read-only — user session/claims from JWT; no new tables) (027-diagnosis-popup)
- C# / .NET 8 (backend), TypeScript / React 18 (frontend) + HugeRTE v1.0.9 + @hugerte/hugerte-react v2.0.2, nspell (Hunspell-compatible JS spell checker), MediatR, FluentValidation, Entity Framework Core 9, React Query, Zustand, Axios (029-editor-spell-check)
- SQL Server (PXMain database, PXReports schema) via EF Core (029-editor-spell-check)
- C# / .NET 10 + ASP.NET Core 10, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, SQLite (Microsoft.Data.Sqlite), MediatR, FluentValidation (001-local-semantic-search-api)
- SQLite with vector storage (file-based, absolute paths under IIS ContentRootPath) (001-local-semantic-search-api)

- C# on .NET 8 (ASP.NET Core Web API) + ASP.NET Core, Entity Framework Core, MediatR, FluentValidation, Swagger/OpenAPI (001-report-context-api)

## Project Structure

```text
backend/
frontend/
tests/
```

## Commands

# Add commands for C# on .NET 8 (ASP.NET Core Web API)

## Code Style

C# on .NET 8 (ASP.NET Core Web API): Follow standard conventions

## Recent Changes
- 001-local-semantic-search-api: Added C# / .NET 10 + ASP.NET Core 10, Microsoft.ML.OnnxRuntime, Microsoft.ML.Tokenizers, SQLite (Microsoft.Data.Sqlite), MediatR, FluentValidation
- 029-editor-spell-check: Added C# / .NET 8 (backend), TypeScript / React 18 (frontend) + HugeRTE v1.0.9 + @hugerte/hugerte-react v2.0.2, nspell (Hunspell-compatible JS spell checker), MediatR, FluentValidation, Entity Framework Core 9, React Query, Zustand, Axios
- 027-diagnosis-popup: Added C# on .NET 10 (ASP.NET Core Web API) + TypeScript 5.7 (React 18) + ASP.NET Core, MediatR 13, FluentValidation 11, AesEncryptionService (existing), React 18, React Query, Zustand, i18next, Tailwind CSS


<!-- MANUAL ADDITIONS START -->
<!-- MANUAL ADDITIONS END -->
