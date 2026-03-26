using FluentValidation;
using MediatR;
using SemanticSearch.Application.Common;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Commands;

public sealed record AddWorkItemCommentCommand(int WorkItemId, string Text)
    : IRequest<TfsWorkItemComment?>;

public sealed class AddWorkItemCommentCommandHandler : IRequestHandler<AddWorkItemCommentCommand, TfsWorkItemComment?>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;
    private readonly ITfsApiClient _tfsClient;

    public AddWorkItemCommentCommandHandler(
        ICredentialRepository repo,
        ICredentialEncryption encryption,
        ITfsApiClient tfsClient)
    {
        _repo = repo;
        _encryption = encryption;
        _tfsClient = tfsClient;
    }

    public async Task<TfsWorkItemComment?> Handle(AddWorkItemCommentCommand request, CancellationToken cancellationToken)
    {
        var cred = await _repo.GetTfsCredentialAsync(cancellationToken);
        if (cred is null) return null;
        var pat = _encryption.Decrypt(cred.EncryptedPat);
        return await _tfsClient.AddWorkItemCommentAsync(cred.ServerUrl, pat, request.WorkItemId, request.Text, cancellationToken);
    }
}

public sealed class AddWorkItemCommentCommandValidator : AbstractValidator<AddWorkItemCommentCommand>
{
    public AddWorkItemCommentCommandValidator()
    {
        RuleFor(x => x.WorkItemId)
            .GreaterThan(0).WithMessage("Work item ID must be a positive integer.");

        RuleFor(x => x.Text)
            .NotEmpty().WithMessage("Comment text is required.")
            .MaximumLength(4000).WithMessage("Comment text must not exceed 4000 characters.");
    }
}
