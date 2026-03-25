namespace SemanticSearch.Domain.Interfaces;

public interface ISlackApiClient
{
    Task<bool> TestConnectionAsync(string botToken, CancellationToken cancellationToken = default);
    Task<bool> PostMessageAsync(string botToken, string channel, string text, CancellationToken cancellationToken = default);
    Task<bool> SetUserStatusAsync(string userToken, string statusText, string statusEmoji, long statusExpirationUnix, CancellationToken cancellationToken = default);
}
