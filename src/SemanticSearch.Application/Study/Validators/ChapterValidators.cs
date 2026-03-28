using FluentValidation;
using SemanticSearch.Application.Study.Commands;

namespace SemanticSearch.Application.Study.Validators;

public sealed class AddChapterCommandValidator : AbstractValidator<AddChapterCommand>
{
    public AddChapterCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Chapter title is required.")
            .MaximumLength(500).WithMessage("Chapter title must not exceed 500 characters.");
        RuleFor(x => x.StartPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.EndPage).GreaterThanOrEqualTo(x => x.StartPage);
    }
}

public sealed class UpdateChapterCommandValidator : AbstractValidator<UpdateChapterCommand>
{
    public UpdateChapterCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Chapter title is required.")
            .MaximumLength(500).WithMessage("Chapter title must not exceed 500 characters.");
        RuleFor(x => x.StartPage).GreaterThanOrEqualTo(1);
        RuleFor(x => x.EndPage).GreaterThanOrEqualTo(x => x.StartPage);
    }
}

public sealed class DeleteChapterCommandValidator : AbstractValidator<DeleteChapterCommand>
{
    public DeleteChapterCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
    }
}

public sealed class UpdateChapterNotesCommandValidator : AbstractValidator<UpdateChapterNotesCommand>
{
    public UpdateChapterNotesCommandValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
        RuleFor(x => x.Notes)
            .MaximumLength(10000).WithMessage("Notes must not exceed 10000 characters.");
    }
}