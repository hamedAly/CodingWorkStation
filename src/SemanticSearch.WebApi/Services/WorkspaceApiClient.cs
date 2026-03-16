using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using SemanticSearch.WebApi.Contracts.Files;
using SemanticSearch.WebApi.Contracts.Projects;
using SemanticSearch.WebApi.Contracts.Search;

namespace SemanticSearch.WebApi.Services;

public sealed class WorkspaceApiClient
{
    private readonly HttpClient _httpClient;

    public WorkspaceApiClient(NavigationManager navigationManager)
    {
        _httpClient = new HttpClient
        {
            BaseAddress = new Uri(navigationManager.BaseUri)
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
}
