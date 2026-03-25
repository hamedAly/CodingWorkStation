namespace SemanticSearch.Domain.Entities;

public sealed class TfsCredential
{
    public string CredentialId { get; init; } = string.Empty;
    public string ServerUrl { get; init; } = string.Empty;
    public string EncryptedPat { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public DateTime CreatedUtc { get; init; }
    public DateTime UpdatedUtc { get; init; }
}
