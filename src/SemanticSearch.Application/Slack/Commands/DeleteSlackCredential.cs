using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Commands;

public sealed record DeleteSlackCredentialCommand : IRequest;

public sealed class DeleteSlackCredentialCommandHandler : IRequestHandler<DeleteSlackCredentialCommand>
{
    private readonly ICredentialRepository _repo;

    public DeleteSlackCredentialCommandHandler(ICredentialRepository repo)
    {
        _repo = repo;
    }

    public Task Handle(DeleteSlackCredentialCommand request, CancellationToken cancellationToken)
        => _repo.DeleteSlackCredentialAsync(cancellationToken);
}
