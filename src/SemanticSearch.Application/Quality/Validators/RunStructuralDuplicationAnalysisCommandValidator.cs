using FluentValidation;
using SemanticSearch.Application.Quality.Commands;

namespace SemanticSearch.Application.Quality.Validators;

public sealed class RunStructuralDuplicationAnalysisCommandValidator : AbstractValidator<RunStructuralDuplicationAnalysisCommand>
{
    public RunStructuralDuplicationAnalysisCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.ScopePath)
            .MaximumLength(512).When(x => !string.IsNullOrWhiteSpace(x.ScopePath))
            .Must(BeSafeScopePath).When(x => !string.IsNullOrWhiteSpace(x.ScopePath))
            .WithMessage("ScopePath must be a relative folder path.");

        RuleFor(x => x.MinimumLines)
            .GreaterThan(0).When(x => x.MinimumLines.HasValue)
            .WithMessage("MinimumLines must be greater than zero.");
    }

    private static bool BeSafeScopePath(string? scopePath)
    {
        if (string.IsNullOrWhiteSpace(scopePath))
        {
            return true;
        }

        var normalized = scopePath.Replace('\\', '/');
        return !Path.IsPathRooted(scopePath) &&
               !normalized.Contains("..", StringComparison.Ordinal) &&
               !normalized.StartsWith("/", StringComparison.Ordinal);
    }
}
