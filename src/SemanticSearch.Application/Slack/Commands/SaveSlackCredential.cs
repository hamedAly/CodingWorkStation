using FluentValidation;
using MediatR;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Commands;

public sealed record SaveSlackCredentialCommand(
    string BotToken,
    string? UserToken,
    string DefaultChannel) : IRequest<SlackCredentialSaveResult>;

public sealed record SlackCredentialSaveResult(bool Success, string? Error = null);

public sealed class SaveSlackCredentialCommandHandler : IRequestHandler<SaveSlackCredentialCommand, SlackCredentialSaveResult>
{
    private readonly ICredentialRepository _repo;
    private readonly ICredentialEncryption _encryption;

    public SaveSlackCredentialCommandHandler(ICredentialRepository repo, ICredentialEncryption encryption)
    {
        _repo = repo;
        _encryption = encryption;
    }

    public async Task<SlackCredentialSaveResult> Handle(SaveSlackCredentialCommand request, CancellationToken cancellationToken)
    {
        var existing = await _repo.GetSlackCredentialAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var credential = new SlackCredential
        {
            CredentialId = existing?.CredentialId ?? Guid.NewGuid().ToString(),
            EncryptedBotToken = _encryption.Encrypt(request.BotToken),
            EncryptedUserToken = request.UserToken is not null ? _encryption.Encrypt(request.UserToken) : null,
            DefaultChannel = request.DefaultChannel,
            CreatedUtc = existing?.CreatedUtc ?? now,
            UpdatedUtc = now
        };
        await _repo.SaveSlackCredentialAsync(credential, cancellationToken);
        return new SlackCredentialSaveResult(true);
    }
}

public sealed class SaveSlackCredentialCommandValidator : AbstractValidator<SaveSlackCredentialCommand>
{
    public SaveSlackCredentialCommandValidator()
    {
        RuleFor(x => x.BotToken)
            .NotEmpty().WithMessage("Bot token is required.")
            .Must(t => t.StartsWith("xoxb-")).WithMessage("Bot token must start with 'xoxb-'.");

        RuleFor(x => x.UserToken)
            .Must(t => t == null || t.StartsWith("xoxp-")).WithMessage("User token must start with 'xoxp-'.")
            .When(x => x.UserToken is not null);

        RuleFor(x => x.DefaultChannel)
            .NotEmpty().WithMessage("Default channel is required.");
    }
}
