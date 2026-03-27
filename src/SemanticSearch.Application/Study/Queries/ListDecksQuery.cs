using MediatR;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record ListDecksQuery() : IRequest<IReadOnlyList<DeckSummaryModel>>;

public sealed class ListDecksQueryHandler : IRequestHandler<ListDecksQuery, IReadOnlyList<DeckSummaryModel>>
{
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;

    public ListDecksQueryHandler(IFlashCardRepository flashCardRepository, IStudyRepository studyRepository)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
    }

    public async Task<IReadOnlyList<DeckSummaryModel>> Handle(ListDecksQuery request, CancellationToken cancellationToken)
    {
        var decks = await _flashCardRepository.GetAllDecksAsync(cancellationToken);
        var results = new List<DeckSummaryModel>(decks.Count);
        var today = DateTime.UtcNow.Date;

        foreach (var deck in decks)
        {
            var cards = await _flashCardRepository.GetCardsByDeckIdAsync(deck.Id, cancellationToken);
            var dueCards = cards.Count(card => card.NextReviewDate.Date <= today);
            var bookTitle = string.IsNullOrWhiteSpace(deck.BookId)
                ? null
                : (await _studyRepository.GetBookByIdAsync(deck.BookId, cancellationToken))?.Title;

            results.Add(new DeckSummaryModel(deck.Id, deck.Title, deck.BookId, bookTitle, cards.Count, dueCards));
        }

        return results;
    }
}