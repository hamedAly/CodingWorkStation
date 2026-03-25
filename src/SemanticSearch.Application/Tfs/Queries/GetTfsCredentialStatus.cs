using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Queries;

public sealed record TfsCredentialStatusModel(
    bool IsConfigured,
    string? ServerUrl,
    string? Username,
    DateTime? UpdatedUtc);

public sealed record GetTfsCredentialStatusQuery : IRequest<TfsCredentialStatusModel>;

public sealed class GetTfsCredentialStatusQueryHandler : IRequestHandler<GetTfsCredentialStatusQuery, TfsCredentialStatusModel>
{
    private readonly ICredentialRepository _repo;

    public GetTfsCredentialStatusQueryHandler(ICredentialRepository repo)
    {
        _repo = repo;
    }

    public async Task<TfsCredentialStatusModel> Handle(GetTfsCredentialStatusQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return new TfsCredentialStatusModel(false, null, null, null);
        return new TfsCredentialStatusModel(true, cred.ServerUrl, cred.Username, cred.UpdatedUtc);
    }
}
