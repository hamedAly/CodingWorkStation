using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using SemanticSearch.WebApi.Contracts.Architecture;
using SemanticSearch.WebApi.Contracts.Files;
using SemanticSearch.WebApi.Contracts.Projects;
using SemanticSearch.WebApi.Contracts.Quality;
using SemanticSearch.WebApi.Contracts.Search;
using SemanticSearch.WebApi.Contracts.Slack;
using SemanticSearch.WebApi.Contracts.Study;
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
            Timeout = TimeSpan.FromMinutes(20)
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

    public Task<UpdateWorkItemStateResponse> UpdateWorkItemStateAsync(int id, string state, CancellationToken cancellationToken = default)
        => PatchAsync<UpdateWorkItemStateRequest, UpdateWorkItemStateResponse>(
            $"api/tfs/workitems/{id}/state",
            new UpdateWorkItemStateRequest(state),
            cancellationToken);

    public Task<WorkItemCommentsResponse?> GetWorkItemCommentsAsync(int id, CancellationToken cancellationToken = default)
        => GetAsync<WorkItemCommentsResponse>($"api/tfs/workitems/{id}/comments", cancellationToken);

    public Task<AddWorkItemCommentResponse> AddWorkItemCommentAsync(int id, string text, CancellationToken cancellationToken = default)
        => PostAsync<AddWorkItemCommentRequest, AddWorkItemCommentResponse>(
            $"api/tfs/workitems/{id}/comments",
            new AddWorkItemCommentRequest(text),
            cancellationToken);

    public Task<IReadOnlyList<BookSummaryResponse>?> GetBooksAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<BookSummaryResponse>>("api/study/books", cancellationToken);

    public Task<BookDetailResponse?> GetBookAsync(string bookId, CancellationToken cancellationToken = default)
        => GetAsync<BookDetailResponse>($"api/study/books/{Uri.EscapeDataString(bookId)}", cancellationToken);

    public string GetBookPdfUrl(string bookId)
        => new Uri(_httpClient.BaseAddress!, $"api/study/books/{Uri.EscapeDataString(bookId)}/pdf").ToString();

    public async Task<BookDetailResponse> AddBookAsync(string title, string? author, string? description, IBrowserFile pdfFile, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(title), "Title");
        if (!string.IsNullOrWhiteSpace(author))
            content.Add(new StringContent(author), "Author");
        if (!string.IsNullOrWhiteSpace(description))
            content.Add(new StringContent(description), "Description");

        await using var stream = pdfFile.OpenReadStream(209_715_200, cancellationToken);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(pdfFile.ContentType);
        content.Add(fileContent, "PdfFile", pdfFile.Name);

        using var response = await _httpClient.PostAsync("api/study/books", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<BookDetailResponse>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    public Task<BookDetailResponse> FinalizeBookImportAsync(string bookId, FinalizeBookImportRequest request, CancellationToken cancellationToken = default)
        => PostAsync<FinalizeBookImportRequest, BookDetailResponse>($"api/study/books/{Uri.EscapeDataString(bookId)}/auto-setup", request, cancellationToken);

    public Task<BookDetailResponse> UpdateBookAsync(string bookId, UpdateBookRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateBookRequest, BookDetailResponse>($"api/study/books/{Uri.EscapeDataString(bookId)}", request, cancellationToken);

    public async Task DeleteBookAsync(string bookId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/study/books/{Uri.EscapeDataString(bookId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateLastReadPageAsync(string bookId, int page, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Patch, $"api/study/books/{Uri.EscapeDataString(bookId)}/last-read-page")
        {
            Content = JsonContent.Create(new UpdateLastReadPageRequest(page))
        };

        using var response = await _httpClient.SendAsync(request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<ChapterResponse> AddChapterAsync(string bookId, AddChapterRequest request, CancellationToken cancellationToken = default)
        => PostAsync<AddChapterRequest, ChapterResponse>($"api/study/books/{Uri.EscapeDataString(bookId)}/chapters", request, cancellationToken);

    public Task<ChapterResponse> UpdateChapterAsync(string bookId, string chapterId, UpdateChapterRequest request, CancellationToken cancellationToken = default)
        => PutAsync<UpdateChapterRequest, ChapterResponse>($"api/study/books/{Uri.EscapeDataString(bookId)}/chapters/{Uri.EscapeDataString(chapterId)}", request, cancellationToken);

    public async Task DeleteChapterAsync(string bookId, string chapterId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/study/books/{Uri.EscapeDataString(bookId)}/chapters/{Uri.EscapeDataString(chapterId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UpdateChapterNotesAsync(string bookId, string chapterId, string? notes, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.PutAsJsonAsync(
            $"api/study/books/{Uri.EscapeDataString(bookId)}/chapters/{Uri.EscapeDataString(chapterId)}/notes",
            new UpdateChapterNotesRequest(notes),
            cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public async Task UploadChapterAudioAsync(string bookId, string chapterId, IBrowserFile audioFile, CancellationToken cancellationToken = default)
    {
        using var content = new MultipartFormDataContent();
        await using var stream = audioFile.OpenReadStream(104_857_600, cancellationToken);
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(audioFile.ContentType);
        content.Add(fileContent, "AudioFile", audioFile.Name);

        using var response = await _httpClient.PostAsync($"api/study/books/{Uri.EscapeDataString(bookId)}/chapters/{Uri.EscapeDataString(chapterId)}/audio", content, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public string GetChapterAudioUrl(string bookId, string chapterId)
        => new Uri(_httpClient.BaseAddress!, $"api/study/books/{Uri.EscapeDataString(bookId)}/chapters/{Uri.EscapeDataString(chapterId)}/audio").ToString();

    public Task<IReadOnlyList<StudyPlanSummaryResponse>?> GetStudyPlansAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<StudyPlanSummaryResponse>>("api/study/plans", cancellationToken);

    public Task<StudyPlanDetailResponse> CreateStudyPlanAsync(CreateStudyPlanRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateStudyPlanRequest, StudyPlanDetailResponse>("api/study/plans", request, cancellationToken);

    public Task<StudyPlanDetailResponse?> GetStudyPlanAsync(string planId, CancellationToken cancellationToken = default)
        => GetAsync<StudyPlanDetailResponse>($"api/study/plans/{Uri.EscapeDataString(planId)}", cancellationToken);

    public Task<StudyPlanDetailResponse> AutoGeneratePlanItemsAsync(string planId, CancellationToken cancellationToken = default)
        => PostAsync<object, StudyPlanDetailResponse>($"api/study/plans/{Uri.EscapeDataString(planId)}/auto-generate", new { }, cancellationToken);

    public Task<PlanItemResponse> UpdatePlanItemStatusAsync(string planId, string itemId, string status, CancellationToken cancellationToken = default)
        => PatchAsync<UpdatePlanItemStatusRequest, PlanItemResponse>($"api/study/plans/{Uri.EscapeDataString(planId)}/items/{Uri.EscapeDataString(itemId)}/status", new UpdatePlanItemStatusRequest(status), cancellationToken);

    public Task<TodayStudyItemsResponse?> GetTodayStudyItemsAsync(CancellationToken cancellationToken = default)
        => GetAsync<TodayStudyItemsResponse>("api/study/today", cancellationToken);

    public Task<IReadOnlyList<CalendarDayResponse>?> GetCalendarDataAsync(int year, int month, CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<CalendarDayResponse>>($"api/study/calendar?year={year}&month={month}", cancellationToken);

    public Task<IReadOnlyList<DeckSummaryResponse>?> GetDecksAsync(CancellationToken cancellationToken = default)
        => GetAsync<IReadOnlyList<DeckSummaryResponse>>("api/study/decks", cancellationToken);

    public Task<DeckDetailResponse> CreateDeckAsync(CreateDeckRequest request, CancellationToken cancellationToken = default)
        => PostAsync<CreateDeckRequest, DeckDetailResponse>("api/study/decks", request, cancellationToken);

    public Task<DeckDetailResponse?> GetDeckAsync(string deckId, CancellationToken cancellationToken = default)
        => GetAsync<DeckDetailResponse>($"api/study/decks/{Uri.EscapeDataString(deckId)}", cancellationToken);

    public async Task DeleteDeckAsync(string deckId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/study/decks/{Uri.EscapeDataString(deckId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<FlashCardResponse> AddFlashCardAsync(string deckId, AddFlashCardRequest request, CancellationToken cancellationToken = default)
        => PostAsync<AddFlashCardRequest, FlashCardResponse>($"api/study/decks/{Uri.EscapeDataString(deckId)}/cards", request, cancellationToken);

    public Task<FlashCardResponse> UpdateFlashCardAsync(string deckId, string cardId, AddFlashCardRequest request, CancellationToken cancellationToken = default)
        => PutAsync<AddFlashCardRequest, FlashCardResponse>($"api/study/decks/{Uri.EscapeDataString(deckId)}/cards/{Uri.EscapeDataString(cardId)}", request, cancellationToken);

    public async Task DeleteFlashCardAsync(string deckId, string cardId, CancellationToken cancellationToken = default)
    {
        using var response = await _httpClient.DeleteAsync($"api/study/decks/{Uri.EscapeDataString(deckId)}/cards/{Uri.EscapeDataString(cardId)}", cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
    }

    public Task<GeneratedCardsResponse> GenerateCardsFromChapterAsync(string deckId, string chapterId, CancellationToken cancellationToken = default)
        => PostAsync<object, GeneratedCardsResponse>($"api/study/decks/{Uri.EscapeDataString(deckId)}/generate-from-chapter/{Uri.EscapeDataString(chapterId)}", new { }, cancellationToken);

    public Task<DueCardsResponse?> GetDueCardsAsync(CancellationToken cancellationToken = default)
        => GetAsync<DueCardsResponse>("api/study/review/due", cancellationToken);

    public Task<ReviewResultResponse> ReviewCardAsync(string cardId, int quality, CancellationToken cancellationToken = default)
        => PostAsync<ReviewCardRequest, ReviewResultResponse>($"api/study/review/{Uri.EscapeDataString(cardId)}", new ReviewCardRequest(quality), cancellationToken);

    public Task<ReviewStatsResponse?> GetReviewStatsAsync(CancellationToken cancellationToken = default)
        => GetAsync<ReviewStatsResponse>("api/study/review/stats", cancellationToken);

    public Task<StudySessionResponse> StartStudySessionAsync(StartStudySessionRequest request, CancellationToken cancellationToken = default)
        => PostAsync<StartStudySessionRequest, StudySessionResponse>("api/study/sessions", request, cancellationToken);

    public Task<StudySessionResponse> EndStudySessionAsync(string sessionId, CancellationToken cancellationToken = default)
        => PatchAsync<object, StudySessionResponse>($"api/study/sessions/{Uri.EscapeDataString(sessionId)}/end", new { }, cancellationToken);

    public Task<StudyDashboardResponse?> GetStudyDashboardAsync(CancellationToken cancellationToken = default)
        => GetAsync<StudyDashboardResponse>("api/study/dashboard", cancellationToken);

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

    private async Task<TResponse> PutAsync<TRequest, TResponse>(string url, TRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync(url, request, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The server returned an empty response.");
    }

    private async Task<TResponse> PatchAsync<TRequest, TResponse>(
        string url,
        TRequest request,
        CancellationToken cancellationToken)
    {
        using var httpRequest = new HttpRequestMessage(HttpMethod.Patch, url)
        {
            Content = JsonContent.Create(request)
        };
        using var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
        await EnsureSuccessAsync(response, cancellationToken);
        var payload = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
        return payload ?? throw new InvalidOperationException("The server returned an empty response.");
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
