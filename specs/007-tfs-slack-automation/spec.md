# Feature Specification: TFS Integration & Background Automation (Slack/Hangfire)

**Feature Branch**: `007-tfs-slack-automation`  
**Created**: 2026-03-25  
**Status**: Draft  
**Input**: User description: "Integrate TFS/Azure DevOps for work items and use Hangfire for Slack automation — expanding the dashboard into a daily workflow command center."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - View My Assigned Work Items on a Kanban Board (Priority: P1)

As a developer, I open the dashboard and see a "My Work" Kanban-style board that displays all tasks and bugs currently assigned to me in TFS/Azure DevOps. The board is organized by work item state (e.g., New, Active, Resolved, Closed) so I can quickly see what needs attention. I can click a work item to view its details and navigate to it in TFS.

**Why this priority**: This is the core value proposition — giving the user a single-pane-of-glass view of their assigned work without switching to the TFS web interface. Every other TFS feature builds on the ability to authenticate and fetch work items.

**Independent Test**: Can be fully tested by configuring TFS credentials, navigating to the "My Work" board, and verifying assigned work items appear organized by state columns.

**Acceptance Scenarios**:

1. **Given** valid TFS credentials are stored, **When** the user navigates to the "My Work" board, **Then** all tasks and bugs assigned to the user are displayed in columns grouped by work item state.
2. **Given** a work item changes state in TFS, **When** the user refreshes the board, **Then** the work item appears in the updated state column.
3. **Given** the user clicks a work item card, **Then** the work item detail is shown with title, description, state, and a link to open it in TFS.
4. **Given** no TFS credentials are configured, **When** the user navigates to the "My Work" board, **Then** a prompt is shown to configure credentials first.

---

### User Story 2 - Securely Store TFS Credentials (Priority: P1)

As a developer, I need to configure my TFS/Azure DevOps connection by providing a server URL and Personal Access Token (PAT). The credentials are stored securely and used for all TFS API calls. I can update or remove my stored credentials at any time.

**Why this priority**: All TFS features depend on valid, securely-stored credentials. This is a prerequisite for every other TFS integration story.

**Independent Test**: Can be fully tested by navigating to a settings/credentials page, entering TFS credentials, verifying they persist across sessions, and confirming they can be updated or cleared.

**Acceptance Scenarios**:

1. **Given** the user is on the TFS settings page, **When** they enter a server URL and PAT and save, **Then** the credentials are stored securely (not in plaintext configuration files).
2. **Given** credentials are already stored, **When** the user updates the PAT, **Then** subsequent TFS API calls use the new token.
3. **Given** credentials are stored, **When** the user chooses to remove them, **Then** all stored credential data is deleted.
4. **Given** the user provides an invalid PAT, **When** a TFS API call is attempted, **Then** a clear authentication error is displayed.

---

### User Story 3 - Automated Daily Standup Message to Slack (Priority: P2)

As a developer, I want the system to automatically post a preset standup message to a designated Slack channel every weekday at 09:30 AM. This removes the friction of remembering to post a daily standup update manually.

**Why this priority**: This is the highest-value automation — it runs daily and saves time every working day. It also validates the background job infrastructure that all other automated jobs depend on.

**Independent Test**: Can be fully tested by configuring a Slack channel and token, setting the standup message, triggering the job manually or waiting for schedule, and verifying the message appears in the Slack channel.

**Acceptance Scenarios**:

1. **Given** Slack credentials and channel are configured, **When** the scheduled time (09:30 AM, Mon–Fri) arrives, **Then** the preset standup message is posted to the configured Slack channel.
2. **Given** it is a weekend (Saturday or Sunday), **When** the scheduled time arrives, **Then** no message is posted.
3. **Given** the Slack API is unreachable, **When** the job fires, **Then** the failure is logged and the job retries according to the configured retry policy.
4. **Given** the user wants to change the standup message, **When** they update it in settings, **Then** the next scheduled post uses the updated message.

---

### User Story 4 - Background Job Dashboard (Priority: P2)

As an administrator of my dashboard, I want to access a background job management interface at `/hangfire` to monitor all scheduled, recurring, and completed jobs. This lets me verify automations are running correctly, inspect failures, and manually trigger jobs if needed.

**Why this priority**: Visibility into background jobs is essential for trusting the automation. Without it, failures are silent and hard to diagnose.

**Independent Test**: Can be fully tested by navigating to `/hangfire` and verifying that recurring jobs, scheduled jobs, and job history are visible.

**Acceptance Scenarios**:

1. **Given** the application is running, **When** the user navigates to `/hangfire`, **Then** the background job dashboard is displayed showing recurring jobs, scheduled jobs, and job history.
2. **Given** a recurring job exists, **When** viewing the dashboard, **Then** the job's schedule, last execution time, and next execution time are visible.
3. **Given** a job has failed, **When** viewing the dashboard, **Then** the failure details and exception information are displayed.

---

### User Story 5 - Pull Requests Radar (Priority: P2)

As a developer, I want to see a "Pull Requests Radar" panel on the dashboard that shows all active pull requests where I am either the author or a reviewer. This helps me stay on top of code reviews and track the status of my submitted PRs.

**Why this priority**: Pull request awareness is a daily need for developers. Being able to see PRs alongside work items creates a complete daily workflow view.

**Independent Test**: Can be fully tested by having active PRs in TFS, navigating to the radar panel, and verifying PRs are listed with correct status and reviewer information.

**Acceptance Scenarios**:

1. **Given** valid TFS credentials are stored, **When** the user views the Pull Requests Radar, **Then** all active PRs where the user is author or reviewer are displayed.
2. **Given** a PR has reviewers assigned, **When** viewing the PR card, **Then** reviewer names and their vote status (Approved, Waiting, Rejected) are shown.
3. **Given** a PR is listed, **When** the user clicks on it, **Then** the PR details are shown with a link to open it in TFS.
4. **Given** no active PRs exist for the user, **When** the PR Radar is viewed, **Then** an appropriate empty state message is displayed.

---

### User Story 6 - Automated Prayer Time Status Updates (Priority: P3)

As a developer, I want the system to automatically fetch today's prayer times at midnight each day, and then schedule individual jobs at each prayer time to update my Slack status to "Praying" (🕌) with an automatic expiration after 30 minutes. This ensures colleagues know when I am briefly away for prayers without manual effort.

**Why this priority**: This is a personalized automation that adds convenience but is not essential for core workflow tracking. It depends on both the background job infrastructure and Slack integration being in place.

**Independent Test**: Can be fully tested by triggering the midnight fetcher job, verifying 5 prayer-time jobs are scheduled, and confirming that when a prayer-time job fires, the Slack status is updated with the correct emoji and expiration.

**Acceptance Scenarios**:

1. **Given** Slack credentials and location/city are configured, **When** midnight arrives, **Then** the system fetches 5 prayer times for today from an external prayer-time service.
2. **Given** prayer times are fetched, **Then** 5 individual jobs are scheduled at the exact prayer times.
3. **Given** a prayer time arrives, **When** the scheduled job fires, **Then** the user's Slack status is set to "Praying" with the 🕌 emoji and the status expiration is set to 30 minutes from the current time.
4. **Given** 30 minutes have elapsed after a prayer time, **Then** the Slack status automatically clears (handled by Slack's native status expiration).
5. **Given** the prayer-time service is unreachable, **When** the midnight job fires, **Then** the failure is logged and the job retries.

---

### User Story 7 - Contribution Heatmap (Priority: P3)

As a developer, I want to see a "Contribution Heatmap" (similar to GitHub's contribution graph) on the dashboard that visualizes my TFS activity over time. This gives me a visual overview of my productivity patterns — commits, work item updates, and PR activity.

**Why this priority**: This is a nice-to-have visualization that provides long-term insights but is not critical for daily workflow management. It can be built independently once TFS integration is established.

**Independent Test**: Can be fully tested by having historical TFS activity, viewing the heatmap, and verifying that days with activity show colored squares proportional to activity volume.

**Acceptance Scenarios**:

1. **Given** valid TFS credentials are stored, **When** the user views the Contribution Heatmap, **Then** a grid of squares is displayed representing the last 12 months of activity.
2. **Given** the user had activity on a particular day, **Then** that day's square is colored with intensity proportional to the number of contributions.
3. **Given** the user hovers over a day square, **Then** a tooltip displays the date and contribution count.
4. **Given** the user had no TFS activity in the last 12 months, **Then** all squares are shown in the baseline (empty) color with an informational message.

---

### Edge Cases

- What happens when the TFS server is unreachable or returns an error? The dashboard displays a clear error banner indicating connectivity issues and shows cached/last-known data if available.
- What happens when the Slack API token expires or is revoked? Jobs log the authentication failure, the user is notified on next dashboard visit, and jobs continue to retry with exponential backoff until credentials are updated.
- What happens when the prayer-time service returns unexpected data or is down? The midnight fetcher logs the error, retries, and if all retries fail, no prayer-time status jobs are scheduled for that day (the user is not impacted beyond missing automatic status updates).
- What happens when the user has zero assigned work items? The Kanban board displays an empty state with a message like "No work items assigned."
- What happens when a background job fails repeatedly? The Hangfire dashboard shows the failed job with retry history and exception details; after the maximum retry count, the job moves to the "Failed" state.
- What happens if the scheduled standup time falls on a public holiday? The system posts as scheduled (weekday logic only); holiday awareness is out of scope for this feature.
- What happens when the TFS PAT is near expiration? The system does not proactively detect PAT expiry; it will surface authentication errors when they occur and prompt the user to update credentials.

## Requirements *(mandatory)*

### Functional Requirements

**TFS Integration**

- **FR-001**: System MUST provide a credentials management interface allowing the user to store, update, and remove a TFS/Azure DevOps server URL and Personal Access Token.
- **FR-002**: System MUST store TFS credentials securely — the PAT MUST NOT be stored in plaintext in configuration files or client-accessible storage.
- **FR-003**: System MUST fetch work items (tasks and bugs) assigned to the authenticated user from TFS via its REST API.
- **FR-004**: System MUST display assigned work items in a Kanban-style board with columns representing work item states (e.g., New, Active, Resolved, Closed).
- **FR-005**: System MUST allow the user to click a work item to view its details (title, description, state, assigned to, area path) and provide a link to open it in TFS.
- **FR-006**: System MUST display a "Pull Requests Radar" showing all active pull requests where the user is the author or an assigned reviewer.
- **FR-007**: Each pull request card MUST show the PR title, source/target branch, status, and reviewer vote statuses.
- **FR-008**: System MUST display a "Contribution Heatmap" visualizing the user's TFS activity over the past 12 months as a grid of colored squares (similar to GitHub's contribution graph).
- **FR-009**: The heatmap MUST show activity intensity based on the number of contributions per day (commits, work item updates, PR activity).
- **FR-010**: Hovering over a heatmap square MUST display a tooltip with the date and contribution count.

**Background Job Processing**

- **FR-011**: System MUST include a background job processing engine with in-memory storage for job state.
- **FR-012**: System MUST expose a job management dashboard at the `/hangfire` URL path.
- **FR-013**: System MUST support recurring jobs, delayed/scheduled jobs, and fire-and-forget jobs.

**Slack Automation**

- **FR-014**: System MUST provide configuration for Slack integration (API token, default channel).
- **FR-015**: System MUST store Slack credentials securely, consistent with TFS credential storage practices.
- **FR-016**: System MUST run a recurring job that posts a preset standup message to a configured Slack channel at 09:30 AM on weekdays (Monday through Friday).
- **FR-017**: The standup message content MUST be configurable by the user.
- **FR-018**: System MUST run a recurring job at midnight that fetches the day's 5 prayer times from an external prayer-time service.
- **FR-019**: The midnight fetcher MUST schedule 5 individual one-time jobs, each triggered at the exact prayer time.
- **FR-020**: Each prayer-time job MUST update the user's Slack profile status to "Praying" with the 🕌 emoji and set the status expiration to 30 minutes from the current time.
- **FR-021**: The prayer-time location/city MUST be configurable by the user.
- **FR-022**: All background jobs MUST log execution outcomes (success and failure) for observability.

### Key Entities

- **TFS Credential**: Represents the stored connection to TFS/Azure DevOps — includes server URL and encrypted PAT. One credential set per user.
- **Slack Credential**: Represents the stored Slack integration — includes API token and default channel. One credential set per user.
- **Work Item**: A task or bug fetched from TFS — includes title, description, state, type, assigned user, and area path.
- **Pull Request**: An active PR from TFS — includes title, source branch, target branch, status, author, and reviewer votes.
- **Contribution Activity**: A daily aggregate of the user's TFS contributions — includes date and activity count.
- **Recurring Job Configuration**: Defines a scheduled automation — includes job name, schedule expression, and enabled/disabled state.
- **Standup Message**: The user-configurable message template posted to Slack daily.
- **Prayer Time Configuration**: The user's location/city used for fetching prayer times, plus the prayer-time service source.

## Assumptions

- The TFS/Azure DevOps instance supports REST API access (TFS 2015+ or Azure DevOps Services).
- The user has a valid Personal Access Token with appropriate scopes (work items read, code read, identity read).
- The Slack workspace has a bot/app configured with permissions for `chat:write` (posting messages) and `users.profile:write` (updating status).
- The prayer-time service (e.g., Aladhan API) is publicly accessible and free to use.
- The dashboard is a single-user application — there is one set of TFS credentials and one set of Slack credentials.
- The application server's timezone is the user's local timezone for scheduling accuracy.
- In-memory job storage is acceptable (jobs will be lost on application restart and re-created on startup).

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can view all their assigned TFS work items organized by state within 5 seconds of opening the "My Work" board.
- **SC-002**: The Pull Requests Radar displays all active PRs (authored or reviewing) within 5 seconds of page load.
- **SC-003**: The Contribution Heatmap renders 12 months of activity data within 5 seconds.
- **SC-004**: The daily standup message is posted to Slack within 1 minute of the scheduled 09:30 AM weekday time, with 99% reliability over a 30-day period.
- **SC-005**: Prayer-time status updates are applied to Slack within 1 minute of the actual prayer time, with the correct emoji and 30-minute expiration.
- **SC-006**: The background job dashboard is accessible and displays all job states (recurring, scheduled, succeeded, failed) with zero additional configuration.
- **SC-007**: TFS credential setup takes less than 2 minutes from first visit to seeing work items on the board.
- **SC-008**: All background job failures are recorded with full error details and visible in the job dashboard within 1 minute of occurrence.
