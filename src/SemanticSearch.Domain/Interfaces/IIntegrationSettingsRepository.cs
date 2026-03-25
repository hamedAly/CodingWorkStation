using SemanticSearch.Domain.Entities;

namespace SemanticSearch.Domain.Interfaces;

public interface IIntegrationSettingsRepository
{
    Task<IntegrationSettings?> GetAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(IntegrationSettings settings, CancellationToken cancellationToken = default);
}
