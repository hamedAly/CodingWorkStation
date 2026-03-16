# Data Model: Local Semantic Search Workspace

## 1. ProjectWorkspace

- **Purpose**: Represents one indexed codebase isolated by `ProjectKey`.
- **Fields**:
  - `ProjectKey` (string, primary key, 1-64 chars, lowercase slug recommended)
  - `SourceRootPath` (absolute path, required)
  - `Status` (enum: `NotIndexed`, `Queued`, `Indexing`, `Indexed`, `Failed`, `Degraded`)
  - `TotalFiles` (integer, `>= 0`)
  - `TotalSegments` (integer, `>= 0`)
  - `LastIndexedUtc` (datetime, nullable until first successful run)
  - `LastRunId` (string/guid, nullable)
  - `LastError` (string, nullable, summary only)
- **Validation Rules**:
  - `ProjectKey` must be unique across all workspaces.
  - `SourceRootPath` must be absolute and exist when a full indexing request is accepted.
  - `Status` changes only through indexing lifecycle transitions.
- **Relationships**:
  - One `ProjectWorkspace` has many `IndexedFile` records.
  - One `ProjectWorkspace` has many `IndexingRun` records.
  - One `ProjectWorkspace` has many `SearchSegment` records through `IndexedFile`.

## 2. IndexingRun

- **Purpose**: Tracks one full-index or single-file refresh execution.
- **Fields**:
  - `RunId` (string/guid, primary key)
  - `ProjectKey` (foreign key to `ProjectWorkspace`)
  - `RunType` (enum: `Full`, `SingleFile`)
  - `Status` (enum: `Queued`, `Running`, `Completed`, `Failed`, `Cancelled`)
  - `RequestedUtc` (datetime, required)
  - `StartedUtc` (datetime, nullable)
  - `CompletedUtc` (datetime, nullable)
  - `RequestedFilePath` (relative path, nullable for full runs)
  - `FilesScanned` (integer, `>= 0`)
  - `FilesIndexed` (integer, `>= 0`)
  - `FilesSkipped` (integer, `>= 0`)
  - `SegmentsWritten` (integer, `>= 0`)
  - `WarningCount` (integer, `>= 0`)
  - `FailureReason` (string, nullable)
- **Validation Rules**:
  - `RequestedFilePath` is required for `SingleFile` runs and forbidden for `Full` runs.
  - Only one `Running` run per `ProjectKey` is allowed at a time.
- **State Transitions**:
  - `Queued -> Running`
  - `Running -> Completed`
  - `Running -> Failed`
  - `Queued -> Cancelled`

## 3. IndexedFile

- **Purpose**: Captures the file-level state for content included in a project workspace.
- **Fields**:
  - `ProjectKey` (foreign key, part of composite key)
  - `RelativeFilePath` (string, part of composite key)
  - `AbsoluteFilePath` (string, required for runtime reads)
  - `FileName` (string, required)
  - `Extension` (string, required)
  - `Checksum` (string, required for change detection)
  - `SizeBytes` (long, `>= 0`)
  - `LastModifiedUtc` (datetime, required)
  - `LastIndexedUtc` (datetime, required)
  - `SegmentCount` (integer, `>= 0`)
  - `Availability` (enum: `Available`, `Missing`, `Unreadable`)
- **Validation Rules**:
  - `RelativeFilePath` must stay within the project root and cannot traverse upward.
  - `AbsoluteFilePath` must be rooted beneath `SourceRootPath`.
- **Relationships**:
  - One `IndexedFile` belongs to one `ProjectWorkspace`.
  - One `IndexedFile` has many `SearchSegment` records.

## 4. SearchSegment

- **Purpose**: Stores the searchable unit used for semantic retrieval and snippet presentation.
- **Fields**:
  - `SegmentId` (string/guid, primary key)
  - `ProjectKey` (foreign key, required)
  - `RelativeFilePath` (foreign key component, required)
  - `SegmentOrder` (integer, `>= 0`)
  - `StartLine` (integer, `>= 1`)
  - `EndLine` (integer, `>= StartLine`)
  - `Content` (string, required)
  - `SnippetPreview` (string, required)
  - `ContentHash` (string, required)
  - `EmbeddingVector` (binary/blob, required for semantic search)
  - `TokenCount` (integer, `>= 0`)
  - `CreatedUtc` (datetime, required)
- **Validation Rules**:
  - `Content` cannot be empty.
  - `ProjectKey + RelativeFilePath + SegmentOrder` must be unique.
  - `EmbeddingVector` must use a consistent dimension across all stored segments.
- **Relationships**:
  - Many `SearchSegment` records belong to one `IndexedFile`.

## 5. ProjectTreeNode

- **Purpose**: Represents the browsable explorer structure returned to the UI/API.
- **Fields**:
  - `NodePath` (string, unique within a project)
  - `ParentPath` (string, nullable)
  - `Name` (string, required)
  - `NodeType` (enum: `Directory`, `File`)
  - `RelativeFilePath` (string, nullable for directories)
  - `ChildCount` (integer, `>= 0`)
- **Notes**:
  - This model can be materialized from `IndexedFile` records and does not need separate persistence in the initial design.

## 6. SearchRequest

- **Purpose**: External request model for semantic or exact search.
- **Fields**:
  - `ProjectKey` (string, required)
  - `Mode` (enum: `Semantic`, `Exact`)
  - `QueryText` (string, required, non-empty)
  - `TopK` (integer, default `5`, allowed range `1-50`)
  - `MatchCase` (boolean, exact-search only)
- **Validation Rules**:
  - `QueryText` cannot be empty or whitespace.
  - `MatchCase` is only applicable to exact search mode.

## 7. SearchResult

- **Purpose**: Ranked response item returned to users and AI consumers.
- **Fields**:
  - `ProjectKey` (string)
  - `RelativeFilePath` (string)
  - `AbsoluteFilePath` (string, optional for UI-only display rules)
  - `Score` (decimal/float in normalized range)
  - `Snippet` (string)
  - `StartLine` (integer)
  - `EndLine` (integer)
  - `MatchType` (enum: `Semantic`, `Exact`)

## Relationship Summary

- `ProjectWorkspace 1 -> many IndexedFile`
- `ProjectWorkspace 1 -> many IndexingRun`
- `IndexedFile 1 -> many SearchSegment`
- `ProjectTreeNode` is derived from `IndexedFile`
- `SearchRequest` and `SearchResult` are transport models, not primary persisted entities
