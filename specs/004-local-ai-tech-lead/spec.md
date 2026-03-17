# Feature Specification: Local AI Tech Lead Assistant

**Feature Branch**: `004-local-ai-tech-lead`  
**Created**: 2026-03-16  
**Status**: Draft  
**Input**: User description: "Turn the duplication dashboard into a local AI assistant that produces project-level action plans and duplicate-specific refactoring guidance."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Generate Project Action Plan (Priority: P1)

An engineering lead reviews a project's duplication dashboard and asks the built-in assistant for a concrete remediation plan. The assistant expands below the summary metrics and incrementally produces a three-point architectural action plan grounded in the project's current quality snapshot.

**Why this priority**: The core value of this phase is turning passive duplication metrics into an actionable next-step plan that helps a lead decide where to intervene first.

**Independent Test**: Open the dashboard for an analyzed project, request an action plan, and confirm the expanded panel begins showing a three-point response based on the current project metrics without leaving the page.

**Acceptance Scenarios**:

1. **Given** an analyzed project with available quality metrics, **When** the user requests a tech lead action plan, **Then** the system expands a dedicated summary panel and begins displaying a three-point remediation plan tied to that project's metrics.
2. **Given** the action plan is still being generated, **When** additional content arrives, **Then** the panel updates progressively without removing previously displayed text.
3. **Given** the selected project has not been analyzed yet, **When** the user requests a tech lead action plan, **Then** the system shows a clear message explaining that recommendations require an available quality snapshot.

---

### User Story 2 - Request Duplicate-Specific Refactoring Guidance (Priority: P2)

A developer opens a duplicate comparison and asks the assistant to suggest a merged refactoring approach. The existing comparison remains visible while a separate assistant panel presents proposed consolidated code and a short explanation of how the duplicate logic could be reduced.

**Why this priority**: Once the team identifies a duplicate pair, the highest-value follow-up is a practical refactoring proposal that shortens the path from detection to remediation.

**Independent Test**: Open any duplicate comparison with both code regions available, request an AI fix, and confirm the comparison view remains intact while a new panel shows an in-progress refactoring proposal for that duplicate pair.

**Acceptance Scenarios**:

1. **Given** a duplicate comparison is open and both code regions are available, **When** the user requests a fix, **Then** the system opens a separate assistant panel within the comparison view and begins streaming a proposed consolidation of the duplicate logic.
2. **Given** a duplicate-specific proposal completes successfully, **When** the user reviews the response, **Then** the response includes both suggested replacement code and a short rationale describing the consolidation approach.
3. **Given** the duplicate evidence cannot be used to generate guidance, **When** the user requests a fix, **Then** the system keeps the comparison visible and shows a clear failure or unavailable-state message in the assistant area.

---

### User Story 3 - Read AI Guidance While It Streams (Priority: P3)

An engineer needs long AI responses to stay readable while they are still arriving. Generated guidance appears as formatted content, including structured sections and readable code blocks, so the user can start evaluating the output before generation finishes.

**Why this priority**: Streaming only helps if the content remains understandable during generation; otherwise the experience feels unstable and difficult to review.

**Independent Test**: Trigger both the project-level plan and the duplicate-specific proposal and verify that partially generated responses remain readable, retain formatting, and preserve code block structure as they update.

**Acceptance Scenarios**:

1. **Given** a response contains headings, lists, or code examples, **When** the content is shown while still generating, **Then** the user sees readable formatted output instead of raw markup.
2. **Given** a response includes C# code examples, **When** the content is rendered, **Then** the code remains visually distinct from explanatory text and readable throughout generation.
3. **Given** generation stops early or the user closes the assistant panel, **When** the response ends, **Then** the system preserves the partial output already shown and clearly indicates that generation did not complete.

---

### Edge Cases

- The local assistant is unavailable at application startup or becomes unavailable before a request is made.
- A user requests project-level or duplicate-level guidance before analysis data or duplicate source content is available.
- A duplicate comparison contains a very large combined code sample that cannot be processed within normal response limits.
- A user starts a second request from the same assistant area before the first request has finished.
- A user closes the modal or collapses the assistant area while a response is still being generated.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST provide a project-level action trigger on the quality dashboard for analyzed projects with an available quality snapshot.
- **FR-002**: The system MUST generate a three-point architectural action plan using the current project's quality summary, including lines of code and duplication percentage.
- **FR-003**: The system MUST display project-level recommendations in an expanded panel directly associated with the dashboard summary area.
- **FR-004**: The system MUST reveal generated assistant responses progressively while they are still being produced.
- **FR-005**: The system MUST preserve already displayed assistant content if generation completes normally, fails, is interrupted, or is dismissed by the user.
- **FR-006**: The system MUST provide a duplicate-specific action trigger within the duplicate comparison view.
- **FR-007**: The system MUST generate duplicate-specific guidance using both code regions from the active duplicate comparison.
- **FR-008**: The duplicate-specific guidance MUST present a proposed consolidation approach aimed at reducing the duplicate logic into a shared reusable solution.
- **FR-009**: The duplicate-specific assistant output MUST appear in a separate panel within the comparison view without replacing the original side-by-side comparison.
- **FR-010**: The system MUST include both proposed replacement code and a concise explanation in duplicate-specific guidance responses.
- **FR-011**: The system MUST render assistant responses as formatted rich content while generation is in progress, including support for headings, lists, and code blocks.
- **FR-012**: The system MUST keep source content and project metrics within the local environment and MUST NOT require sending them to an external service to produce recommendations.
- **FR-013**: After the assistant becomes available for the first time in an application run, repeated requests MUST reuse that available assistant state rather than forcing a full reload before every request.
- **FR-014**: The system MUST clearly manage overlapping requests in the same assistant area by either preventing a second request until the active one finishes or replacing the active request with an explicit user-visible state change.
- **FR-015**: The system MUST show a clear unavailable, empty-state, or error message whenever recommendations cannot be generated.

### Key Entities *(include if feature involves data)*

- **Project Quality Snapshot**: The current project-level summary used to ground recommendations, including total lines of code, duplication percentage, and duplicate counts.
- **Recommendation Session**: A single assistant interaction tied either to the dashboard summary or to a duplicate comparison, including request context, visible status, and streamed response content.
- **Architectural Action Plan**: A three-point prioritized recommendation set that explains how a team should address project-wide duplication risk.
- **Refactoring Proposal**: A duplicate-specific recommendation containing proposed consolidated code and a short explanation of the intended reuse pattern.
- **Duplicate Comparison Context**: The pair of duplicated code regions currently under review, including file references, line ranges, and source content used to request a fix suggestion.

### Assumptions

- The Phase 1 duplication dashboard, summary metrics, and duplicate comparison modal already exist and remain the entry points for this feature.
- This phase provides advisory guidance only; users review and apply any suggested code changes manually.
- The project-level plan is always limited to three prioritized actions because that scope was explicitly requested for this phase.
- Duplicate-specific guidance uses only the currently selected duplicate comparison and does not attempt broader multi-file refactoring beyond that pair.
- The assistant is intended for one local user reviewing one selected project at a time.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In at least 90% of project-level requests, users see the first visible action-plan content within 5 seconds of initiating the request.
- **SC-002**: In at least 95% of successful project-level requests, the response contains exactly three distinct recommended actions tied to the current project's quality context.
- **SC-003**: In at least 90% of duplicate-specific requests for comparison views with available source content, users receive a proposed consolidation response within 15 seconds.
- **SC-004**: In usability validation, at least 90% of users can initiate duplicate-specific guidance from an open comparison view in a single interaction.
- **SC-005**: In 100% of tested unavailable-state and failure scenarios, the user receives a clear explanatory message without losing access to the dashboard summary or duplicate comparison they were reviewing.
- **SC-006**: In 100% of verified streamed responses, previously displayed content remains visible and readable throughout generation, including any lists or code blocks already shown.
