using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Queries;

public sealed record SlackCredentialStatusModel(
    bool IsConfigured,
    bool HasUserToken,
    string? DefaultChannel,
    DateTime? UpdatedUtc);

public sealed record GetSlackCredentialStatusQuery : IRequest<SlackCredentialStatusModel>;

public sealed class GetSlackCredentialStatusQueryHandler : IRequestHandler<GetSlackCredentialStatusQuery, SlackCredentialStatusModel>
{
    private readonly ICredentialRepository _repo;

    public GetSlackCredentialStatusQueryHandler(ICredentialRepository repo)
    {
        _repo = repo;
    }

    public async Task<SlackCredentialStatusModel> Handle(GetSlackCredentialStatusQuery request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetSlackCredentialAsync(cancellationToken);
        if (cred is null) return new SlackCredentialStatusModel(false, false, null, null);
        return new SlackCredentialStatusModel(true, cred.EncryptedUserToken is not null, cred.DefaultChannel, cred.UpdatedUtc);
    }
}
