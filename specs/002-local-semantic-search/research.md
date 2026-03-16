# Phase 0 Research: Local Semantic Search Workspace

## Decision 1: Use the existing ASP.NET Core host as a single UI + API runtime

- **Decision**: Keep `src/SemanticSearch.WebApi` as the single IIS-hosted application and add the web UI as Blazor components/pages inside that host.
- **Rationale**: One host keeps routing, deployment, logging, absolute-path handling, and background services in one process. It also matches the feature assumption that UI and API are served from the same local application.
- **Alternatives considered**:
  - Separate React/Vite frontend: rejected because it adds a second build/deploy pipeline and conflicts with the local single-host goal.
  - Separate Blazor frontend project: rejected because it adds a second ASP.NET host without adding meaningful product value.

## Decision 2: Keep embeddings fully local with ONNX `all-MiniLM-L6-v2`

- **Decision**: Continue with the local `all-MiniLM-L6-v2` ONNX model and tokenizer files stored beneath the application content root.
- **Rationale**: The model is already reflected in the repository structure, has acceptable size for local deployment, and balances semantic quality with runtime cost for a single-machine solution.
- **Alternatives considered**:
  - Larger local embedding models: rejected because they increase memory, disk, and indexing latency with limited value for the initial release.
  - Cloud embeddings: rejected because the feature requires 100% offline operation.
  - Semantic Kernel abstraction first: rejected because it adds another abstraction layer without solving a current architecture gap.

## Decision 3: Use a SQLite-backed local vector store with project-scoped filtering

- **Decision**: Store projects, indexed files, searchable segments, and embedding vectors in a single SQLite database file under the application content root, with all read/write operations filtered by `ProjectKey`.
- **Rationale**: SQLite is already present in the solution, matches the file-based offline requirement, is easy to ship on IIS, and keeps operational complexity low. Project-key isolation can be enforced consistently at the storage boundary.
- **Alternatives considered**:
  - `sqlite-vec` or another native SQLite vector extension: rejected for the initial design because native extension deployment under IIS adds packaging risk.
  - Qdrant local: rejected because it introduces another service/process to manage.
  - ChromaDB local: rejected because it adds a Python runtime and cross-process coordination.

## Decision 4: Serialize indexing per project key through the background worker

- **Decision**: Use the existing background indexing channel/worker pattern and extend it with per-project-key concurrency control, persisted run status, and single-file refresh commands.
- **Rationale**: The repository already contains background indexing primitives. Extending them preserves the current architecture while preventing conflicting writes for the same project key.
- **Alternatives considered**:
  - Run indexing directly inside controller actions: rejected because it violates thin-controller and reliability goals for long-running work.
  - Use an external job processor: rejected because it complicates local deployment and is unnecessary for the current single-node scope.

## Decision 5: Model the explorer from indexed file metadata, not live recursive reads

- **Decision**: Build the project tree from indexed file metadata and resolve full-file reads against the stored project root plus validated relative paths.
- **Rationale**: This keeps the explorer aligned with what is actually searchable, supports predictable project isolation, and avoids re-scanning the entire filesystem for every browse request.
- **Alternatives considered**:
  - Live filesystem tree for every request: rejected because it can drift from indexed state and adds avoidable filesystem cost.
  - Persist a fully separate tree document: rejected because it duplicates information that can be derived from indexed files.

## Decision 6: Use .NET-native tests for the UI and API

- **Decision**: Add `xUnit` for application/integration testing, `WebApplicationFactory` for API flows, and `bUnit` for Blazor component coverage.
- **Rationale**: The solution is fully .NET-based, so .NET-native test tooling gives fast feedback without adding a second test ecosystem.
- **Alternatives considered**:
  - Jest/Playwright-first stack: rejected because the chosen UI is Blazor, not React.
  - Manual-only verification: rejected because the constitution requires strong automated coverage on critical flows.

## Decision 7: Keep syntax highlighting local and lightweight

- **Decision**: Provide basic code readability through local styles plus extension-based formatting, with optional lightweight local highlighting assets bundled into `wwwroot`.
- **Rationale**: The feature requires readable code snippets but does not need a heavy client-side editor in the initial release.
- **Alternatives considered**:
  - Monaco editor everywhere: rejected because it adds substantial payload and complexity for a read-focused experience.
  - Plain text with no formatting: rejected because it weakens the usability of search results and file reading.
