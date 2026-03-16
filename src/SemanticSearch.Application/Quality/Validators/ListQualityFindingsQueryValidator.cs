using FluentValidation;
using SemanticSearch.Application.Quality.Queries;

namespace SemanticSearch.Application.Quality.Validators;

public sealed class ListQualityFindingsQueryValidator : AbstractValidator<ListQualityFindingsQuery>
{
    public ListQualityFindingsQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
