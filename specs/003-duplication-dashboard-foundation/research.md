# Phase 0 Research: Code Quality Dashboard Foundation

## Decision 1: Keep duplication analysis inside the existing ASP.NET Core host

- **Decision**: Implement the new quality-analysis commands, queries, contracts, and Blazor components inside the existing `SemanticSearch.WebApi` host and current Clean Architecture layers.
- **Rationale**: The repository already serves both API and UI from one ASP.NET Core process. Reusing that host preserves routing, deployment, background initialization, logging, and content-root-based path handling.
- **Alternatives considered**:
  - Separate frontend or dashboard host: rejected because it adds another deployment/runtime without solving a current product problem.
  - Standalone CLI analysis tool: rejected because the feature requires a live dashboard and reusable API contracts.

## Decision 2: Persist quality analysis snapshots and findings in SQLite

- **Decision**: Store quality analysis runs, latest summary snapshots, duplicate findings, and snippet snapshots in the existing SQLite database under the application content root.
- **Rationale**: The current workspace already persists project metadata and searchable segments in SQLite. Keeping quality results in the same local store avoids recomputing every dashboard load, supports consistent refresh behavior, and preserves offline portability.
- **Alternatives considered**:
  - Recompute quality findings on every dashboard request: rejected because it makes dashboard latency unpredictable and wastes CPU.
  - Write results to flat JSON files: rejected because it fragments persistence and complicates cleanup and querying.

## Decision 3: Detect structural clones from normalized Roslyn syntax fingerprints

- **Decision**: Parse eligible indexed source files with `Microsoft.CodeAnalysis.CSharp`, normalize identifiers and literal values to canonical tokens while preserving control-flow and member-access structure, and fingerprint method-sized or block-sized syntax regions to surface exact logic clones.
- **Rationale**: Roslyn gives deterministic syntax trees for C# and allows structural equivalence checks after normalization. Fingerprinting normalized regions produces stable clone groups that are easy to persist and compare across re-runs.
- **Alternatives considered**:
  - Raw text hashing: rejected because renamed variables or changed literals would hide true logic clones.
  - Full semantic-model analysis: rejected for Phase 1 because it adds compilation complexity beyond what the current user stories require.

## Decision 4: Detect semantic clones from existing embeddings with bounded pair generation

- **Decision**: Reuse persisted search segments and their local embeddings, generate candidate pairs within the same project using segment-length bucketing and nearest-neighbor comparison, compute cosine similarity for those candidates, and keep only pairs scoring `>= 0.95`.
- **Rationale**: The repository already stores embeddings for searchable segments. Reusing them avoids a second embedding pass, while bounded candidate generation controls pair explosion better than exhaustive all-pairs comparison.
- **Alternatives considered**:
  - Exhaustive all-pairs similarity across every segment: rejected because quadratic growth becomes expensive for large projects.
  - Introducing a dedicated ANN/vector database: rejected because it adds new operational/runtime dependencies that conflict with the local offline scope.

## Decision 5: Expose both targeted analysis endpoints and unified dashboard retrieval endpoints

- **Decision**: Provide targeted structural and semantic analysis endpoints for explicit runs, plus unified summary, findings-list, and comparison-detail endpoints for the dashboard and follow-on automation.
- **Rationale**: This satisfies the feature requirement for dedicated duplication-detection endpoints while giving the dashboard one stable way to read the latest persisted quality state.
- **Alternatives considered**:
  - Analysis-only endpoints with no persisted summary API: rejected because the dashboard would need to orchestrate and merge multiple raw responses itself.
  - Summary-only endpoint with hidden internal analyses: rejected because the feature brief explicitly calls for structural and semantic duplication endpoints.

## Decision 6: Use a lightweight chart and diff presentation approach

- **Decision**: Render the quality breakdown with a locally bundled Chart.js doughnut chart and show duplicate evidence in a Blazor modal with side-by-side snippets and line-level highlight metadata supplied by the API.
- **Rationale**: The feature needs visual summary and readable comparisons, not a full editor. A lightweight chart plus focused diff presentation keeps the UI understandable and avoids a heavy client-side code editor dependency.
- **Alternatives considered**:
  - Building custom SVG charts from scratch: rejected because it adds avoidable UI effort for a solved visualization problem.
  - Embedding Monaco or a full IDE widget: rejected because it adds payload and complexity disproportionate to the read-only comparison requirement.

## Decision 7: Keep grade and severity mapping deterministic and data-driven

- **Decision**: Implement the grade bands and severity thresholds defined in the specification as centralized rules shared by summary generation and finding presentation.
- **Rationale**: The dashboard hero, table, and detail modal must stay internally consistent. Centralized rule evaluation prevents the API and UI from drifting.
- **Alternatives considered**:
  - Hardcoding grade/severity logic separately in UI and backend: rejected because it risks inconsistent results.
  - Deferring grade and severity mapping to a later phase: rejected because the hero and findings table require those values now.

## Decision 8: Use .NET-native automated tests for API, analysis logic, and UI states

- **Decision**: Cover clone normalization/fingerprinting, duplicate-pair filtering, API contracts, and dashboard/modal states with xUnit, FluentAssertions, ASP.NET Core integration tests, bUnit component tests, and contract snapshots.
- **Rationale**: The repository is fully .NET-based, and the constitution places heavy weight on automated validation of business logic and public APIs.
- **Alternatives considered**:
  - Manual validation only: rejected because duplicate detection logic and summary consistency are too error-prone.
  - Introducing a separate JavaScript-first UI test stack: rejected because the current UI technology is Blazor, not a standalone SPA.
