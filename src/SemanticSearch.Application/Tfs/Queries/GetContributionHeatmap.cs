using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record ContributionHeatmapResult(IReadOnlyList<ContributionDay> Days, string Username);

public sealed record GetContributionHeatmapQuery(int Months = 12) : IRequest<ContributionHeatmapResult>;

public sealed class GetContributionHeatmapQueryHandler : IRequestHandler<GetContributionHeatmapQuery, ContributionHeatmapResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;
    private readonly IntegrationOptions _options;

    public GetContributionHeatmapQueryHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ITfsApiClient tfsClient,
        IOptions<IntegrationOptions> options)
    {
        _repo = repo;
        _encryption = encryption;
        _tfsClient = tfsClient;
        _options = options.Value;
    }

    public async Task<ContributionHeatmapResult> Handle(GetContributionHeatmapQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return new ContributionHeatmapResult([], string.Empty);
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        var days = await _tfsClient.GetContributionDataAsync(cred.ServerUrl, pat, cred.Username, _options.TfsApiVersion, request.Months, cancellationToken);
        return new ContributionHeatmapResult(days, cred.Username);
    }
}
