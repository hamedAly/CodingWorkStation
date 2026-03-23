using FluentValidation;
using SemanticSearch.Application.Architecture.Queries;

namespace SemanticSearch.Application.Architecture.Validators;

public sealed class GetDependencyGraphQueryValidator : AbstractValidator<GetDependencyGraphQuery>
{
    public GetDependencyGraphQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
