# WebReport Constitution
<!-- Example: Spec Constitution, TaskFlow Constitution, etc. -->

## Core Principles

### I. Code Quality & Maintainability (The Sacred Rules)
<!-- Example: I. Library-First -->
1. Clean Code is NOT optional – it is the minimum bar for merging
2. Boy Scout Rule is mandatory: leave the code cleaner than you found it
3. Maximum function/method length: 40 lines (soft), 60 lines (very hard limit)
4. Maximum class/file length: 400 lines (excluding tests & config)
5. Cognitive Complexity per function ≤ 12 (enforce with tooling)
6. We write code for the next maintainer, not for our current ego
7. No commented-out code in main branch (git history exists)
8. No "TODO:" / "FIXME:" without issue/ticket number

### II. Code Organization & Readability
<!-- Example: II. CLI Interface -->
1. Code must be extremely well organized and understandable at first glance — even by someone seeing it for the first time
2. Enforce consistent coding style across the entire project (Prettier/ESLint/Stylelint/Ruff/black/etc.)
3. Prefer feature-based or domain-driven folder structure over type-based
4. Every file/class/function must have single, clear responsibility (Single Responsibility Principle)
5. Prefer many small files over large monolithic ones — split when a file starts feeling "too big"

### III. Comments & Documentation
<!-- Example: III. Test-First (NON-NEGOTIABLE) -->
1. Good code should be self-documenting — prioritize extremely clear names for variables, functions, classes and modules
2. Write comments to explain WHY (business reason, design decision, performance trade-off), NOT what or how (unless truly complex)
3. All comments must stay up-to-date — delete or fix outdated comments during refactoring
4. Use proper documentation blocks (JSDoc / DocBlock / Python docstrings / Rust doc / etc.) for every public/exported API
5. Never write comments that just repeat the code or explain obvious logic
6. Use inline comments sparingly — only for complicated algorithms, subtle business rules or non-obvious trade-offs

### IV. DRY & Smart Duplication Management
<!-- Example: IV. Integration Testing -->
1. DRY (Don't Repeat Yourself) is a core principle — every piece of logic/knowledge should have a single source of truth
2. When duplication is discovered → seriously consider extracting it into:
   - Shared utility/helper function
   - Custom hook (React)
   - Reusable component
   - Constant/configuration module
   - Shared type/interface/trait
3. Rule of Three (very important):
   - First time → just write it
   - Second time → copy + paste is acceptable (but feel uncomfortable)
   - Third time → refactor and abstract into a proper shared abstraction
4. Prefer clean duplication over a bad/wrong abstraction — premature abstraction is more harmful than controlled duplication
5. Never abstract just because two pieces look similar — they must be truly at the same level of abstraction and will evolve together
6. Always have sufficient tests before major deduplication/refactoring
7. Periodically scan for duplication (SonarQube, CodeClimate, jscpd, etc.)

### V. Type System & Safety
<!-- Example: V. Observability, VI. Versioning & Breaking Changes, VII. Simplicity -->
1. Strictest possible type system is preferred (TypeScript/Zod/t.rs/etc.)
2. 100% of business logic must be covered by static types when possible
3. No `any`, `unknown` or equivalent unless very strong justification + documentation
4. Prefer exhaustive pattern matching / discriminated unions over if-else chains
5. Runtime validation (zod, yup, pydantic, vine, typia, etc.) on every external boundary

### VI. Testing Standards (Non-negotiable)
<!-- Example: Text I/O ensures debuggability; Structured logging required; Or: MAJOR.MINOR.BUILD format; Or: Start simple, YAGNI principles -->
Coverage targets (minimum):
├── Unit tests               → 92–100% (business logic & utils)
├── Component tests          → 85–95% (UI logic, hooks, stores)
├── Integration/E2E          → 75–90% critical user journeys
├── Contract/Consumer tests  → 100% for public APIs (if applicable)

Testing pyramid preference:  
Unit >> Component >> Contract >> Integration >> E2E (fewest)

### VII. User Experience & UI Consistency
1. Design system is law – no one-off colors, spacing, typography
2. All user-facing text comes from translation/i18n files (no hard-coded strings)
3. Every interactive element must have: loading / error / disabled states + full accessibility (keyboard + screen-reader)
4. Maximum nesting of UI components: 4 levels
5. Consistent error handling pattern across whole application
6. We do NOT break existing user flows without strong migration plan

### VIII. Performance Budget (Hard Rules – 2026 mobile-first world)
Page load (cold start, 4G):
├── First Contentful Paint    ≤ 1.4s
├── Largest Contentful Paint  ≤ 2.2s
├── Time to Interactive       ≤ 3.8s
├── Total Blocking Time       ≤ 180ms

Bundle size targets (after compression):
├── Initial JS payload        ≤ 140 KB
├── Total JS (after hydration) ≤ 380 KB

### IX. Development Workflow & Git Hygiene
1. Linear history strongly preferred (almost always rebase)
2. Conventional commits mandatory
3. PR title must follow conventional commit format
4. PR description template is mandatory (What / Why / How / Risks)
5. Minimum 1 approving review + automated checks passing
6. No force-push to protected branches after review started
7. All feature branches must die after merge (no zombie branches)

### X. Operations & Observability
1. Every service must have structured logging from day 1
2. Every user-facing error must be traceable to source (errorId)
3. Important business events must be emitted as events/metrics
4. P95 & P99 latency budgets defined for every public endpoint
5. Alerting only on symptoms that matter to users/business

### XI. Security Baseline (Always)
1. OWASP Top 10 2021 + 2025 additions treated as minimum
2. Secrets never in git (even once → history rewrite)
3. Dependency updates automated + reviewed weekly
4. Rate limiting + proper auth on every public API
5. Input validation everywhere — zero trust on incoming data

### XII. Controller & API Layer Rules (Thin Controllers – Sacred)
1. Controllers MUST be extremely thin — ideally 5–15 lines per action (excluding attributes)
2. Controller actions should almost never contain business logic
3. The only responsibilities of a controller action are:
   - Receive HTTP request / parameters / DTO
   - Call one command / query / use-case / application service / MediatR handler
   - Handle the result and map to proper HTTP response
4. No business rules, calculations, data transformations (except trivial mapping), or repository calls inside controllers
5. No direct dependency on infrastructure (DbContext, repositories, external services) — only application layer abstractions
6. Prefer CQRS style: one action → one command/query → one handler
7. When using MediatR / Minimal APIs / Vertical Slice → controllers can be completely removed in many cases (highly recommended)

### XIII. Validation Strategy (FluentValidation + Automatic Pipeline)
1. Use FluentValidation exclusively for input/command/query validation (no DataAnnotations in DTOs except maybe [FromRoute])
2. Validation MUST live in the Application Layer (next to commands/queries), not in Presentation/Web layer
3. Leverage automatic validation pipeline (MediatR Pipeline Behavior or Action Filter):
   - Register all validators automatically (Assembly Scanning)
   - Validation runs before command/query handler → fails fast
   - Controller never manually validates — just call Send/Handle
4. Benefits we enforce:
   - Separation of concerns → validation is not mixed with HTTP or business logic
   - Clean DTOs/Command classes → no attributes clutter
   - Reusable, testable, composable validation rules
   - Consistent error responses across the whole API
5. ValidationResult should be mapped to proper ProblemDetails / ValidationProblemDetails

### XIV. Separation of Concerns — Enforced Rules (Core Principle)
1. Strict layer separation with one-way dependency (Dependency Rule — inner layers know nothing about outer layers):
   - Domain → pure business rules & entities (no dependencies)
   - Application → use-cases, commands/queries, handlers, interfaces (depends only on Domain)
   - Infrastructure → concrete implementations (EF, external services, logging…)
   - Presentation/Web → controllers, minimal apis, dtos, http pipeline (depends on Application)
2. Controllers never:
   - Know about database
   - Contain if/else business decisions
   - Transform data for business reasons
   - Call multiple services sequentially (that's application layer job)
3. Business logic lives only in:
   - Domain entities/value objects (invariants)
   - Domain services (when needed — rare)
   - Application layer handlers / use-cases / commands
4. Constant question: "If I change database / UI / framework tomorrow — how much code breaks?"  
   Answer should be: almost nothing in Domain & Application layers.

## Additional Constraints

### Technology Stack Requirements
- Backend: .NET 8, ASP.NET Core, Clean Architecture, CQRS, MediatR, FluentValidation, Entity Framework Core
- Frontend: React 18+, TypeScript, Vite, Tailwind CSS, React Query
- Database: SQL Server with multi-tenant schema
- Authentication: JWT tokens, role-based access control, secure password hashing
- Testing: xUnit, Jest, comprehensive coverage requirements
- Deployment: Containerized with Docker, CI/CD pipelines

### Security Requirements
- OWASP Top 10 compliance mandatory
- Input validation on all external boundaries
- Rate limiting and proper authentication on public APIs
- Secrets management with Azure Key Vault or similar
- Regular security audits and dependency updates

### Performance Standards
- API response times: P95 < 500ms for most endpoints
- Database query optimization required
- Caching strategy for frequently accessed data
- Bundle size limits enforced
- Mobile-first performance budgets

## Development Workflow

### Code Review Requirements
- All PRs require minimum 1 approving review
- Automated checks must pass (build, tests, linting)
- PR description must follow template (What/Why/How/Risks)
- Conventional commits enforced
- Linear git history preferred

### Quality Gates
- Code coverage minimums enforced
- Static analysis tools integrated
- Security scanning in CI/CD
- Performance regression testing
- Accessibility compliance checks

## Governance

All PRs/reviews must verify compliance with this constitution. Constitution supersedes all other practices. Amendments require documentation, approval, and migration plan.

**Version**: 1.2 | **Ratified**: 2026-01-18 | **Last Amended**: 2026-01-18
