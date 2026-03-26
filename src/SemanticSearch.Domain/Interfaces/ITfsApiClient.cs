using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public sealed record TfsWorkItem(
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

public sealed record TfsPullRequest(
    int Id,
    string Title,
    string SourceBranch,
    string TargetBranch,
    string Status,
    string CreatedBy,
    DateTime CreationDate,
    IReadOnlyList<TfsPrReviewer> Reviewers,
    string Url);

public sealed record TfsPrReviewer(
    string DisplayName,
    int Vote,
    string VoteLabel);

public sealed record ContributionDay(
    DateOnly Date,
    int Count,
    int Level);

public sealed record TfsConnectionTestResult(bool Success, string? Error, int? HttpStatusCode = null);

public sealed record TfsWorkItemComment(
    int Id,
    string Text,
    string CreatedBy,
    DateTime CreatedDate);

public sealed record TfsWorkItemUpdateResult(bool Success, string? Error, string? NewState);

public interface ITfsApiClient
{
    Task<TfsConnectionTestResult> TestConnectionAsync(string serverUrl, string pat, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TfsWorkItem>> GetAssignedWorkItemsAsync(string serverUrl, string pat, string username, string apiVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TfsPullRequest>> GetActivePullRequestsAsync(string serverUrl, string pat, string username, string apiVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContributionDay>> GetContributionDataAsync(string serverUrl, string pat, string username, string apiVersion, int months, CancellationToken cancellationToken = default);
    Task<TfsWorkItemUpdateResult> UpdateWorkItemStateAsync(string serverUrl, string pat, int workItemId, string newState, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TfsWorkItemComment>> GetWorkItemCommentsAsync(string serverUrl, string pat, int workItemId, CancellationToken cancellationToken = default);
    Task<TfsWorkItemComment?> AddWorkItemCommentAsync(string serverUrl, string pat, int workItemId, string text, CancellationToken cancellationToken = default);
}
