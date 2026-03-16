# Quickstart: Code Quality Dashboard Foundation

## 1. Prerequisites

- Install the .NET 10 SDK and the ASP.NET Core Hosting Bundle on machines that will run under IIS.
- Ensure local model files exist under `D:\Indexing\models\all-MiniLM-L6-v2\`.
- Verify the application can write to `D:\Indexing\data\`.
- Have at least one indexed project key available from the existing workspace flows.

## 2. Development Run

```powershell
dotnet restore D:\Indexing\SemanticSearch.slnx
dotnet build D:\Indexing\SemanticSearch.slnx
dotnet run --project D:\Indexing\src\SemanticSearch.WebApi --urls "http://localhost:5000"
```

## 3. Ensure a Project Is Indexed

1. Open the existing indexing page or call `POST /api/project/index` for a local sample project.
2. Wait until the project status reports `Indexed`.
3. Confirm `GET http://localhost:5000/api/project/status/{projectKey}` returns non-zero file and segment counts.

## 4. Run Structural Duplication Analysis

```powershell
$structural = @{
  projectKey = 'single-file-csharp'
  minimumLines = 5
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/quality/structural -ContentType 'application/json' -Body $structural
```

- Confirm the response includes a `runId`, `mode = Structural`, and at least one finding or an empty result with `findingCount = 0`.

## 5. Run Semantic Duplication Analysis

```powershell
$semantic = @{
  projectKey = 'single-file-csharp'
  similarityThreshold = 0.95
  maxPairs = 200
} | ConvertTo-Json

Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/quality/semantic -ContentType 'application/json' -Body $semantic
```

- Confirm the response includes a `runId`, `mode = Semantic`, and only findings whose `similarityScore` is at least `0.95`.

## 6. Validate the Dashboard Summary APIs

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/quality/single-file-csharp
Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/quality/single-file-csharp/findings
```

- Confirm the summary returns grade, total lines of code, duplication percentage, and structural/semantic counts.
- Confirm the findings list returns severity, type, file paths, and line ranges for each row.

## 7. Validate the Quality Dashboard UI

1. Open `http://localhost:5000/quality` in the local browser.
2. Select the indexed project used in the API checks.
3. Confirm the hero section shows quality grade, total LOC, duplication percentage, and clone counts.
4. Confirm the doughnut chart shows `Unique`, `Structural`, and `Semantic` slices.
5. Confirm the findings table lists severity, type, files, and line numbers.

## 8. Validate the Diff Modal

1. Open one finding from the findings table.
2. Confirm the modal shows both code regions side by side.
3. Confirm matching lines are highlighted in both panes.
4. Confirm the file path and line ranges in the modal match the selected finding row.

## 9. Validate Empty-State Behavior

1. Run quality analysis for a project with no qualifying duplicates.
2. Confirm `GET /api/quality/{projectKey}` returns `duplicationPercent = 0`.
3. Confirm the dashboard shows an empty findings state instead of a failed load.

