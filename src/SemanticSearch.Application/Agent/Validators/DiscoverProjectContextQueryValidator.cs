using FluentValidation;
using SemanticSearch.Application.Agent.Queries;

namespace SemanticSearch.Application.Agent.Validators;

public sealed class DiscoverProjectContextQueryValidator : AbstractValidator<DiscoverProjectContextQuery>
{
    public DiscoverProjectContextQueryValidator()
    {
        RuleFor(query => query.ProjectKey)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(query => query.Query)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
