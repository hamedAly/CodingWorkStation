using FluentValidation;
using SemanticSearch.Application.Quality.Commands;

namespace SemanticSearch.Application.Quality.Validators;

public sealed class RunSemanticDuplicationAnalysisCommandValidator : AbstractValidator<RunSemanticDuplicationAnalysisCommand>
{
    public RunSemanticDuplicationAnalysisCommandValidator()
    {
        RuleFor(x => x.ProjectKey)
            .NotEmpty().WithMessage("ProjectKey is required.")
            .MaximumLength(64).WithMessage("ProjectKey must not exceed 64 characters.");

        RuleFor(x => x.ScopePath)
            .MaximumLength(512).When(x => !string.IsNullOrWhiteSpace(x.ScopePath))
            .Must(BeSafeScopePath).When(x => !string.IsNullOrWhiteSpace(x.ScopePath))
            .WithMessage("ScopePath must be a relative folder path.");

        RuleFor(x => x.SimilarityThreshold)
            .InclusiveBetween(0.85d, 1d).When(x => x.SimilarityThreshold.HasValue)
            .WithMessage("SimilarityThreshold must be between 0.85 and 1.0.");

        RuleFor(x => x.MaxPairs)
            .InclusiveBetween(1, 500).When(x => x.MaxPairs.HasValue)
            .WithMessage("MaxPairs must be between 1 and 500.");
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
