using FluentValidation;
using MediatR;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Slack.Commands;

public sealed record UpdateIntegrationSettingsCommand(
    string StandupMessage,
    bool StandupEnabled,
    string PrayerCity,
    string PrayerCountry,
    int PrayerMethod,
    bool PrayerEnabled) : IRequest;

public sealed class UpdateIntegrationSettingsCommandHandler : IRequestHandler<UpdateIntegrationSettingsCommand>
{
    private readonly IIntegrationSettingsRepository _repo;

    public UpdateIntegrationSettingsCommandHandler(IIntegrationSettingsRepository repo)
    {
        _repo = repo;
    }

    public Task Handle(UpdateIntegrationSettingsCommand request, CancellationToken cancellationToken)
    {
        var settings = new IntegrationSettings
        {
            SettingsId = "default",
            StandupMessage = request.StandupMessage,
            StandupEnabled = request.StandupEnabled,
            PrayerCity = request.PrayerCity,
            PrayerCountry = request.PrayerCountry,
            PrayerMethod = request.PrayerMethod,
            PrayerEnabled = request.PrayerEnabled,
            UpdatedUtc = DateTime.UtcNow
        };
        return _repo.SaveAsync(settings, cancellationToken);
    }
}

public sealed class UpdateIntegrationSettingsCommandValidator : AbstractValidator<UpdateIntegrationSettingsCommand>
{
    public UpdateIntegrationSettingsCommandValidator()
    {
        RuleFor(x => x.StandupMessage)
            .NotEmpty().WithMessage("Standup message is required when standup is enabled.")
            .When(x => x.StandupEnabled);

        RuleFor(x => x.PrayerCity)
            .NotEmpty().WithMessage("Prayer city is required when prayer notifications are enabled.")
            .When(x => x.PrayerEnabled);

        RuleFor(x => x.PrayerCountry)
            .NotEmpty().WithMessage("Prayer country is required when prayer notifications are enabled.")
            .When(x => x.PrayerEnabled);

        RuleFor(x => x.PrayerMethod)
            .InclusiveBetween(1, 15).WithMessage("Prayer method must be between 1 and 15.");
    }
}
