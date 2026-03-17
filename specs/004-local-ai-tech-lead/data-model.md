# Data Model: Local AI Tech Lead Assistant

**Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

## 1. AiAssistantOptions

- **Purpose**: Configuration model for the local assistant runtime and prompt guardrails.
- **Fields**:
  - `ModelPath` (string, required, relative to content root)
  - `ContextSize` (integer, `>= 512`)
  - `MaxTokens` (integer, `>= 64`)
  - `CpuThreads` (integer, `>= 1`)
  - `GpuLayerCount` (integer, `>= 0`, default `0` for CPU-only scope)
  - `AntiPrompts` (list of strings, may be empty)
  - `MaxDuplicateSnippetCharacters` (integer, `>= 500`)
  - `StartupMode` (enum: `FailFast`, `MarkUnavailable`)
- **Validation Rules**:
  - `ModelPath` must resolve to an existing `.gguf` file before the assistant is reported as ready.
  - `MaxTokens` and `ContextSize` must be large enough to support a three-point action plan and one duplicate-fix response.
  - `MaxDuplicateSnippetCharacters` must be smaller than the effective prompt budget.
- **Relationships**:
  - One `AiAssistantOptions` instance governs all assistant requests in one application run.

## 2. AssistantReadiness

- **Purpose**: Read model describing whether the local assistant can accept requests.
- **Fields**:
  - `Status` (enum: `Initializing`, `Ready`, `Unavailable`, `Failed`)
  - `ModelLabel` (string, display-friendly model identifier)
  - `CheckedAtUtc` (datetime, required)
  - `FailureReason` (string, nullable)
- **Validation Rules**:
  - `FailureReason` is required when `Status` is `Unavailable` or `Failed`.
  - `ModelLabel` must not expose secrets or machine-specific absolute paths in UI payloads.
- **State Transitions**:
  - `Initializing -> Ready`
  - `Initializing -> Unavailable`
  - `Initializing -> Failed`
  - `Ready -> Unavailable`
  - `Ready -> Failed`

## 3. RecommendationSession

- **Purpose**: Represents one in-flight or completed assistant interaction in either the dashboard hero or duplicate comparison modal.
- **Fields**:
  - `SessionId` (string/guid, primary identifier)
  - `Surface` (enum: `ProjectPlan`, `DuplicateFix`)
  - `ProjectKey` (string, required)
  - `RunId` (string, nullable for readiness checks, required for project-plan generation)
  - `FindingId` (string, nullable, required for duplicate-fix generation)
  - `Status` (enum: `Queued`, `Streaming`, `Completed`, `Cancelled`, `Failed`)
  - `StartedUtc` (datetime, required)
  - `CompletedUtc` (datetime, nullable)
  - `RenderedMarkdown` (string, accumulated markdown, required and may be partial)
  - `FailureReason` (string, nullable)
- **Validation Rules**:
  - `ProjectPlan` sessions require `ProjectKey` and `RunId`.
  - `DuplicateFix` sessions require `ProjectKey` and `FindingId`.
  - Only one active session is allowed per `Surface` and visible UI instance.
- **State Transitions**:
  - `Queued -> Streaming`
  - `Streaming -> Completed`
  - `Streaming -> Cancelled`
  - `Streaming -> Failed`

## 4. ProjectActionPlanRequest

- **Purpose**: Input model for generating project-level guidance from the latest quality summary.
- **Fields**:
  - `ProjectKey` (string, required)
  - `RunId` (string, required)
  - `TotalLinesOfCode` (integer, `>= 0`)
  - `DuplicationPercent` (decimal, `0-100`)
  - `StructuralFindingCount` (integer, `>= 0`)
  - `SemanticFindingCount` (integer, `>= 0`)
- **Validation Rules**:
  - `RunId` must reference the same quality snapshot the user is currently reviewing.
  - At least one summary metric must be present beyond `ProjectKey`.
- **Relationships**:
  - One `ProjectActionPlanRequest` is derived from one `QualitySummarySnapshot`.

## 5. DuplicateFixRequest

- **Purpose**: Input model for generating duplicate-specific consolidation guidance.
- **Fields**:
  - `ProjectKey` (string, required)
  - `FindingId` (string, required)
  - `LeftFilePath` (string, required)
  - `RightFilePath` (string, required)
  - `LeftSnippet` (string, required, bounded)
  - `RightSnippet` (string, required, bounded)
  - `SimilarityScore` (decimal, `0-1`)
  - `DuplicationType` (enum: `Structural`, `Semantic`)
- **Validation Rules**:
  - `LeftSnippet` and `RightSnippet` must be non-empty after availability checks.
  - The combined snippet length must not exceed the configured maximum.
  - `LeftFilePath` and `RightFilePath` must refer to distinct code regions.
- **Relationships**:
  - One `DuplicateFixRequest` is derived from one `DuplicateComparisonContext`.

## 6. AiStreamEvent

- **Purpose**: Transport event emitted to the Blazor UI while generation is in progress.
- **Fields**:
  - `SessionId` (string, required)
  - `EventType` (enum: `Started`, `Token`, `Completed`, `Cancelled`, `Error`)
  - `Sequence` (integer, `>= 0`)
  - `MarkdownDelta` (string, nullable)
  - `Message` (string, nullable)
  - `OccurredAtUtc` (datetime, required)
- **Validation Rules**:
  - `MarkdownDelta` is required for `Token` events.
  - `Message` is required for `Error` events.
  - `Sequence` must be strictly increasing within one session.
- **Notes**:
  - This is a transport/read model and does not require durable persistence for this phase.

## 7. RenderedMarkdownFrame

- **Purpose**: UI-side projection of the accumulated markdown after one or more stream events.
- **Fields**:
  - `SessionId` (string, required)
  - `RawMarkdown` (string, required)
  - `Html` (string, required)
  - `LastSequence` (integer, `>= 0`)
  - `HasCodeBlocks` (boolean)
  - `IsPartial` (boolean)
- **Validation Rules**:
  - `Html` must be regenerated from `RawMarkdown` whenever `LastSequence` changes.
  - `IsPartial` must remain `true` until a terminal stream event is received.

## Relationship Summary

- `AiAssistantOptions 1 -> 1 AssistantReadiness`
- `AssistantReadiness` governs whether new `RecommendationSession` instances may start
- `ProjectActionPlanRequest` derives from one `QualitySummarySnapshot`
- `DuplicateFixRequest` derives from one `DuplicateComparisonContext`
- `RecommendationSession 1 -> many AiStreamEvent`
- `RenderedMarkdownFrame` is a UI projection of one `RecommendationSession`
