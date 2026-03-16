# SemanticSearch Workspace

Local semantic search workspace for codebases. The application runs entirely offline, serves both a Blazor-based UI and a .NET 10 Web API from one IIS-friendly ASP.NET Core host, stores vectors in a local SQLite database, and uses an ONNX copy of `all-MiniLM-L6-v2` for embeddings.

## What It Includes

- Dashboard for active project keys, indexing status, file counts, and last updated time
- Indexing panel for full project indexing and single-file refresh
- Search workspace with semantic and exact search modes
- Project explorer with indexed tree browsing and full-file reading
- Offline Web API for indexing, search, status, tree, and file-read flows
- Absolute-path storage rooted in `IWebHostEnvironment.ContentRootPath` for IIS hosting stability

## Runtime Requirements

- .NET 10 SDK for development
- ASP.NET Core Hosting Bundle for IIS deployment
- Windows workstation or Windows Server
- Local model files under `models/all-MiniLM-L6-v2/`

## Required NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `FluentValidation` | `12.1.1` | Application-layer validation |
| `MediatR` | `14.1.0` | Command/query dispatch |
| `Microsoft.Data.Sqlite` | `9.0.3` | Local file-based persistence |
| `Microsoft.ML.OnnxRuntime` | `1.20.1` | Offline embedding inference |
| `Microsoft.ML.Tokenizers` | `0.22.0` | Tokenization for the ONNX model |

## Model Setup

Place the model assets here:

```text
models/
└── all-MiniLM-L6-v2/
    ├── model.onnx
    ├── vocab.txt
    └── tokenizer.json
```

PowerShell example:

```powershell
New-Item -ItemType Directory -Force -Path "models\all-MiniLM-L6-v2"
huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 onnx/model.onnx --local-dir models/all-MiniLM-L6-v2
huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 vocab.txt --local-dir models/all-MiniLM-L6-v2
huggingface-cli download sentence-transformers/all-MiniLM-L6-v2 tokenizer.json --local-dir models/all-MiniLM-L6-v2
```

## Development

```powershell
dotnet restore SemanticSearch.slnx
dotnet build SemanticSearch.slnx
dotnet run --project src/SemanticSearch.WebApi --urls "http://localhost:5000"
```

Open:

- UI: `http://localhost:5000/`
- API: `http://localhost:5000/api/...`

## Configuration

`src/SemanticSearch.WebApi/appsettings.json`

```json
{
  "SemanticSearch": {
    "ModelPath": "models/all-MiniLM-L6-v2",
    "DatabasePath": "data/vectorstore.db",
    "Chunking": {
      "ChunkSize": 200,
      "Overlap": 40
    },
    "Ui": {
      "DashboardPollSeconds": 5,
      "DefaultSemanticTopK": 5,
      "DefaultExactTopK": 50
    },
    "Indexing": {
      "ExcludedDirectories": ["bin", "obj", ".git", "node_modules", ".vs", "packages"],
      "AllowedExtensions": [".cs", ".ts", ".js", ".py", ".md", ".json", ".razor"]
    }
  }
}
```

All configured paths are resolved relative to the application content root.

## API Routes

- `GET /api/project`
- `POST /api/project/index`
- `POST /api/project/index/file`
- `GET /api/project/status/{projectKey}`
- `GET /api/project/tree/{projectKey}`
- `POST /api/search/semantic`
- `POST /api/search/exact`
- `POST /api/file/read`

## API Examples

Full indexing:

```powershell
$body = @{
  projectPath = 'D:\Indexing\sample-projects\single-file-csharp'
  projectKey = 'single-file-csharp'
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/project/index -ContentType 'application/json' -Body $body
```

Semantic search:

```powershell
$semantic = @{
  query = 'report service'
  projectKey = 'single-file-csharp'
  topK = 5
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/search/semantic -ContentType 'application/json' -Body $semantic
```

Exact search:

```powershell
$exact = @{
  keyword = 'GenerateReport'
  projectKey = 'single-file-csharp'
  matchCase = $false
  topK = 20
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/search/exact -ContentType 'application/json' -Body $exact
```

Read a full file:

```powershell
$file = @{
  projectKey = 'single-file-csharp'
  relativeFilePath = 'ReportService.cs'
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/file/read -ContentType 'application/json' -Body $file
```

## IIS Deployment Notes

Publish:

```powershell
dotnet publish src/SemanticSearch.WebApi -c Release -o publish/
```

Recommended IIS application pool settings:

| Setting | Value |
|---------|-------|
| `.NET CLR version` | `No Managed Code` |
| `Start Mode` | `AlwaysRunning` |
| `Idle Time-out` | `0` |
| `Regular Time Interval` | `0` |
| `preloadEnabled` | `true` |

Important notes:

- Keep `models/` and `data/` beside the published application root.
- The app creates and reads the SQLite database through absolute paths under the content root.
- The background indexing worker starts with the web host, so app-pool cold starts affect indexing availability.
- If the content-root path changes between environments, move the model and database folders with the deployment.

## Repository Layout

```text
src/
├── SemanticSearch.Domain/
├── SemanticSearch.Application/
├── SemanticSearch.Infrastructure/
└── SemanticSearch.WebApi/
models/
data/
sample-projects/
specs/
```
