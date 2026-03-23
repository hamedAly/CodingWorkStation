# Feature Specification: Visual Architecture & Impact Analysis

**Feature Branch**: `006-visual-architecture-analysis`  
**Created**: 2026-03-18  
**Status**: Draft  
**Input**: User description: "Phase 3: Visual Architecture & Impact Analysis — Implement endpoints to analyze code relationships and visualize them using interactive graphs. Dependency graph via code analysis, ER-diagram generation, interactive dependency visualization, architecture & DB visualizer, and code duplication heatmap."

## User Scenarios & Testing *(mandatory)*

### User Story 1 — Explore Dependency Graph (Priority: P1)

A developer or tech lead opens the Code Quality Dashboard for an indexed project and navigates to the "Architecture" view. The system displays an interactive graph of classes and methods in the project, showing which components call or depend on each other. The user can click on a node to highlight its upstream callers and downstream dependencies, pan/zoom the graph, and filter to a specific namespace or file. This helps the user understand the coupling between components and identify high-risk areas before making changes.

**Why this priority**: Understanding code dependencies is the core value proposition. Without the dependency graph, the other visualizations (ER diagram, heatmap) lack the architectural context needed to drive refactoring decisions.

**Independent Test**: Can be fully tested by indexing a sample C# project, requesting the dependency graph, and verifying nodes and edges render correctly in the interactive view. Delivers actionable visibility into code coupling.

**Acceptance Scenarios**:

1. **Given** an indexed project with multiple classes that reference each other, **When** the user opens the Architecture view, **Then** an interactive graph displays nodes (classes/methods) and edges (calls/references) with labels.
2. **Given** the dependency graph is displayed, **When** the user clicks on a node, **Then** direct callers (upstream) and callees (downstream) are visually highlighted while other nodes are dimmed.
3. **Given** a large project with many nodes, **When** the user types a namespace or class name in the filter box, **Then** only matching nodes and their direct connections are shown.
4. **Given** the dependency graph is displayed, **When** the user zooms, pans, or drags nodes, **Then** the layout responds smoothly without freezing.
5. **Given** a project that has not yet been analyzed for dependencies, **When** the user opens the Architecture view, **Then** an empty state is shown with a prompt to run dependency analysis.

---

### User Story 2 — View Code Duplication Heatmap (Priority: P2)

A developer opens the Code Quality Dashboard and navigates to the "Heatmap" view. The system displays a treemap of all project files sized by lines of code. Files are color-coded from green (no duplicates) through yellow to red (most duplicates). The user can hover over a file to see its duplicate count and click to navigate to its findings. This helps the user quickly spot the most problematic files in the codebase.

**Why this priority**: The heatmap builds directly on existing duplication data (already collected by the quality analysis runs) and provides an intuitive, at-a-glance view of project health — bridging the gap between the summary grade and the detailed findings table.

**Independent Test**: Can be tested by running a quality analysis on a project with known duplicates, opening the heatmap, and verifying that files with more duplicates appear in warmer colors and files with none appear green.

**Acceptance Scenarios**:

1. **Given** a project with a completed quality analysis run, **When** the user opens the Heatmap view, **Then** a treemap is displayed with one rectangle per source file, sized proportionally to lines of code.
2. **Given** the heatmap is displayed, **When** the user hovers over a file rectangle, **Then** a tooltip shows the file name, line count, structural duplicate count, and semantic duplicate count.
3. **Given** the heatmap is displayed, **When** a file has zero duplicates, **Then** it appears in green; files with the highest duplicate density appear in red, with a smooth gradient in between.
4. **Given** the heatmap is displayed, **When** the user clicks on a file rectangle, **Then** they are navigated to the filtered findings list for that file.
5. **Given** a project with no quality analysis data, **When** the user opens the Heatmap view, **Then** an empty state is shown with a prompt to run quality analysis first.

---

### User Story 3 — View Database Entity Relationship Diagram (Priority: P3)

A developer or tech lead opens the Architecture view and selects the "Data Model" tab. The system displays an auto-generated entity relationship diagram showing the project's persistent data entities, their attributes, and relationships. The diagram is rendered from a dynamically generated definition and supports zooming and panning. This helps the user understand the data model without manually maintaining documentation.

**Why this priority**: The ER diagram provides valuable architectural documentation but has a narrower audience (those working on the persistence layer) and depends on the data model reflection capability being implemented.

**Independent Test**: Can be tested by requesting the ER diagram for the current application's data model and verifying that entities, attributes, and relationships render correctly in the diagram.

**Acceptance Scenarios**:

1. **Given** the application's data model has been scanned, **When** the user opens the Data Model tab, **Then** an entity relationship diagram is displayed showing entities as boxes with attribute lists and relationship lines between them.
2. **Given** the ER diagram is displayed, **When** the user zooms or pans, **Then** the diagram responds smoothly.
3. **Given** the data model includes one-to-many and many-to-many relationships, **When** the ER diagram renders, **Then** the relationship cardinality is indicated on each connecting line.
4. **Given** no data model scan has been performed, **When** the user opens the Data Model tab, **Then** an empty state is shown with a prompt to run the data model scan.

---

### Edge Cases

- What happens when the project has circular dependencies (A calls B calls C calls A)?
- How does the dependency graph handle very large projects (500+ classes) without performance degradation?
- What happens when the heatmap is displayed for a project with only one file?
- How does the system handle projects with no source files or only non-C# files?
- What happens when a file referenced in the dependency graph has been deleted since the last analysis?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST analyze eligible indexed source files and extract class-level and method-level call relationships to produce a dependency graph.
- **FR-002**: System MUST represent the dependency graph as a collection of nodes (classes or methods with name, file path, namespace) and directed edges (caller-to-callee relationships with edge type).
- **FR-003**: System MUST detect the following relationship types: method invocations, property accesses, constructor calls, and type references (inheritance, interface implementation, field/parameter types).
- **FR-004**: System MUST handle circular dependencies gracefully, representing cycles without infinite loops or duplicated edges.
- **FR-005**: System MUST provide the dependency graph data through an API endpoint scoped by project key.
- **FR-006**: System MUST display the dependency graph as an interactive node-and-edge visualization with pan, zoom, and drag capabilities.
- **FR-007**: System MUST highlight upstream (callers) and downstream (callees) nodes when a user selects a single node.
- **FR-008**: System MUST provide a search/filter mechanism to narrow the dependency graph by namespace, class name, or file path.
- **FR-009**: System MUST scan the application's persistent data model and generate an entity relationship diagram definition containing entities, attributes, and relationships. The ER diagram is generated from a dual-source approach: SQLite schema introspection (via PRAGMA queries for tables, columns, and foreign keys) combined with domain entity class reflection (for C# type names and property metadata). See research.md R2.
- **FR-010**: System MUST render the entity relationship diagram as a visual diagram with entities, attributes, and relationship cardinality indicators.
- **FR-011**: System MUST provide the ER diagram data through an API endpoint.
- **FR-012**: System MUST display a treemap heatmap of all source files in the project, with rectangle size proportional to lines of code and color indicating duplication density (green = no duplicates, red = highest duplication).
- **FR-013**: System MUST show a tooltip on hover for each heatmap file rectangle containing file name, line count, and duplicate counts (structural and semantic).
- **FR-014**: System MUST allow users to click a heatmap file rectangle to navigate to the filtered findings for that file.
- **FR-015**: System MUST provide the heatmap data through an API endpoint scoped by project key, returning per-file line counts and duplication counts.
- **FR-016**: System MUST show an appropriate empty state for each visualization when the required analysis has not been performed.
- **FR-017**: System MUST integrate the new visualizations into the existing Code Quality Dashboard as additional tabs or views alongside the current summary and findings display.

### Key Entities

- **DependencyNode**: Represents a class or method in the dependency graph. Key attributes: unique identifier, display name, kind (class/method), namespace, file path, line number.
- **DependencyEdge**: Represents a directed relationship between two nodes. Key attributes: source node reference, target node reference, relationship type (invocation, inheritance, type reference).
- **DependencyGraph**: A complete dependency graph for a project. Key attributes: project key, collection of nodes, collection of edges, analysis timestamp.
- **FileHeatmapEntry**: Per-file duplication density data for the heatmap. Key attributes: file path, total lines, structural duplicate count, semantic duplicate count, duplication density score.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view the dependency graph for a project within 15 seconds of requesting it (analysis + rendering for projects up to 200 files).
- **SC-002**: 100% of direct call relationships in the analyzed project are represented as edges in the dependency graph (no false negatives for direct calls within the project boundary).
- **SC-003**: Users can identify all upstream callers and downstream callees of any component in 2 or fewer interactions (click node, view highlights).
- **SC-004**: The duplication heatmap loads and renders within 5 seconds for projects up to 500 files.
- **SC-005**: 100% of files with known duplicates appear in warm colors (yellow to red) on the heatmap, and 100% of files with zero duplicates appear in green.
- **SC-006**: Users can navigate from a heatmap file to its duplication findings in 1 click.
- **SC-007**: The entity relationship diagram accurately displays all persistent data entities and their relationships with correct cardinality.
- **SC-008**: All three visualizations (dependency graph, heatmap, ER diagram) are accessible from the existing dashboard without navigating to a separate application.

## Assumptions

- This feature builds on the existing Code Quality Dashboard (feature 003) and its infrastructure: the command/query pipeline, persistence layer, interactive server rendering, and charting integration.
- Dependency graph analysis targets C# source files only and operates on already-indexed projects.
- The treemap heatmap leverages existing quality analysis data (duplication findings, code regions, summary snapshots) and does not require a separate analysis pass.
- For projects exceeding practical visualization limits (e.g., 500+ nodes), the system may apply grouping or pagination strategies to maintain responsiveness.
- The user has specified preferences for specific visualization libraries (interactive graph library, diagram renderer, treemap charting plugin) which will be evaluated during the planning phase.
- The ER diagram feature depends on the resolution of FR-009 regarding the source of entity/relationship metadata.