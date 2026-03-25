using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface ICredentialRepository
{
    Task<TfsCredential?> GetTfsCredentialAsync(CancellationToken cancellationToken = default);
    Task SaveTfsCredentialAsync(TfsCredential credential, CancellationToken cancellationToken = default);
    Task DeleteTfsCredentialAsync(CancellationToken cancellationToken = default);

    Task<SlackCredential?> GetSlackCredentialAsync(CancellationToken cancellationToken = default);
    Task SaveSlackCredentialAsync(SlackCredential credential, CancellationToken cancellationToken = default);
    Task DeleteSlackCredentialAsync(CancellationToken cancellationToken = default);
}
