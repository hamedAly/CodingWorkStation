using FluentValidation;
using SemanticSearch.Application.Study.Commands;

namespace SemanticSearch.Application.Study.Validators;

public sealed class CreateDeckCommandValidator : AbstractValidator<CreateDeckCommand>
{
    public CreateDeckCommandValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Deck title is required.")
            .MaximumLength(500).WithMessage("Deck title must not exceed 500 characters.");
    }
}

public sealed class AddFlashCardCommandValidator : AbstractValidator<AddFlashCardCommand>
{
    public AddFlashCardCommandValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
        RuleFor(x => x.Front)
            .NotEmpty().WithMessage("Card front is required.")
            .MaximumLength(5000).WithMessage("Card front must not exceed 5000 characters.");
        RuleFor(x => x.Back)
            .NotEmpty().WithMessage("Card back is required.")
            .MaximumLength(5000).WithMessage("Card back must not exceed 5000 characters.");
    }
}

public sealed class UpdateFlashCardCommandValidator : AbstractValidator<UpdateFlashCardCommand>
{
    public UpdateFlashCardCommandValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Front)
            .NotEmpty().WithMessage("Card front is required.")
            .MaximumLength(5000).WithMessage("Card front must not exceed 5000 characters.");
        RuleFor(x => x.Back)
            .NotEmpty().WithMessage("Card back is required.")
            .MaximumLength(5000).WithMessage("Card back must not exceed 5000 characters.");
    }
}

public sealed class DeleteFlashCardCommandValidator : AbstractValidator<DeleteFlashCardCommand>
{
    public DeleteFlashCardCommandValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
        RuleFor(x => x.CardId).NotEmpty();
    }
}

public sealed class DeleteDeckCommandValidator : AbstractValidator<DeleteDeckCommand>
{
    public DeleteDeckCommandValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
    }
}

public sealed class ReviewCardCommandValidator : AbstractValidator<ReviewCardCommand>
{
    public ReviewCardCommandValidator()
    {
        RuleFor(x => x.CardId).NotEmpty();
        RuleFor(x => x.Quality).InclusiveBetween(0, 5);
    }
}

public sealed class GenerateCardsFromChapterCommandValidator : AbstractValidator<GenerateCardsFromChapterCommand>
{
    public GenerateCardsFromChapterCommandValidator()
    {
        RuleFor(x => x.DeckId).NotEmpty();
        RuleFor(x => x.ChapterId).NotEmpty();
    }
}