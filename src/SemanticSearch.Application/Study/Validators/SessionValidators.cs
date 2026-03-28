using FluentValidation;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Validators;

public sealed class StartStudySessionCommandValidator : AbstractValidator<StartStudySessionCommand>
{
    public StartStudySessionCommandValidator()
    {
        RuleFor(x => x.SessionType)
            .Must(type => StudySessionType.All.Contains(type))
            .WithMessage("Session type must be Reading, Review, or Listening.");

        RuleFor(x => x.FocusDurationMinutes)
            .NotNull().WithMessage("Focus duration is required for Pomodoro sessions.")
            .InclusiveBetween(1, 120).WithMessage("Focus duration must be between 1 and 120 minutes.")
            .When(x => x.IsPomodoro);
    }
}

public sealed class EndStudySessionCommandValidator : AbstractValidator<EndStudySessionCommand>
{
    public EndStudySessionCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}