using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record GetMyWorkItemsQuery : IRequest<IReadOnlyList<TfsWorkItem>>;

public sealed class GetMyWorkItemsQueryHandler : IRequestHandler<GetMyWorkItemsQuery, IReadOnlyList<TfsWorkItem>>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;
    private readonly IntegrationOptions _options;

    public GetMyWorkItemsQueryHandler(
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

    public async Task<IReadOnlyList<TfsWorkItem>> Handle(GetMyWorkItemsQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return [];
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        return await _tfsClient.GetAssignedWorkItemsAsync(cred.ServerUrl, pat, cred.Username, _options.TfsApiVersion, cancellationToken);
    }
}
