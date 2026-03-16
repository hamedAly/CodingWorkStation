using FluentValidation;
using SemanticSearch.Application.Quality.Queries;

namespace SemanticSearch.Application.Quality.Validators;

public sealed class GetQualitySummaryQueryValidator : AbstractValidator<GetQualitySummaryQuery>
{
    public GetQualitySummaryQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
