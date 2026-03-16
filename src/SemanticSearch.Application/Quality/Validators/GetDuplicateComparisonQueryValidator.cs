using FluentValidation;
using SemanticSearch.Application.Quality.Queries;

namespace SemanticSearch.Application.Quality.Validators;

public sealed class GetDuplicateComparisonQueryValidator : AbstractValidator<GetDuplicateComparisonQuery>
{
    public GetDuplicateComparisonQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.FindingId)
            .NotEmpty().WithMessage("FindingId is required.");
    }
}
