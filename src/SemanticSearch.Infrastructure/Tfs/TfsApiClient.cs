using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Tfs;

public sealed class TfsApiClient : ITfsApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TfsApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public TfsApiClient(IHttpClientFactory httpClientFactory, ILogger<TfsApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string serverUrl, string pat)
    {
        var client = _httpClientFactory.CreateClient("TfsClient");
        client.BaseAddress = new Uri(serverUrl.TrimEnd('/') + "/");
        var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);
        client.DefaultRequestHeaders.Accept.Clear();
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        return client;
    }

    public async Task<TfsConnectionTestResult> TestConnectionAsync(string serverUrl, string pat, CancellationToken cancellationToken = default)
    {
        // Try connectionData without api-version first (works on all TFS/AzDevOps versions),
        // then fall back to explicit versions supported by Azure DevOps Server 2019–2022.
        var probeUrls = new[]
        {
            "_apis/connectionData",
            "_apis/connectionData?api-version=7.1",
            "_apis/connectionData?api-version=6.0",
            "_apis/connectionData?api-version=5.1",
        };

        HttpClient client;
        try { client = CreateClient(serverUrl, pat); }
        catch (UriFormatException)
        {
            return new TfsConnectionTestResult(false, "Invalid server URL format. Use https://server/collection");
        }

        HttpResponseMessage? lastResponse = null;
        Exception? lastException = null;

        foreach (var probe in probeUrls)
        {
            try
            {
                using var response = await client.GetAsync(probe, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("TFS connection test succeeded for {ServerUrl} via {Probe}.", serverUrl, probe);
                    return new TfsConnectionTestResult(true, null);
                }

                lastResponse = response;

                // 401 = wrong credentials — no point retrying other api-versions
                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    break;
            }
            catch (HttpRequestException ex) when (ex.Message.Contains("SSL") || ex.Message.Contains("certificate") || ex.Message.Contains("TLS"))
            {
                _logger.LogWarning(ex, "TFS SSL error for {ServerUrl}.", serverUrl);
                return new TfsConnectionTestResult(false,
                    "SSL/TLS error connecting to the server. If this is an on-premises server with a self-signed certificate, " +
                    "enable 'IgnoreTlsErrors' in appsettings.json under SemanticSearch:Integration.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "TFS network error for {ServerUrl}.", serverUrl);
                lastException = ex;
                break; // Network unreachable — retrying with different api-version won't help
            }
            catch (TaskCanceledException)
            {
                return new TfsConnectionTestResult(false, "Connection timed out. Check that the server URL is reachable.");
            }
        }

        if (lastException is not null)
            return new TfsConnectionTestResult(false, $"Could not reach server: {lastException.Message}");

        if (lastResponse is not null)
        {
            var statusCode = (int)lastResponse.StatusCode;
            var hint = lastResponse.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    "Authentication failed (401). Verify your PAT has not expired and has 'Read' scope on your Azure DevOps organization.",
                System.Net.HttpStatusCode.Forbidden =>
                    "Access denied (403). The PAT may lack required permissions.",
                System.Net.HttpStatusCode.NotFound =>
                    "Server URL not found (404). Check the collection path, e.g. https://dev.azure.com/org or https://server/DefaultCollection.",
                System.Net.HttpStatusCode.ServiceUnavailable or System.Net.HttpStatusCode.BadGateway =>
                    $"Server is unavailable ({statusCode}). Try again later.",
                _ => $"Server returned HTTP {statusCode}."
            };
            return new TfsConnectionTestResult(false, hint, statusCode);
        }

        return new TfsConnectionTestResult(false, "Connection failed for an unknown reason.");
    }

    public async Task<IReadOnlyList<TfsWorkItem>> GetAssignedWorkItemsAsync(
        string serverUrl, string pat, string username, string apiVersion,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);

        // Try api-versions in descending order; on-prem Azure DevOps Server 2019/2020
        // does not support 7.1, so we fall back to 6.0 and 5.1.
        var apiVersionsToTry = new[] { apiVersion, "6.0", "5.1", "5.0" }
            .Distinct()
            .ToArray();

        string? lastWiqlError = null;
        int? lastWiqlStatus = null;

        foreach (var ver in apiVersionsToTry)
        {
            // Try @Me first (correct when PAT resolves identity), then fall back to
            // username string (e.g. "hamed.aly@paxerahealth.com") if @Me yields nothing.
            var queriesToTry = new List<string>
            {
                $"SELECT [System.Id] FROM WorkItems WHERE [System.AssignedTo] = @Me AND [System.State] <> 'Closed' AND [System.State] <> 'Done' AND [System.State] <> 'Removed' ORDER BY [System.ChangedDate] DESC"
            };
            if (!string.IsNullOrWhiteSpace(username))
            {
                queriesToTry.Add(
                    $"SELECT [System.Id] FROM WorkItems WHERE [System.AssignedTo] = '{username.Replace("'", "''")}' AND [System.State] <> 'Closed' AND [System.State] <> 'Done' AND [System.State] <> 'Removed' ORDER BY [System.ChangedDate] DESC");
            }

            foreach (var wiql in queriesToTry)
            {
                var wiqlBody = JsonSerializer.Serialize(new { query = wiql });
                using var wiqlResponse = await client.PostAsync(
                    $"_apis/wit/wiql?api-version={ver}",
                    new StringContent(wiqlBody, Encoding.UTF8, "application/json"),
                    cancellationToken);

                _logger.LogDebug("WIQL api-version={Ver} → HTTP {Status}", ver, (int)wiqlResponse.StatusCode);

                if (!wiqlResponse.IsSuccessStatusCode)
                {
                    lastWiqlStatus = (int)wiqlResponse.StatusCode;
                    lastWiqlError = await wiqlResponse.Content.ReadAsStringAsync(cancellationToken);
                    break; // This api-version failed — try the next one
                }

                var wiqlJson = await wiqlResponse.Content.ReadAsStringAsync(cancellationToken);
                var wiqlNode = JsonNode.Parse(wiqlJson);
                var workItemRefs = wiqlNode?["workItems"]?.AsArray();

                if (workItemRefs is null || workItemRefs.Count == 0)
                {
                    // @Me returned nothing — retry with explicit username (handled by next loop iteration)
                    lastWiqlError = null;
                    lastWiqlStatus = null;
                    continue;
                }

                // Got results — fetch the full work item details
                var ids = workItemRefs
                    .Select(r => r?["id"]?.GetValue<int>())
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .Take(200)
                    .ToList();

                var idsParam = string.Join(",", ids);
                using var itemsResponse = await client.GetAsync(
                    $"_apis/wit/workitems?ids={idsParam}&fields=System.Id,System.Title,System.WorkItemType,System.State,System.AssignedTo,System.TeamProject,System.AreaPath,System.IterationPath,Microsoft.VSTS.Common.Priority,System.CreatedDate,System.ChangedDate&api-version={ver}",
                    cancellationToken);

                if (!itemsResponse.IsSuccessStatusCode)
                {
                    var errBody = await itemsResponse.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Work items batch fetch failed HTTP {Status}: {Body}", (int)itemsResponse.StatusCode, errBody);
                    throw new InvalidOperationException($"Fetched {ids.Count} work item IDs but the detail request failed (HTTP {(int)itemsResponse.StatusCode}). Check API version compatibility.");
                }

                var itemsJson = await itemsResponse.Content.ReadAsStringAsync(cancellationToken);
                var itemsNode = JsonNode.Parse(itemsJson);
                var values = itemsNode?["value"]?.AsArray();
                if (values is null) return [];

                _logger.LogInformation("Loaded {Count} work items from TFS via api-version={Ver}.", values.Count, ver);

                return values.Select(v =>
                {
                    var f = v?["fields"];
                    // System.AssignedTo can be an object {displayName, id, ...} in API 7.x
                    // or a plain string (display name) in older API versions
                    var assignedToNode = f?["System.AssignedTo"];
                    var assignedTo = assignedToNode is JsonObject
                        ? assignedToNode["displayName"]?.GetValue<string>()
                        : assignedToNode?.GetValue<string>();

                    return new TfsWorkItem(
                        Id: v?["id"]?.GetValue<int>() ?? 0,
                        Title: f?["System.Title"]?.GetValue<string>() ?? string.Empty,
                        WorkItemType: f?["System.WorkItemType"]?.GetValue<string>() ?? string.Empty,
                        State: f?["System.State"]?.GetValue<string>() ?? string.Empty,
                        AssignedTo: assignedTo,
                        TeamProject: f?["System.TeamProject"]?.GetValue<string>(),
                        AreaPath: f?["System.AreaPath"]?.GetValue<string>(),
                        IterationPath: f?["System.IterationPath"]?.GetValue<string>(),
                        Priority: f?["Microsoft.VSTS.Common.Priority"]?.ToString(),
                        CreatedDate: TryParseDate(f?["System.CreatedDate"]?.GetValue<string>()),
                        ChangedDate: TryParseDate(f?["System.ChangedDate"]?.GetValue<string>()),
                        Url: BuildWorkItemWebUrl(serverUrl, v?["id"]?.GetValue<int>() ?? 0)
                    );
                }).ToList();
            }
        }

        // All api-versions and query strategies failed
        if (lastWiqlStatus.HasValue)
        {
            _logger.LogError("WIQL query failed after all api-version attempts. Last status: {Status}, body: {Body}", lastWiqlStatus, lastWiqlError);
            throw new InvalidOperationException(
                $"The TFS server rejected the work items query (HTTP {lastWiqlStatus}). " +
                $"This usually means the API version is not supported by this Azure DevOps Server version. Details: {Truncate(lastWiqlError, 300)}");
        }

        // All queries returned 0 results (no error, genuinely empty)
        return [];
    }

    private static string BuildWorkItemWebUrl(string serverUrl, int id)
    {
        var baseUrl = serverUrl.TrimEnd('/');
        return $"{baseUrl}/_workitems/edit/{id}";
    }

    private static string? Truncate(string? value, int maxLength) =>
        value is null ? null : value.Length <= maxLength ? value : value[..maxLength] + "…";

    public async Task<IReadOnlyList<TfsPullRequest>> GetActivePullRequestsAsync(
        string serverUrl, string pat, string username, string apiVersion,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);

        var byCreator = await FetchPullRequestsAsync(client, apiVersion, $"searchCriteria.creatorId={Uri.EscapeDataString(username)}", cancellationToken);
        var byReviewer = await FetchPullRequestsAsync(client, apiVersion, $"searchCriteria.reviewerId={Uri.EscapeDataString(username)}", cancellationToken);

        var all = byCreator.Concat(byReviewer)
            .GroupBy(pr => pr.Id)
            .Select(g => g.First())
            .ToList();

        return all;
    }

    private async Task<IReadOnlyList<TfsPullRequest>> FetchPullRequestsAsync(
        HttpClient client, string apiVersion, string searchParam, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.GetAsync(
                $"_apis/git/pullrequests?{searchParam}&searchCriteria.status=active&api-version={apiVersion}",
                cancellationToken);

            if (!response.IsSuccessStatusCode) return [];

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            var values = node?["value"]?.AsArray();
            if (values is null) return [];

            return values.Select(v =>
            {
                var reviewers = v?["reviewers"]?.AsArray()?.Select(r => new TfsPrReviewer(
                    DisplayName: r?["displayName"]?.GetValue<string>() ?? string.Empty,
                    Vote: r?["vote"]?.GetValue<int>() ?? 0,
                    VoteLabel: MapVote(r?["vote"]?.GetValue<int>() ?? 0)
                )).ToList() ?? [];

                return new TfsPullRequest(
                    Id: v?["pullRequestId"]?.GetValue<int>() ?? 0,
                    Title: v?["title"]?.GetValue<string>() ?? string.Empty,
                    SourceBranch: (v?["sourceRefName"]?.GetValue<string>() ?? string.Empty).Replace("refs/heads/", ""),
                    TargetBranch: (v?["targetRefName"]?.GetValue<string>() ?? string.Empty).Replace("refs/heads/", ""),
                    Status: v?["status"]?.GetValue<string>() ?? string.Empty,
                    CreatedBy: v?["createdBy"]?["displayName"]?.GetValue<string>() ?? string.Empty,
                    CreationDate: TryParseDate(v?["creationDate"]?.GetValue<string>()) ?? DateTime.UtcNow,
                    Reviewers: reviewers,
                    Url: v?["url"]?.GetValue<string>() ?? string.Empty
                );
            }).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch pull requests.");
            return [];
        }
    }

    public async Task<IReadOnlyList<ContributionDay>> GetContributionDataAsync(
        string serverUrl, string pat, string username, string apiVersion,
        int months, CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);
        var since = DateTime.UtcNow.AddMonths(-months);
        var counts = new Dictionary<DateOnly, int>();

        var versions = new[] { apiVersion, "6.0", "5.1", "5.0" }.Distinct().ToArray();

        // ── 1. Git commits ────────────────────────────────────────────────────
        // TFS on-prem uses US-style date format for searchCriteria.fromDate.
        // We try both ISO and US formats so it works on all versions.
        var isoDate = since.ToString("yyyy-MM-dd");
        var usDate = since.ToString("MM/dd/yyyy HH:mm:ss");

        bool commitsLoaded = false;
        foreach (var ver in versions)
        {
            foreach (var dateStr in new[] { isoDate, usDate })
            {
                try
                {
                    // author filter accepts email or display name — try the stored username
                    var url = $"_apis/git/commits?searchCriteria.author={Uri.EscapeDataString(username)}" +
                              $"&searchCriteria.fromDate={Uri.EscapeDataString(dateStr)}" +
                              $"&searchCriteria.$top=1000&api-version={ver}";

                    using var resp = await client.GetAsync(url, cancellationToken);
                    _logger.LogDebug("Git commits api-version={Ver} dateFormat={Date} → HTTP {Status}", ver, dateStr, (int)resp.StatusCode);

                    if (!resp.IsSuccessStatusCode) continue;

                    var json = await resp.Content.ReadAsStringAsync(cancellationToken);
                    var node = JsonNode.Parse(json);
                    var commits = node?["value"]?.AsArray() ?? [];

                    foreach (var commit in commits)
                    {
                        // committer.date or author.date
                        var d = TryParseDate(commit?["committer"]?["date"]?.GetValue<string>())
                             ?? TryParseDate(commit?["author"]?["date"]?.GetValue<string>());
                        if (d is not null)
                        {
                            var day = DateOnly.FromDateTime(d.Value);
                            counts[day] = counts.GetValueOrDefault(day) + 1;
                        }
                    }

                    _logger.LogInformation("Loaded {Count} commits from TFS via api-version={Ver}.", commits.Count, ver);
                    commitsLoaded = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Commits probe api-version={Ver} failed.", ver);
                }
            }
            if (commitsLoaded) break;
        }

        if (!commitsLoaded)
            _logger.LogWarning("Could not load git commits for contribution heatmap — all api-versions failed.");

        // ── 2. Work item changes ──────────────────────────────────────────────
        // WIQL returns only id+url — we must batch-fetch to get ChangedDate.
        // Use explicit username as fallback since @Me may not resolve via PAT.
        var wiqlQueries = new List<string>
        {
            $"SELECT [System.Id] FROM WorkItems WHERE [System.ChangedBy] = @Me AND [System.ChangedDate] >= '{isoDate}T00:00:00.000Z' ORDER BY [System.ChangedDate] DESC",
        };
        if (!string.IsNullOrWhiteSpace(username))
        {
            wiqlQueries.Add(
                $"SELECT [System.Id] FROM WorkItems WHERE [System.ChangedBy] = '{username.Replace("'", "''")}' AND [System.ChangedDate] >= '{isoDate}T00:00:00.000Z' ORDER BY [System.ChangedDate] DESC");
        }

        bool workItemsLoaded = false;
        foreach (var ver in versions)
        {
            foreach (var wiql in wiqlQueries)
            {
                try
                {
                    var wiqlBody = JsonSerializer.Serialize(new { query = wiql });
                    using var wiResp = await client.PostAsync(
                        $"_apis/wit/wiql?$top=1000&api-version={ver}",
                        new StringContent(wiqlBody, Encoding.UTF8, "application/json"),
                        cancellationToken);

                    _logger.LogDebug("WIQL contributions api-version={Ver} → HTTP {Status}", ver, (int)wiResp.StatusCode);

                    if (!wiResp.IsSuccessStatusCode) continue;

                    var wiJson = await wiResp.Content.ReadAsStringAsync(cancellationToken);
                    var wiNode = JsonNode.Parse(wiJson);
                    var refs = wiNode?["workItems"]?.AsArray();
                    if (refs is null || refs.Count == 0)
                    {
                        // @Me returned nothing — try explicit username
                        continue;
                    }

                    // Batch-fetch ChangedDate for up to 200 items at a time
                    var ids = refs
                        .Select(r => r?["id"]?.GetValue<int>())
                        .Where(id => id.HasValue)
                        .Select(id => id!.Value)
                        .Take(500)
                        .ToList();

                    for (int i = 0; i < ids.Count; i += 200)
                    {
                        var batch = ids.Skip(i).Take(200);
                        using var batchResp = await client.GetAsync(
                            $"_apis/wit/workitems?ids={string.Join(",", batch)}&fields=System.Id,System.ChangedDate&api-version={ver}",
                            cancellationToken);

                        if (!batchResp.IsSuccessStatusCode) continue;

                        var batchJson = await batchResp.Content.ReadAsStringAsync(cancellationToken);
                        var batchNode = JsonNode.Parse(batchJson);
                        foreach (var item in batchNode?["value"]?.AsArray() ?? [])
                        {
                            var dateStr = item?["fields"]?["System.ChangedDate"]?.GetValue<string>();
                            if (TryParseDate(dateStr) is { } date && date >= since)
                            {
                                var day = DateOnly.FromDateTime(date);
                                counts[day] = counts.GetValueOrDefault(day) + 1;
                            }
                        }
                    }

                    _logger.LogInformation("Counted {Count} work item change days via api-version={Ver}.", ids.Count, ver);
                    workItemsLoaded = true;
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Work items contributions probe api-version={Ver} failed.", ver);
                }
            }
            if (workItemsLoaded) break;
        }

        if (!workItemsLoaded)
            _logger.LogWarning("Could not load work item changes for contribution heatmap — all api-versions/queries failed.");

        // Fill in every calendar day in the range (including zeros) so the grid renders fully
        var allDays = Enumerable.Range(0, months * 31)
            .Select(i => DateOnly.FromDateTime(since.Date.AddDays(i)))
            .Where(d => d <= DateOnly.FromDateTime(DateTime.UtcNow))
            .ToList();

        if (counts.Count == 0)
            return allDays.Select(d => new ContributionDay(d, 0, 0)).ToList();

        var maxCount = counts.Values.Max();
        return allDays.Select(d =>
        {
            var count = counts.GetValueOrDefault(d, 0);
            var level = (maxCount == 0 || count == 0) ? 0 : Math.Clamp((int)Math.Ceiling((double)count / maxCount * 4), 1, 4);
            return new ContributionDay(d, count, level);
        }).ToList();
    }

    private static string MapVote(int vote) => vote switch
    {
        10 => "Approved",
        5 => "Approved with suggestions",
        0 => "No vote",
        -5 => "Waiting for author",
        -10 => "Rejected",
        _ => "No vote"
    };

    private static DateTime? TryParseDate(string? value)
        => DateTime.TryParse(value, out var dt) ? dt : null;

    public async Task<TfsWorkItemUpdateResult> UpdateWorkItemStateAsync(
        string serverUrl, string pat, int workItemId, string newState,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);
        // JSON Patch requires a special content type
        var patchBody = JsonSerializer.Serialize(new[]
        {
            new { op = "replace", path = "/fields/System.State", value = newState }
        });

        var versions = new[] { "7.1", "6.0", "5.1" };
        foreach (var ver in versions)
        {
            try
            {
                using var request = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"_apis/wit/workitems/{workItemId}?api-version={ver}");
                request.Content = new StringContent(patchBody, Encoding.UTF8, "application/json-patch+json");

                using var response = await client.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync(cancellationToken);
                    var node = JsonNode.Parse(json);
                    var confirmedState = node?["fields"]?["System.State"]?.GetValue<string>();
                    _logger.LogInformation("Updated work item {Id} state to {State} via api-version={Ver}.", workItemId, newState, ver);
                    return new TfsWorkItemUpdateResult(true, null, confirmedState ?? newState);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                    var errorNode = JsonNode.Parse(errorBody);
                    var message = errorNode?["message"]?.GetValue<string>() ?? errorBody;
                    _logger.LogWarning("TFS rejected state update for work item {Id}: {Error}", workItemId, message);
                    return new TfsWorkItemUpdateResult(false, $"TFS Error: {Truncate(message, 300)}", null);
                }

                if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    return new TfsWorkItemUpdateResult(false, "Authentication failed (401). Check your PAT.", null);

                if ((int)response.StatusCode is 404)
                    return new TfsWorkItemUpdateResult(false, $"Work item {workItemId} not found.", null);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error updating work item {Id}.", workItemId);
                return new TfsWorkItemUpdateResult(false, $"Network error: {ex.Message}", null);
            }
        }

        return new TfsWorkItemUpdateResult(false, "Failed to update work item state — all API versions failed.", null);
    }

    public async Task<IReadOnlyList<TfsWorkItemComment>> GetWorkItemCommentsAsync(
        string serverUrl, string pat, int workItemId,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);
        try
        {
            using var response = await client.GetAsync(
                $"_apis/wit/workitems/{workItemId}/comments?api-version=7.1-preview.4",
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Failed to load comments for work item {Id}: HTTP {Status}", workItemId, (int)response.StatusCode);
                return [];
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            var comments = node?["comments"]?.AsArray();
            if (comments is null) return [];

            return comments.Select(c => new TfsWorkItemComment(
                Id: c?["id"]?.GetValue<int>() ?? 0,
                Text: c?["text"]?.GetValue<string>() ?? string.Empty,
                CreatedBy: c?["createdBy"]?["displayName"]?.GetValue<string>()
                           ?? c?["createdBy"]?.GetValue<string>()
                           ?? "Unknown",
                CreatedDate: TryParseDate(c?["createdDate"]?.GetValue<string>()) ?? DateTime.UtcNow
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception loading comments for work item {Id}.", workItemId);
            return [];
        }
    }

    public async Task<TfsWorkItemComment?> AddWorkItemCommentAsync(
        string serverUrl, string pat, int workItemId, string text,
        CancellationToken cancellationToken = default)
    {
        var client = CreateClient(serverUrl, pat);
        var body = JsonSerializer.Serialize(new { text });
        try
        {
            using var response = await client.PostAsync(
                $"_apis/wit/workitems/{workItemId}/comments?api-version=7.1-preview.4",
                new StringContent(body, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync(cancellationToken);
                var c = JsonNode.Parse(json);
                return new TfsWorkItemComment(
                    Id: c?["id"]?.GetValue<int>() ?? 0,
                    Text: c?["text"]?.GetValue<string>() ?? text,
                    CreatedBy: c?["createdBy"]?["displayName"]?.GetValue<string>()
                               ?? c?["createdBy"]?.GetValue<string>()
                               ?? "Unknown",
                    CreatedDate: TryParseDate(c?["createdDate"]?.GetValue<string>()) ?? DateTime.UtcNow
                );
            }

            // Fallback for older TFS: write to System.History field via PATCH
            _logger.LogWarning("Comments API not available (HTTP {Status}) for work item {Id}; falling back to System.History.", (int)response.StatusCode, workItemId);
            var patchBody = JsonSerializer.Serialize(new[]
            {
                new { op = "add", path = "/fields/System.History", value = text }
            });
            var versions = new[] { "7.1", "6.0", "5.1" };
            foreach (var ver in versions)
            {
                using var patchRequest = new HttpRequestMessage(
                    HttpMethod.Patch,
                    $"_apis/wit/workitems/{workItemId}?api-version={ver}");
                patchRequest.Content = new StringContent(patchBody, Encoding.UTF8, "application/json-patch+json");
                using var patchResponse = await client.SendAsync(patchRequest, cancellationToken);
                if (patchResponse.IsSuccessStatusCode)
                {
                    return new TfsWorkItemComment(0, text, "You", DateTime.UtcNow);
                }
            }

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Exception adding comment to work item {Id}.", workItemId);
            return null;
        }
    }
}
