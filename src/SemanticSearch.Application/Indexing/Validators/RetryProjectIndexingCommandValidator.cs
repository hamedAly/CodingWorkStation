using FluentValidation;
using SemanticSearch.Application.Indexing.Commands;

namespace SemanticSearch.Application.Indexing.Validators;

public sealed class RetryProjectIndexingCommandValidator : AbstractValidator<RetryProjectIndexingCommand>
{
    public RetryProjectIndexingCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");
    }
}
