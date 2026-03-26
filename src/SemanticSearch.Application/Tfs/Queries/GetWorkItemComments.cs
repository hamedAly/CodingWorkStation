using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record GetWorkItemCommentsQuery(int WorkItemId)
    : IRequest<IReadOnlyList<TfsWorkItemComment>>;

public sealed class GetWorkItemCommentsQueryHandler : IRequestHandler<GetWorkItemCommentsQuery, IReadOnlyList<TfsWorkItemComment>>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;

    public GetWorkItemCommentsQueryHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ITfsApiClient tfsClient)
    {
        _repo = repo;
        _encryption = encryption;
        _tfsClient = tfsClient;
    }

    public async Task<IReadOnlyList<TfsWorkItemComment>> Handle(GetWorkItemCommentsQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return [];
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        return await _tfsClient.GetWorkItemCommentsAsync(cred.ServerUrl, pat, request.WorkItemId, cancellationToken);
    }
}
