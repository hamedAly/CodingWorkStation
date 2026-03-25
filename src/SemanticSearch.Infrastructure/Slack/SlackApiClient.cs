using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Infrastructure.Slack;

public sealed class SlackApiClient : ISlackApiClient
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SlackApiClient> _logger;

    public SlackApiClient(IHttpClientFactory httpClientFactory, ILogger<SlackApiClient> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private HttpClient CreateClient(string token)
    {
        var client = _httpClientFactory.CreateClient("SlackClient");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    public async Task<bool> TestConnectionAsync(string botToken, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(botToken);
            using var response = await client.PostAsync("auth.test", new StringContent("{}", Encoding.UTF8, "application/json"), cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            return node?["ok"]?.GetValue<bool>() == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Slack connection test failed.");
            return false;
        }
    }

    public async Task<bool> PostMessageAsync(string botToken, string channel, string text, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(botToken);
            var body = JsonSerializer.Serialize(new { channel, text });
            using var response = await client.PostAsync("chat.postMessage", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            return node?["ok"]?.GetValue<bool>() == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to post Slack message to {Channel}.", channel);
            return false;
        }
    }

    public async Task<bool> SetUserStatusAsync(
        string userToken, string statusText, string statusEmoji,
        long statusExpirationUnix, CancellationToken cancellationToken = default)
    {
        try
        {
            var client = CreateClient(userToken);
            var body = JsonSerializer.Serialize(new
            {
                profile = new
                {
                    status_text = statusText,
                    status_emoji = statusEmoji,
                    status_expiration = statusExpirationUnix
                }
            });
            using var response = await client.PostAsync("users.profile.set", new StringContent(body, Encoding.UTF8, "application/json"), cancellationToken);
            if (!response.IsSuccessStatusCode) return false;
            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var node = JsonNode.Parse(json);
            return node?["ok"]?.GetValue<bool>() == true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to set Slack user status.");
            return false;
        }
    }
}
