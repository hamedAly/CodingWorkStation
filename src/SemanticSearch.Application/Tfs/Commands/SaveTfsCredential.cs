using FluentValidation;
using MediatR;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Tfs.Commands;

public sealed record SaveTfsCredentialCommand(
    string ServerUrl,
    string Pat,
    string Username) : IRequest<TfsCredentialSaveResult>;

public sealed record TfsCredentialSaveResult(bool Success, string? Error = null);

public sealed class SaveTfsCredentialCommandHandler : IRequestHandler<SaveTfsCredentialCommand, TfsCredentialSaveResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;

    public SaveTfsCredentialCommandHandler(ICredentialRepository repo, ICredentialEncryption encryption)
    {
        _repo = repo;
        _encryption = encryption;
    }

    public async Task<TfsCredentialSaveResult> Handle(SaveTfsCredentialCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetTfsCredentialAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var credential = new TfsCredential
        {
            CredentialId = existing?.CredentialId ?? Guid.NewGuid().ToString(),
            ServerUrl = request.ServerUrl.TrimEnd('/'),
            EncryptedPat = _encryption.Encrypt(request.Pat),
            Username = request.Username,
            CreatedUtc = existing?.CreatedUtc ?? now,
            UpdatedUtc = now
        };
        await _repo.SaveTfsCredentialAsync(credential, cancellationToken);
        return new TfsCredentialSaveResult(true);
    }
}

public sealed class SaveTfsCredentialCommandValidator : AbstractValidator<SaveTfsCredentialCommand>
{
    public SaveTfsCredentialCommandValidator()
    {
        RuleFor(x => x.ServerUrl)
            .NotEmpty().WithMessage("Server URL is required.")
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out var u) && (u.Scheme == "http" || u.Scheme == "https"))
            .WithMessage("Server URL must be a valid http or https URL.");

        RuleFor(x => x.Pat)
            .NotEmpty().WithMessage("Personal access token is required.")
            .MinimumLength(10).WithMessage("PAT appears too short.");

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(256).WithMessage("Username must not exceed 256 characters.");
    }
}
