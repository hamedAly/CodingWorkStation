# Phase 0 Research: Local AI Tech Lead Assistant

## Decision 1: Keep the AI assistant inside the existing quality dashboard host and slice

- **Decision**: Implement the assistant as an extension of the current `Quality` feature area inside `SemanticSearch.WebApi`, `SemanticSearch.Application`, and `SemanticSearch.Infrastructure`.
- **Rationale**: The repository already exposes quality summary, findings, and duplicate comparison endpoints from one ASP.NET Core host, and the Blazor dashboard already consumes those contracts through `WorkspaceApiClient`. Reusing that path preserves routing, content-root path resolution, structured error handling, and the existing developer workflow.
- **Alternatives considered**:
  - Separate AI microservice: rejected because it adds deployment and coordination overhead to a strictly local feature.
  - Direct component-to-infrastructure calls only: rejected because it bypasses the current controller/contracts boundary used everywhere else in the quality UI.

## Decision 2: Load model weights once, then create request-scoped inference contexts

- **Decision**: Register a singleton infrastructure service that loads the GGUF weights once during application startup and reuses those weights to create fresh inference contexts and executors per request.
- **Rationale**: LLamaSharp's documented startup pattern loads `LLamaWeights` from `ModelParams`, then creates a context and executor from those weights. Reusing the loaded weights avoids repeated file I/O and RAM churn, while request-scoped contexts avoid shared executor state bleeding across concurrent or sequential recommendation sessions.
- **Alternatives considered**:
  - Load the model on every request: rejected because it multiplies latency and memory pressure.
  - Share one singleton executor across all requests: rejected because executor state is conversation-specific and would complicate concurrency, cancellation, and prompt isolation.

## Decision 3: Stream assistant output over the existing HTTP boundary using NDJSON

- **Decision**: Keep the current `WorkspaceApiClient` pattern and expose streaming assistant endpoints that return newline-delimited JSON (`application/x-ndjson`) events while the underlying AI service yields `IAsyncEnumerable<string>` token fragments.
- **Rationale**: The current Blazor UI already talks to thin controllers through `HttpClient`. NDJSON is easy to produce from an async token stream, easy to consume with `ResponseHeadersRead`, and easier to test and replay than raw chunked text. It also avoids adding SignalR or custom JavaScript just to receive partial tokens.
- **Alternatives considered**:
  - Wait for a full response before returning: rejected because it eliminates the core streaming requirement.
  - Server-Sent Events or SignalR: rejected because they add more moving parts than this bounded local streaming workflow needs.

## Decision 4: Add dedicated assistant options under the existing semantic search configuration

- **Decision**: Extend `SemanticSearchOptions` with assistant-specific settings for the GGUF model path, context size, maximum generated tokens, anti-prompts, CPU thread count, maximum duplicate-snippet characters, and optional startup readiness behavior.
- **Rationale**: The repository already centralizes model, database, UI, and quality settings in `SemanticSearchOptions`. Keeping assistant settings in the same configuration object preserves content-root path handling and allows environment-specific tuning without scattering constants through the codebase.
- **Alternatives considered**:
  - Hardcoded inference settings: rejected because model size, latency, and available CPU resources vary by machine.
  - Separate settings file outside the existing options tree: rejected because it fragments deployment and makes configuration drift more likely.

## Decision 5: Build prompts from existing quality read models with bounded duplicate snippets

- **Decision**: Construct two prompt templates: one for project-level action plans using the current quality summary, and one for duplicate-specific refactoring guidance using the current duplicate comparison payload after bounding snippet length.
- **Rationale**: The existing quality APIs already provide the exact project metrics and comparison data the assistant needs. Reusing those read models prevents duplicate data-loading logic and keeps prompt construction deterministic. Bounding the duplicate code samples is necessary to protect latency and context budget when files are large.
- **Alternatives considered**:
  - Send entire source files to the model: rejected because it creates unpredictable prompt size and slows local inference.
  - Let the UI assemble prompts itself: rejected because prompt templates and truncation rules belong in the backend where data validation already lives.

## Decision 6: Render streamed content with a shared Markdig pipeline and code highlighting extension

- **Decision**: Parse the accumulated markdown in the Blazor UI with a shared Markdig pipeline and enable syntax highlighting for fenced C# code blocks using the Markdig-compatible highlighting extension referenced by the Markdig package documentation.
- **Rationale**: Markdig is the requested markdown renderer and supports advanced extensions and fenced code blocks. Rendering the accumulated markdown after each chunk keeps partial output readable, while code highlighting makes duplicate-fix proposals usable without forcing users to copy them elsewhere first.
- **Alternatives considered**:
  - Render raw text in a `<pre>` block: rejected because it makes long responses and code samples hard to review.
  - Delay markdown conversion until completion: rejected because it degrades the value of streaming.

## Decision 7: Enforce one active stream per assistant surface with cancellation and partial-output retention

- **Decision**: Treat the dashboard hero assistant and diff-modal assistant as separate surfaces, each allowing only one active request at a time, with explicit cancellation tokens and persistent partial output after interruption or failure.
- **Rationale**: The specification requires overlapping requests to be managed clearly and partial output to remain visible. Per-surface cancellation keeps the UI predictable, avoids interleaved token streams, and lets users close or restart a session without losing already generated guidance.
- **Alternatives considered**:
  - Allow unlimited concurrent requests in the same panel: rejected because it creates ambiguous UI state and difficult token ownership.
  - Clear all partial output on cancellation: rejected because the spec explicitly requires preserving already shown content.

## Decision 8: Use controller-thin orchestration with application-layer validation and infrastructure-only inference

- **Decision**: Keep controller actions limited to receiving the request, invoking one application-layer query/service path, and streaming the response. Keep model loading, inference, and prompt-size guardrails in infrastructure services, with input validation in FluentValidation.
- **Rationale**: This mirrors the existing quality controller style and complies with the constitution's thin-controller and validation-pipeline rules. It also isolates LLamaSharp-specific code to the infrastructure layer.
- **Alternatives considered**:
  - Place inference logic directly in controllers: rejected because it violates current layering rules.
  - Move LLamaSharp types into application contracts: rejected because it leaks infrastructure concerns upward.

## Decision 9: Surface assistant readiness explicitly

- **Decision**: Add a lightweight assistant readiness contract so the UI can show whether the local model is ready, unavailable, or failed to initialize before or during generation attempts.
- **Rationale**: Startup availability is an explicit edge case in the spec. A small readiness contract lets the UI show disabled/loading/error states intentionally instead of discovering failure only after opening a stream.
- **Alternatives considered**:
  - Infer readiness only from failed stream requests: rejected because it delays clear user feedback and complicates button-state management.
  - Assume readiness whenever the app starts: rejected because model files may be missing or incompatible.

## Decision 10: Test streaming behavior at three levels

- **Decision**: Cover prompt building and truncation with unit tests, streaming controller behavior with integration tests that verify ordered NDJSON events, and assistant panel rendering with bUnit tests that verify loading, progressive rendering, error states, and preserved partial output.
- **Rationale**: The feature's risk is not only in prompt generation but also in incremental delivery and UI state handling. Splitting tests by layer preserves fast feedback while still validating the public streaming contract.
- **Alternatives considered**:
  - Manual testing only: rejected because streaming edge cases are easy to miss and regress.
  - UI-only testing without contract validation: rejected because the streaming protocol itself is part of the feature's external behavior.
