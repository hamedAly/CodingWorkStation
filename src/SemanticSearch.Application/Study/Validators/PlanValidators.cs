using FluentValidation;
using SemanticSearch.Application.Study.Commands;
using SemanticSearch.Domain.ValueObjects;

namespace SemanticSearch.Application.Study.Validators;

public sealed class CreateStudyPlanCommandValidator : AbstractValidator<CreateStudyPlanCommand>
{
    public CreateStudyPlanCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Plan title is required.")
            .MaximumLength(500).WithMessage("Plan title must not exceed 500 characters.");
        RuleFor(x => x.EndDate)
            .GreaterThanOrEqualTo(x => x.StartDate)
            .WithMessage("The end date must be on or after the start date.");
    }
}

public sealed class AutoGeneratePlanItemsCommandValidator : AbstractValidator<AutoGeneratePlanItemsCommand>
{
    public AutoGeneratePlanItemsCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
    }
}

public sealed class UpdatePlanItemStatusCommandValidator : AbstractValidator<UpdatePlanItemStatusCommand>
{
    public UpdatePlanItemStatusCommandValidator()
    {
        RuleFor(x => x.PlanId).NotEmpty();
        RuleFor(x => x.ItemId).NotEmpty();
        RuleFor(x => x.Status)
            .Must(status => PlanItemStatus.All.Contains(status))
            .WithMessage("Status must be Pending, InProgress, Done, or Skipped.");
    }
}