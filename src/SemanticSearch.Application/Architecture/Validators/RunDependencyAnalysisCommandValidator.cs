using FluentValidation;
using SemanticSearch.Application.Architecture.Commands;

namespace SemanticSearch.Application.Architecture.Validators;

public sealed class RunDependencyAnalysisCommandValidator : AbstractValidator<RunDependencyAnalysisCommand>
{
    public RunDependencyAnalysisCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
