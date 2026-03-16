# Data Model: Code Quality Dashboard Foundation

**Spec**: [spec.md](spec.md) | **Research**: [research.md](research.md)

## 1. QualityAnalysisRun

- **Purpose**: Tracks one project-scoped quality analysis execution that produces structural findings, semantic findings, or both.
- **Fields**:
  - `RunId` (string/guid, primary key)
  - `ProjectKey` (string, foreign key to `ProjectWorkspace`)
  - `RequestedModes` (set: `Structural`, `Semantic`)
  - `Status` (enum: `Queued`, `Running`, `Completed`, `Failed`)
  - `RequestedUtc` (datetime, required)
  - `StartedUtc` (datetime, nullable)
  - `CompletedUtc` (datetime, nullable)
  - `TotalFilesScanned` (integer, `>= 0`)
  - `TotalLinesAnalyzed` (integer, `>= 0`)
  - `StructuralFindingCount` (integer, `>= 0`)
  - `SemanticFindingCount` (integer, `>= 0`)
  - `FailureReason` (string, nullable)
- **Validation Rules**:
  - `RequestedModes` must contain at least one analysis mode.
  - Only one `Queued` or `Running` quality analysis run is allowed per project at a time.
- **State Transitions**:
  - `Queued -> Running`
  - `Running -> Completed`
  - `Running -> Failed`

## 2. QualitySummarySnapshot

- **Purpose**: Represents the latest persisted dashboard summary for one project after a completed analysis run.
- **Fields**:
  - `ProjectKey` (string, primary key / foreign key to `ProjectWorkspace`)
  - `RunId` (foreign key to `QualityAnalysisRun`)
  - `QualityGrade` (enum: `A`, `B`, `C`, `D`, `E`)
  - `TotalLinesOfCode` (integer, `>= 0`)
  - `UniqueLineCount` (integer, `>= 0`)
  - `StructuralDuplicateLineCount` (integer, `>= 0`)
  - `SemanticDuplicateLineCount` (integer, `>= 0`)
  - `DuplicationPercent` (decimal, `0-100`)
  - `StructuralFindingCount` (integer, `>= 0`)
  - `SemanticFindingCount` (integer, `>= 0`)
  - `LastAnalyzedUtc` (datetime, required)
- **Validation Rules**:
  - `UniqueLineCount + StructuralDuplicateLineCount + SemanticDuplicateLineCount` must equal `TotalLinesOfCode` after reconciliation.
  - `DuplicationPercent` must be derived from duplicate line counts rather than manually entered.
- **Relationships**:
  - One `QualitySummarySnapshot` belongs to one `ProjectWorkspace` and one completed `QualityAnalysisRun`.

## 3. DuplicationFinding

- **Purpose**: Stores one surfaced duplicate relationship between two code regions for the findings table and detail modal.
- **Fields**:
  - `FindingId` (string/guid, primary key)
  - `ProjectKey` (string, foreign key)
  - `RunId` (foreign key to `QualityAnalysisRun`)
  - `Type` (enum: `Structural`, `Semantic`)
  - `Severity` (enum: `High`, `Medium`, `Low`)
  - `SimilarityScore` (decimal, range `0.95-1.00` for semantic; `1.00` for structural fingerprints)
  - `MatchingLineCount` (integer, `>= 1`)
  - `NormalizedFingerprint` (string, nullable for semantic findings)
  - `LeftRegionId` (foreign key to `CodeRegion`)
  - `RightRegionId` (foreign key to `CodeRegion`)
  - `CreatedUtc` (datetime, required)
- **Validation Rules**:
  - `LeftRegionId` and `RightRegionId` must reference two distinct code regions.
  - The same unordered region pair may appear only once per `RunId` and `Type`.
  - `SimilarityScore` must meet the configured threshold for the finding type.
- **Relationships**:
  - Many `DuplicationFinding` records belong to one `QualityAnalysisRun`.
  - Each `DuplicationFinding` references exactly two `CodeRegion` records.

## 4. CodeRegion

- **Purpose**: Captures the persisted snapshot of one code span used in a duplicate finding and comparison view.
- **Fields**:
  - `RegionId` (string/guid, primary key)
  - `ProjectKey` (string, foreign key)
  - `RelativeFilePath` (string, required)
  - `StartLine` (integer, `>= 1`)
  - `EndLine` (integer, `>= StartLine`)
  - `Snippet` (string, required)
  - `ContentHash` (string, required)
  - `SourceSegmentId` (string, nullable when not derived from a stored search segment)
  - `Availability` (enum: `Available`, `Missing`, `Stale`)
- **Validation Rules**:
  - `Snippet` must be non-empty when the region is first captured.
  - `RelativeFilePath` must stay within the indexed project root.
  - `Availability` may only move from `Available` to `Missing` or `Stale` after source drift is detected.
- **Relationships**:
  - One `CodeRegion` may be referenced by many `DuplicationFinding` records across analysis runs.

## 5. QualityBreakdownSlice

- **Purpose**: Transport model for the dashboard chart that explains how analyzed code is divided between unique, structural duplicate, and semantic duplicate lines.
- **Fields**:
  - `Category` (enum: `Unique`, `Structural`, `Semantic`)
  - `LineCount` (integer, `>= 0`)
  - `Percent` (decimal, `0-100`)
- **Notes**:
  - This model can be derived from `QualitySummarySnapshot` and does not require separate persistence.

## 6. DuplicateComparisonView

- **Purpose**: Read model returned to the diff modal so the UI can render one duplicate finding side by side.
- **Fields**:
  - `FindingId` (string)
  - `Type` (enum: `Structural`, `Semantic`)
  - `Severity` (enum: `High`, `Medium`, `Low`)
  - `SimilarityScore` (decimal)
  - `LeftRegion` (`CodeRegion` projection plus highlighted line numbers)
  - `RightRegion` (`CodeRegion` projection plus highlighted line numbers)
- **Notes**:
  - This is a transport model composed from `DuplicationFinding` and `CodeRegion`, not a primary persisted entity.

## Relationship Summary

- `ProjectWorkspace 1 -> many QualityAnalysisRun`
- `ProjectWorkspace 1 -> 1 current QualitySummarySnapshot`
- `QualityAnalysisRun 1 -> many DuplicationFinding`
- `DuplicationFinding 1 -> 2 CodeRegion`
- `QualityBreakdownSlice` and `DuplicateComparisonView` are derived read models
