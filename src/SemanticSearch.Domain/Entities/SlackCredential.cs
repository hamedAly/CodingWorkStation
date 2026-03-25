namespace SemanticSearch.Domain.Entities;

public sealed class SlackCredential
{
    public string CredentialId { get; init; } = string.Empty;
    public string EncryptedBotToken { get; init; } = string.Empty;
    public string? EncryptedUserToken { get; init; }
    public string DefaultChannel { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
