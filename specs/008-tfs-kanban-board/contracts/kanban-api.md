# API Contracts: TFS Kanban Board

**Feature**: 008-tfs-kanban-board  
**Pattern**: Follows existing `sealed record` DTOs in `SemanticSearch.WebApi/Contracts/Tfs/TfsContracts.cs`  
**Route prefix**: `api/tfs`

---

## New Endpoints

### 1. Update Work Item State

```
PATCH /api/tfs/workitems/{id}/state
```

**Description**: Update a work item's state in TFS (triggered by drag-and-drop).

**Path Parameters**:
- `id` (int, required): TFS work item ID

**Request**:
```csharp
public sealed record UpdateWorkItemStateRequest(string State);
```

**Validation** (FluentValidation):
- `State`: Required, non-empty. Must be one of: "New", "Active", "Resolved", "Closed".

**Response** `200 OK`:
```csharp
public sealed record UpdateWorkItemStateResponse(
    bool Success,
    string? Error,
    string? NewState);
```

**Response** `400 Bad Request`: `ValidationProblemDetails` (invalid state value)  
**Response** `502 Bad Gateway`: TFS rejected the state transition or network error

**Example Success**:
```json
{
  "success": true,
  "error": null,
  "newState": "Active"
}
```

**Example Failure (TFS rejection)**:
```json
{
  "success": false,
  "error": "TFS Error: The field 'State' contains the value 'Closed' that is not valid. Allowed values are: 'Active', 'Resolved'.",
  "newState": null
}
```

---

### 2. Get Work Item Comments

```
GET /api/tfs/workitems/{id}/comments
```

**Description**: Retrieve all comments for a work item from TFS.

**Path Parameters**:
- `id` (int, required): TFS work item ID

**Response** `200 OK`:
```csharp
public sealed record WorkItemCommentsResponse(
    IReadOnlyList<WorkItemCommentResponse> Comments,
    int TotalCount);

public sealed record WorkItemCommentResponse(
    int Id,
    string Text,
    string CreatedBy,
    DateTime CreatedDate);
```

**Response** `502 Bad Gateway`: TFS API error

**Example**:
```json
{
  "comments": [
    {
      "id": 1,
      "text": "Started working on this task.",
      "createdBy": "Hamed Aly",
      "createdDate": "2026-03-20T10:30:00Z"
    },
    {
      "id": 2,
      "text": "Blocked by dependency on backend API.",
      "createdBy": "Hamed Aly",
      "createdDate": "2026-03-21T14:15:00Z"
    }
  ],
  "totalCount": 2
}
```

---

### 3. Add Work Item Comment

```
POST /api/tfs/workitems/{id}/comments
```

**Description**: Add a new comment to a work item in TFS.

**Path Parameters**:
- `id` (int, required): TFS work item ID

**Request**:
```csharp
public sealed record AddWorkItemCommentRequest(string Text);
```

**Validation** (FluentValidation):
- `Text`: Required, non-empty, max length 4000 characters.

**Response** `200 OK`:
```csharp
public sealed record AddWorkItemCommentResponse(
    bool Success,
    WorkItemCommentResponse? Comment,
    string? Error);
```

**Response** `400 Bad Request`: `ValidationProblemDetails` (empty text)  
**Response** `502 Bad Gateway`: TFS API error

**Example Success**:
```json
{
  "success": true,
  "comment": {
    "id": 3,
    "text": "Fix deployed to staging for verification.",
    "createdBy": "Hamed Aly",
    "createdDate": "2026-03-25T09:00:00Z"
  },
  "error": null
}
```

---

## New DTOs (appended to TfsContracts.cs)

```csharp
// State update
public sealed record UpdateWorkItemStateRequest(string State);
public sealed record UpdateWorkItemStateResponse(bool Success, string? Error, string? NewState);

// Comments
public sealed record WorkItemCommentResponse(int Id, string Text, string CreatedBy, DateTime CreatedDate);
public sealed record WorkItemCommentsResponse(IReadOnlyList<WorkItemCommentResponse> Comments, int TotalCount);
public sealed record AddWorkItemCommentRequest(string Text);
public sealed record AddWorkItemCommentResponse(bool Success, WorkItemCommentResponse? Comment, string? Error);
```

---

## New Domain Interface Methods (ITfsApiClient)

```csharp
// Added to existing ITfsApiClient interface:
Task<TfsWorkItemUpdateResult> UpdateWorkItemStateAsync(
    string serverUrl, string pat, int workItemId, string newState, string apiVersion,
    CancellationToken cancellationToken = default);

Task<IReadOnlyList<TfsWorkItemComment>> GetWorkItemCommentsAsync(
    string serverUrl, string pat, int workItemId, string apiVersion,
    CancellationToken cancellationToken = default);

Task<TfsWorkItemComment> AddWorkItemCommentAsync(
    string serverUrl, string pat, int workItemId, string text, string apiVersion,
    CancellationToken cancellationToken = default);
```

### New Domain Records

```csharp
public sealed record TfsWorkItemComment(
    int Id,
    string Text,
    string CreatedBy,
    DateTime CreatedDate);

public sealed record TfsWorkItemUpdateResult(
    bool Success,
    string? Error,
    string? NewState);
```

---

## New WorkspaceApiClient Methods

```csharp
public Task<UpdateWorkItemStateResponse> UpdateWorkItemStateAsync(
    int workItemId, UpdateWorkItemStateRequest request, CancellationToken cancellationToken = default)
    => PatchAsync<UpdateWorkItemStateRequest, UpdateWorkItemStateResponse>(
        $"api/tfs/workitems/{workItemId}/state", request, cancellationToken);

public Task<WorkItemCommentsResponse?> GetWorkItemCommentsAsync(
    int workItemId, CancellationToken cancellationToken = default)
    => GetAsync<WorkItemCommentsResponse>($"api/tfs/workitems/{workItemId}/comments", cancellationToken);

public Task<AddWorkItemCommentResponse> AddWorkItemCommentAsync(
    int workItemId, AddWorkItemCommentRequest request, CancellationToken cancellationToken = default)
    => PostAsync<AddWorkItemCommentRequest, AddWorkItemCommentResponse>(
        $"api/tfs/workitems/{workItemId}/comments", request, cancellationToken);
```
