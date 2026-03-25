using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record GetMyPullRequestsQuery : IRequest<IReadOnlyList<TfsPullRequest>>;

public sealed class GetMyPullRequestsQueryHandler : IRequestHandler<GetMyPullRequestsQuery, IReadOnlyList<TfsPullRequest>>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;
    private readonly IntegrationOptions _options;

    public GetMyPullRequestsQueryHandler(
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

    public async Task<IReadOnlyList<TfsPullRequest>> Handle(GetMyPullRequestsQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return [];
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        return await _tfsClient.GetActivePullRequestsAsync(cred.ServerUrl, pat, cred.Username, _options.TfsApiVersion, cancellationToken);
    }
}
