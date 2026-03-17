using FluentValidation;
using SemanticSearch.Application.Quality.Assistant.Queries;

namespace SemanticSearch.Application.Quality.Assistant.Validators;

public sealed class StreamFindingFixQueryValidator : AbstractValidator<StreamFindingFixQuery>
{
    public StreamFindingFixQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.FindingId)
            .NotEmpty().WithMessage("FindingId is required.");
    }
}
