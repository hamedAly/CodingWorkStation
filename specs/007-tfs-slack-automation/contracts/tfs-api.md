# API Contracts: TFS Integration

**Feature**: 007-tfs-slack-automation  
**Pattern**: Follows existing `sealed record` DTOs in `SemanticSearch.WebApi/Contracts/`  
**Route prefix**: `api/tfs`

---

## Endpoints

### 1. Get TFS Credential Status

```
GET /api/tfs/credentials
```

**Description**: Returns whether TFS credentials are configured (does NOT return the PAT).

**Response** `200 OK`:
```csharp
public sealed record TfsCredentialStatusResponse(
    bool IsConfigured,
    string? ServerUrl,
    string? Username,
    DateTime? UpdatedUtc);
```

**Response** `200 OK` (not configured):
```json
{
  "isConfigured": false,
  "serverUrl": null,
  "username": null,
  "updatedUtc": null
}
```

---

### 2. Save TFS Credentials

```
POST /api/tfs/credentials
```

**Description**: Store or update TFS connection credentials. PAT is encrypted before storage.

**Request**:
```csharp
public sealed record SaveTfsCredentialRequest(
    string ServerUrl,
    string Pat,
    string? Username);
```

**Validation** (FluentValidation):
- `ServerUrl`: Required, must be valid HTTP/HTTPS URL.
- `Pat`: Required, non-empty.

**Response** `200 OK`:
```csharp
public sealed record SaveCredentialResponse(bool Success, string Message);
```

**Response** `400 Bad Request`: Standard `ValidationProblemDetails`.

---

### 3. Delete TFS Credentials

```
DELETE /api/tfs/credentials
```

**Description**: Remove stored TFS credentials.

**Response** `200 OK`:
```csharp
public sealed record DeleteCredentialResponse(bool Success, string Message);
```

**Response** `404 Not Found`: No credentials stored.

---

### 4. Test TFS Connection

```
POST /api/tfs/credentials/test
```

**Description**: Validates stored credentials by making a simple API call to TFS.

**Response** `200 OK`:
```csharp
public sealed record TestConnectionResponse(
    bool Success,
    string? DisplayName,
    string? ErrorMessage);
```

---

### 5. Get My Work Items

```
GET /api/tfs/workitems
```

**Description**: Fetches all tasks and bugs assigned to the authenticated user from TFS.

**Response** `200 OK`:
```csharp
public sealed record WorkItemsResponse(
    IReadOnlyList<WorkItemResponse> Items,
    DateTime FetchedUtc);

public sealed record WorkItemResponse(
    int Id,
    string Title,
    string State,
    string WorkItemType,
    string? AssignedTo,
    string? AreaPath,
    string? Description,
    string Url);
```

**Response** `401 Unauthorized`: TFS credentials not configured or invalid.

---

### 6. Get My Pull Requests

```
GET /api/tfs/pullrequests
```

**Description**: Fetches all active pull requests where the user is author or reviewer.

**Response** `200 OK`:
```csharp
public sealed record PullRequestsResponse(
    IReadOnlyList<PullRequestResponse> PullRequests,
    DateTime FetchedUtc);

public sealed record PullRequestResponse(
    int PullRequestId,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string Status,
    string CreatedBy,
    DateTime CreationDate,
    string Url,
    IReadOnlyList<ReviewerResponse> Reviewers);

public sealed record ReviewerResponse(
    string DisplayName,
    int Vote,
    string VoteLabel,
    string? ImageUrl);
```

**Response** `401 Unauthorized`: TFS credentials not configured or invalid.

---

### 7. Get Contribution Heatmap

```
GET /api/tfs/contributions?months=12
```

**Description**: Returns daily contribution counts for the heatmap visualization.

**Query Parameters**:
- `months` (optional, default `12`): Number of months of history to fetch.

**Response** `200 OK`:
```csharp
public sealed record ContributionHeatmapResponse(
    IReadOnlyList<ContributionDayResponse> Days,
    int TotalContributions,
    DateTime FetchedUtc);

public sealed record ContributionDayResponse(
    string Date,
    int Count,
    int Level);
```

**Response** `401 Unauthorized`: TFS credentials not configured or invalid.

---

## MediatR Commands & Queries

| Type | Name | Handler Returns |
|------|------|----------------|
| Command | `SaveTfsCredentialCommand` | `SaveCredentialResult` |
| Command | `DeleteTfsCredentialCommand` | `DeleteCredentialResult` |
| Query | `GetTfsCredentialStatusQuery` | `TfsCredentialStatus` |
| Query | `TestTfsConnectionQuery` | `TestConnectionResult` |
| Query | `GetMyWorkItemsQuery` | `WorkItemsResult` |
| Query | `GetMyPullRequestsQuery` | `PullRequestsResult` |
| Query | `GetContributionHeatmapQuery` | `ContributionHeatmapResult` |
