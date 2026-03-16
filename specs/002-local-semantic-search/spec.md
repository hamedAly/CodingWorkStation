# Feature Specification: Local Semantic Search Workspace

**Feature Branch**: `002-local-semantic-search`  
**Created**: 2026-03-14  
**Status**: Draft  
**Input**: User description: "Build a centralized, 100% local Semantic Search solution with a Web API hosted as a background service and a complete Web UI for humans and another AI agent to search and navigate local codebases."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Index a Project Workspace (Priority: P1)

A user or AI agent registers a local codebase under a unique project key so the system can prepare it for later retrieval. The system scans the selected path, ignores irrelevant folders, processes eligible files, and records a searchable representation that stays isolated to that project key.

**Why this priority**: Without indexing, there is no searchable workspace and none of the other value can be delivered.

**Independent Test**: Can be fully tested by submitting a valid project path and project key, then confirming the workspace appears with indexed-file counts, indexing status, and a searchable project record.

**Acceptance Scenarios**:

1. **Given** a valid local project path and a new project key, **When** the user starts full indexing, **Then** the system creates a project record, processes eligible files, and reports indexing progress and completion status.
2. **Given** a project path that does not exist or cannot be accessed, **When** indexing is requested, **Then** the system rejects the request with a clear error and no project data is created.
3. **Given** a previously indexed project key, **When** full indexing is run again for the same project, **Then** the system refreshes the indexed content for that project without creating duplicate searchable entries.

---

### User Story 2 - Search Within a Selected Project (Priority: P1)

A user or AI agent chooses an indexed project key, enters a search query, and receives ranked results from only that project. The same interface supports both semantic search and exact keyword search so the user can move between intent-based discovery and literal matching.

**Why this priority**: Search is the primary outcome of the product and the main reason to index local codebases.

**Independent Test**: Can be fully tested by indexing a known project, running both semantic and exact searches, and verifying that returned results include the expected file paths, snippets, and relevance details for the selected project only.

**Acceptance Scenarios**:

1. **Given** an indexed project is selected, **When** the user submits a semantic search query, **Then** the system returns the most relevant matching snippets with file paths and relevance scores.
2. **Given** an indexed project is selected, **When** the user switches to exact search and submits a keyword, **Then** the system returns literal matches from that project and reflects the selected matching mode in the results.
3. **Given** multiple indexed projects exist, **When** the user searches while one project key is selected, **Then** no results from other project keys are returned.
4. **Given** no matches are found, **When** the search completes, **Then** the system returns an empty state that explains no results were found for the selected project and query.

---

### User Story 3 - Explore Project Structure and Read Files (Priority: P2)

A user browses the indexed directory tree for a selected project, expands folders, and opens a file to inspect its full contents without leaving the workspace. This helps users understand where a snippet lives in the broader codebase before making decisions.

**Why this priority**: Search results are more useful when users can immediately navigate the project structure and inspect full files in context.

**Independent Test**: Can be fully tested by selecting an indexed project, expanding folders in the project explorer, opening a file, and verifying that the full file contents match the indexed project.

**Acceptance Scenarios**:

1. **Given** a project has been indexed, **When** the user opens the project explorer, **Then** the system shows a navigable directory tree for that project.
2. **Given** the directory tree is visible, **When** the user selects a file, **Then** the system displays the full file contents in a dedicated reading surface without changing the selected project.
3. **Given** a file path no longer exists on disk, **When** the user attempts to open it, **Then** the system shows a clear error and preserves the rest of the explorer state.

---

### User Story 4 - Monitor Status and Keep an Index Current (Priority: P2)

An operator monitors the dashboard to see which project keys are active, whether indexing is in progress or complete, how many files are included, and when each project was last updated. When a single file changes, the operator or AI agent can refresh just that file instead of rebuilding the entire project.

**Why this priority**: Ongoing usability depends on trust in index freshness and visibility into system state, especially for large projects that change over time.

**Independent Test**: Can be fully tested by viewing dashboard status for indexed projects, updating one file in a project, triggering a single-file refresh, and verifying that project statistics and search behavior reflect the change.

**Acceptance Scenarios**:

1. **Given** one or more projects have been indexed, **When** the user opens the dashboard, **Then** the system lists active project keys, indexing status, total indexed files, and last updated times.
2. **Given** a file in an indexed project has changed, **When** a single-file update is requested for that file, **Then** the system replaces the old searchable content for that file and keeps the rest of the project index unchanged.
3. **Given** an indexing operation is already running for a project key, **When** another indexing request for the same project key is submitted, **Then** the system prevents conflicting updates and communicates how the request was handled.

### Edge Cases

- What happens when the selected project path contains no eligible source files? The system should complete gracefully and report that nothing was indexed.
- How does the system handle locked, unreadable, or binary files during indexing? The system should skip those files, continue processing the rest of the project, and report that some files were excluded.
- What happens when the user submits an empty project key, search query, or relative file path? The system should reject the request with a clear validation message.
- How does the system behave if the local search assets are unavailable at startup or after a host recycle? The system should mark affected operations as unavailable and explain what dependency could not be loaded.
- What happens when a file is removed after indexing but before a user opens it from the explorer or search results? The system should explain that the file is no longer available and preserve the rest of the session.
- How does the system handle very large projects or long-running indexing jobs? The system should surface in-progress status, avoid duplicate records, and remain responsive for other read operations.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow a user or AI agent to start a full indexing run by providing a local project path and a unique project key.
- **FR-002**: The system MUST create and maintain an isolated searchable workspace for each project key so that content from different projects never appears together unless explicitly requested in separate operations.
- **FR-003**: The system MUST ignore common build, dependency, cache, and version-control directories during indexing.
- **FR-004**: The system MUST process only readable text-based files that are relevant to source-code understanding and skip unsupported or unreadable files without stopping the rest of the indexing run.
- **FR-005**: The system MUST break indexed file content into searchable segments that preserve enough surrounding context for users to understand the returned result.
- **FR-006**: The system MUST store searchable project content locally so the full indexing and search workflow works without internet access.
- **FR-007**: The system MUST refresh existing indexed content for a project key when a full indexing run is repeated, without leaving duplicate searchable records behind.
- **FR-008**: The system MUST provide a dashboard view that shows active project keys, current indexing status, total indexed files, and last updated time for each indexed project.
- **FR-009**: The system MUST provide an indexing panel where the user can enter a local project path and project key, start a full indexing run, and see loading, success, or failure feedback for the request.
- **FR-010**: The system MUST provide a search interface with a project-key selector, query input, and a visible switch between semantic search and exact search modes.
- **FR-011**: The system MUST support semantic search for natural-language queries and return ranked results for the selected project key.
- **FR-012**: The system MUST support exact or keyword-based search for literal text matching within the selected project key, including an option to control case-sensitive matching.
- **FR-013**: The system MUST return search results with the file path, a relevance indicator, and a code snippet that helps the user evaluate the match.
- **FR-014**: The system MUST preserve code readability in the results view so users can distinguish snippets from surrounding text at a glance.
- **FR-015**: The system MUST allow the caller to limit how many search results are returned and MUST order results from most relevant to least relevant within the selected search mode.
- **FR-016**: The system MUST provide a project tree for each indexed project key so users can browse the indexed directory structure.
- **FR-017**: The system MUST allow a user to open the full contents of an indexed file from the project explorer or a search result in a dedicated reading surface.
- **FR-018**: The system MUST provide a single-file refresh operation that replaces previously indexed content for one file without rebuilding the full project index.
- **FR-019**: The system MUST provide a project-status operation that reports whether a project key has been indexed, whether indexing is in progress, how many files are currently indexed, and when the project was last updated.
- **FR-020**: The system MUST validate request data and return clear error responses for invalid paths, missing project keys, missing file paths, empty queries, or unsupported operations.
- **FR-021**: The system MUST show progress, loading, success, and failure states for long-running actions in the user interface.
- **FR-022**: The system MUST remain available as a continuously running local service that can continue serving status, browsing, and search requests after host restarts.
- **FR-023**: The system MUST resolve all model, index, and related local storage locations through stable absolute paths rooted in the application’s configured host environment so background hosting does not break file access.
- **FR-024**: The system MUST avoid making outbound calls to external paid or cloud services as part of indexing, search, status, browsing, or file-reading workflows.
- **FR-025**: The system MUST expose a full-indexing operation at `POST /api/project/index` that accepts `projectPath` and `projectKey`.
- **FR-026**: The system MUST expose a semantic-search operation at `POST /api/search/semantic` that accepts `query`, `projectKey`, and a result limit.
- **FR-027**: The system MUST expose an exact-search operation at `POST /api/search/exact` that accepts `keyword`, `projectKey`, and a case-sensitivity option.
- **FR-028**: The system MUST expose a project-status operation at `GET /api/project/status/{projectKey}`.
- **FR-029**: The system MUST expose a project-tree operation at `GET /api/project/tree/{projectKey}`.
- **FR-030**: The system MUST expose a full-file-read operation at `POST /api/file/read` that accepts `projectKey` and `relativeFilePath`.
- **FR-031**: The system MUST expose a single-file-refresh operation at `POST /api/project/index/file` that accepts `projectKey` and `relativeFilePath`.
- **FR-032**: The system MUST provide setup and deployment documentation that explains local prerequisites, package dependencies, configuration inputs, and background-hosting considerations.

### Key Entities *(include if feature involves data)*

- **Project Workspace**: An indexed codebase identified by a unique project key and associated with a source path, indexing status, total indexed files, and last updated time.
- **Indexed File**: A readable file that belongs to a project workspace and can be shown in the project tree or opened in full.
- **Search Segment**: A searchable portion of an indexed file that contains a snippet, its location within the file, and the project key it belongs to.
- **Indexing Run**: A tracked indexing action for a project workspace, including start time, completion state, excluded-file counts, and any warnings.
- **Search Request**: A user or API request containing a project key, search mode, query text, result limit, and optional case-sensitivity setting for literal matching.
- **Search Result**: A ranked match that includes a file path, relevance indicator, snippet, and enough location context for a user to navigate to the full file.
- **Project Tree Node**: A directory or file entry used to represent the browsable structure of an indexed project.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can register and complete indexing for a new medium-sized project of 500 files within 10 minutes on the target deployment machine.
- **SC-002**: After indexing completes, 95% of semantic and exact searches return results or a no-results state within 3 seconds for typical interactive queries.
- **SC-003**: In a controlled relevance test set, the expected file appears within the top 5 semantic results for at least 85% of representative developer questions.
- **SC-004**: 100% of search, browse, and file-read operations return content only from the selected project key during isolation tests across multiple indexed projects.
- **SC-005**: 90% of first-time users can start indexing, run a search, and open a file from the explorer without assistance in a usability walkthrough.
- **SC-006**: A repeated full re-index of an unchanged project does not increase the stored searchable record count for that project by more than 1%.
- **SC-007**: A single-file refresh updates searchable results for the changed file within 1 minute for files up to 1 MB in size.
- **SC-008**: The full index-search-browse workflow succeeds while the host machine has no internet connectivity.

## Assumptions

- The primary human users are developers or operators who need to understand local repositories before making changes.
- Each project key maps to one active local source path at a time.
- The initial release targets a single-machine deployment where the UI and API are served from the same local application.
- Search snippets may contain only a relevant excerpt rather than the entire file, while full-file viewing remains available through a separate read action.
- Authentication and authorization are handled outside this feature’s scope because the system is intended for trusted local environments.
