using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record AddFlashCardCommand(string DeckId, string Front, string Back, string? ChapterId) : IRequest<FlashCardModel>;

public sealed class AddFlashCardCommandHandler : IRequestHandler<AddFlashCardCommand, FlashCardModel>
{
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;

    public AddFlashCardCommandHandler(IFlashCardRepository flashCardRepository, IStudyRepository studyRepository)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
    }

    public async Task<FlashCardModel> Handle(AddFlashCardCommand request, CancellationToken cancellationToken)
    {
        var deck = await _flashCardRepository.GetDeckByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard deck '{request.DeckId}' was not found.");

        await ValidateChapterAsync(deck.BookId, request.ChapterId, cancellationToken);

        var card = new FlashCard
        {
            Id = Guid.NewGuid().ToString("N"),
            DeckId = request.DeckId,
            ChapterId = string.IsNullOrWhiteSpace(request.ChapterId) ? null : request.ChapterId,
            Front = request.Front.Trim(),
            Back = request.Back.Trim(),
            Interval = 0,
            Repetitions = 0,
            EaseFactor = 2.5d,
            NextReviewDate = DateTime.UtcNow.Date,
            CreatedAt = DateTime.UtcNow
        };

        await _flashCardRepository.InsertCardAsync(card, cancellationToken);
        return card.ToModel();
    }

    private async Task ValidateChapterAsync(string? deckBookId, string? chapterId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(chapterId))
            return;

        var chapter = await _studyRepository.GetChapterByIdAsync(chapterId, cancellationToken)
            ?? throw new NotFoundException($"Study chapter '{chapterId}' was not found.");

        if (!string.IsNullOrWhiteSpace(deckBookId) && !string.Equals(chapter.BookId, deckBookId, StringComparison.Ordinal))
            throw new InvalidOperationException("The selected chapter does not belong to the deck's book.");
    }
}