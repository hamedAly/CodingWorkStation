using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetDueCardsQuery() : IRequest<DueCardsModel>;

public sealed class GetDueCardsQueryHandler : IRequestHandler<GetDueCardsQuery, DueCardsModel>
{
    private readonly IFlashCardRepository _flashCardRepository;

    public GetDueCardsQueryHandler(IFlashCardRepository flashCardRepository)
    {
        _flashCardRepository = flashCardRepository;
    }

    public async Task<DueCardsModel> Handle(GetDueCardsQuery request, CancellationToken cancellationToken)
    {
        var dueCards = await _flashCardRepository.GetDueCardsAsync(DateTime.UtcNow.Date, cancellationToken);
        var decks = await _flashCardRepository.GetAllDecksAsync(cancellationToken);
        var deckTitles = decks.ToDictionary(deck => deck.Id, deck => deck.Title, StringComparer.Ordinal);

        return new DueCardsModel(
            dueCards.Select(card => new DueCardModel(
                card.Id,
                card.DeckId,
                deckTitles.GetValueOrDefault(card.DeckId, "Untitled deck"),
                card.Front,
                card.Back,
                card.Interval,
                card.Repetitions,
                card.EaseFactor,
                card.NextReviewDate)).ToList(),
            dueCards.Count);
    }
}