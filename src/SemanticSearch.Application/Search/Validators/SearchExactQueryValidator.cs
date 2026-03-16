using FluentValidation;
using SemanticSearch.Application.Search.Queries;

namespace SemanticSearch.Application.Search.Validators;

public sealed class SearchExactQueryValidator : AbstractValidator<SearchExactQuery>
{
    public SearchExactQueryValidator()
    {
        RuleFor(x => x.Keyword)
            .NotEmpty().WithMessage("Keyword is required.");

        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.TopK)
            .InclusiveBetween(1, 100).WithMessage("TopK must be between 1 and 100.");
    }
}
