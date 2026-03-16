using FluentValidation;
using SemanticSearch.Application.Indexing.Commands;

namespace SemanticSearch.Application.Indexing.Validators;

public sealed class RefreshProjectFileCommandValidator : AbstractValidator<RefreshProjectFileCommand>
{
    public RefreshProjectFileCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.RelativeFilePath)
            .NotEmpty().WithMessage("RelativeFilePath is required.")
            .Must(path => !Path.IsPathRooted(path)).WithMessage("RelativeFilePath must be relative.");
    }
}
