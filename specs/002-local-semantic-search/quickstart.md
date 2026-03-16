# Quickstart: Local Semantic Search Workspace

## 1. Prerequisites

- Install the .NET 10 SDK and the ASP.NET Core Hosting Bundle on machines that will run under IIS.
- Ensure local model files are available under `[content-root]\models\all-MiniLM-L6-v2\`.
- Verify the application has write access to `[content-root]\data\` and `[content-root]\logs\`.

## 2. Development Run

```powershell
dotnet restore D:\Indexing\SemanticSearch.slnx
dotnet build D:\Indexing\SemanticSearch.slnx
dotnet run --project D:\Indexing\src\SemanticSearch.WebApi
```

## 3. Open the Workspace UI

- Browse to the local application URL shown by `dotnet run`.
- Confirm the dashboard loads and shows no project keys or existing indexed projects.
- Confirm `GET /api/project` returns an empty array before the first indexing run.

## 4. Index a Project

1. Open the indexing panel.
2. Enter a local source path such as `D:\Indexing\sample-projects\example-app`.
3. Enter a project key such as `example-app`.
4. Start indexing and wait for the dashboard status to move from queued/running to indexed.

## 5. Validate Search and Explorer

1. Open the search page and select `example-app`.
2. Run a semantic query such as `background indexing worker`.
3. Switch to exact search and search for a literal symbol or class name.
4. Open a result and confirm the corresponding file can be viewed in full.
5. Open the project explorer, browse the tree, and open a second file from the tree.

## 6. Validate API Contracts

```powershell
$body = @{
  projectPath = 'D:\Indexing\sample-projects\example-app'
  projectKey  = 'example-app'
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/project/index -ContentType 'application/json' -Body $body
Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/project/status/example-app

$semantic = @{
  query = 'background indexing worker'
  projectKey = 'example-app'
  topK = 5
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/search/semantic -ContentType 'application/json' -Body $semantic
```

## 7. Validate Single-File Refresh

1. Modify one indexed file in the sample project.
2. Call `POST /api/project/index/file` with the project key and relative file path.
3. Re-run a search that should hit the changed content and confirm the snippet updates.

## 8. IIS Smoke Test

1. Publish the app to a local IIS site directory.
2. Ensure the application pool is `AlwaysRunning`, idle timeout is disabled, and preload is enabled.
3. Confirm the dashboard loads after an app-pool recycle.
4. Confirm existing project status remains available and new search requests succeed.
