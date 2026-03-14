using FluentValidation;
using SemanticSearch.Application.Status.Queries;

namespace SemanticSearch.Application.Status.Queries;

public sealed class GetProjectStatusQueryValidator : AbstractValidator<GetProjectStatusQuery>
{
    public GetProjectStatusQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(128).WithMessage("ProjectKey must not exceed 128 characters.");
    }
}
