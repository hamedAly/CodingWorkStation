# Quickstart: TFS Integration & Background Automation (Slack/Hangfire)

**Feature**: 007-tfs-slack-automation  
**Branch**: `007-tfs-slack-automation`

## Prerequisites

1. **.NET 10 SDK** installed.
2. **TFS/Azure DevOps** access with a Personal Access Token (PAT) having scopes: Work Items (Read), Code (Read), Identity (Read).
3. **Slack App** configured in your workspace with:
   - Bot Token (`xoxb-...`) with `chat:write` scope.
   - User Token (`xoxp-...`) with `users.profile:write` scope.
   - Bot invited to the target standup channel.
4. Existing project builds and runs (`dotnet build` from repo root).

## New NuGet Packages

Add to `SemanticSearch.WebApi.csproj`:
```xml
<PackageReference Include="Hangfire.Core" Version="1.*" />
<PackageReference Include="Hangfire.AspNetCore" Version="1.*" />
<PackageReference Include="Hangfire.InMemory" Version="1.*" />
```

## Configuration

Add to `appsettings.json` under the `"SemanticSearch"` section:

```json
{
  "SemanticSearch": {
    "Integration": {
      "DefaultPrayerMethod": 5,
      "StandupCron": "30 9 * * MON-FRI",
      "PrayerFetchCron": "0 0 * * *",
      "TfsApiVersion": "7.1"
    }
  }
}
```

> **Note**: Actual credentials (PAT, Slack tokens) are NOT stored in appsettings. They are entered via the UI and stored encrypted in the SQLite database.

## Setup Steps

### 1. Install packages and build

```bash
cd src/SemanticSearch.WebApi
dotnet add package Hangfire.Core
dotnet add package Hangfire.AspNetCore
dotnet add package Hangfire.InMemory
dotnet build
```

### 2. Run the application

```bash
dotnet run
```

### 3. Configure TFS credentials

1. Navigate to the **Integration Settings** page in the dashboard.
2. Enter your TFS Server URL (e.g., `https://tfs.company.com/tfs/DefaultCollection`).
3. Enter your Personal Access Token.
4. Click **Test Connection** to verify.
5. Click **Save**.

### 4. Configure Slack credentials

1. On the same Integration Settings page, enter your Slack Bot Token and User Token.
2. Enter the target channel ID for standup messages.
3. Click **Test** to verify.
4. Click **Save**.

### 5. Configure automations

1. Set your daily standup message text.
2. Enable/disable the standup job.
3. Set your city and country for prayer times.
4. Enable/disable prayer-time status updates.
5. Click **Save Settings**.

### 6. Verify background jobs

Navigate to `/hangfire` to see:
- **standup-daily**: Recurring, Mon–Fri 09:30 AM.
- **prayer-time-fetcher**: Recurring, daily at midnight.
- Any scheduled prayer-time status jobs for today.

## Verification Checklist

- [ ] Application starts without errors.
- [ ] TFS credentials can be saved and tested.
- [ ] "My Work" board shows assigned work items in state columns.
- [ ] "Pull Requests Radar" shows active PRs.
- [ ] Slack credentials can be saved and tested.
- [ ] Hangfire dashboard is accessible at `/hangfire`.
- [ ] Standup job appears in recurring jobs list.
- [ ] Prayer-time fetcher job appears in recurring jobs list.
- [ ] Manual job trigger works from Integration Settings page.
- [ ] Contribution Heatmap renders with activity data.

## API Endpoints Summary

| Method | Path | Description |
|--------|------|-------------|
| GET | `/api/tfs/credentials` | TFS credential status |
| POST | `/api/tfs/credentials` | Save TFS credentials |
| DELETE | `/api/tfs/credentials` | Remove TFS credentials |
| POST | `/api/tfs/credentials/test` | Test TFS connection |
| GET | `/api/tfs/workitems` | Fetch assigned work items |
| GET | `/api/tfs/pullrequests` | Fetch active PRs |
| GET | `/api/tfs/contributions?months=12` | Contribution heatmap data |
| GET | `/api/slack/credentials` | Slack credential status |
| POST | `/api/slack/credentials` | Save Slack credentials |
| DELETE | `/api/slack/credentials` | Remove Slack credentials |
| POST | `/api/slack/credentials/test` | Test Slack connection |
| GET | `/api/integration/settings` | Get integration settings |
| PUT | `/api/integration/settings` | Update integration settings |
| POST | `/api/integration/jobs/{name}/trigger` | Manual job trigger |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| TFS 401 Unauthorized | PAT expired or wrong scopes — regenerate with Work Items (Read), Code (Read) |
| Slack `not_authed` | Token invalid or revoked — reconfigure in Integration Settings |
| Hangfire jobs not firing | Check `/hangfire` dashboard for errors; verify app is running continuously |
| Prayer times wrong | Verify city/country spelling matches Aladhan API expectations |
| Standup not posting on weekday | Check server timezone matches your local timezone |
