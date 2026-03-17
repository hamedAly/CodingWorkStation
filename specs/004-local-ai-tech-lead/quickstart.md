# Quickstart: Local AI Tech Lead Assistant

## 1. Prerequisites

- Install the .NET 10 SDK and the ASP.NET Core Hosting Bundle on machines that will run under IIS.
- Place a compatible GGUF coding model under `D:\Indexing\models\llm\`.
- Verify the application can read the GGUF model file and write to `D:\Indexing\data\`.
- Ensure at least one project has already produced a quality snapshot and duplicate findings.

## 2. Configure the Local Assistant

Add assistant settings to `src/SemanticSearch.WebApi/appsettings.Development.json` under `SemanticSearch`:

```json
{
  "SemanticSearch": {
    "Assistant": {
      "ModelPath": "models/llm/qwen2.5-coder-instruct.gguf",
      "ContextSize": 8192,
      "MaxTokens": 768,
      "CpuThreads": 8,
      "GpuLayerCount": 0,
      "AntiPrompts": [ "User:", "System:" ],
      "MaxDuplicateSnippetCharacters": 12000,
      "StartupMode": "MarkUnavailable"
    }
  }
}
```

## 3. Build and Run

```powershell
dotnet restore D:\Indexing\SemanticSearch.slnx
dotnet build D:\Indexing\SemanticSearch.slnx
dotnet run --project D:\Indexing\src\SemanticSearch.WebApi --urls "http://localhost:5000"
```

## 4. Verify Assistant Readiness

```powershell
Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/quality/ai/status
```

- Confirm the response reports `status = Ready`.
- If the response reports `Unavailable` or `Failed`, verify the configured GGUF file path and backend package deployment.
- When the GGUF file is intentionally absent, the endpoint should still respond successfully with `status = Unavailable` and a clear local file-path message.

## 5. Get the Current Quality Snapshot

```powershell
$summary = Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/quality/single-file-csharp
$summary
```

- Note the returned `projectKey` and `runId`; the project-level assistant stream uses both values.

## 6. Stream a Project-Level Action Plan

```powershell
$payload = '{"projectKey":"single-file-csharp","runId":"REPLACE_RUN_ID"}'
curl.exe -N -X POST ^
  -H "Content-Type: application/json" ^
  -H "Accept: application/x-ndjson" ^
  --data $payload ^
  http://localhost:5000/api/quality/ai/project-plan/stream
```

- Confirm the stream emits ordered NDJSON events.
- Confirm at least one `Token` event arrives before completion.
- Confirm the final response content forms a three-point action plan.

## 7. Stream a Duplicate-Specific Refactoring Proposal

```powershell
$findings = Invoke-RestMethod -Method Get -Uri http://localhost:5000/api/quality/single-file-csharp/findings
$findingId = $findings.findings[0].findingId
$payload = "{""projectKey"":""single-file-csharp"",""findingId"":""$findingId""}"

curl.exe -N -X POST ^
  -H "Content-Type: application/json" ^
  -H "Accept: application/x-ndjson" ^
  --data $payload ^
  http://localhost:5000/api/quality/ai/finding-fix/stream
```

- Confirm the stream returns ordered NDJSON events for the selected finding.
- Confirm the final markdown includes both explanatory text and a fenced C# code block.

## 8. Validate the Blazor UI

1. Open `http://localhost:5000/quality`.
2. Select the indexed project used in the API checks.
3. Confirm the hero section shows a full-width assistant trigger below the metric cards.
4. Start a project-level action plan and confirm the panel expands, streams partial output, and preserves prior text while updating.
5. Open a duplicate finding and confirm the comparison modal still shows both code regions.
6. Start a duplicate-fix request and confirm a dedicated assistant panel opens inside the modal without replacing the side-by-side comparison.
7. Confirm fenced C# output is rendered as formatted markdown with code highlighting.

## 9. Validate Failure and Cancellation States

1. Temporarily point `Assistant:ModelPath` to a missing file and restart the app.
2. Confirm `GET /api/quality/ai/status` reports `Unavailable` or `Failed`.
3. Confirm the UI shows a clear message instead of hanging when assistant actions are triggered.
4. Restore a valid model path, restart, and start a stream.
5. Close the modal or restart the same assistant surface while streaming.
6. Confirm partial output remains visible and the UI shows that the earlier request did not complete.
