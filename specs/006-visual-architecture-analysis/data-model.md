# Data Model: Visual Architecture & Impact Analysis

**Feature**: 006-visual-architecture-analysis  
**Date**: 2026-03-18

## New Domain Entities

### DependencyAnalysisRun

Tracks metadata for a dependency graph analysis run. Mirrors the existing `QualityAnalysisRun` pattern.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| RunId | string | PK, GUID | Unique identifier for the analysis run |
| ProjectKey | string | FK → ProjectWorkspaces, NOT NULL | Project being analyzed |
| Status | DependencyAnalysisStatus | NOT NULL | Queued / Running / Completed / Failed |
| RequestedUtc | DateTime | NOT NULL | When analysis was requested |
| StartedUtc | DateTime? | | When analysis execution began |
| CompletedUtc | DateTime? | | When analysis finished |
| TotalFilesScanned | int | NOT NULL, DEFAULT 0 | Number of source files processed |
| TotalNodesFound | int | NOT NULL, DEFAULT 0 | Number of dependency nodes extracted |
| TotalEdgesFound | int | NOT NULL, DEFAULT 0 | Number of dependency edges extracted |
| FailureReason | string? | | Error message if analysis failed |

### DependencyNode

Represents a class or method in the dependency graph.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| NodeId | string | PK, GUID-hash | Unique identifier (hash of ProjectKey\|Namespace\|Name\|Kind) |
| ProjectKey | string | FK → ProjectWorkspaces, NOT NULL | Owning project |
| RunId | string | FK → DependencyAnalysisRuns, NOT NULL | Analysis run that produced this node |
| Name | string | NOT NULL | Short name (class name or method name) |
| FullName | string | NOT NULL | Fully qualified name (Namespace.Class or Namespace.Class.Method) |
| Kind | DependencyNodeKind | NOT NULL | Class / Method |
| Namespace | string | NOT NULL | Containing namespace |
| FilePath | string | NOT NULL | Relative file path |
| StartLine | int | NOT NULL | Starting line number in source file |
| ParentNodeId | string? | FK → DependencyNodes | For methods: the containing class node |

### DependencyEdge

Represents a directed relationship between two nodes.

| Field | Type | Constraints | Description |
|-------|------|-------------|-------------|
| EdgeId | string | PK, GUID-hash | Unique identifier (hash of SourceNodeId\|TargetNodeId\|Type) |
| ProjectKey | string | FK → ProjectWorkspaces, NOT NULL | Owning project |
| RunId | string | FK → DependencyAnalysisRuns, NOT NULL | Analysis run that produced this edge |
| SourceNodeId | string | FK → DependencyNodes, NOT NULL | Caller / referencing node |
| TargetNodeId | string | FK → DependencyNodes, NOT NULL | Callee / referenced node |
| RelationshipType | DependencyRelationshipType | NOT NULL | Invocation / Inheritance / TypeReference / Construction |

## New Enums

### DependencyAnalysisStatus

```
Queued | Running | Completed | Failed
```

### DependencyNodeKind

```
Class | Method
```

### DependencyRelationshipType

```
Invocation | Inheritance | TypeReference | Construction
```

## New SQLite Tables

### DependencyAnalysisRuns

```sql
CREATE TABLE IF NOT EXISTS DependencyAnalysisRuns (
    RunId           TEXT PRIMARY KEY,
    ProjectKey      TEXT NOT NULL,
    Status          TEXT NOT NULL DEFAULT 'Queued',
    RequestedUtc    TEXT NOT NULL,
    StartedUtc      TEXT,
    CompletedUtc    TEXT,
    TotalFilesScanned   INTEGER NOT NULL DEFAULT 0,
    TotalNodesFound     INTEGER NOT NULL DEFAULT 0,
    TotalEdgesFound     INTEGER NOT NULL DEFAULT 0,
    FailureReason       TEXT,
    FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey)
);
CREATE INDEX IF NOT EXISTS IX_DependencyAnalysisRuns_ProjectKey
    ON DependencyAnalysisRuns(ProjectKey);
```

### DependencyNodes

```sql
CREATE TABLE IF NOT EXISTS DependencyNodes (
    NodeId          TEXT PRIMARY KEY,
    ProjectKey      TEXT NOT NULL,
    RunId           TEXT NOT NULL,
    Name            TEXT NOT NULL,
    FullName        TEXT NOT NULL,
    Kind            TEXT NOT NULL,
    Namespace       TEXT NOT NULL,
    FilePath        TEXT NOT NULL,
    StartLine       INTEGER NOT NULL,
    ParentNodeId    TEXT,
    FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey),
    FOREIGN KEY (RunId) REFERENCES DependencyAnalysisRuns(RunId)
);
CREATE INDEX IF NOT EXISTS IX_DependencyNodes_ProjectKey
    ON DependencyNodes(ProjectKey);
CREATE INDEX IF NOT EXISTS IX_DependencyNodes_RunId
    ON DependencyNodes(RunId);
```

### DependencyEdges

```sql
CREATE TABLE IF NOT EXISTS DependencyEdges (
    EdgeId          TEXT PRIMARY KEY,
    ProjectKey      TEXT NOT NULL,
    RunId           TEXT NOT NULL,
    SourceNodeId    TEXT NOT NULL,
    TargetNodeId    TEXT NOT NULL,
    RelationshipType TEXT NOT NULL,
    FOREIGN KEY (ProjectKey) REFERENCES ProjectWorkspaces(ProjectKey),
    FOREIGN KEY (RunId) REFERENCES DependencyAnalysisRuns(RunId),
    FOREIGN KEY (SourceNodeId) REFERENCES DependencyNodes(NodeId),
    FOREIGN KEY (TargetNodeId) REFERENCES DependencyNodes(NodeId)
);
CREATE INDEX IF NOT EXISTS IX_DependencyEdges_ProjectKey
    ON DependencyEdges(ProjectKey);
CREATE INDEX IF NOT EXISTS IX_DependencyEdges_RunId
    ON DependencyEdges(RunId);
```

## Read Models (Application Layer DTOs — not persisted)

### FileHeatmapEntry

Computed by aggregating existing quality data. Not a new table — derived from `IndexedFiles`, `DuplicationFindings`, and `CodeRegions` at query time.

| Field | Type | Description |
|-------|------|-------------|
| RelativeFilePath | string | File path relative to project root |
| FileName | string | File name only |
| TotalLines | int | Total lines of code in file |
| StructuralDuplicateCount | int | Number of structural duplication findings involving this file |
| SemanticDuplicateCount | int | Number of semantic duplication findings involving this file |
| DuplicationDensity | double | (StructuralDuplicateCount + SemanticDuplicateCount) / TotalLines |

### ErDiagramResult

Computed at request time by querying SQLite PRAGMAs and reflecting domain entity classes. Not persisted.

| Field | Type | Description |
|-------|------|-------------|
| MermaidMarkup | string | Complete Mermaid.js `erDiagram` definition string |
| EntityCount | int | Number of entities in the diagram |
| RelationshipCount | int | Number of relationships between entities |

## Entity Relationships

```
ProjectWorkspaces 1 ──── * DependencyAnalysisRuns
DependencyAnalysisRuns 1 ──── * DependencyNodes
DependencyAnalysisRuns 1 ──── * DependencyEdges
DependencyNodes 1 ──── * DependencyEdges (as source)
DependencyNodes 1 ──── * DependencyEdges (as target)
DependencyNodes 1 ──── * DependencyNodes (parent-child for class→method)
```

## Integration Points

- **Heatmap data** reuses existing `IndexedFiles` (line counts), `DuplicationFindings` (duplicate counts by file), and `CodeRegions` (file associations) — no new tables needed.
- **ER diagram** queries the live SQLite database schema using `PRAGMA table_info` and `PRAGMA foreign_key_list` — no new tables needed.
- **Dependency graph** introduces 3 new tables following the existing `QualityAnalysisRuns` + `DuplicationFindings` + `CodeRegions` pattern.
- All new tables are created in `SqliteSchemaInitializer.Schema` alongside existing schema definitions.
- The `IDependencyRepository` interface is implemented by `SqliteVectorStore`, following the existing `IQualityRepository` pattern.
