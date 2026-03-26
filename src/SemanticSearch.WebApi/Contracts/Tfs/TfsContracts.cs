namespace SemanticSearch.WebApi.Contracts.Tfs;

public sealed record SaveTfsCredentialRequest(
    string ServerUrl,
    string Pat,
    string Username);

public sealed record TfsCredentialStatusResponse(
    bool IsConfigured,
    string? ServerUrl,
    string? Username,
    DateTime? UpdatedUtc);

public sealed record TestConnectionResponse(
    bool Success,
    string? Error);

public sealed record SaveCredentialResponse(bool Success, string? Error);

public sealed record DeleteCredentialResponse(bool Success);

public sealed record WorkItemResponse(
    int Id,
    string Title,
    string WorkItemType,
    string State,
    string? AssignedTo,
    string? TeamProject,
    string? AreaPath,
    string? IterationPath,
    string? Priority,
    DateTime? CreatedDate,
    DateTime? ChangedDate,
    string Url);

public sealed record WorkItemsResponse(IReadOnlyList<WorkItemResponse> Items);

public sealed record PullRequestResponse(
    int Id,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string Status,
    string CreatedBy,
    DateTime CreationDate,
    IReadOnlyList<ReviewerResponse> Reviewers,
    string Url);

public sealed record ReviewerResponse(
    string DisplayName,
    int Vote,
    string VoteLabel);

public sealed record PullRequestsResponse(IReadOnlyList<PullRequestResponse> Items);

public sealed record ContributionDayResponse(
    string Date,
    int Count,
    int Level);

public sealed record ContributionHeatmapResponse(
    IReadOnlyList<ContributionDayResponse> Days,
    int TotalContributions,
    string Username = "");

public sealed record UpdateWorkItemStateRequest(string State);

public sealed record UpdateWorkItemStateResponse(
    bool Success,
    string? Error,
    string? NewState);

public sealed record WorkItemCommentResponse(
    int Id,
    string Text,
    string CreatedBy,
    DateTime CreatedDate);

public sealed record WorkItemCommentsResponse(
    IReadOnlyList<WorkItemCommentResponse> Comments,
    int TotalCount);

public sealed record AddWorkItemCommentRequest(string Text);

public sealed record AddWorkItemCommentResponse(
    bool Success,
    WorkItemCommentResponse? Comment,
    string? Error);
