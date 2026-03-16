using FluentValidation;
using SemanticSearch.Application.Projects.Queries;

namespace SemanticSearch.Application.Projects.Validators;

public sealed class GetProjectTreeQueryValidator : AbstractValidator<GetProjectTreeQuery>
{
    public GetProjectTreeQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
