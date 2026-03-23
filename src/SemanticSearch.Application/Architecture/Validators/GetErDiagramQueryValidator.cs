using FluentValidation;
using SemanticSearch.Application.Architecture.Queries;

namespace SemanticSearch.Application.Architecture.Validators;

public sealed class GetErDiagramQueryValidator : AbstractValidator<GetErDiagramQuery>
{
    public GetErDiagramQueryValidator()
    {
        // No parameters to validate.
    }
}
