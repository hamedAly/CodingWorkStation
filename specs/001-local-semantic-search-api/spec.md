# Feature Specification: Local Semantic Search Web API

**Feature Branch**: `001-local-semantic-search-api`  
**Created**: 2026-03-14  
**Status**: Draft  
**Input**: User description: "Build a centralized, 100% local Semantic Search Web API using .NET 10. The API is hosted on IIS as a continuous background service, consumed by an AI Agent to search and understand local codebases before modifying them. It requires local embeddings, a local vector database with project-level partitioning, and endpoints for indexing, querying, and status."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Index a Local Codebase (Priority: P1)

An AI Agent sends a request to the Semantic Search API with a local project path and a unique project key. The system traverses the project directory, ignores build and dependency folders, reads all relevant source files, chunks the content logically, generates embeddings locally, and stores the vectors in the local vector database tagged with the project key. The AI Agent receives a confirmation that indexing has begun or completed.

**Why this priority**: Without indexing, there is no searchable data. This is the foundational capability that all other features depend on.

**Independent Test**: Can be fully tested by sending a POST request with a valid project path and project key, then verifying that the vector database contains chunks tagged with that project key.

**Acceptance Scenarios**:

1. **Given** a valid project path containing source files and a unique project key, **When** the AI Agent sends a POST to `/api/search/index`, **Then** the system traverses the directory, skips excluded folders (bin, obj, node_modules, .git, dist), chunks relevant source files, generates local embeddings, and upserts them into the vector database tagged with the project key.
2. **Given** a project path that does not exist, **When** the AI Agent sends a POST to `/api/search/index`, **Then** the system returns an appropriate error response indicating the path is invalid.
3. **Given** a project path that contains no relevant source files, **When** the AI Agent sends a POST to `/api/search/index`, **Then** the system returns a response indicating zero files were indexed.
4. **Given** a project that was previously indexed, **When** the AI Agent sends another indexing request with the same project key, **Then** the system upserts (updates existing and adds new) chunks in the vector database rather than creating duplicates.

---

### User Story 2 - Search an Indexed Codebase (Priority: P1)

An AI Agent sends a natural language query along with a project key to the Semantic Search API. The system generates an embedding for the query, searches the vector database filtered by the project key, and returns the most semantically relevant code snippets with file paths, relevance scores, and line numbers.

**Why this priority**: Search is the primary value proposition of the system. Without it, the indexed data cannot be utilized.

**Independent Test**: Can be fully tested by first indexing a known codebase, then sending a query and verifying that the returned snippets are semantically relevant and contain the expected metadata fields.

**Acceptance Scenarios**:

1. **Given** an indexed project with project key "my-project" and the query "user authentication logic", **When** the AI Agent sends a POST to `/api/search/query` with topK=5, **Then** the system returns up to 5 results, each containing FilePath, RelevanceScore, Snippet, StartLine, and EndLine.
2. **Given** multiple indexed projects, **When** the AI Agent searches with a specific project key, **Then** results are strictly filtered to only that project's data — no cross-project leakage occurs.
3. **Given** a project key that has not been indexed, **When** the AI Agent sends a search query, **Then** the system returns an empty results array or an informative message indicating no indexed data exists for that key.
4. **Given** a search query, **When** the AI Agent specifies topK=10, **Then** the system returns at most 10 results, ordered by descending relevance score.

---

### User Story 3 - Check Indexing Status (Priority: P2)

An AI Agent queries the status endpoint to determine whether a particular project has been indexed, how many files and chunks are stored, and when the last indexing occurred. This allows the agent to decide whether to re-index or proceed with searching.

**Why this priority**: Status checking is important for workflow automation but the core indexing and search capabilities must work first.

**Independent Test**: Can be fully tested by indexing a project, then calling the status endpoint and verifying the returned statistics match the expected state.

**Acceptance Scenarios**:

1. **Given** a project that has been indexed, **When** the AI Agent sends a GET to `/api/search/status/{projectKey}`, **Then** the system returns IsIndexed=true, TotalFiles, TotalChunks, and LastUpdated with accurate values.
2. **Given** a project key that has never been indexed, **When** the AI Agent sends a GET to `/api/search/status/{projectKey}`, **Then** the system returns IsIndexed=false with TotalFiles=0 and TotalChunks=0.

---

### User Story 4 - Continuous Operation Under IIS (Priority: P2)

The Semantic Search API runs continuously as a background service hosted on IIS. It uses absolute paths for the vector database and embedding model files, ensuring reliable operation regardless of IIS worker process recycling or path context changes.

**Why this priority**: Reliable IIS hosting is essential for production use but can be validated after core functionality works.

**Independent Test**: Can be tested by deploying the API to IIS, verifying it starts correctly, sending indexing and search requests, and confirming the vector database and model files are accessed via absolute paths.

**Acceptance Scenarios**:

1. **Given** the API is deployed to IIS, **When** the IIS application pool starts, **Then** the API initializes successfully, loads the embedding model, and is ready to accept requests.
2. **Given** the API is running on IIS, **When** the IIS worker process recycles, **Then** the API restarts and retains access to all previously indexed data via absolute file paths.

---

### User Story 5 - 100% Offline Operation (Priority: P1)

The entire system operates without any external network calls. Embedding generation, vector storage, and search all happen locally on the machine. No paid APIs or cloud services are required.

**Why this priority**: This is a hard constraint — the system must be fully offline. If this fails, the entire product fails.

**Independent Test**: Can be tested by disabling network connectivity and performing a full index-then-search workflow, verifying everything completes successfully.

**Acceptance Scenarios**:

1. **Given** the machine has no internet connectivity, **When** the AI Agent indexes a project and then searches it, **Then** the entire workflow completes successfully without errors.
2. **Given** the system is running, **When** network traffic is monitored, **Then** no outbound requests to external services are observed.

---

### Edge Cases

- What happens when the project path contains thousands of files? The system should handle large codebases without crashing (may take longer but should complete).
- What happens when a source file is binary or contains non-text content? The system should skip or gracefully handle non-text files.
- What happens when the project key is an empty string or null? The system should reject the request with a validation error.
- What happens when a source file is locked by another process? The system should skip the file and continue indexing, logging a warning.
- What happens when the vector database file is corrupted? The system should report a meaningful error rather than crashing silently.
- What happens when two concurrent indexing requests target the same project key? The system should handle this gracefully without data corruption.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST provide an indexing endpoint (`POST /api/search/index`) that accepts a project path and project key, traverses the directory, and indexes all relevant source files.
- **FR-002**: System MUST ignore the following directories during indexing: `bin`, `obj`, `node_modules`, `.git`, `dist`, and any other common build/dependency output folders.
- **FR-003**: System MUST support indexing at minimum these file extensions: `.cs`, `.md`, `.ts`, `.js`, `.json`, `.xml`, `.yaml`, `.yml`, `.razor`, `.html`, `.css`, `.sql`, `.py`, `.java`, `.cpp`, `.h`, `.go`, `.rs`.
- **FR-004**: System MUST chunk source file content into logical segments (preserving context such as class/method boundaries where possible) before generating embeddings.
- **FR-005**: System MUST generate text embeddings using a local, offline embedding model — no external API calls permitted.
- **FR-006**: System MUST store all generated embeddings in a local, file-based vector database, tagged with the provided project key for partitioning.
- **FR-007**: System MUST upsert chunks on re-indexing — updating existing chunks and adding new ones rather than creating duplicates.
- **FR-008**: System MUST provide a search endpoint (`POST /api/search/query`) that accepts a query string, project key, and optional topK parameter.
- **FR-009**: System MUST generate an embedding for the search query using the same local model used for indexing.
- **FR-010**: System MUST filter search results strictly by the provided project key — no cross-project data leakage.
- **FR-011**: System MUST return search results containing: FilePath, RelevanceScore, Snippet, StartLine, and EndLine for each match.
- **FR-012**: System MUST order search results by descending relevance score and limit results to the specified topK (default: 5).
- **FR-013**: System MUST provide a status endpoint (`GET /api/search/status/{projectKey}`) that returns IsIndexed, TotalFiles, TotalChunks, and LastUpdated.
- **FR-014**: System MUST use absolute file paths (derived from the hosting environment's content root) for all local storage: vector database files and embedding model files.
- **FR-015**: System MUST validate all incoming request payloads — rejecting empty or null project keys, non-existent project paths, and empty queries with appropriate error responses.
- **FR-016**: System MUST skip files that cannot be read (locked, permission denied, binary) during indexing and continue processing remaining files, logging warnings for skipped files.
- **FR-017**: System MUST use Clean Architecture principles with Dependency Injection and interface segregation.
- **FR-018**: System MUST be deployable to IIS as a continuously running background service.

### Key Entities

- **Project**: Represents an indexed codebase. Identified by a unique ProjectKey. Tracks metadata including total files, total chunks, and last indexing timestamp.
- **SourceFile**: A single file within a project. Has a file path, file extension, and belongs to a Project.
- **Chunk**: A logical segment of a source file's content. Contains the text snippet, start line, end line, source file path, and belongs to a Project via ProjectKey.
- **Embedding**: A vector representation of a Chunk. Stored alongside the chunk in the vector database, used for similarity search.
- **SearchResult**: A result object returned by the search endpoint. Contains FilePath, RelevanceScore, Snippet, StartLine, and EndLine.
- **IndexingStatus**: Metadata about a project's indexing state. Contains IsIndexed, TotalFiles, TotalChunks, and LastUpdated.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The AI Agent can index a medium-sized codebase (500+ files) and receive a successful response within 5 minutes.
- **SC-002**: After indexing, the AI Agent can perform a semantic search and receive relevant results within 2 seconds for a typical query.
- **SC-003**: Search results are semantically relevant — for known code patterns, the correct file appears in the top 3 results at least 80% of the time.
- **SC-004**: The system operates fully offline — a complete index-then-search workflow succeeds with no network connectivity.
- **SC-005**: All three endpoints (index, query, status) return well-formed responses with correct data for valid inputs and meaningful error messages for invalid inputs.
- **SC-006**: The system correctly isolates project data — searching with Project A's key never returns results from Project B.
- **SC-007**: The API runs stably under IIS for 24+ hours without memory leaks or crashes during continuous use.
- **SC-008**: Re-indexing a previously indexed project updates the data without creating duplicate chunks.

## Assumptions

- The consuming AI Agent is the sole consumer of this API; there is no human-facing UI.
- The embedding model (e.g., all-MiniLM-L6-v2 via ONNX) is pre-downloaded and placed in a known directory on the server before first use.
- The host machine has sufficient disk space and memory to store vector indexes for multiple codebases simultaneously.
- IIS is pre-configured on the host machine; IIS installation/configuration is outside the scope of this feature (though deployment instructions will be provided).
- Authentication is not required for this API since it runs on a trusted local network. Access control, if needed, would be handled at the network/IIS level.
- File extensions for indexing can be extended in the future, but the initial set covers the most common source file types.
- The default `topK` value of 5 is sufficient for most queries; the caller can override this as needed.
