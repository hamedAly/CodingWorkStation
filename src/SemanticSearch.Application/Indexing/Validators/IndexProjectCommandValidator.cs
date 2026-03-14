using FluentValidation;
using SemanticSearch.Application.Indexing.Commands;

namespace SemanticSearch.Application.Indexing.Validators;

public sealed class IndexProjectCommandValidator : AbstractValidator<IndexProjectCommand>
{
    public IndexProjectCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(128).WithMessage("ProjectKey must not exceed 128 characters.");

        RuleFor(x => x.ProjectPath)
            .NotEmpty().WithMessage("ProjectPath is required.")
            .Must(Directory.Exists).WithMessage("ProjectPath must be an existing directory.");
    }
}
