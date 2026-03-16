# Feature Specification: Code Quality Dashboard Foundation

**Feature Branch**: `003-duplication-dashboard-foundation`  
**Created**: 2026-03-16  
**Status**: Draft  
**Input**: User description: "Phase 1 code quality API and dashboard foundation for structural and semantic duplication detection"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Review Project Quality Snapshot (Priority: P1)

An engineering lead opens the local developer command center for a selected project and immediately sees whether duplication is a material quality risk. The view summarizes the overall quality grade, the amount of code analyzed, the percentage of duplicated code, and the number of structural and semantic clone findings.

**Why this priority**: The feature has no value unless users can quickly understand the current duplication risk for a project and decide whether deeper inspection is needed.

**Independent Test**: Analyze an indexed project, open the quality dashboard, and confirm the summary metrics and duplication breakdown are visible without opening any detail views.

**Acceptance Scenarios**:

1. **Given** an indexed project with qualifying duplicate findings, **When** the user opens the quality dashboard, **Then** the system shows the overall quality grade, total lines of code analyzed, duplication percentage, structural clone count, and semantic clone count.
2. **Given** an indexed project with no qualifying duplicate findings, **When** the user opens the quality dashboard, **Then** the system shows a clean result with zero duplication percentage and no duplicate rows.
3. **Given** an indexed project with both unique and duplicated code, **When** the dashboard loads, **Then** the summary totals, visual breakdown, and duplicate table reflect the same underlying counts.

---

### User Story 2 - Inspect Duplicate Evidence (Priority: P2)

A developer selects a duplicate finding from the dashboard and reviews the two matching code regions side by side so they can judge whether the duplication should be refactored, ignored, or tracked for later work.

**Why this priority**: Summary metrics alone do not support action. Teams need evidence at the file and line level before deciding how to respond.

**Independent Test**: Open any duplicate finding from the table and verify that both code regions, file references, line ranges, duplication type, and highlighted matching lines are visible in a comparison view.

**Acceptance Scenarios**:

1. **Given** a duplicate finding is listed in the dashboard table, **When** the user opens its detail view, **Then** the system shows both code regions side by side with file names, line ranges, and visibly highlighted matching lines.
2. **Given** a duplicate finding is labeled with a severity and duplication type, **When** the user opens its detail view, **Then** the same severity and duplication type remain visible with the comparison.
3. **Given** a duplicate finding references source content that is no longer available, **When** the user opens its detail view, **Then** the system shows a clear message explaining that the comparison cannot be displayed in full.

---

### User Story 3 - Retrieve Consistent Quality Findings (Priority: P3)

The local developer command center retrieves duplication summary data and detailed findings in a consistent format so the dashboard and any follow-on automation use the same numbers and the same finding list.

**Why this priority**: The dashboard foundation depends on a reliable quality data source. Without that contract, the interface cannot remain accurate or reusable.

**Independent Test**: Request the quality summary and duplicate findings for an analyzed project and confirm the response includes the same grade, counts, types, severities, file references, and line ranges shown in the dashboard.

**Acceptance Scenarios**:

1. **Given** an analyzed project with duplicate findings, **When** the command center requests quality data, **Then** the system returns the overall summary together with a detailed list of duplicate findings.
2. **Given** an analyzed project is run again after code changes, **When** the command center requests quality data, **Then** the system returns the latest available findings without stale duplicate entries from older analysis runs.
3. **Given** a project has not been analyzed yet or has no eligible source files, **When** the command center requests quality data, **Then** the system returns a clear empty-state response instead of a misleading duplicate result set.

---

### Edge Cases

- A project contains no eligible indexed source files and therefore has no duplication data to display.
- The same code region qualifies as both a structural and a semantic duplicate; the system must keep the categories understandable and avoid inflating totals.
- A file included in an older finding has been moved, renamed, or deleted before a user opens the comparison view.
- A project contains a large number of duplicate findings; the system must still provide a usable summary and navigable detail list.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST analyze eligible indexed source files within a selected project and produce duplication results for that project.
- **FR-002**: The system MUST produce a project-level quality summary containing an overall quality grade, total lines of code analyzed, total duplication percentage, structural clone count, and semantic clone count.
- **FR-003**: The system MUST identify structural duplicates when two code regions express the same logic despite differences in names or literal values.
- **FR-004**: The system MUST identify semantic duplicates when two code regions meet or exceed a semantic similarity score of `0.95`.
- **FR-005**: The system MUST record each duplicate finding with duplication type, severity, source file reference, and start/end line numbers for both compared code regions.
- **FR-006**: The system MUST classify duplicate findings into `Structural` and `Semantic` categories and present those categories consistently in summaries and detail views.
- **FR-007**: The system MUST avoid double-counting the same duplicate evidence within a single category and MUST reconcile any evidence that appears in more than one category.
- **FR-008**: The system MUST make the quality summary and duplicate findings available through a consistent retrieval interface for the local developer command center.
- **FR-009**: The system MUST display a dashboard summary section showing the overall quality grade, total lines of code analyzed, duplication percentage, and clone counts for the selected project.
- **FR-010**: The system MUST display a visual breakdown of `Unique`, `Structural`, and `Semantic` code for the selected project.
- **FR-011**: The system MUST display a duplicate findings table that includes severity, duplication type, affected files, and line numbers for each finding.
- **FR-012**: Users MUST be able to open any duplicate finding from the table and inspect both code regions side by side with matching lines visibly highlighted.
- **FR-013**: The system MUST display a clear empty state when no duplication findings are available or when the selected project has not yet been analyzed.
- **FR-014**: The system MUST refresh the quality summary and duplicate findings when a project is analyzed again so the dashboard reflects the latest available results.
- **FR-015**: The system MUST assign an overall quality grade from `A` to `E` using one consistent grading scale for all analyzed projects in this feature.
- **FR-016**: The system MUST assign each duplicate finding a severity of `High`, `Medium`, or `Low` using one consistent severity scale for all analyzed projects in this feature.

### Key Entities *(include if feature involves data)*

- **Quality Summary**: The project-level view of code quality for this feature, including overall grade, analyzed lines of code, duplication percentage, and clone counts by category.
- **Duplication Finding**: A single reported duplicate relationship between two code regions, including category, severity, supporting file references, and line ranges.
- **Code Region**: A specific span of code in a file, identified by project, file reference, and start/end line numbers.
- **Duplicate Comparison**: The detailed side-by-side presentation of one duplication finding, including both code regions and highlighted matching lines.

### Assumptions

- Phase 1 applies to one selected project at a time and analyzes eligible indexed source files only.
- The semantic duplicate threshold is fixed at `0.95` for this phase.
- The quality grade uses duplication-percentage bands of `A: 0-2%`, `B: >2-5%`, `C: >5-10%`, `D: >10-20%`, and `E: >20%`.
- The severity scale uses duplicated line counts per finding: `High: 50 or more`, `Medium: 20-49`, and `Low: 1-19`.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In at least 95% of attempts, a user can open the quality dashboard for an analyzed project and see the summary metrics within 10 seconds.
- **SC-002**: 100% of duplicate findings shown in the dashboard table include file references and line ranges that match the detail comparison view.
- **SC-003**: A user can open the comparison view for any duplicate finding in no more than two interactions from the dashboard table.
- **SC-004**: For validation datasets with known duplicate samples, at least 95% of qualifying structural duplicates and 90% of qualifying semantic duplicates are surfaced in the results.
- **SC-005**: For projects with no qualifying duplicates, the system reports `0%` duplication and zero duplicate findings in 100% of verification runs.
- **SC-006**: For 100% of analyzed projects, the summary metrics, visual breakdown, and detailed findings remain internally consistent with one another.

