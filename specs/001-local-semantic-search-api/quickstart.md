# Quickstart: Local Semantic Search Web API

**Branch**: `001-local-semantic-search-api` | **Date**: 2026-03-14

---

## Prerequisites

- **.NET 10 SDK** installed ([download](https://dotnet.microsoft.com/download/dotnet/10.0))
- **Windows** with IIS enabled (for production) or any OS for development
- **~200 MB disk space** for the ONNX model + vector database

## 1. Download the Embedding Model

Download the all-MiniLM-L6-v2 ONNX model files from HuggingFace and place them in the `models/` directory:

```
models/
└── all-MiniLM-L6-v2/
    ├── model.onnx         (~90 MB)
    ├── tokenizer.json     (~700 KB)
    └── vocab.txt          (~232 KB)
```

Download URLs:
- `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/onnx/model.onnx`
- `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/tokenizer.json`
- `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2/resolve/main/vocab.txt`

## 2. Build and Run (Development)

```bash
# From repository root
dotnet restore src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj
dotnet build src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj
dotnet run --project src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj
```

The API starts on `http://localhost:5000` by default.

`appsettings.Development.json` resolves `ModelPath` to `..\..\models\all-MiniLM-L6-v2` and `DatabasePath` to `..\..\data\vectorstore.db`, so `dotnet run` uses the repository-root folders in development.

## 3. Test the Endpoints

### Index a Project

```bash
curl -X POST http://localhost:5000/api/search/index \
  -H "Content-Type: application/json" \
  -d '{"projectPath": "D:\\Projects\\MyApp", "projectKey": "my-app"}'
```

Expected response (202 Accepted):
```json
{
  "projectKey": "my-app",
  "status": "queued",
  "message": "Indexing has been queued for project 'my-app'."
}
```

### Check Status

```bash
curl http://localhost:5000/api/search/status/my-app
```

Expected response (200 OK):
```json
{
  "isIndexed": true,
  "totalFiles": 142,
  "totalChunks": 3847,
  "lastUpdated": "2026-03-14T15:30:00Z"
}
```

### Search

```bash
curl -X POST http://localhost:5000/api/search/query \
  -H "Content-Type: application/json" \
  -d '{"query": "authentication middleware", "projectKey": "my-app", "topK": 5}'
```

Expected response (200 OK):
```json
{
  "results": [
    {
      "filePath": "D:\\Projects\\MyApp\\src\\Auth\\AuthMiddleware.cs",
      "relevanceScore": 0.87,
      "snippet": "public class AuthMiddleware...",
      "startLine": 12,
      "endLine": 45
    }
  ]
}
```

## 4. Run Tests

```bash
dotnet test
```

## 5. IIS Deployment

### Publish

```bash
dotnet publish src/SemanticSearch.WebApi/SemanticSearch.WebApi.csproj \
  -c Release -o ./publish
```

### Model Files

If the repository-root `models/` directory exists when `dotnet publish` runs, it is copied into the publish output automatically. Confirm that the publish folder contains:

```
publish/
├── models/
│   └── all-MiniLM-L6-v2/
│       ├── model.onnx
│       ├── tokenizer.json
│       └── vocab.txt
├── SemanticSearch.WebApi.dll
└── web.config
```

If publish runs on a machine that does not have the repository-root `models/` directory, copy it into `publish/models/` manually before deploying to IIS.

### Configure IIS

1. **Create Application Pool**: Name it `SemanticSearchPool`
   - .NET CLR Version: **No Managed Code** (runs via ASP.NET Core Module)
   - Start Mode: **AlwaysRunning**
   - Idle Timeout: **0** (never idle-stop)
   - Periodic Restart Time: **0** (disable periodic recycling)

2. **Create Website/Application**: Point to the `publish/` folder
   - Application Pool: `SemanticSearchPool`
   - Preload Enabled: **True**

3. **Ensure ASP.NET Core Hosting Bundle** is installed on the IIS server

### Verify

```bash
curl http://localhost/SemanticSearch/api/search/status/test
```

## NuGet Packages (Complete List)

### Runtime Dependencies

| Package | Version | Project |
|---------|---------|---------|
| Microsoft.ML.OnnxRuntime | 1.20.1 | Infrastructure |
| Microsoft.ML.Tokenizers | 0.22.0 | Infrastructure |
| Microsoft.Data.Sqlite | 9.0.3 | Infrastructure |
| MediatR | 14.1.0 | Application |
| FluentValidation | 12.1.1 | Application |
| FluentValidation.DependencyInjectionExtensions | 12.1.1 | Application |

### Test Dependencies

| Package | Version |
|---------|---------|
| xUnit | latest |
| FluentAssertions | latest |
| NSubstitute | latest |
| Microsoft.AspNetCore.Mvc.Testing | latest |

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
      "ExcludedDirectories": ["bin", "obj", "node_modules", ".git", "dist", ".vs", ".idea", "packages", "TestResults"],
      "SupportedExtensions": [".cs", ".md", ".ts", ".js", ".json", ".xml", ".yaml", ".yml", ".razor", ".html", ".css", ".sql", ".py", ".java", ".cpp", ".h", ".go", ".rs"]
    }
  }
}
```

Path values are relative to `ContentRootPath` and resolved to absolute paths at startup.
