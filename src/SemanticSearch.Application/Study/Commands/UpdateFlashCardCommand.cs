using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Entities;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Commands;

public sealed record UpdateFlashCardCommand(string DeckId, string CardId, string Front, string Back, string? ChapterId) : IRequest<FlashCardModel>;

public sealed class UpdateFlashCardCommandHandler : IRequestHandler<UpdateFlashCardCommand, FlashCardModel>
{
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;

    public UpdateFlashCardCommandHandler(IFlashCardRepository flashCardRepository, IStudyRepository studyRepository)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
    }

    public async Task<FlashCardModel> Handle(UpdateFlashCardCommand request, CancellationToken cancellationToken)
    {
        var deck = await _flashCardRepository.GetDeckByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard deck '{request.DeckId}' was not found.");
        var existingCard = await _flashCardRepository.GetCardByIdAsync(request.CardId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard '{request.CardId}' was not found.");

        if (!string.Equals(existingCard.DeckId, request.DeckId, StringComparison.Ordinal))
            throw new NotFoundException($"Flashcard '{request.CardId}' was not found in deck '{request.DeckId}'.");

        await ValidateChapterAsync(deck.BookId, request.ChapterId, cancellationToken);

        var updatedCard = new FlashCard
        {
            Id = existingCard.Id,
            DeckId = existingCard.DeckId,
            ChapterId = string.IsNullOrWhiteSpace(request.ChapterId) ? null : request.ChapterId,
            Front = request.Front.Trim(),
            Back = request.Back.Trim(),
            Interval = existingCard.Interval,
            Repetitions = existingCard.Repetitions,
            EaseFactor = existingCard.EaseFactor,
            NextReviewDate = existingCard.NextReviewDate,
            LastReviewDate = existingCard.LastReviewDate,
            CreatedAt = existingCard.CreatedAt
        };

        await _flashCardRepository.UpdateCardAsync(updatedCard, cancellationToken);
        return updatedCard.ToModel();
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