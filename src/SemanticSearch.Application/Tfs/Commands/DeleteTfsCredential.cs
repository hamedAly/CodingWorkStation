using MediatR;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Commands;

public sealed record DeleteTfsCredentialCommand : IRequest;

public sealed class DeleteTfsCredentialCommandHandler : IRequestHandler<DeleteTfsCredentialCommand>
{
    private readonly ICredentialRepository _repo;

    public DeleteTfsCredentialCommandHandler(ICredentialRepository repo)
    {
        _repo = repo;
    }

    public Task Handle(DeleteTfsCredentialCommand request, CancellationToken cancellationToken)
        => _repo.DeleteTfsCredentialAsync(cancellationToken);
}
