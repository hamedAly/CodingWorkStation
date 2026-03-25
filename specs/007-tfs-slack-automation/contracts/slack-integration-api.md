# API Contracts: Slack & Integration Settings

**Feature**: 007-tfs-slack-automation  
**Pattern**: Follows existing `sealed record` DTOs in `SemanticSearch.WebApi/Contracts/`  
**Route prefix**: `api/slack`, `api/integration`

---

## Slack Credential Endpoints

### 1. Get Slack Credential Status

```
GET /api/slack/credentials
```

**Description**: Returns whether Slack credentials are configured (does NOT return tokens).

**Response** `200 OK`:
```csharp
public sealed record SlackCredentialStatusResponse(
    bool IsConfigured,
    bool HasBotToken,
    bool HasUserToken,
    string? DefaultChannel,
    DateTime? UpdatedUtc);
```

---

### 2. Save Slack Credentials

```
POST /api/slack/credentials
```

**Request**:
```csharp
public sealed record SaveSlackCredentialRequest(
    string BotToken,
    string? UserToken,
    string DefaultChannel);
```

**Validation** (FluentValidation):
- `BotToken`: Required, must start with `xoxb-`.
- `UserToken`: Optional, if provided must start with `xoxp-`.
- `DefaultChannel`: Required, must start with `C`.

**Response** `200 OK`:
```csharp
public sealed record SaveCredentialResponse(bool Success, string Message);
```

---

### 3. Delete Slack Credentials

```
DELETE /api/slack/credentials
```

**Response** `200 OK`:
```csharp
public sealed record DeleteCredentialResponse(bool Success, string Message);
```

---

### 4. Test Slack Connection

```
POST /api/slack/credentials/test
```

**Description**: Validates stored Slack credentials by calling `auth.test`.

**Response** `200 OK`:
```csharp
public sealed record TestConnectionResponse(
    bool Success,
    string? DisplayName,
    string? ErrorMessage);
```

---

## Integration Settings Endpoints

### 5. Get Integration Settings

```
GET /api/integration/settings
```

**Response** `200 OK`:
```csharp
public sealed record IntegrationSettingsResponse(
    string StandupMessage,
    bool StandupEnabled,
    string? PrayerCity,
    string? PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled,
    DateTime UpdatedUtc);
```

---

### 6. Update Integration Settings

```
PUT /api/integration/settings
```

**Request**:
```csharp
public sealed record UpdateIntegrationSettingsRequest(
    string StandupMessage,
    bool StandupEnabled,
    string? PrayerCity,
    string? PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled);
```

**Validation** (FluentValidation):
- `StandupMessage`: Required when `StandupEnabled` is true.
- `PrayerCity`: Required when `PrayerEnabled` is true.
- `PrayerCountry`: Required when `PrayerEnabled` is true.
- `PrayerMethod`: Must be between 1 and 15.

**Response** `200 OK`:
```csharp
public sealed record UpdateSettingsResponse(bool Success, string Message);
```

---

### 7. Trigger Job Manually

```
POST /api/integration/jobs/{jobName}/trigger
```

**Description**: Manually trigger a background job for testing purposes.

**Path Parameters**:
- `jobName`: `"standup"`, `"prayer-fetch"`, or `"prayer-status"`.

**Response** `200 OK`:
```csharp
public sealed record TriggerJobResponse(bool Success, string JobId, string Message);
```

**Response** `404 Not Found`: Unknown job name.

---

## MediatR Commands & Queries

| Type | Name | Handler Returns |
|------|------|----------------|
| Command | `SaveSlackCredentialCommand` | `SaveCredentialResult` |
| Command | `DeleteSlackCredentialCommand` | `DeleteCredentialResult` |
| Command | `UpdateIntegrationSettingsCommand` | `UpdateSettingsResult` |
| Command | `TriggerJobCommand` | `TriggerJobResult` |
| Query | `GetSlackCredentialStatusQuery` | `SlackCredentialStatus` |
| Query | `TestSlackConnectionQuery` | `TestConnectionResult` |
| Query | `GetIntegrationSettingsQuery` | `IntegrationSettingsResult` |

---

## Background Job Contracts (Internal — No HTTP Endpoints)

These are internal service contracts used by Hangfire job classes, not exposed via HTTP:

### StandupJob

```csharp
// Injected service interface
public interface ISlackApiClient
{
    Task<bool> PostMessageAsync(string channel, string text, CancellationToken ct);
    Task<bool> SetUserStatusAsync(string statusText, string statusEmoji, 
        long statusExpiration, CancellationToken ct);
}
```

### PrayerTimeFetcherJob

```csharp
// Injected service interface
public interface IAladhanApiClient
{
    Task<PrayerTimesResult?> GetPrayerTimesAsync(string city, string country, 
        int method, CancellationToken ct);
}

public sealed record PrayerTimesResult(
    TimeOnly Fajr,
    TimeOnly Dhuhr,
    TimeOnly Asr,
    TimeOnly Maghrib,
    TimeOnly Isha);
```

### TfsApiClient

```csharp
// Injected service interface  
public interface ITfsApiClient
{
    Task<IReadOnlyList<TfsWorkItemResult>> GetAssignedWorkItemsAsync(CancellationToken ct);
    Task<IReadOnlyList<TfsPullRequestResult>> GetActivePullRequestsAsync(CancellationToken ct);
    Task<IReadOnlyList<ContributionDayResult>> GetContributionDataAsync(int months, CancellationToken ct);
    Task<TfsConnectionTestResult> TestConnectionAsync(CancellationToken ct);
}
```
