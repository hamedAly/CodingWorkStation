using FluentValidation;
using SemanticSearch.Application.Files.Queries;

namespace SemanticSearch.Application.Files.Validators;

public sealed class ReadProjectFileQueryValidator : AbstractValidator<ReadProjectFileQuery>
{
    public ReadProjectFileQueryValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.RelativeFilePath)
            .NotEmpty().WithMessage("RelativeFilePath is required.")
            .Must(path => !Path.IsPathRooted(path)).WithMessage("RelativeFilePath must be relative.");
    }
}
