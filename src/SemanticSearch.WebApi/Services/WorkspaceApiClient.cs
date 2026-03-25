using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SemanticSearch.WebApi.Contracts.Architecture;
using SemanticSearch.WebApi.Contracts.Files;
using SemanticSearch.WebApi.Contracts.Projects;
using SemanticSearch.WebApi.Contracts.Quality;
using SemanticSearch.WebApi.Contracts.Search;
using SemanticSearch.WebApi.Contracts.Slack;
using SemanticSearch.WebApi.Contracts.Tfs;

namespace SemanticSearch.WebApi.Services;

public sealed class WorkspaceApiClient
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public WorkspaceApiClient(NavigationManager navigationManager)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(navigationManager.BaseUri),
            Timeout = TimeSpan.FromMinutes(5)
        };
    }

    public Task<IReadOnlyList<ProjectWorkspaceSummaryResponse>?> ListProjectsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ProjectWorkspaceSummaryResponse>>("api/project", cancellationToken);

    public Task<ProjectStatusResponse?> GetStatusAsync(string projectKey, CancellationToken cancellationToken = default)
        => GetAsync<ProjectStatusResponse>($"api/project/status/{Uri.EscapeDataString(projectKey)}", cancellationToken);

    public Task<IReadOnlyList<ProjectTreeNodeResponse>?> GetTreeAsync(string projectKey, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<ProjectTreeNodeResponse>>($"api/project/tree/{Uri.EscapeDataString(projectKey)}", cancellationToken);

    public Task<IndexAcceptedResponse> IndexProjectAsync(IndexProjectRequest request, CancellationToken cancellationToken = default)
        => PostAsync<IndexProjectRequest, IndexAcceptedResponse>("api/project/index", request, cancellationToken);

    public Task<IndexAcceptedResponse> RefreshFileAsync(RefreshProjectFileRequest request, CancellationToken cancellationToken = default)
        => PostAsync<RefreshProjectFileRequest, IndexAcceptedResponse>("api/project/index/file", request, cancellationToken);

    public Task<IndexingControlResponse> PauseIndexingAsync(string projectKey, CancellationToken cancellationToken = default)
        => PostAsync<object, IndexingControlResponse>($"api/project/pause/{Uri.EscapeDataString(projectKey)}", new { }, cancellationToken);

    public Task<IndexingControlResponse> ResumeIndexingAsync(string projectKey, CancellationToken cancellationToken = default)
        => PostAsync<object, IndexingControlResponse>($"api/project/resume/{Uri.EscapeDataString(projectKey)}", new { }, cancellationToken);

    public Task<IndexAcceptedResponse> RetryIndexingAsync(string projectKey, CancellationToken cancellationToken = default)
        => PostAsync<object, IndexAcceptedResponse>($"api/project/retry/{Uri.EscapeDataString(projectKey)}", new { }, cancellationToken);

    public Task<SearchResponse> SearchSemanticAsync(SemanticSearchRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SemanticSearchRequest, SearchResponse>("api/search/semantic", request, cancellationToken);

    public Task<SearchResponse> SearchExactAsync(ExactSearchRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ExactSearchRequest, SearchResponse>("api/search/exact", request, cancellationToken);

    public Task<ReadFileResponse> ReadFileAsync(ReadFileRequest request, CancellationToken cancellationToken = default)
        => PostAsync<ReadFileRequest, ReadFileResponse>("api/file/read", request, cancellationToken);

    public Task<QualitySnapshotResponse> GenerateQualitySnapshotAsync(QualitySnapshotRequest request, CancellationToken cancellationToken = default)
        => PostAsync<QualitySnapshotRequest, QualitySnapshotResponse>("api/quality/snapshot", request, cancellationToken);

    public Task<QualitySummaryResponse?> GetQualitySummaryAsync(string projectKey, CancellationToken cancellationToken = default)
        => GetAsync<QualitySummaryResponse>($"api/quality/{Uri.EscapeDataString(projectKey)}", cancellationToken);

    public Task<QualityFindingsResponse?> GetQualityFindingsAsync(string projectKey, CancellationToken cancellationToken = default)
        => GetAsync<QualityFindingsResponse>($"api/quality/{Uri.EscapeDataString(projectKey)}/findings", cancellationToken);

    public Task<DuplicateComparisonResponse?> GetDuplicateComparisonAsync(string projectKey, string findingId, CancellationToken cancellationToken = default)
        => GetAsync<DuplicateComparisonResponse>($"api/quality/{Uri.EscapeDataString(projectKey)}/findings/{Uri.EscapeDataString(findingId)}", cancellationToken);

    public Task<QualityRunResponse> RunStructuralAnalysisAsync(StructuralDuplicationRequest request, CancellationToken cancellationToken = default)
        => PostAsync<StructuralDuplicationRequest, QualityRunResponse>("api/quality/structural", request, cancellationToken);

    public Task<QualityRunResponse> RunSemanticAnalysisAsync(SemanticDuplicationRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SemanticDuplicationRequest, QualityRunResponse>("api/quality/semantic", request, cancellationToken);

    public Task<AssistantStatusResponse?> GetAssistantStatusAsync(CancellationToken cancellationToken = default)
        => GetAsync<AssistantStatusResponse>("api/quality/ai/status", cancellationToken);

    public IAsyncEnumerable<AiStreamEventResponse> StreamProjectPlanAsync(
        ProjectPlanStreamRequest request,
        CancellationToken cancellationToken = default)
        => StreamAsync("api/quality/ai/project-plan/stream", request, cancellationToken);

    public IAsyncEnumerable<AiStreamEventResponse> StreamFindingFixAsync(
        FindingFixStreamRequest request,
        CancellationToken cancellationToken = default)
        => StreamAsync("api/quality/ai/finding-fix/stream", request, cancellationToken);

    // Architecture endpoints
    public Task<DependencyAnalysisRunResponse> RunDependencyAnalysisAsync(string projectKey, CancellationToken cancellationToken = default)
        => PostAsync<object, DependencyAnalysisRunResponse>($"api/architecture/{Uri.EscapeDataString(projectKey)}/dependency-graph", new { }, cancellationToken);

    public Task<DependencyGraphResponse?> GetDependencyGraphAsync(string projectKey, string? ns = null, string? filePath = null, CancellationToken cancellationToken = default)
        => GetAsync<DependencyGraphResponse>(BuildGraphUrl(projectKey, ns, filePath), cancellationToken);

    public Task<FileHeatmapResponse?> GetFileHeatmapAsync(string projectKey, CancellationToken cancellationToken = default)
        => GetAsync<FileHeatmapResponse>($"api/architecture/{Uri.EscapeDataString(projectKey)}/heatmap", cancellationToken);

    public Task<ErDiagramResponse?> GetErDiagramAsync(CancellationToken cancellationToken = default)
        => GetAsync<ErDiagramResponse>("api/architecture/er-diagram", cancellationToken);

    // TFS endpoints
    public Task<TfsCredentialStatusResponse?> GetTfsCredentialStatusAsync(CancellationToken cancellationToken = default)
        => GetAsync<TfsCredentialStatusResponse>("api/tfs/credentials", cancellationToken);

    public Task<SaveCredentialResponse> SaveTfsCredentialAsync(SaveTfsCredentialRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SaveTfsCredentialRequest, SaveCredentialResponse>("api/tfs/credentials", request, cancellationToken);

    public Task<TestConnectionResponse> TestTfsConnectionAsync(CancellationToken cancellationToken = default)
        => PostAsync<object, TestConnectionResponse>("api/tfs/credentials/test", new { }, cancellationToken);

    public Task<WorkItemsResponse?> GetWorkItemsAsync(CancellationToken cancellationToken = default)
        => GetAsync<WorkItemsResponse>("api/tfs/workitems", cancellationToken);

    public Task<PullRequestsResponse?> GetPullRequestsAsync(CancellationToken cancellationToken = default)
        => GetAsync<PullRequestsResponse>("api/tfs/pullrequests", cancellationToken);

    public Task<ContributionHeatmapResponse?> GetContributionHeatmapAsync(int months = 12, CancellationToken cancellationToken = default)
        => GetAsync<ContributionHeatmapResponse>($"api/tfs/contributions?months={months}", cancellationToken);

    // Slack endpoints
    public Task<SlackCredentialStatusResponse?> GetSlackCredentialStatusAsync(CancellationToken cancellationToken = default)
        => GetAsync<SlackCredentialStatusResponse>("api/slack/credentials", cancellationToken);

    public Task<SaveCredentialResponse> SaveSlackCredentialAsync(SaveSlackCredentialRequest request, CancellationToken cancellationToken = default)
        => PostAsync<SaveSlackCredentialRequest, SaveCredentialResponse>("api/slack/credentials", request, cancellationToken);

    public Task<TestConnectionResponse> TestSlackConnectionAsync(CancellationToken cancellationToken = default)
        => PostAsync<object, TestConnectionResponse>("api/slack/credentials/test", new { }, cancellationToken);

    // Integration settings endpoints
    public Task<IntegrationSettingsResponse?> GetIntegrationSettingsAsync(CancellationToken cancellationToken = default)
        => GetAsync<IntegrationSettingsResponse>("api/integration/settings", cancellationToken);

    public Task UpdateIntegrationSettingsAsync(UpdateIntegrationSettingsRequest request, CancellationToken cancellationToken = default)
        => PutAsync("api/integration/settings", request, cancellationToken);

    public Task<TriggerJobResponse> TriggerJobAsync(string jobName, CancellationToken cancellationToken = default)
        => PostAsync<object, TriggerJobResponse>($"api/integration/jobs/{Uri.EscapeDataString(jobName)}/trigger", new { }, cancellationToken);

    public Task<DeleteCredentialResponse> DeleteTfsCredentialAsync(CancellationToken cancellationToken = default)
        => DeleteAsync<DeleteCredentialResponse>("api/tfs/credentials", cancellationToken);

    public Task<DeleteCredentialResponse> DeleteSlackCredentialAsync(CancellationToken cancellationToken = default)
        => DeleteAsync<DeleteCredentialResponse>("api/slack/credentials", cancellationToken);

    private static string BuildGraphUrl(string projectKey, string? ns, string? filePath)
    {
        var url = $"api/architecture/{Uri.EscapeDataString(projectKey)}/dependency-graph";
        var query = new List<string>();
        if (!string.IsNullOrWhiteSpace(ns)) query.Add($"namespace={Uri.EscapeDataString(ns)}");
        if (!string.IsNullOrWhiteSpace(filePath)) query.Add($"filePath={Uri.EscapeDataString(filePath)}");
        return query.Count > 0 ? $"{url}?{string.Join('&', query)}" : url;
    }

    private async Task<TResponse?> GetAsync<TResponse>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        return await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task PutAsync<TRequest>(string url, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(url, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    private async Task<TResponse> DeleteAsync<TResponse>(string url, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(url, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
            return;

        var error = await ReadErrorAsync(response, cancellationToken);
        throw new InvalidOperationException(error);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var raw = await response.Content.ReadAsStringAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(raw))
            return "Request failed.";

        try
        {
            using var document = JsonDocument.Parse(raw);
            if (document.RootElement.TryGetProperty("detail", out var detail) &&
                detail.ValueKind is JsonValueKind.String)
            {
                return detail.GetString() ?? "Request failed.";
            }

            if (document.RootElement.TryGetProperty("errors", out var errors) &&
                errors.ValueKind is JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    foreach (var item in property.Value.EnumerateArray())
                    {
                        if (item.ValueKind is JsonValueKind.String && !string.IsNullOrWhiteSpace(item.GetString()))
                            messages.Add(item.GetString()!);
                    }
                }

                if (messages.Count > 0)
                    return string.Join(Environment.NewLine, messages);
            }
        }
        catch (JsonException)
        {
        }

        return raw;
    }

    private async IAsyncEnumerable<AiStreamEventResponse> StreamAsync<TRequest>(
        string url,
        TRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = JsonContent.Create(request)
        };
        httpRequest.Headers.Accept.ParseAdd("application/x-ndjson");

        using var response = await _httpClient.SendAsync(
            httpRequest,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var reader = new StreamReader(stream);
        while (!cancellationToken.IsCancellationRequested)
        {
            var line = await reader.ReadLineAsync(cancellationToken);
            if (line is null)
            {
                yield break;
            }

            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var item = JsonSerializer.Deserialize<AiStreamEventResponse>(line, JsonOptions);
            if (item is not null)
            {
                yield return item;
            }
        }
    }
}
