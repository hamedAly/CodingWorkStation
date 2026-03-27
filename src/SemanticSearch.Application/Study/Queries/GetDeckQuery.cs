using MediatR;
using SemanticSearch.Application.Common.Exceptions;
using SemanticSearch.Application.Study.Models;
using SemanticSearch.Domain.Interfaces;

namespace SemanticSearch.Application.Study.Queries;

public sealed record GetDeckQuery(string DeckId) : IRequest<DeckDetailModel>;

public sealed class GetDeckQueryHandler : IRequestHandler<GetDeckQuery, DeckDetailModel>
{
    private readonly IFlashCardRepository _flashCardRepository;
    private readonly IStudyRepository _studyRepository;

    public GetDeckQueryHandler(IFlashCardRepository flashCardRepository, IStudyRepository studyRepository)
    {
        _flashCardRepository = flashCardRepository;
        _studyRepository = studyRepository;
    }

    public async Task<DeckDetailModel> Handle(GetDeckQuery request, CancellationToken cancellationToken)
    {
        var deck = await _flashCardRepository.GetDeckByIdAsync(request.DeckId, cancellationToken)
            ?? throw new NotFoundException($"Flashcard deck '{request.DeckId}' was not found.");

        var cards = await _flashCardRepository.GetCardsByDeckIdAsync(deck.Id, cancellationToken);
        var dueCards = cards.Count(card => card.NextReviewDate.Date <= DateTime.UtcNow.Date);
        var bookTitle = string.IsNullOrWhiteSpace(deck.BookId)
            ? null
            : (await _studyRepository.GetBookByIdAsync(deck.BookId, cancellationToken))?.Title;

        return new DeckDetailModel(deck.Id, deck.Title, deck.BookId, bookTitle, cards.Select(card => card.ToModel()).ToList(), dueCards);
    }
}