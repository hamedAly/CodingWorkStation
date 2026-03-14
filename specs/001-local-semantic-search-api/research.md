# Phase 0 Research: Local Semantic Search Web API

**Branch**: `001-local-semantic-search-api` | **Date**: 2026-03-14 | **Spec**: [spec.md](spec.md) | **Plan**: [plan.md](plan.md)

---

## Topic 1: Local ONNX Embedding in .NET

### Decision

Use **Microsoft.ML.OnnxRuntime** (v1.24.3) for direct ONNX inference and **Microsoft.ML.Tokenizers** (v2.0.0) for WordPiece tokenization. Load the `all-MiniLM-L6-v2` ONNX model as a **singleton** `InferenceSession` registered in DI. Tokenize with a `WordPieceTokenizer` loaded from the model's `tokenizer.json`/`vocab.txt` files. Extract the 384-dimensional embedding via **mean pooling** over token embeddings, then **L2-normalize** for cosine similarity.

### Rationale

- ONNX Runtime is the official, Microsoft-supported inference engine with the best .NET integration. Direct `InferenceSession` usage avoids the overhead and complexity of the ML.NET pipeline (`MLContext` → `ApplyOnnxModel`) which is designed for tabular ML workflows, not sentence embedding extraction.
- `Microsoft.ML.Tokenizers` 2.0.0 provides a native .NET WordPiece tokenizer that matches the BERT-based tokenization required by all-MiniLM-L6-v2, eliminating any Python/native dependency for tokenization.
- Singleton lifecycle for `InferenceSession` is critical: creating sessions is expensive (model loading + graph optimization). ONNX Runtime sessions are **thread-safe** for concurrent `Run()` calls, making singleton the optimal pattern.

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.ML.OnnxRuntime` | 1.24.3 | ONNX model inference (CPU) |
| `Microsoft.ML.Tokenizers` | 2.0.0 | WordPiece tokenizer for BERT models |

### Model Files (Pre-downloaded from HuggingFace)

Download from: `https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2`

Required files to place in `models/all-MiniLM-L6-v2/`:

| File | Source Path | Size | Purpose |
|------|-------------|------|---------|
| `model.onnx` | `/onnx/model.onnx` | ~90 MB | The ONNX model (float32, unoptimized) |
| `tokenizer.json` | `/tokenizer.json` (repo root) | ~700 KB | HuggingFace tokenizer config (contains vocab + rules) |
| `vocab.txt` | `/vocab.txt` (repo root) | ~232 KB | WordPiece vocabulary (30,522 tokens) |

**Note**: Optimized variants exist (`model_O1.onnx` through `model_O4.onnx`, ~45-90 MB) and quantized variants (`model_qint8_avx512.onnx`, ~23 MB). Start with `model.onnx` for correctness; consider `model_O2.onnx` or quantized variants for production performance optimization.

### Inference Pipeline (Pseudocode)

```csharp
// 1. Tokenization (WordPiece)
//    - Load tokenizer from vocab.txt or tokenizer.json
//    - Tokenize: [CLS] + tokens + [SEP], pad/truncate to max 256 tokens
//    - Produce: input_ids, attention_mask, token_type_ids (all zeros for single-sentence)

// 2. ONNX Inference
//    - Create OrtValue tensors from token arrays: shape [1, sequence_length]
//    - Run session with inputs: {"input_ids", "attention_mask", "token_type_ids"}
//    - Output: "last_hidden_state" tensor of shape [1, sequence_length, 384]

// 3. Mean Pooling
//    - Multiply each token embedding by its attention_mask value (0 or 1)
//    - Sum along sequence dimension → [1, 384]
//    - Divide by sum of attention_mask → [1, 384]

// 4. L2 Normalization
//    - Compute L2 norm: sqrt(sum(x_i^2))
//    - Divide each element by the norm → unit vector in 384-dim space
//    - Pre-normalized vectors enable cosine similarity via simple dot product
```

### Model Lifecycle Management

```
Registration: services.AddSingleton<InferenceSession>(sp => {
    var modelPath = Path.Combine(contentRootPath, "models", "all-MiniLM-L6-v2", "model.onnx");
    var options = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
    return new InferenceSession(modelPath, options);
});

Disposal: InferenceSession implements IDisposable — the DI container handles cleanup on shutdown.
Thread safety: InferenceSession.Run() is thread-safe; no locking needed.
```

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|-------------|
| **ML.NET pipeline** (`MLContext.Transforms.ApplyOnnxModel`) | Designed for tabular ML workflows; awkward for extracting intermediate tensor outputs (last_hidden_state); adds unnecessary ML.NET dependency overhead |
| **Semantic Kernel / Microsoft.Extensions.AI** | These are orchestration layers that delegate to backends; they don't provide direct model hosting — would still need ONNX Runtime underneath |
| **SharpToken / custom tokenizer** | SharpToken is BPE-only (GPT models); all-MiniLM-L6-v2 uses WordPiece. Microsoft.ML.Tokenizers supports both natively |
| **Per-request InferenceSession** | ~100-500ms session creation per request; completely unacceptable for P95 < 2s search latency target |

---

## Topic 2: SQLite as a Vector Database in .NET

### Decision

Use a **hybrid approach**: **`Microsoft.Data.Sqlite`** (v10.0.5) for all relational storage (chunks, metadata, project partitioning) with embeddings stored as **BLOBs**, and **brute-force cosine similarity computed in C#** after retrieval. Do NOT use `sqlite-vec` for the initial implementation.

### Rationale

- **sqlite-vec** (v0.1.6, pre-v1, alpha releases still shipping as of v0.1.7-alpha.10) is a native C extension with no official .NET/NuGet binding. Loading a native SQLite extension from `Microsoft.Data.Sqlite` requires `connection.LoadExtension()` with a platform-specific `.dll`/`.so` path, which is fragile under IIS deployment and adds significant operational complexity.
- For the target scale (codebases of 500-5000 files, producing ~5,000-50,000 chunks per project), brute-force cosine similarity in C# over in-memory float arrays is **fast enough**. Computing cosine similarity over 50,000 × 384-dim vectors takes ~10-50ms on modern hardware — well within the 2-second P95 target.
- `Microsoft.Data.Sqlite` is the official Microsoft ADO.NET provider, ships with .NET 10, and integrates cleanly with IIS hosting and absolute path configuration.
- If performance becomes an issue at larger scales, `sqlite-vec` can be introduced later as an optimization without changing the storage schema (embeddings already stored as BLOBs).

### Schema Design

```sql
-- Core chunks table with project partitioning
CREATE TABLE IF NOT EXISTS Chunks (
    Id TEXT PRIMARY KEY,                           -- Deterministic: SHA256(ProjectKey + FilePath + StartLine)
    ProjectKey TEXT NOT NULL,
    FilePath TEXT NOT NULL,
    StartLine INTEGER NOT NULL,
    EndLine INTEGER NOT NULL,
    Content TEXT NOT NULL,
    Embedding BLOB NOT NULL,                       -- float32[384] as raw bytes (1,536 bytes per chunk)
    CreatedAt TEXT NOT NULL,                        -- ISO 8601
    UNIQUE(ProjectKey, FilePath, StartLine)
);

-- Index for project-scoped queries (required for partition filtering)
CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey ON Chunks(ProjectKey);

-- Index for upsert lookups
CREATE INDEX IF NOT EXISTS IX_Chunks_ProjectKey_FilePath ON Chunks(ProjectKey, FilePath);

-- Project metadata table for status endpoint
CREATE TABLE IF NOT EXISTS ProjectMetadata (
    ProjectKey TEXT PRIMARY KEY,
    TotalFiles INTEGER NOT NULL DEFAULT 0,
    TotalChunks INTEGER NOT NULL DEFAULT 0,
    LastUpdated TEXT NOT NULL                       -- ISO 8601
);
```

### Upsert Strategy

```sql
-- Upsert chunks using deterministic Id
INSERT INTO Chunks (Id, ProjectKey, FilePath, StartLine, EndLine, Content, Embedding, CreatedAt)
VALUES (@Id, @ProjectKey, @FilePath, @StartLine, @EndLine, @Content, @Embedding, @CreatedAt)
ON CONFLICT(Id) DO UPDATE SET
    EndLine = excluded.EndLine,
    Content = excluded.Content,
    Embedding = excluded.Embedding,
    CreatedAt = excluded.CreatedAt;

-- Delete stale chunks for files that no longer exist or changed structure
DELETE FROM Chunks WHERE ProjectKey = @ProjectKey AND FilePath = @FilePath AND Id NOT IN (@NewChunkIds);
```

### Embedding Storage Format

```csharp
// Store: float[] → byte[] (1,536 bytes for 384 floats)
byte[] embeddingBlob = MemoryMarshal.AsBytes(embedding.AsSpan()).ToArray();

// Retrieve: byte[] → float[]
float[] embedding = MemoryMarshal.Cast<byte, float>(blobSpan).ToArray();
```

### Search Implementation (Brute-Force in C#)

```csharp
// 1. Retrieve all chunks + embeddings for a project
//    SELECT Id, FilePath, StartLine, EndLine, Content, Embedding
//    FROM Chunks WHERE ProjectKey = @ProjectKey

// 2. Compute cosine similarity in C# (embeddings are pre-normalized → dot product)
//    similarity = sum(query[i] * chunk[i]) for i in 0..383

// 3. Sort by similarity descending, take topK
```

### Performance Characteristics

| Scale (chunks per project) | Retrieval + Similarity | Meets P95 < 2s? |
|----------------------------|----------------------|------------------|
| 5,000 (small project) | ~5-10ms | Yes |
| 25,000 (medium project) | ~25-50ms | Yes |
| 50,000 (large project) | ~50-100ms | Yes |
| 100,000+ | ~200-500ms | Yes, but consider sqlite-vec |
| 500,000+ | >1s | Needs sqlite-vec or dedicated vector DB |

**Bottleneck analysis**: The dominant cost is embedding generation during indexing (~5-15ms per chunk), not search. For a 500-file project with ~25,000 chunks, indexing takes ~2-5 minutes; search takes <100ms.

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|-------------|
| **sqlite-vec extension** | Pre-v1, no .NET NuGet package, requires native extension loading which is fragile on IIS. Good future optimization but premature for v1 |
| **sqlite-vss** | Predecessor to sqlite-vec, officially deprecated in favor of sqlite-vec. Built on FAISS — even larger native dependency |
| **Custom `CreateFunction` for cosine similarity in SQLite** | `Microsoft.Data.Sqlite` supports `connection.CreateFunction()` for scalar UDFs, but calling a C# function per-row from SQLite for 50K rows has significant interop overhead. Brute-force in C# after bulk retrieval is faster |
| **Qdrant / Milvus / Pinecone** | External services; violate the "100% local, file-based" constraint. Qdrant can run locally but requires Docker — unnecessary complexity |
| **Entity Framework Core** | Adds abstraction overhead for simple BLOB storage. Raw ADO.NET via `Microsoft.Data.Sqlite` is more direct and performant for this use case |
| **LiteDB / RavenDB embedded** | No native vector search support; would still require brute-force approach but with less mature SQLite ecosystem |

---

## Topic 3: Code Chunking Strategies for Source Files

### Decision

Use a **line-based sliding window** chunking strategy with **configurable chunk size (default: 200 lines)** and **overlap (default: 40 lines, ~20%)**. Track `StartLine` and `EndLine` per chunk. For files smaller than the chunk size, emit a single chunk containing the entire file.

### Rationale

- **all-MiniLM-L6-v2 truncates at 256 word pieces** (tokens). Average code has ~1.5-2.5 tokens per line (depending on language). A 200-line chunk produces ~300-500 tokens, which gets truncated to 256. This is intentional: we want chunks large enough to capture meaningful context, and the model's truncation ensures we don't exceed its capacity.
- **Line-based chunking is deterministic and simple**: given a file and chunk parameters, the same chunks are always produced. This enables deterministic chunk IDs for upsert (SHA256 of ProjectKey + FilePath + StartLine).
- **Overlap (40 lines)** ensures that code constructs spanning a chunk boundary appear complete in at least one chunk, improving search recall for boundary-crossing queries.
- **Syntax-aware chunking** (splitting at class/method boundaries) was considered but rejected for v1: it requires a parser per language (C#, TypeScript, Python, etc.), dramatically increasing complexity for marginal quality gains on 18+ file types.

### Chunking Algorithm

```
Input: file content (lines[]), chunkSize=200, overlap=40
Output: list of (startLine, endLine, content)

stride = chunkSize - overlap  // 160

If lines.Length <= chunkSize:
    emit single chunk (1, lines.Length, full content)
Else:
    for startIndex = 0; startIndex < lines.Length; startIndex += stride:
        endIndex = min(startIndex + chunkSize, lines.Length)
        emit chunk (startIndex + 1, endIndex, lines[startIndex..endIndex])
        if endIndex == lines.Length: break  // last chunk
```

### Token Budget Analysis

| Content Type | Avg Tokens/Line | 200 Lines = Tokens | After 256 Truncation |
|-------------|----------------|-------------------|---------------------|
| C# code | ~2.5 | ~500 | First ~100 lines captured |
| Markdown | ~3.0 | ~600 | First ~85 lines captured |
| JSON/XML | ~2.0 | ~400 | First ~130 lines captured |
| Python | ~2.0 | ~400 | First ~130 lines captured |

**Key insight**: The 256-token limit means the model effectively "sees" the first ~85-130 lines of a 200-line chunk. The overlap ensures later portions of code also get embedded as the start of subsequent chunks.

### Handling Edge Cases

| Edge Case | Strategy |
|-----------|----------|
| File smaller than chunk size | Single chunk containing entire file |
| Empty file | Skip (no chunks emitted) |
| Binary file | Skip via detection (check for null bytes in first 8KB) |
| Very long lines (minified JS) | Line-based chunking still works; model truncation handles token limits |
| Files with only whitespace | Skip |

### Chunk ID Generation

```csharp
// Deterministic ID enables upsert without duplication
string chunkId = SHA256($"{projectKey}|{filePath}|{startLine}");
```

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|-------------|
| **Syntax-aware chunking (AST-based)** | Requires language-specific parsers for 18+ file types (C#, TS, Python, Java, etc.). Enormous implementation complexity for marginal quality improvement. Can be added later as an enhancement for specific languages |
| **Fixed token-count chunks** | Requires tokenizing the entire file first to determine boundaries, then re-tokenizing each chunk for embedding. Double tokenization cost; line-based is simpler and nearly equivalent |
| **Overlapping token windows** | Same double-tokenization problem. Token boundaries don't align with line boundaries, making StartLine/EndLine tracking imprecise |
| **Paragraph/section splitting** | Appropriate for prose documents, not source code. Code structure doesn't follow paragraph conventions |
| **Smaller chunks (50 lines)** | Too granular; loses contextual information. A single method body might span 40 lines — splitting it produces meaningless fragments |
| **No overlap** | Significantly degrades search recall for queries matching code at chunk boundaries |

---

## Topic 4: IIS Hosting for .NET 10 Web API

### Decision

Use **`BackgroundService`** (via `IHostedService`) for indexing operations, resolve all file paths via **`IWebHostEnvironment.ContentRootPath`**, and configure the IIS application pool for **AlwaysRunning** with **preload enabled**. Indexing requests return `202 Accepted` immediately; the actual work runs on the background service.

### Rationale

- IIS has a **default request timeout of 120 seconds** (configurable but not recommended to extend). Indexing a 500-file project takes 2-5 minutes, which would exceed this timeout and cause `502 Bad Gateway` errors.
- `BackgroundService` is the standard .NET pattern for long-running work in ASP.NET Core hosted on IIS. It runs on the host's lifetime, independent of individual HTTP requests.
- Absolute paths via `ContentRootPath` are essential because IIS application pools can recycle, and the working directory is not guaranteed to be the application root (it defaults to `%SystemRoot%\System32\inetsrv`).

### Path Resolution

```csharp
// In DI configuration
services.AddSingleton(sp =>
{
    var env = sp.GetRequiredService<IWebHostEnvironment>();
    var dbPath = Path.Combine(env.ContentRootPath, "data", "vectorstore.db");
    var modelPath = Path.Combine(env.ContentRootPath, "models", "all-MiniLM-L6-v2", "model.onnx");
    // Both paths are absolute, surviving IIS recycles
});
```

### Indexing Architecture

```
HTTP Request (POST /api/search/index)
    → Controller validates input, enqueues IndexingCommand to Channel<T>
    → Returns 202 Accepted with { projectKey, status: "queued" }

BackgroundService (IndexingWorker)
    → Reads from Channel<T> (unbounded, single consumer)
    → Processes: traverse → chunk → embed → store
    → Updates ProjectMetadata on completion
    → Errors logged via ILogger; status queryable via /api/search/status/{projectKey}
```

### IIS Application Pool Configuration

```xml
<!-- Required IIS configuration for continuous operation -->
<applicationPool name="SemanticSearchPool"
                 startMode="AlwaysRunning"
                 idleTimeout="00:00:00"
                 recycling.periodicRestart.time="00:00:00">
  <!-- startMode=AlwaysRunning: pool starts when IIS starts, not on first request -->
  <!-- idleTimeout=0: never shut down due to inactivity -->
  <!-- periodicRestart.time=0: disable automatic periodic recycling -->
</applicationPool>

<!-- Application-level preload -->
<application preloadEnabled="true" />
```

### IIS-Specific Concerns and Mitigations

| Concern | Mitigation |
|---------|-----------|
| Request timeout (120s default) | Background service pattern; indexing returns 202 immediately |
| Worker process recycling | Absolute file paths; SQLite DB survives recycles; in-flight indexing lost but re-requestable |
| Working directory not app root | `ContentRootPath` for all path resolution; never use `Directory.GetCurrentDirectory()` |
| App pool idle shutdown | `startMode=AlwaysRunning` + `idleTimeout=00:00:00` |
| Long-running singleton resources | `InferenceSession` singleton survives request lifecycle; disposed on host shutdown |
| Concurrent indexing requests | `Channel<T>` provides ordered, thread-safe queue; one project indexed at a time |

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|-------------|
| **Synchronous indexing in request** | Exceeds IIS 120s timeout for medium+ projects; blocks thread pool; UI shows timeout error |
| **Hangfire / Quartz.NET** | Adds external dependency for a simple background queue. `Channel<T>` + `BackgroundService` is built-in and sufficient for single-server deployment |
| **Windows Service (separate process)** | Adds deployment complexity; IIS-hosted `BackgroundService` is simpler and keeps everything in one process |
| **Kestrel-only (no IIS)** | Viable for development, but the spec requires IIS deployment for production. Both are supported — IIS reverse-proxies to Kestrel |
| **`IHostedService` with `Task.Run`** | `BackgroundService` is the recommended base class; provides `ExecuteAsync` with `CancellationToken` for graceful shutdown |

---

## Topic 5: MediatR + FluentValidation Pipeline in .NET 10

### Decision

Use **MediatR 14.1.0** with **FluentValidation 12.1.1** and **FluentValidation.DependencyInjectionExtensions 12.1.1**. Register a generic `ValidationBehavior<TRequest, TResponse>` as an open generic pipeline behavior. Register all validators and handlers via assembly scanning.

### Rationale

- MediatR 14.1.0 is the latest stable version, targets .NET 8+ (compatible with .NET 10), and provides mature CQRS/mediator patterns.
- FluentValidation 12.1.1 is the latest stable version with full .NET 8+ support.
- The `ValidationBehavior` pattern is the standard, well-documented approach for automatic request validation in MediatR pipelines — every request passes through validation before reaching its handler.

### NuGet Packages

| Package | Version | Purpose |
|---------|---------|---------|
| `MediatR` | 14.1.0 | Mediator pattern (request/response, CQRS) |
| `FluentValidation` | 12.1.1 | Validation rule definitions |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Assembly scanning for validator DI registration |

### License Key Note

MediatR 14.x (under LuckyPennySoftware) requires a **license key** for production use. Without it, a warning is logged but the library functions. Options:
- Register at [mediatr.io](https://mediatr.io/) for a license key
- Use the license key configuration: `cfg.LicenseKey = "<key>";`
- Suppress the warning: `builder.Logging.AddFilter("LuckyPennySoftware.MediatR.License", LogLevel.None);`

### Registration Pattern

```csharp
// Program.cs or DI configuration
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);

    // Register open generic pipeline behaviors (order matters)
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
});

// Register all validators from the Application assembly
builder.Services.AddValidatorsFromAssemblyContaining<IndexProjectCommandValidator>();
```

### ValidationBehavior Implementation

```csharp
public sealed class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f != null)
            .ToList();

        if (failures.Count != 0)
            throw new ValidationException(failures);

        return await next();
    }
}
```

### Example Validator

```csharp
public sealed class IndexProjectCommandValidator : AbstractValidator<IndexProjectCommand>
{
    public IndexProjectCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(128);

        RuleFor(x => x.ProjectPath)
            .NotEmpty().WithMessage("ProjectPath is required.")
            .Must(Directory.Exists).WithMessage("ProjectPath does not exist.");
    }
}
```

### Exception Handling

`ValidationException` thrown by the behavior should be caught by middleware and converted to a `400 Bad Request` with `ValidationProblemDetails`:

```csharp
// Middleware or exception filter
app.UseExceptionHandler(appBuilder =>
{
    appBuilder.Run(async context =>
    {
        var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
        if (exception is ValidationException validationEx)
        {
            context.Response.StatusCode = 400;
            var errors = validationEx.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            await context.Response.WriteAsJsonAsync(new ValidationProblemDetails(errors));
        }
    });
});
```

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|-------------|
| **Wolverine** | Newer mediator library with more features, but less ecosystem maturity and documentation. MediatR is the established standard for .NET CQRS |
| **Manual validation in controllers** | Violates thin controllers principle (Constitution rule XII); scatters validation logic; not reusable across handlers |
| **DataAnnotations** | Limited expressiveness; can't express complex rules (e.g., "directory must exist"); doesn't integrate with MediatR pipeline |
| **FluentValidation.AspNetCore (auto-validation)** | Deprecated in FluentValidation 12.x; automatic validation via model binding is discouraged in favor of manual/pipeline validation |

---

## Consolidated NuGet Package Matrix

| Package | Version | Project | Purpose |
|---------|---------|---------|---------|
| `Microsoft.ML.OnnxRuntime` | 1.24.3 | Infrastructure | ONNX model inference |
| `Microsoft.ML.Tokenizers` | 2.0.0 | Infrastructure | WordPiece tokenization |
| `Microsoft.Data.Sqlite` | 10.0.5 | Infrastructure | SQLite database access |
| `MediatR` | 14.1.0 | Application | CQRS mediator pattern |
| `FluentValidation` | 12.1.1 | Application | Validation rule definitions |
| `FluentValidation.DependencyInjectionExtensions` | 12.1.1 | Application | Validator DI registration |

### Test Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `xUnit` | latest | Test framework |
| `FluentAssertions` | latest | Assertion library |
| `NSubstitute` | latest | Mocking framework |
| `Microsoft.AspNetCore.Mvc.Testing` | latest | Integration test host |

---

## Open Questions for Phase 1

1. **Chunk size tuning**: Should chunk size be configurable per file type (e.g., smaller for JSON, larger for C#)?
2. **Concurrent project indexing**: Should `Channel<T>` allow parallel indexing of different projects, or strictly serial?
3. **Stale chunk cleanup**: When re-indexing, should chunks from deleted files be automatically purged?
4. **Model warming**: Should the ONNX session run a dummy inference at startup to warm the JIT/graph optimization, reducing first-request latency?
