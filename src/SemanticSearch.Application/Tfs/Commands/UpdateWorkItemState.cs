using FluentValidation;
using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;
using Microsoft.Extensions.Options;

namespace SemanticSearch.Application.Tfs.Commands;

public sealed record UpdateWorkItemStateCommand(int WorkItemId, string State)
    : IRequest<TfsWorkItemUpdateResult>;

public sealed class UpdateWorkItemStateCommandHandler : IRequestHandler<UpdateWorkItemStateCommand, TfsWorkItemUpdateResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;

    public UpdateWorkItemStateCommandHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ITfsApiClient tfsClient)
    {
        _repo = repo;
        _encryption = encryption;
        _tfsClient = tfsClient;
    }

    public async Task<TfsWorkItemUpdateResult> Handle(UpdateWorkItemStateCommand request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null)
            return new TfsWorkItemUpdateResult(false, "TFS credentials are not configured.", null);
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        return await _tfsClient.UpdateWorkItemStateAsync(cred.ServerUrl, pat, request.WorkItemId, request.State, cancellationToken);
    }
}

public sealed class UpdateWorkItemStateCommandValidator : AbstractValidator<UpdateWorkItemStateCommand>
{
    public UpdateWorkItemStateCommandValidator()
    {
        RuleFor(x => x.WorkItemId)
            .GreaterThan(0).WithMessage("Work item ID must be a positive integer.");

        RuleFor(x => x.State)
            .NotEmpty().WithMessage("State is required.")
            .MaximumLength(100).WithMessage("State name must be 100 characters or fewer.");
    }
}
