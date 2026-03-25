using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public sealed record TfsWorkItem(
    int Id,
    string Title,
    string WorkItemType,
    string State,
    string? AssignedTo,
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

public interface ITfsApiClient
{
    Task<TfsConnectionTestResult> TestConnectionAsync(string serverUrl, string pat, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TfsWorkItem>> GetAssignedWorkItemsAsync(string serverUrl, string pat, string username, string apiVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TfsPullRequest>> GetActivePullRequestsAsync(string serverUrl, string pat, string username, string apiVersion, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ContributionDay>> GetContributionDataAsync(string serverUrl, string pat, string username, string apiVersion, int months, CancellationToken cancellationToken = default);
}
