# SemanticSearch — Local Semantic Search Web API

A 100% local, offline Semantic Search API for codebases. Uses ONNX embeddings (all-MiniLM-L6-v2) and SQLite for vector storage. Consumed by AI Agents to index and semantically search local code.

## Features

- **POST /api/search/index** — Queue a background indexing job for a local project directory
- **POST /api/search/query** — Semantically search an indexed project for relevant code snippets
- **GET /api/search/status/{projectKey}** — Check indexing progress and statistics
- 100% offline — no external API calls; all inference is local via ONNX Runtime
- IIS-hosted with absolute paths for production stability

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (or .NET 10 Runtime for IIS)
- Windows Server with IIS + [ASP.NET Core Hosting Bundle](https://dotnet.microsoft.com/download/dotnet/10.0) installed
- ONNX model files downloaded (see below)

## NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.ML.OnnxRuntime` | 1.20.1 | Local ONNX inference |
| `Microsoft.ML.Tokenizers` | 0.22.0 | BERT WordPiece tokenizer |
| `Microsoft.Data.Sqlite` | 9.0.3 | Vector store (SQLite) |
| `MediatR` | 14.1.0 | CQRS mediator |
| `FluentValidation` | 12.1.1 | Request validation |

## Model Download

Download `all-MiniLM-L6-v2` from HuggingFace and place files in `models/all-MiniLM-L6-v2/` at the application root:

```
models/
└── all-MiniLM-L6-v2/
    ├── model.onnx        (~90 MB) — from /onnx/model.onnx
    ├── vocab.txt         (~232 KB) — from /vocab.txt
    └── tokenizer.json    (~700 KB) — optional, for reference
```

### Download commands (PowerShell)

```powershell
New-Item -ItemType Directory -Force -Path "models\all-MiniLM-L6-v2"

# Using huggingface-cli (pip install huggingface_hub):
huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 onnx/model.onnx --local-dir models/all-MiniLM-L6-v2
huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 vocab.txt --local-dir models/all-MiniLM-L6-v2

# Or manually download from:
# https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx
# https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt
```

## Build & Run (Development)

```powershell
# From repository root
dotnet build SemanticSearch.slnx

# Run (Kestrel)
dotnet run --project src/SemanticSearch.WebApi --urls "http://localhost:5000"
```

In development, `appsettings.Development.json` resolves `ModelPath` and `DatabasePath` to the repository-root `models/` and `data/` folders so `dotnet run` works from this repo layout.

## Configuration (appsettings.json)

```json
{
  "SemanticSearch": {
    "ModelPath": "models/all-MiniLM-L6-v2",
    "DatabasePath": "data/vectorstore.db",
    "Chunking": {
      "ChunkSize": 200,
      "Overlap": 40
    },
    "Indexing": {
      "ExcludedDirectories": ["bin", "obj", ".git", "node_modules"],
      "AllowedExtensions": [".cs", ".ts", ".py", ".go", ".md", ...]
    }
  }
}
```

Paths are relative to `ContentRootPath` (the application directory). Under IIS, this is always the published application folder.

## API Usage

### Index a Project

```bash
curl -X POST http://localhost:5000/api/search/index \
  -H "Content-Type: application/json" \
  -d '{"projectPath": "C:\\Projects\\MyApp", "projectKey": "myapp"}'

# Response (202 Accepted):
# {"projectKey":"myapp","status":"queued","message":"Indexing queued..."}
```

### Search

```bash
curl -X POST http://localhost:5000/api/search/query \
  -H "Content-Type: application/json" \
  -d '{"query": "authentication middleware", "projectKey": "myapp", "topK": 5}'

# Response (200 OK):
# {"results": [{"filePath": "...", "relevanceScore": 0.87, "snippet": "...", "startLine": 42, "endLine": 241}]}
```

### Check Status

```bash
curl http://localhost:5000/api/search/status/myapp

# Response (200 OK):
# {"isIndexed": true, "totalFiles": 142, "totalChunks": 1823, "lastUpdated": "2026-03-14T..."}
```

## IIS Deployment

### Publish

```powershell
dotnet publish src/SemanticSearch.WebApi -c Release -o publish/
```

### IIS Application Pool — Required Settings

| Setting | Value | Reason |
|---------|-------|--------|
| `.NET CLR version` | `No Managed Code` | ASP.NET Core handles its own runtime |
| `Start Mode` | `AlwaysRunning` | Background worker must start with IIS, not on first request |
| `Idle Time-out` | `0` (disabled) | Prevents pool shutdown during inactive periods |
| `Regular Time Interval (minutes)` | `0` (disabled) | Prevents periodic recycling that kills the background worker |
| `preloadEnabled` | `true` | Warms up the app pool on IIS start |

### PowerShell: Configure App Pool

```powershell
Import-Module WebAdministration

$poolName = "SemanticSearchPool"
New-WebAppPool -Name $poolName
Set-ItemProperty "IIS:\AppPools\$poolName" managedRuntimeVersion ""
Set-ItemProperty "IIS:\AppPools\$poolName" startMode "AlwaysRunning"
Set-ItemProperty "IIS:\AppPools\$poolName" processModel.idleTimeout ([TimeSpan]::Zero)
Set-ItemProperty "IIS:\AppPools\$poolName" recycling.periodicRestart.time ([TimeSpan]::Zero)

# Create site
New-Website -Name "SemanticSearch" -Port 80 -PhysicalPath "C:\inetpub\SemanticSearch" -ApplicationPool $poolName
Set-ItemProperty "IIS:\Sites\SemanticSearch" -Name applicationDefaults.preloadEnabled -Value $true
```

### Model files in publish output

If the repository-root `models/` folder exists when `dotnet publish` runs, it is copied into the publish directory automatically. Verify that the publish output contains `models\all-MiniLM-L6-v2\model.onnx` and `vocab.txt`.

If you publish from a machine or pipeline that does not have the repository-root `models/` folder, copy it manually after publish:
```powershell
Copy-Item -Recurse models\ publish\models\
```

### Notes

- All file paths use absolute paths derived from `IWebHostEnvironment.ContentRootPath`
- SQLite database is at `{ContentRootPath}\data\vectorstore.db`
- ONNX model is at `{ContentRootPath}\models\all-MiniLM-L6-v2\model.onnx`
- In-flight indexing is lost on app pool recycle; re-POST to `/api/search/index` to restart
- The `data/` and `logs/` directories are created automatically on startup

## Project Structure

```
src/
├── SemanticSearch.Domain/         # Entities, value objects, interfaces
├── SemanticSearch.Application/    # Commands, queries, validators, behaviors
├── SemanticSearch.Infrastructure/ # ONNX, SQLite, file system, background worker
└── SemanticSearch.WebApi/         # Controllers, middleware, Program.cs
models/                            # ONNX model files (not in git)
data/                              # SQLite database (not in git)
specs/                             # Feature specifications (speckit)
```
