# Data Model: Local Semantic Search Web API

**Branch**: `001-local-semantic-search-api` | **Date**: 2026-03-14  
**Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

---

## Entities

### Chunk (Primary Storage Entity)

The fundamental unit of indexed data. Represents a logical segment of a source file, its text content, and its vector embedding.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| Id | string | PK, deterministic (SHA256) | `SHA256(ProjectKey \| FilePath \| StartLine)` |
| ProjectKey | string | NOT NULL, indexed | Partition key — isolates projects from each other |
| FilePath | string | NOT NULL | Absolute path of the source file |
| StartLine | int | NOT NULL, >= 1 | First line number of the chunk (1-based) |
| EndLine | int | NOT NULL, >= StartLine | Last line number of the chunk (1-based, inclusive) |
| Content | string | NOT NULL | Raw text content of the chunk |
| Embedding | float[384] | NOT NULL, stored as BLOB | L2-normalized embedding vector (1,536 bytes) |
| CreatedAt | DateTime | NOT NULL | ISO 8601 timestamp of creation/last update |

**Indexes**:
- `IX_Chunks_ProjectKey` on `(ProjectKey)` — filters search to a single project
- `IX_Chunks_ProjectKey_FilePath` on `(ProjectKey, FilePath)` — supports stale chunk cleanup during re-index

**Unique constraint**: `(ProjectKey, FilePath, StartLine)` — prevents duplicate chunks

---

### ProjectMetadata (Status Tracking Entity)

Tracks per-project indexing statistics. Updated atomically at the end of each indexing operation.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| ProjectKey | string | PK | Unique project identifier |
| TotalFiles | int | NOT NULL, >= 0 | Number of files successfully indexed |
| TotalChunks | int | NOT NULL, >= 0 | Total chunks stored for this project |
| LastUpdated | DateTime | NOT NULL | ISO 8601 timestamp of last completed indexing |

---

## Value Objects (Domain Layer — not persisted directly)

### ChunkInfo

Represents a chunk before embedding generation. Produced by the chunking stage, consumed by the embedding stage.

| Field | Type | Description |
|-------|------|-------------|
| FilePath | string | Source file absolute path |
| Content | string | Raw text of the chunk |
| StartLine | int | First line (1-based) |
| EndLine | int | Last line (1-based, inclusive) |

---

### EmbeddingVector

Wraps a float[384] array with equality semantics. Used in domain logic to represent embeddings without leaking storage details.

| Field | Type | Description |
|-------|------|-------------|
| Values | float[384] | L2-normalized 384-dimensional vector |

---

### SearchResult

Returned by the search operation. Maps to the API response DTO.

| Field | Type | Description |
|-------|------|-------------|
| FilePath | string | Source file where the match was found |
| RelevanceScore | float | Cosine similarity score (0.0 to 1.0) |
| Snippet | string | Code chunk text content |
| StartLine | int | First line of the matched chunk |
| EndLine | int | Last line of the matched chunk |

---

### IndexingStatus

Returned by the status endpoint. Maps to the API response DTO.

| Field | Type | Description |
|-------|------|-------------|
| IsIndexed | bool | Whether any chunks exist for this project |
| TotalFiles | int | Number of indexed files |
| TotalChunks | int | Total chunks in the vector store |
| LastUpdated | DateTime? | Timestamp of last indexing (null if never indexed) |

---

## Relationships

```
ProjectMetadata (1) ───────< Chunk (many)
     │                         │
  ProjectKey ════════════ ProjectKey
                               │
                          FilePath ──── groups chunks from same file
```

- One `ProjectMetadata` per unique `ProjectKey`
- Many `Chunk` records per `ProjectMetadata` (one per file segment)
- Chunks are grouped by `FilePath` within a project — enables stale chunk cleanup on re-index

---

## State Transitions

### Project Indexing Lifecycle

```
[Not Indexed] ──POST /api/search/index──> [Indexing In Progress] ──success──> [Indexed]
                                               │                                  │
                                             error                         POST /index again
                                               │                                  │
                                               v                                  v
                                          [Failed]                        [Re-Indexing]──> [Indexed]
```

| State | IsIndexed | Queryable? | Description |
|-------|-----------|------------|-------------|
| Not Indexed | false | No (empty results) | No ProjectMetadata row exists |
| Indexing In Progress | previous value | Yes (stale data OK) | Background worker processing; existing data still queryable |
| Indexed | true | Yes | ProjectMetadata reflects latest stats |
| Failed | previous value | Yes (if previously indexed) | Error logged; previous data preserved |
| Re-Indexing | true | Yes (stale data OK) | New chunks upserting; stale chunks cleaned up on completion |

---

## Validation Rules

| Entity | Field | Rule |
|--------|-------|------|
| Chunk | ProjectKey | Not empty, max 128 chars |
| Chunk | FilePath | Not empty, must be valid path |
| Chunk | StartLine | >= 1 |
| Chunk | EndLine | >= StartLine |
| Chunk | Content | Not empty |
| Chunk | Embedding | Exactly 384 float values |
| ProjectMetadata | ProjectKey | Not empty, max 128 chars |
| ProjectMetadata | TotalFiles | >= 0 |
| ProjectMetadata | TotalChunks | >= 0 |

---

## Storage Schema (SQLite)

```sql
CREATE TABLE IF NOT EXISTS Chunks (
    Id TEXT PRIMARY KEY,
    ProjectKey TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    StartLine INTEGER NOT NULL,
    EndLine INTEGER NOT NULL,
    Content TEXT NOT NULL,
    Embedding BLOB NOT NULL,
    CreatedAt TEXT NOT NULL,
    UNIQUE(ProjectKey, FilePath, StartLine)
);

CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey ON Chunks(ProjectKey);
CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey_FilePath ON Chunks(ProjectKey, FilePath);

CREATE TABLE IF NOT EXISTS ProjectMetadata (
    ProjectKey TEXT PRIMARY KEY,
    TotalFiles INTEGER NOT NULL DEFAULT 0,
    TotalChunks INTEGER NOT NULL DEFAULT 0,
    LastUpdated TEXT NOT NULL
);
```
